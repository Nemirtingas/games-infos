using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SteamKit2;
using SteamKit2.Internal;

namespace steam_retriever
{
    public partial class SteamUserStats : ClientMsgHandler
    {
        Dictionary<EMsg, Action<IPacketMsg>> dispatchMap;

        internal SteamUserStats()
        {
            dispatchMap = new Dictionary<EMsg, Action<IPacketMsg>>
            {
                { EMsg.ClientGetUserStatsResponse, HandleUserStatsResponse },
            };
        }

        // define our custom callback class
        // this will pass data back to the user of the handler
        public class GetUserStatsCallback : CallbackMsg
        {
            public sealed class Stat
            {
                uint StatId;
                uint StatValue;

                public Stat(CMsgClientGetUserStatsResponse.Stats stat)
                {
                    StatId = stat.stat_id;
                    StatValue = stat.stat_value;
                }
            }

            public EResult Result;
            public ReadOnlyCollection<Stat> Stats;
            public uint CRCStats;
            public ulong GameId;
            public KeyValue Schema;

            // generally we don't want user code to instantiate callback objects,
            // but rather only let handlers create them
            internal GetUserStatsCallback(JobID jobID, CMsgClientGetUserStatsResponse msg)
            {
                this.JobID = jobID;

                this.Result = (EResult)msg.eresult;
                if (this.Result == EResult.OK)
                {
                    var stats_list = msg.stats.Select(s => new Stat(s))
                        .ToList();

                    this.Stats = new ReadOnlyCollection<Stat>(stats_list);
                    this.CRCStats = msg.crc_stats;
                    this.GameId = msg.game_id;
                    this.Schema = new KeyValue();
                    if (!Schema.TryReadAsBinary(new MemoryStream(msg.schema)))
                        this.Schema = null;
                }
            }
        }

        public override void HandleMsg(IPacketMsg packetMsg)
        {
            if (packetMsg == null)
            {
                throw new ArgumentNullException(nameof(packetMsg));
            }

            if (!dispatchMap.TryGetValue(packetMsg.MsgType, out var handlerFunc))
            {
                // ignore messages that we don't have a handler function for
                return;
            }

            handlerFunc(packetMsg);
        }

        void HandleUserStatsResponse(IPacketMsg packetMsg)
        {
            var userStats = new ClientMsgProtobuf<CMsgClientGetUserStatsResponse>(packetMsg);

            var callback = new GetUserStatsCallback(packetMsg.TargetJobID, userStats.Body);
            this.Client.PostCallback(callback);
        }

        public AsyncJob<GetUserStatsCallback> GetUserStats(uint appid, ulong user_id)
        {
            var request = new ClientMsgProtobuf<CMsgClientGetUserStats>(EMsg.ClientGetUserStats);
            request.SourceJobID = this.Client.GetNextJobID();

            request.Body.steam_id_for_user = user_id;
            request.Body.game_id = appid;
            request.Body.crc_stats = 0;
            request.Body.schema_local_version = -1;

            this.Client.Send(request);

            return new AsyncJob<GetUserStatsCallback>(this.Client, request.SourceJobID);
        }
    }

    class Steam3Session
    {
        public class Credentials
        {
            public bool LoggedOn { get; set; }
            public ulong SessionToken { get; set; }

            public bool IsValid
            {
                get { return LoggedOn; }
            }
        }

        public ReadOnlyCollection<SteamApps.LicenseListCallback.License> Licenses
        {
            get;
            private set;
        }

        public Dictionary<uint, ulong> AppTokens { get; private set; }
        public Dictionary<uint, ulong> PackageTokens { get; private set; }
        public Dictionary<uint, byte[]> DepotKeys { get; private set; }
        public ConcurrentDictionary<string, TaskCompletionSource<SteamApps.CDNAuthTokenCallback>> CDNAuthTokens { get; private set; }
        public Dictionary<uint, SteamApps.PICSProductInfoCallback.PICSProductInfo> AppInfo { get; private set; }
        public Dictionary<uint, SteamApps.PICSProductInfoCallback.PICSProductInfo> PackageInfo { get; private set; }
        public Dictionary<string, byte[]> AppBetaPasswords { get; private set; }

        public SteamClient steamClient;
        public SteamUser steamUser;
        public SteamGameServer steamGameserver;
        public SteamContent steamContent;
        readonly SteamApps steamApps;
        readonly SteamCloud steamCloud;
        readonly SteamUserStats steamUserStats;
        readonly SteamUnifiedMessages.UnifiedService<IPublishedFile> steamPublishedFile;
        readonly SteamUnifiedMessages.UnifiedService<IInventory> steamInventory;

        readonly CallbackManager callbacks;

        readonly bool authenticatedUser;
        bool bConnected;
        bool bConnecting;
        bool bAborted;
        bool bExpectingDisconnectRemote;
        bool bDidDisconnect;
        bool bDidReceiveLoginKey;
        bool bIsConnectionRecovery;
        int connectionBackoff;
        int seq; // more hack fixes
        DateTime connectTime;

        // input
        readonly SteamUser.LogOnDetails logonDetails;

        // output
        readonly Credentials credentials;

        static readonly TimeSpan STEAM3_TIMEOUT = TimeSpan.FromSeconds(30);


        public Steam3Session(SteamUser.LogOnDetails details)
        {
            this.logonDetails = details;

            this.authenticatedUser = details.Username != null;
            this.credentials = new Credentials();
            this.bConnected = false;
            this.bConnecting = false;
            this.bAborted = false;
            this.bExpectingDisconnectRemote = false;
            this.bDidDisconnect = false;
            this.bDidReceiveLoginKey = false;
            this.seq = 0;

            this.AppTokens = new Dictionary<uint, ulong>();
            this.PackageTokens = new Dictionary<uint, ulong>();
            this.DepotKeys = new Dictionary<uint, byte[]>();
            this.CDNAuthTokens = new ConcurrentDictionary<string, TaskCompletionSource<SteamApps.CDNAuthTokenCallback>>();
            this.AppInfo = new Dictionary<uint, SteamApps.PICSProductInfoCallback.PICSProductInfo>();
            this.PackageInfo = new Dictionary<uint, SteamApps.PICSProductInfoCallback.PICSProductInfo>();
            this.AppBetaPasswords = new Dictionary<string, byte[]>();

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
            this.steamPublishedFile = steamUnifiedMessages.CreateService<IPublishedFile>();
            this.steamInventory = steamUnifiedMessages.CreateService<IInventory>();
            this.steamContent = this.steamClient.GetHandler<SteamContent>();

            this.callbacks = new CallbackManager(this.steamClient);

            this.callbacks.Subscribe<SteamClient.ConnectedCallback>(ConnectedCallback);
            this.callbacks.Subscribe<SteamClient.DisconnectedCallback>(DisconnectedCallback);
            this.callbacks.Subscribe<SteamUser.LoggedOnCallback>(LogOnCallback);
            this.callbacks.Subscribe<SteamUser.SessionTokenCallback>(SessionTokenCallback);
            this.callbacks.Subscribe<SteamApps.LicenseListCallback>(LicenseListCallback);
            this.callbacks.Subscribe<SteamUser.UpdateMachineAuthCallback>(UpdateMachineAuthCallback);
            this.callbacks.Subscribe<SteamUser.LoginKeyCallback>(LoginKeyCallback);

            Console.Write("Connecting to Steam3...");

            if (authenticatedUser)
            {
                var fi = new FileInfo(String.Format("{0}.sentryFile", logonDetails.Username));
                if (AccountSettingsStore.Instance.SentryData != null && AccountSettingsStore.Instance.SentryData.ContainsKey(logonDetails.Username))
                {
                    logonDetails.SentryFileHash = Util.SHAHash(AccountSettingsStore.Instance.SentryData[logonDetails.Username]);
                }
                else if (fi.Exists && fi.Length > 0)
                {
                    var sentryData = File.ReadAllBytes(fi.FullName);
                    logonDetails.SentryFileHash = Util.SHAHash(sentryData);
                    AccountSettingsStore.Instance.SentryData[logonDetails.Username] = sentryData;
                    AccountSettingsStore.Save();
                }
            }

            Connect();
        }

        public delegate bool WaitCondition();

        private readonly object steamLock = new object();

        public bool WaitUntilCallback(Action submitter, WaitCondition waiter)
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

        public Credentials WaitForCredentials()
        {
            if (credentials.IsValid || bAborted)
                return credentials;

            WaitUntilCallback(() => { }, () => { return credentials.IsValid; });

            return credentials;
        }

        public void RequestAppsInfo(IEnumerable<uint> appIds, bool bForce = false)
        {
            List<uint> request_appids = new List<uint>();
            {
                foreach (var item in appIds)
                {
                    if(!AppInfo.ContainsKey(item) || bForce)
                    {
                        request_appids.Add(item);
                    }
                }

                if (request_appids.Count == 0 || bAborted)
                    return;
            }

            var completed = false;
            Action<SteamApps.PICSTokensCallback> cbMethodTokens = appTokens =>
            {
                completed = true;
                foreach (var item in request_appids)
                {
                    if (appTokens.AppTokensDenied.Contains(item))
                    {
                        //Console.WriteLine("Insufficient privileges to get access token for app {0}", item);
                    }
                }

                foreach (var token_dict in appTokens.AppTokens)
                {
                    this.AppTokens[token_dict.Key] = token_dict.Value;
                }
            };

            WaitUntilCallback(() =>
            {
                callbacks.Subscribe(steamApps.PICSGetAccessTokens(request_appids, new List<uint>()), cbMethodTokens);
            }, () => { return completed; });

            completed = false;
            Action<SteamApps.PICSProductInfoCallback> cbMethod = appInfo =>
            {
                completed = !appInfo.ResponsePending;

                foreach (var app_value in appInfo.Apps)
                {
                    var app = app_value.Value;

                    //Console.WriteLine("Got AppInfo for {0}", app.ID);
                    AppInfo[app.ID] = app;
                }

                foreach (var app in appInfo.UnknownApps)
                {
                    AppInfo[app] = null;
                }
            };

            List<SteamApps.PICSRequest> requests = new List<SteamApps.PICSRequest>(request_appids.Count);
            foreach (var item in request_appids)
            {
                var pic = new SteamApps.PICSRequest(item);
                if (AppTokens.ContainsKey(item))
                {
                    pic.AccessToken = AppTokens[item];
                }
                requests.Add(pic);
            }

            WaitUntilCallback(() =>
            {
                callbacks.Subscribe(steamApps.PICSGetProductInfo(requests, new List<SteamApps.PICSRequest>()), cbMethod);
            }, () => { return completed; });
        }

        public void RequestAppInfo(uint appId, bool bForce = false)
        {
            RequestAppsInfo(new List<uint>{ appId }, bForce);
        }

        public string GetInventoryDigest(uint appid)
        {
            CInventory_GetItemDefMeta_Request itemdef_req = new CInventory_GetItemDefMeta_Request
            {
                appid = appid,
            };

            var completed = false;
            string digest = null;

            Action<SteamUnifiedMessages.ServiceMethodResponse> cbMethod = callback =>
            {
                completed = true;
                if (callback.Result == EResult.OK)
                {
                    var response = callback.GetDeserializedResponse<CInventory_GetItemDefMeta_Response>();
                    digest = response.digest;
                }
                else
                {
                    throw new Exception($"EResult {(int)callback.Result} ({callback.Result}) while retrieving items definition for {appid}.");
                }
            };

            WaitUntilCallback(() =>
            {
                callbacks.Subscribe(steamInventory.SendMessage(api => api.GetItemDefMeta(itemdef_req)), cbMethod);
            }, () => { return completed; });

            return digest;
        }

        public SteamUserStats.GetUserStatsCallback GetUserStats(uint appId, ulong steamId)
        {
            SteamUserStats.GetUserStatsCallback res = null;
            var completed = false;

            Action<SteamUserStats.GetUserStatsCallback> cbMethod = userStats =>
            {
                completed = true;
                res = userStats;
            };

            AsyncJob<SteamUserStats.GetUserStatsCallback> aj = null;

            WaitUntilCallback(() =>
            {
                aj = steamUserStats.GetUserStats(appId, steamId);
                aj.Timeout = TimeSpan.FromSeconds(5);
                callbacks.Subscribe(aj, cbMethod);
            }, () => {
                if (aj != null && aj.ToTask().IsCompleted)
                {
                    completed = true;
                }
                return completed;
            });

            return res;
        }

        public void RequestPackageInfo(IEnumerable<uint> packageIds)
        {
            var packages = packageIds.ToList();
            packages.RemoveAll(pid => PackageInfo.ContainsKey(pid));

            if (packages.Count == 0 || bAborted)
                return;

            var completed = false;
            Action<SteamApps.PICSProductInfoCallback> cbMethod = packageInfo =>
            {
                completed = !packageInfo.ResponsePending;

                foreach (var package_value in packageInfo.Packages)
                {
                    var package = package_value.Value;
                    PackageInfo[package.ID] = package;
                }

                foreach (var package in packageInfo.UnknownPackages)
                {
                    PackageInfo[package] = null;
                }
            };

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

            WaitUntilCallback(() =>
            {
                callbacks.Subscribe(steamApps.PICSGetProductInfo(new List<SteamApps.PICSRequest>(), packageRequests), cbMethod);
            }, () => { return completed; });
        }

        public bool RequestFreeAppLicense(uint appId)
        {
            var success = false;
            var completed = false;
            Action<SteamApps.FreeLicenseCallback> cbMethod = resultInfo =>
            {
                completed = true;
                success = resultInfo.GrantedApps.Contains(appId);
            };

            WaitUntilCallback(() =>
            {
                callbacks.Subscribe(steamApps.RequestFreeLicense(appId), cbMethod);
            }, () => { return completed; });

            return success;
        }

        public void RequestDepotKey(uint depotId, uint appid = 0)
        {
            if (DepotKeys.ContainsKey(depotId) || bAborted)
                return;

            var completed = false;

            Action<SteamApps.DepotKeyCallback> cbMethod = depotKey =>
            {
                completed = true;
                Console.WriteLine("Got depot key for {0} result: {1}", depotKey.DepotID, depotKey.Result);

                if (depotKey.Result != EResult.OK)
                {
                    Abort();
                    return;
                }

                DepotKeys[depotKey.DepotID] = depotKey.DepotKey;
            };

            WaitUntilCallback(() =>
            {
                callbacks.Subscribe(steamApps.GetDepotDecryptionKey(depotId, appid), cbMethod);
            }, () => { return completed; });
        }


        public async Task<ulong> GetDepotManifestRequestCodeAsync(uint depotId, uint appId, ulong manifestId, string branch)
        {
            if (bAborted)
                return 0;

            var requestCode = await steamContent.GetManifestRequestCode(depotId, appId, manifestId, branch);

            Console.WriteLine("Got manifest request code for {0} {1} result: {2}",
                depotId, manifestId,
                requestCode);

            return requestCode;
        }

        public void CheckAppBetaPassword(uint appid, string password)
        {
            var completed = false;
            Action<SteamApps.CheckAppBetaPasswordCallback> cbMethod = appPassword =>
            {
                completed = true;

                Console.WriteLine("Retrieved {0} beta keys with result: {1}", appPassword.BetaPasswords.Count, appPassword.Result);

                foreach (var entry in appPassword.BetaPasswords)
                {
                    AppBetaPasswords[entry.Key] = entry.Value;
                }
            };

            WaitUntilCallback(() =>
            {
                callbacks.Subscribe(steamApps.CheckAppBetaPassword(appid, password), cbMethod);
            }, () => { return completed; });
        }

        public PublishedFileDetails GetPublishedFileDetails(uint? appId, PublishedFileID pubFile)
        {
            var pubFileRequest = new CPublishedFile_GetDetails_Request();
            if (appId != null)
                pubFileRequest.appid = appId.Value;

            pubFileRequest.publishedfileids.Add(pubFile);

            var completed = false;
            PublishedFileDetails details = null;

            Action<SteamUnifiedMessages.ServiceMethodResponse> cbMethod = callback =>
            {
                completed = true;
                if (callback.Result == EResult.OK)
                {
                    var response = callback.GetDeserializedResponse<CPublishedFile_GetDetails_Response>();
                    details = response.publishedfiledetails.FirstOrDefault();
                }
                else
                {
                    throw new Exception($"EResult {(int)callback.Result} ({callback.Result}) while retrieving file details for pubfile {pubFile}.");
                }
            };

            WaitUntilCallback(() =>
            {
                callbacks.Subscribe(steamPublishedFile.SendMessage(api => api.GetDetails(pubFileRequest)), cbMethod);
            }, () => { return completed; });

            return details;
        }


        public SteamCloud.UGCDetailsCallback GetUGCDetails(UGCHandle ugcHandle)
        {
            var completed = false;
            SteamCloud.UGCDetailsCallback details = null;

            Action<SteamCloud.UGCDetailsCallback> cbMethod = callback =>
            {
                completed = true;
                if (callback.Result == EResult.OK)
                {
                    details = callback;
                }
                else if (callback.Result == EResult.FileNotFound)
                {
                    details = null;
                }
                else
                {
                    throw new Exception($"EResult {(int)callback.Result} ({callback.Result}) while retrieving UGC details for {ugcHandle}.");
                }
            };

            WaitUntilCallback(() =>
            {
                callbacks.Subscribe(steamCloud.RequestUGCDetails(ugcHandle), cbMethod);
            }, () => { return completed; });

            return details;
        }

        private void ResetConnectionFlags()
        {
            bExpectingDisconnectRemote = false;
            bDidDisconnect = false;
            bIsConnectionRecovery = false;
            bDidReceiveLoginKey = false;
        }

        void Connect()
        {
            bAborted = false;
            bConnected = false;
            bConnecting = true;
            connectionBackoff = 0;

            ResetConnectionFlags();

            this.connectTime = DateTime.Now;
            this.steamClient.Connect();
        }

        private void Abort(bool sendLogOff = true)
        {
            Disconnect(sendLogOff);
        }

        public void Disconnect(bool sendLogOff = true)
        {
            if (sendLogOff)
            {
                steamUser.LogOff();
            }

            bAborted = true;
            bConnected = false;
            bConnecting = false;
            bIsConnectionRecovery = false;
            steamClient.Disconnect();

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

        public void TryWaitForLoginKey()
        {
            if (logonDetails.Username == null || !credentials.LoggedOn || !ContentDownloader.Config.RememberPassword) return;

            var totalWaitPeriod = DateTime.Now.AddSeconds(3);

            while (true)
            {
                var now = DateTime.Now;
                if (now >= totalWaitPeriod) break;

                if (bDidReceiveLoginKey) break;

                callbacks.RunWaitAllCallbacks(TimeSpan.FromMilliseconds(100));
            }
        }

        private void WaitForCallbacks()
        {
            callbacks.RunWaitCallbacks(TimeSpan.FromSeconds(1));

            var diff = DateTime.Now - connectTime;

            if (diff > STEAM3_TIMEOUT && !bConnected)
            {
                Console.WriteLine("Timeout connecting to Steam3.");
                Abort();
            }
        }

        private void ConnectedCallback(SteamClient.ConnectedCallback connected)
        {
            Console.WriteLine(" Done!");
            bConnecting = false;
            bConnected = true;
            if (!authenticatedUser)
            {
                Console.Write("Logging anonymously into Steam3...");
                //steamUser.LogOnAnonymous();
                steamGameserver.LogOnAnonymous();
            }
            else
            {
                Console.Write("Logging '{0}' into Steam3...", logonDetails.Username);
                steamUser.LogOn(logonDetails);
            }
        }

        private void DisconnectedCallback(SteamClient.DisconnectedCallback disconnected)
        {
            bDidDisconnect = true;

            // When recovering the connection, we want to reconnect even if the remote disconnects us
            if (!bIsConnectionRecovery && (disconnected.UserInitiated || bExpectingDisconnectRemote))
            {
                Console.WriteLine("Disconnected from Steam");

                // Any operations outstanding need to be aborted
                bAborted = true;
            }
            else if (connectionBackoff >= 10)
            {
                Console.WriteLine("Could not connect to Steam after 10 tries");
                Abort(false);
            }
            else if (!bAborted)
            {
                if (bConnecting)
                {
                    Console.WriteLine("Connection to Steam failed. Trying again");
                }
                else
                {
                    Console.WriteLine("Lost connection to Steam. Reconnecting");
                }

                Thread.Sleep(1000 * ++connectionBackoff);

                // Any connection related flags need to be reset here to match the state after Connect
                ResetConnectionFlags();
                steamClient.Connect();
            }
        }

        private void LogOnCallback(SteamUser.LoggedOnCallback loggedOn)
        {
            var isSteamGuard = loggedOn.Result == EResult.AccountLogonDenied;
            var is2FA = loggedOn.Result == EResult.AccountLoginDeniedNeedTwoFactor;
            var isLoginKey = ContentDownloader.Config.RememberPassword && logonDetails.LoginKey != null && loggedOn.Result == EResult.InvalidPassword;

            if (isSteamGuard || is2FA || isLoginKey)
            {
                bExpectingDisconnectRemote = true;
                Abort(false);

                if (!isLoginKey)
                {
                    Console.WriteLine("This account is protected by Steam Guard.");
                }

                if (is2FA)
                {
                    do
                    {
                        Console.Write("Please enter your 2 factor auth code from your authenticator app: ");
                        logonDetails.TwoFactorCode = Console.ReadLine();
                    } while (String.Empty == logonDetails.TwoFactorCode);
                }
                else if (isLoginKey)
                {
                    AccountSettingsStore.Instance.LoginKeys.Remove(logonDetails.Username);
                    AccountSettingsStore.Save();

                    logonDetails.LoginKey = null;

                    if (ContentDownloader.Config.SuppliedPassword != null)
                    {
                        Console.WriteLine("Login key was expired. Connecting with supplied password.");
                        logonDetails.Password = ContentDownloader.Config.SuppliedPassword;
                    }
                    else
                    {
                        Console.Write("Login key was expired. Please enter your password: ");
                        logonDetails.Password = Util.ReadPassword();
                    }
                }
                else
                {
                    do
                    {
                        Console.Write("Please enter the authentication code sent to your email address: ");
                        logonDetails.AuthCode = Console.ReadLine();
                    } while (string.Empty == logonDetails.AuthCode);
                }

                Console.Write("Retrying Steam3 connection...");
                Connect();

                return;
            }

            if (loggedOn.Result == EResult.TryAnotherCM)
            {
                Console.Write("Retrying Steam3 connection (TryAnotherCM)...");

                Reconnect();

                return;
            }

            if (loggedOn.Result == EResult.ServiceUnavailable)
            {
                Console.WriteLine("Unable to login to Steam3: {0}", loggedOn.Result);
                Abort(false);

                return;
            }

            if (loggedOn.Result != EResult.OK)
            {
                Console.WriteLine("Unable to login to Steam3: {0}", loggedOn.Result);
                Abort();

                return;
            }

            Console.WriteLine(" Done!");

            this.seq++;
            credentials.LoggedOn = true;

            if (ContentDownloader.Config.CellID == 0)
            {
                Console.WriteLine("Using Steam3 suggested CellID: " + loggedOn.CellID);
                ContentDownloader.Config.CellID = (int)loggedOn.CellID;
            }
        }

        private void SessionTokenCallback(SteamUser.SessionTokenCallback sessionToken)
        {
            Console.WriteLine("Got session token!");
            credentials.SessionToken = sessionToken.SessionToken;
        }

        private void LicenseListCallback(SteamApps.LicenseListCallback licenseList)
        {
            if (licenseList.Result != EResult.OK)
            {
                Console.WriteLine("Unable to get license list: {0} ", licenseList.Result);
                Abort();

                return;
            }

            Console.WriteLine("Got {0} licenses for account!", licenseList.LicenseList.Count);
            Licenses = licenseList.LicenseList;

            foreach (var license in licenseList.LicenseList)
            {
                if (license.AccessToken > 0)
                {
                    PackageTokens.TryAdd(license.PackageID, license.AccessToken);
                }
            }
        }

        private void UpdateMachineAuthCallback(SteamUser.UpdateMachineAuthCallback machineAuth)
        {
            var hash = Util.SHAHash(machineAuth.Data);
            Console.WriteLine("Got Machine Auth: {0} {1} {2} {3}", machineAuth.FileName, machineAuth.Offset, machineAuth.BytesToWrite, machineAuth.Data.Length, hash);

            AccountSettingsStore.Instance.SentryData[logonDetails.Username] = machineAuth.Data;
            AccountSettingsStore.Save();

            var authResponse = new SteamUser.MachineAuthDetails
            {
                BytesWritten = machineAuth.BytesToWrite,
                FileName = machineAuth.FileName,
                FileSize = machineAuth.BytesToWrite,
                Offset = machineAuth.Offset,

                SentryFileHash = hash, // should be the sha1 hash of the sentry file we just wrote

                OneTimePassword = machineAuth.OneTimePassword, // not sure on this one yet, since we've had no examples of steam using OTPs

                LastError = 0, // result from win32 GetLastError
                Result = EResult.OK, // if everything went okay, otherwise ~who knows~

                JobID = machineAuth.JobID, // so we respond to the correct server job
            };

            // send off our response
            steamUser.SendMachineAuthResponse(authResponse);
        }

        private void LoginKeyCallback(SteamUser.LoginKeyCallback loginKey)
        {
            //Console.WriteLine("Accepted new login key for account {0}", logonDetails.Username);

            AccountSettingsStore.Instance.LoginKeys[logonDetails.Username] = loginKey.LoginKey;
            AccountSettingsStore.Save();

            steamUser.AcceptNewLoginKey(loginKey);

            bDidReceiveLoginKey = true;
        }
    }
}
