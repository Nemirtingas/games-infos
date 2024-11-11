using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using QRCoder;
using SteamKit2;
using SteamKit2.Authentication;
using SteamKit2.CDN;
using SteamKit2.Internal;

namespace SteamRetriever
{
    class Steam3Session
    {
        internal bool IsLoggedOn { get; private set; }

        internal ReadOnlyCollection<SteamApps.LicenseListCallback.License> Licenses
        {
            get;
            private set;
        }

        internal Dictionary<uint, ulong> AppTokens { get; } = [];
        internal Dictionary<uint, ulong> PackageTokens { get; } = [];
        internal Dictionary<uint, byte[]> DepotKeys { get; } = [];
        internal ConcurrentDictionary<(uint, string), TaskCompletionSource<SteamContent.CDNAuthToken>> CDNAuthTokens { get; } = [];
        internal Dictionary<uint, SteamApps.PICSProductInfoCallback.PICSProductInfo> AppInfo { get; } = [];
        internal Dictionary<uint, SteamApps.PICSProductInfoCallback.PICSProductInfo> PackageInfo { get; } = [];
        internal Dictionary<string, byte[]> AppBetaPasswords { get; } = [];

        internal SteamClient steamClient;
        internal SteamUser steamUser;
        internal SteamGameServer steamGameserver;
        internal SteamContent steamContent;
        internal readonly SteamApps steamApps;
        internal readonly SteamCloud steamCloud;
        internal readonly SteamUserStats steamUserStats;
        internal readonly SteamUnifiedMessages.UnifiedService<IPlayer> steamPlayer;
        internal readonly SteamUnifiedMessages.UnifiedService<IPublishedFile> steamPublishedFile;
        internal readonly SteamUnifiedMessages.UnifiedService<IInventory> steamInventory;

        readonly CallbackManager callbacks;

        readonly bool authenticatedUser;
        bool bConnected;
        bool bConnecting;
        bool bAborted;
        bool bExpectingDisconnectRemote;
        bool bDidDisconnect;
        bool bIsConnectionRecovery;
        int connectionBackoff;
        int seq; // more hack fixes
        DateTime connectTime;
        AuthSession authSession;

        Task CallbackTask;
        CancellationTokenSource CallbackCancellationSource;

        // input
        readonly string password;
        readonly SteamUser.LogOnDetails logonDetails;

        static readonly TimeSpan STEAM3_TIMEOUT = TimeSpan.FromSeconds(30);


        internal Steam3Session(SteamUser.LogOnDetails details)
        {
            password = details.Password;
            this.logonDetails = details;
            this.authenticatedUser = details.Username != null || ContentDownloader.Config.UseQrCode;

            var clientConfiguration = SteamConfiguration.Create(config =>
                config
                    .WithHttpClientFactory(HttpClientFactory.CreateHttpClient)
            );

            this.steamClient = new SteamClient(clientConfiguration);

            this.steamClient.AddHandler(new SteamUserStats());

            this.steamUserStats = this.steamClient.GetHandler<SteamUserStats>();
            this.steamUser = this.steamClient.GetHandler<SteamUser>();
            this.steamGameserver = this.steamClient.GetHandler<SteamGameServer>();
            this.steamApps = this.steamClient.GetHandler<SteamApps>();
            this.steamCloud = this.steamClient.GetHandler<SteamCloud>();
            var steamUnifiedMessages = this.steamClient.GetHandler<SteamUnifiedMessages>();
            this.steamPlayer = steamUnifiedMessages.CreateService<IPlayer>();
            this.steamPublishedFile = steamUnifiedMessages.CreateService<IPublishedFile>();
            this.steamInventory = steamUnifiedMessages.CreateService<IInventory>();
            this.steamContent = this.steamClient.GetHandler<SteamContent>();

            this.callbacks = new CallbackManager(this.steamClient);

            this.callbacks.Subscribe<SteamClient.ConnectedCallback>(ConnectedCallback);
            this.callbacks.Subscribe<SteamClient.DisconnectedCallback>(DisconnectedCallback);
            this.callbacks.Subscribe<SteamUser.LoggedOnCallback>(LogOnCallback);
            this.callbacks.Subscribe<SteamApps.LicenseListCallback>(LicenseListCallback);

            Program.Instance._logger.Info("Connecting to Steam3...");
            Connect();
        }

        internal delegate bool WaitCondition();

        private readonly object steamLock = new();

        internal bool WaitUntilCallback(Action submitter, WaitCondition waiter)
        {
            while (!bAborted && !waiter())
            {
                lock (steamLock)
                {
                    submitter();
                }

                var seq = this.seq;
                do
                {
                    lock (steamLock)
                    {
                        WaitForCallbacks();
                    }
                } while (!bAborted && this.seq == seq && !waiter());
            }

            return bAborted;
        }

        void StopCallbackTask()
        {
            if (CallbackCancellationSource != null)
            {
                CallbackCancellationSource.Cancel();
                CallbackTask.Wait();

                CallbackCancellationSource = null;
            }
        }

        void CallbackProc(object arg)
        {
            CancellationToken tk = (CancellationToken)arg;
            while (!tk.IsCancellationRequested)
            {
                callbacks.RunWaitCallbacks(TimeSpan.FromMilliseconds(100));
            }
        }

        internal bool WaitForCredentials()
        {
            if (IsLoggedOn || bAborted)
                return IsLoggedOn;

            WaitUntilCallback(() => { }, () => IsLoggedOn);

            if (IsLoggedOn)
            {
                if (CallbackCancellationSource != null)
                {
                    CallbackCancellationSource.Cancel();
                    CallbackTask.Wait();
                }

                CallbackCancellationSource = new CancellationTokenSource();
                CallbackTask = Task.Factory.StartNew(CallbackProc, CallbackCancellationSource.Token);
            }

            return IsLoggedOn;
        }

        internal async Task RequestAppsInfo(IEnumerable<uint> appIds, bool bForce = false)
        {
            List<uint> request_appids = new List<uint>();
            {
                foreach (var item in appIds)
                {
                    if (!AppInfo.ContainsKey(item) || bForce)
                    {
                        request_appids.Add(item);
                    }
                }

                if (request_appids.Count == 0 || bAborted)
                    return;
            }
            List<SteamApps.PICSRequest> requests = new List<SteamApps.PICSRequest>(request_appids.Count);

            {
                var job = await steamApps.PICSGetAccessTokens(request_appids, new List<uint>());

                foreach (var appToken in request_appids)
                {
                    if (job.AppTokensDenied.Contains(appToken))
                    {
                        //Console.WriteLine("Insufficient privileges to get access token for app {0}", item);
                    }
                }

                foreach (var token_dict in job.AppTokens)
                {
                    AppTokens[token_dict.Key] = token_dict.Value;
                }

                foreach (var item in request_appids)
                {
                    var pic = new SteamApps.PICSRequest(item);
                    if (AppTokens.ContainsKey(item))
                    {
                        pic.AccessToken = AppTokens[item];
                    }
                    requests.Add(pic);
                }
            }

            {
                var jobs = await steamApps.PICSGetProductInfo(requests, new List<SteamApps.PICSRequest>());

                foreach (var result in jobs.Results)
                {
                    foreach (var app_value in result.Apps)
                    {
                        //Console.WriteLine("Got AppInfo for {0}", app.ID);
                        AppInfo[app_value.Key] = app_value.Value;
                    }

                    foreach (var app in result.UnknownApps)
                    {
                        AppInfo[app] = null;
                    }
                }
            }
        }

        internal async Task RequestAppInfoAsync(uint appId, bool bForce = false)
        {
            await RequestAppsInfo(new List<uint> { appId }, bForce);
        }

        internal async Task<string> GetInventoryDigest(uint appid)
        {
            CInventory_GetItemDefMeta_Request itemdef_req = new CInventory_GetItemDefMeta_Request
            {
                appid = appid,
            };

            var job = await steamInventory.SendMessage(api => api.GetItemDefMeta(itemdef_req));

            if (job.Result != EResult.OK)
                throw new Exception($"EResult {(int)job.Result} ({job.Result}) while retrieving items definition for {appid}.");

            return job.GetDeserializedResponse<CInventory_GetItemDefMeta_Response>().digest;
        }

        internal async Task<List<CPlayer_GetOwnedGames_Response.Game>> GetPlayerOwnedAppIds(ulong steamId = 0)
        {
            CPlayer_GetOwnedGames_Request owned_games_req = new CPlayer_GetOwnedGames_Request
            {
                steamid = steamId == 0 ? steamUser.SteamID.ConvertToUInt64() : steamId,
            };

            var job = await steamPlayer.SendMessage(api => api.GetOwnedGames(owned_games_req));

            if (job.Result != EResult.OK)
                throw new Exception($"EResult {(int)job.Result} ({job.Result}) while getting owned appids.");

            return job.GetDeserializedResponse<CPlayer_GetOwnedGames_Response>().games;
        }

        internal async Task<SteamUserStats.GetUserStatsCallback> GetUserStats(uint appId, ulong steamId)
        {
            var async_job = steamUserStats.GetUserStats(appId, steamId);
            async_job.Timeout = TimeSpan.FromSeconds(5);
            return await async_job;
        }

        internal async Task RequestPackageInfo(IEnumerable<uint> packageIds)
        {
            var packages = packageIds.ToList();
            packages.RemoveAll(PackageInfo.ContainsKey);

            if (packages.Count == 0 || bAborted)
                return;

            var packageRequests = new List<SteamApps.PICSRequest>();

            foreach (var package in packages)
            {
                var request = new SteamApps.PICSRequest(package);

                if (PackageTokens.TryGetValue(package, out var token))
                {
                    request.AccessToken = token;
                }

                packageRequests.Add(request);
            }

            var packageInfoMultiple = await steamApps.PICSGetProductInfo([], packageRequests);

            foreach (var packageInfo in packageInfoMultiple.Results)
            {
                foreach (var package_value in packageInfo.Packages)
                {
                    var package = package_value.Value;
                    PackageInfo[package.ID] = package;
                }

                foreach (var package in packageInfo.UnknownPackages)
                {
                    PackageInfo[package] = null;
                }
            }
        }

        internal async Task<bool> RequestFreeAppLicense(uint appId)
        {
            var resultInfo = await steamApps.RequestFreeLicense(appId);

            return resultInfo.GrantedApps.Contains(appId);
        }

        internal async Task<EResult> RequestDepotKeyAsync(uint depotId, uint appid = 0)
        {
            if (DepotKeys.ContainsKey(depotId))
                return EResult.OK;

            if (bAborted)
                return EResult.Cancelled;

            var job = await steamApps.GetDepotDecryptionKey(depotId, appid);

            if (job.Result != EResult.OK)
                return job.Result;

            //Console.WriteLine("Got depot key for {0} result: {1}", job.DepotID, job.Result);

            DepotKeys[job.DepotID] = job.DepotKey;
            return EResult.OK;
        }

        internal async Task<ulong> GetDepotManifestRequestCodeAsync(uint depotId, uint appId, ulong manifestId, string branch)
        {
            if (bAborted)
                return 0;

            var requestCode = await steamContent.GetManifestRequestCode(depotId, appId, manifestId, branch);

            //Console.WriteLine("Got manifest request code for {0} {1} result: {2}",
            //    depotId, manifestId,
            //    requestCode);

            return requestCode;
        }

        internal async Task RequestCDNAuthToken(uint appid, uint depotid, Server server)
        {
            var cdnKey = (depotid, server.Host);
            var completion = new TaskCompletionSource<SteamContent.CDNAuthToken>();

            if (bAborted || !CDNAuthTokens.TryAdd(cdnKey, completion))
            {
                return;
            }

            DebugLog.WriteLine(nameof(Steam3Session), $"Requesting CDN auth token for {server.Host}");

            var cdnAuth = await steamContent.GetCDNAuthToken(appid, depotid, server.Host);

            Console.WriteLine($"Got CDN auth token for {server.Host} result: {cdnAuth.Result} (expires {cdnAuth.Expiration})");

            if (cdnAuth.Result != EResult.OK)
            {
                return;
            }

            completion.TrySetResult(cdnAuth);
        }

        internal async Task CheckAppBetaPassword(uint appid, string password)
        {
            var appPassword = await steamApps.CheckAppBetaPassword(appid, password);

            //Console.WriteLine("Retrieved {0} beta keys with result: {1}", appPassword.BetaPasswords.Count, appPassword.Result);

            foreach (var entry in appPassword.BetaPasswords)
            {
                AppBetaPasswords[entry.Key] = entry.Value;
            }
        }

        internal async Task<PublishedFileDetails> GetPublishedFileDetails(uint? appId, PublishedFileID pubFile)
        {
            var pubFileRequest = new CPublishedFile_GetDetails_Request();
            if (appId != null)
                pubFileRequest.appid = appId.Value;

            pubFileRequest.publishedfileids.Add(pubFile);

            var job = await steamPublishedFile.SendMessage(api => api.GetDetails(pubFileRequest));

            if (job.Result != EResult.OK)
                throw new Exception($"EResult {(int)job.Result} ({job.Result}) while retrieving file details for pubfile {pubFile}.");

            return job.GetDeserializedResponse<CPublishedFile_GetDetails_Response>().publishedfiledetails.FirstOrDefault();
        }

        internal async Task<SteamCloud.UGCDetailsCallback> GetUGCDetails(UGCHandle ugcHandle)
        {
            var callback = await steamCloud.RequestUGCDetails(ugcHandle);

            if (callback.Result == EResult.OK)
            {
                return callback;
            }
            else if (callback.Result == EResult.FileNotFound)
            {
                return null;
            }

            throw new Exception($"EResult {(int)callback.Result} ({callback.Result}) while retrieving UGC details for {ugcHandle}.");
        }

        private void ResetConnectionFlags()
        {
            bExpectingDisconnectRemote = false;
            bDidDisconnect = false;
            bIsConnectionRecovery = false;
        }

        void Connect()
        {
            bAborted = false;
            bConnected = false;
            bConnecting = true;
            connectionBackoff = 0;
            authSession = null;

            ResetConnectionFlags();

            this.connectTime = DateTime.Now;
            this.steamClient.Connect();
        }

        private void Abort(bool sendLogOff = true)
        {
            Disconnect(sendLogOff);
        }

        internal void Disconnect(bool sendLogOff = true)
        {
            StopCallbackTask();

            if (sendLogOff)
            {
                steamUser.LogOff();
            }


            bAborted = true;
            bConnected = false;
            bConnecting = false;
            bIsConnectionRecovery = false;
            steamClient.Disconnect();

            Ansi.Progress(Ansi.ProgressState.Hidden);

            // flush callbacks until our disconnected event
            while (!bDidDisconnect)
            {
                callbacks.RunWaitAllCallbacks(TimeSpan.FromMilliseconds(100));
            }
        }

        private void Reconnect()
        {
            bIsConnectionRecovery = true;
            steamClient.Disconnect();
        }

        private void WaitForCallbacks()
        {
            callbacks.RunWaitCallbacks(TimeSpan.FromSeconds(1));

            var diff = DateTime.Now - connectTime;

            if (diff > STEAM3_TIMEOUT && !bConnected)
            {
                Program.Instance._logger.InfoFormat("Timeout connecting to Steam3.");
                Abort();
            }
        }

        private async void ConnectedCallback(SteamClient.ConnectedCallback connected)
        {
            Program.Instance._logger.InfoFormat(" Done!");
            bConnecting = false;
            bConnected = true;

            // Update our tracking so that we don't time out, even if we need to reconnect multiple times,
            // e.g. if the authentication phase takes a while and therefore multiple connections.
            connectTime = DateTime.Now;
            connectionBackoff = 0;

            if (!authenticatedUser)
            {
                Program.Instance._logger.InfoFormat("Logging anonymously into Steam3...");
                steamUser.LogOnAnonymous();
            }
            else
            {
                if (logonDetails.Username != null)
                {
                    Program.Instance._logger.Info($"Logging '{logonDetails.Username}' into Steam3...");
                }

                if (authSession is null)
                {
                    if (logonDetails.Username != null && logonDetails.Password != null && logonDetails.AccessToken is null)
                    {
                        try
                        {
                            _ = AccountSettingsStore.Instance.Settings.GuardData.TryGetValue(logonDetails.Username, out var guarddata);
                            authSession = await steamClient.Authentication.BeginAuthSessionViaCredentialsAsync(new SteamKit2.Authentication.AuthSessionDetails
                            {
                                Username = logonDetails.Username,
                                Password = logonDetails.Password,
                                IsPersistentSession = ContentDownloader.Config.RememberPassword,
                                GuardData = guarddata,
                                Authenticator = new UserConsoleAuthenticator(),
                            });
                        }
                        catch (TaskCanceledException)
                        {
                            return;
                        }
                        catch (Exception ex)
                        {
                            Program.Instance._logger.Error($"Failed to authenticate with Steam: {ex.Message}");
                            Abort(false);
                            return;
                        }
                    }
                    else if (logonDetails.AccessToken is null && ContentDownloader.Config.UseQrCode)
                    {
                        Program.Instance._logger.Info("Logging in with QR code...");

                        try
                        {
                            var session = await steamClient.Authentication.BeginAuthSessionViaQRAsync(new AuthSessionDetails
                            {
                                IsPersistentSession = ContentDownloader.Config.RememberPassword,
                                Authenticator = new UserConsoleAuthenticator(),
                            });

                            authSession = session;

                            // Steam will periodically refresh the challenge url, so we need a new QR code.
                            session.ChallengeURLChanged = () =>
                            {
                                Program.Instance._logger.Info("The QR code has changed:");

                                DisplayQrCode(session.ChallengeURL);
                            };

                            // Draw initial QR code immediately
                            DisplayQrCode(session.ChallengeURL);
                        }
                        catch (TaskCanceledException)
                        {
                            return;
                        }
                        catch (Exception ex)
                        {
                            Program.Instance._logger.Error($"Failed to authenticate with Steam: {ex.Message}");
                            Abort(false);
                            return;
                        }
                    }
                }

                if (authSession != null)
                {
                    try
                    {
                        var result = await authSession.PollingWaitForResultAsync();

                        logonDetails.Username = result.AccountName;
                        logonDetails.Password = null;
                        logonDetails.AccessToken = result.RefreshToken;

                        if (result.NewGuardData != null)
                        {
                            AccountSettingsStore.Instance.Settings.GuardData[result.AccountName] = result.NewGuardData;
                        }
                        else
                        {
                            AccountSettingsStore.Instance.Settings.GuardData.Remove(result.AccountName);
                        }
                        AccountSettingsStore.Instance.Settings.LoginTokens[result.AccountName] = result.RefreshToken;
                        AccountSettingsStore.Instance.Save();
                    }
                    catch (TaskCanceledException)
                    {
                        return;
                    }
                    catch (Exception ex)
                    {
                        Program.Instance._logger.Error($"Failed to authenticate with Steam: {ex.Message}");
                        Abort(false);
                        return;
                    }

                    authSession = null;
                }

                steamUser.LogOn(logonDetails);
            }
        }

        private void DisconnectedCallback(SteamClient.DisconnectedCallback disconnected)
        {
            bDidDisconnect = true;

            StopCallbackTask();
            Program.Instance._logger.Debug($"Disconnected: bIsConnectionRecovery = {bIsConnectionRecovery}, UserInitiated = {disconnected.UserInitiated}, bExpectingDisconnectRemote = {bExpectingDisconnectRemote}");

            // When recovering the connection, we want to reconnect even if the remote disconnects us
            if (!bIsConnectionRecovery && (disconnected.UserInitiated || bExpectingDisconnectRemote))
            {
                Program.Instance._logger.Warn("Disconnected from Steam");

                // Any operations outstanding need to be aborted
                bAborted = true;
            }
            else if (connectionBackoff >= 10)
            {
                Program.Instance._logger.Error("Could not connect to Steam after 10 tries");
                Abort(false);
            }
            else if (!bAborted)
            {
                connectionBackoff += 1;

                if (bConnecting)
                {
                    Program.Instance._logger.Warn("Connection to Steam failed. Trying again");
                }
                else
                {
                    Program.Instance._logger.Warn("Lost connection to Steam. Reconnecting");
                }

                Thread.Sleep(1000 * connectionBackoff);

                // Any connection related flags need to be reset here to match the state after Connect
                ResetConnectionFlags();
                steamClient.Connect();
            }
        }

        private void LogOnCallback(SteamUser.LoggedOnCallback loggedOn)
        {
            var isSteamGuard = loggedOn.Result == EResult.AccountLogonDenied;
            var is2FA = loggedOn.Result == EResult.AccountLoginDeniedNeedTwoFactor;
            var isAccessToken = ContentDownloader.Config.RememberPassword && logonDetails.AccessToken != null &&
                loggedOn.Result is EResult.InvalidPassword
                or EResult.InvalidSignature
                or EResult.AccessDenied
                or EResult.Expired
                or EResult.Revoked;

            if (isSteamGuard || is2FA || isAccessToken)
            {
                bExpectingDisconnectRemote = true;
                Abort(false);

                if (!isAccessToken)
                {
                    Program.Instance._logger.Info("This account is protected by Steam Guard.");
                }

                if (is2FA)
                {
                    do
                    {
                        Console.Write("Please enter your 2 factor auth code from your authenticator app: ");
                        logonDetails.TwoFactorCode = Console.ReadLine();
                    } while (string.IsNullOrWhiteSpace(logonDetails.TwoFactorCode));
                }
                else if (isAccessToken)
                {
                    AccountSettingsStore.Instance.Settings.LoginTokens.Remove(logonDetails.Username, out var _);
                    AccountSettingsStore.Instance.Save();

                    // TODO: Handle gracefully by falling back to password prompt?
                    Program.Instance._logger.Info("Access token was rejected.");
                    Abort(false);
                    return;
                }
                else
                {
                    do
                    {
                        Program.Instance._logger.Info("Please enter the authentication code sent to your email address: ");
                        logonDetails.AuthCode = Console.ReadLine();
                    } while (string.Empty == logonDetails.AuthCode);
                }

                Program.Instance._logger.Info("Retrying Steam3 connection...");
                Connect();

                return;
            }

            if (loggedOn.Result == EResult.TryAnotherCM)
            {
                Program.Instance._logger.Info("Retrying Steam3 connection (TryAnotherCM)...");

                Reconnect();

                return;
            }

            if (loggedOn.Result == EResult.ServiceUnavailable)
            {
                Program.Instance._logger.Info($"Unable to login to Steam3: {loggedOn.Result}");
                Abort(false);

                return;
            }

            if (loggedOn.Result == EResult.Expired)
            {
                Program.Instance._logger.Info($"Unable to login to Steam3: {loggedOn.Result}");

                AccountSettingsStore.Instance.Settings.LoginTokens.Remove(logonDetails.Username, out var _);
                AccountSettingsStore.Instance.Save();

                logonDetails.Password = password;
                logonDetails.AccessToken = null;
                bExpectingDisconnectRemote = true;
                Abort(false);
                Connect();

                return;
            }

            if (loggedOn.Result != EResult.OK)
            {
                Program.Instance._logger.Info($"Unable to login to Steam3: {loggedOn.Result}");
                Abort();

                return;
            }

            Program.Instance._logger.Info(" Done!");

            this.seq++;
            IsLoggedOn = true;

            if (ContentDownloader.Config.CellID == 0)
            {
                Program.Instance._logger.Info($"Using Steam3 suggested CellID: {loggedOn.CellID}");
                ContentDownloader.Config.CellID = (int)loggedOn.CellID;
            }
        }

        private void LicenseListCallback(SteamApps.LicenseListCallback licenseList)
        {
            if (licenseList.Result != EResult.OK)
            {
                Program.Instance._logger.ErrorFormat("Unable to get license list: {0} ", licenseList.Result);
                Abort();

                return;
            }

            Program.Instance._logger.ErrorFormat("Got {0} licenses for account!", licenseList.LicenseList.Count);
            Licenses = licenseList.LicenseList;

            foreach (var license in licenseList.LicenseList)
            {
                if (license.AccessToken > 0)
                {
                    PackageTokens.TryAdd(license.PackageID, license.AccessToken);
                }
            }
        }

        private static void DisplayQrCode(string challengeUrl)
        {
            // Encode the link as a QR code
            using var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(challengeUrl, QRCodeGenerator.ECCLevel.L);
            using var qrCode = new AsciiQRCode(qrCodeData);
            var qrCodeAsAsciiArt = qrCode.GetGraphic(1, drawQuietZones: false);

            Program.Instance._logger.Info("Use the Steam Mobile App to sign in with this QR code:");
            Console.WriteLine(qrCodeAsAsciiArt);
        }
    }
}
