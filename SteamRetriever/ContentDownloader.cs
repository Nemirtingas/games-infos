using log4net.Repository.Hierarchy;
using SteamKit2;
using SteamKit2.CDN;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace SteamRetriever
{
    class ContentDownloaderException(string value) : Exception(value)
    {
    }

    static class ContentDownloader
    {
        public const uint INVALID_APP_ID = uint.MaxValue;
        public const uint INVALID_DEPOT_ID = uint.MaxValue;
        public const ulong INVALID_MANIFEST_ID = ulong.MaxValue;
        public const string DEFAULT_BRANCH = "Public";
        public static Dictionary<ulong, UGCFileInfo> UGCFilesDownloaded = new();

        public static DownloadConfig Config = new();

        internal static ConcurrentDictionary<string, int> ContentServerPenalty { get; set; } = new();
        public static Steam3Session Steam3;
        private static CDNClientPool CDNPool;

        private const string DEFAULT_DOWNLOAD_DIR = "depots";
        private const string CONFIG_DIR = ".DepotDownloader";
        private static readonly string STAGING_DIR = Path.Combine(CONFIG_DIR, "staging");

        private sealed class DepotDownloadInfo(
            uint depotid, uint appId, ulong manifestId, string branch,
            string installDir, byte[] depotKey)
        {
            public uint DepotId { get; } = depotid;
            public uint AppId { get; } = appId;
            public ulong ManifestId { get; } = manifestId;
            public string Branch { get; } = branch;
            public string InstallDir { get; } = installDir;
            public byte[] DepotKey { get; } = depotKey;
        }

        private sealed class DepotDownloadInfoWithManifest(uint depotid, uint appId, ulong manifestId, string branch,
        string installDir, byte[] depotKey, DepotManifest depotManifest)
        {
            internal DepotDownloadInfo depotDownloadInfo { get; } = new DepotDownloadInfo(depotid, appId, manifestId, branch, installDir, depotKey);
            internal DepotManifest depotManifest { get; } = depotManifest;
        }

        static bool CreateDirectories(uint depotId, uint depotVersion, out string installDir)
        {
            installDir = null;
            try
            {
                if (string.IsNullOrWhiteSpace(Config.InstallDirectory))
                {
                    Directory.CreateDirectory(DEFAULT_DOWNLOAD_DIR);

                    var depotPath = Path.Combine(DEFAULT_DOWNLOAD_DIR, depotId.ToString());
                    Directory.CreateDirectory(depotPath);

                    installDir = Path.Combine(depotPath, depotVersion.ToString());
                    Directory.CreateDirectory(installDir);

                    Directory.CreateDirectory(Path.Combine(installDir, CONFIG_DIR));
                    Directory.CreateDirectory(Path.Combine(installDir, STAGING_DIR));
                }
                else
                {
                    Directory.CreateDirectory(Config.InstallDirectory);

                    installDir = Config.InstallDirectory;

                    Directory.CreateDirectory(Path.Combine(installDir, CONFIG_DIR));
                    Directory.CreateDirectory(Path.Combine(installDir, STAGING_DIR));
                }
            }
            catch
            {
                return false;
            }

            return true;
        }

        static bool TestIsFileIncluded(string filename)
        {
            if (!Config.UsingFileList)
                return true;

            filename = filename.Replace('\\', '/');

            if (Config.FilesToDownload.Contains(filename))
            {
                return true;
            }

            foreach (var rgx in Config.FilesToDownloadRegex)
            {
                var m = rgx.Match(filename);

                if (m.Success)
                    return true;
            }

            return false;
        }

        static async Task<bool> AccountHasAccessAsync(uint appId, uint depotId)
        {
            if (Steam3 == null || Steam3.steamUser.SteamID == null || (Steam3.Licenses == null && Steam3.steamUser.SteamID.AccountType != EAccountType.AnonUser))
                return false;

            IEnumerable<uint> licenseQuery;
            if (Steam3.steamUser.SteamID.AccountType == EAccountType.AnonUser)
            {
                licenseQuery = [17906];
            }
            else
            {
                licenseQuery = Steam3.Licenses.Select(x => x.PackageID).Distinct();
            }

            await Steam3.RequestPackageInfo(licenseQuery);

            foreach (var license in licenseQuery)
            {
                if (Steam3.PackageInfo.TryGetValue(license, out var package) && package != null)
                {
                    if (package.KeyValues["appids"].Children.Any(child => child.AsUnsignedInteger() == depotId))
                        return true;

                    if (package.KeyValues["depotids"].Children.Any(child => child.AsUnsignedInteger() == depotId))
                        return true;
                }
            }

            // Check if this app is free to download without a license
            var info = GetSteam3AppSection(appId, EAppInfoSection.Common);
            if (info != null && info["FreeToDownload"].AsBoolean())
                return true;

            return false;
        }

        internal static KeyValue Getsteam3AppSection(uint appId, EAppInfoSection section)
        {
            if (Steam3 == null || Steam3.AppInfo == null)
            {
                return null;
            }

            if (!Steam3.AppInfo.TryGetValue(appId, out var app) || app == null)
            {
                return null;
            }

            var appinfo = app.KeyValues;
            var section_key = section switch
            {
                EAppInfoSection.Common => "common",
                EAppInfoSection.Extended => "extended",
                EAppInfoSection.Config => "config",
                EAppInfoSection.Depots => "depots",
                _ => throw new NotImplementedException(),
            };
            var section_kv = appinfo.Children.Where(c => c.Name == section_key).FirstOrDefault();
            return section_kv;
        }

        static uint GetSteam3AppBuildNumber(uint appId, string branch)
        {
            if (appId == INVALID_APP_ID)
                return 0;


            var depots = GetSteam3AppSection(appId, EAppInfoSection.Depots);
            var branches = depots["branches"];
            var node = branches[branch];

            if (node == KeyValue.Invalid)
                return 0;

            var buildid = node["buildid"];

            if (buildid == KeyValue.Invalid)
                return 0;

            return uint.Parse(buildid.Value);
        }

        static uint GetSteam3DepotProxyAppId(uint depotId, uint appId)
        {
            var depots = GetSteam3AppSection(appId, EAppInfoSection.Depots);
            var depotChild = depots[depotId.ToString()];

            if (depotChild == KeyValue.Invalid)
                return INVALID_APP_ID;

            if (depotChild["depotfromapp"] == KeyValue.Invalid)
                return INVALID_APP_ID;

            return depotChild["depotfromapp"].AsUnsignedInteger();
        }

        static string GetAppName(uint appId)
        {
            var info = GetSteam3AppSection(appId, EAppInfoSection.Common);
            if (info == null)
                return string.Empty;

            return info["name"].AsString();
        }

        static async Task<ulong> GetSteam3DepotManifestAsync(uint depotId, uint appId, string branch)
        {
            var depots = GetSteam3AppSection(appId, EAppInfoSection.Depots);
            var depotChild = depots[depotId.ToString()];

            if (depotChild == KeyValue.Invalid)
                return INVALID_MANIFEST_ID;

            // Shared depots can either provide manifests, or leave you relying on their parent app.
            // It seems that with the latter, "sharedinstall" will exist (and equals 2 in the one existance I know of).
            // Rather than relay on the unknown sharedinstall key, just look for manifests. Test cases: 111710, 346680.
            if (depotChild["manifests"] == KeyValue.Invalid && depotChild["depotfromapp"] != KeyValue.Invalid)
            {
                var otherAppId = depotChild["depotfromapp"].AsUnsignedInteger();
                if (otherAppId == appId)
                {
                    // This shouldn't ever happen, but ya never know with Valve. Don't infinite loop.
                    return INVALID_MANIFEST_ID;
                }

                await Steam3.RequestAppInfoAsync(otherAppId);

                return await GetSteam3DepotManifestAsync(depotId, otherAppId, branch);
            }

            var manifests = depotChild["manifests"];

            if (manifests.Children.Count == 0)
                return INVALID_MANIFEST_ID;

            var node = manifests[branch]["gid"];

            // Non passworded branch, found the manifest
            if (node.Value != null)
                return ulong.Parse(node.Value);

            // If we requested public branch and it had no manifest, nothing to do
            if (string.Equals(branch, DEFAULT_BRANCH, StringComparison.OrdinalIgnoreCase))
                return INVALID_MANIFEST_ID;

            // Either the branch just doesn't exist, or it has a password
            if (string.IsNullOrEmpty(Config.BetaPassword))
            {
                return INVALID_MANIFEST_ID;
            }

            if (!Steam3.AppBetaPasswords.ContainsKey(branch))
            {
                // Submit the password to Steam now to get encryption keys
                await Steam3.CheckAppBetaPassword(appId, Config.BetaPassword);

                if (!Steam3.AppBetaPasswords.ContainsKey(branch))
                {
                    return INVALID_MANIFEST_ID;
                }
            }

            // Got the password, request private depot section
            // TODO: We're probably repeating this request for every depot?
            var privateDepotSection = await Steam3.GetPrivateBetaDepotSectionAsync(appId, branch);

            // Now repeat the same code to get the manifest gid from depot section
            depotChild = privateDepotSection[depotId.ToString()];

            if (depotChild == KeyValue.Invalid)
                return INVALID_MANIFEST_ID;

            manifests = depotChild["manifests"];

            if (manifests.Children.Count == 0)
                return INVALID_MANIFEST_ID;

            node = manifests[branch]["gid"];

            if (node.Value == null)
                return INVALID_MANIFEST_ID;

            return ulong.Parse(node.Value);
        }

        static string GetAppOrDepotName(uint depotId, uint appId)
        {
            if (depotId == INVALID_DEPOT_ID)
            {
                var info = Getsteam3AppSection(appId, EAppInfoSection.Common);

                if (info == null)
                    return String.Empty;

                return info["name"].AsString();
            }

            var depots = Getsteam3AppSection(appId, EAppInfoSection.Depots);

            if (depots == null)
                return String.Empty;

            var depotChild = depots[depotId.ToString()];

            if (depotChild == null)
                return String.Empty;

            return depotChild["name"].AsString();
        }

        public static async Task<bool> InitializeSteam3Async(string username, string password)
        {
            string loginToken = null;

            if (username != null && Config.RememberPassword)
            {
                _ = AccountSettingsStore.Instance.Settings.LoginTokens.TryGetValue(username, out loginToken);
            }

            Steam3 = new Steam3Session(
                new SteamUser.LogOnDetails
                {
                    Username = username,
                    Password = loginToken == null ? password : null,
                    ShouldRememberPassword = Config.RememberPassword,
                    AccessToken = loginToken,
                    LoginID = Config.LoginID ?? 0x534B32, // "SK2"
                }
            );

            if (!Steam3.WaitForCredentials())
            {
                //Program.Instance._logger.Error("Unable to get steam3 credentials.");
                return false;
            }

            CDNPool = new CDNClientPool(Steam3, ContentServerPenalty);
            await CDNPool.UpdateServerList();

            return true;
        }

        public static void ShutdownSteam3()
        {
            if (Steam3 == null)
                return;

            Steam3.Disconnect();
        }

        internal static KeyValue GetSteam3AppSection(uint appId, EAppInfoSection section)
        {
            if (Steam3 == null || Steam3.AppInfo == null)
            {
                return null;
            }

            if (!Steam3.AppInfo.TryGetValue(appId, out var app) || app == null)
            {
                return null;
            }

            var appinfo = app.KeyValues;
            var section_key = section switch
            {
                EAppInfoSection.Common => "common",
                EAppInfoSection.Extended => "extended",
                EAppInfoSection.Config => "config",
                EAppInfoSection.Depots => "depots",
                _ => throw new NotImplementedException(),
            };
            var section_kv = appinfo.Children.Where(c => c.Name == section_key).FirstOrDefault();
            return section_kv;
        }

        public static async Task DownloadPubfileAsync(uint appId, uint? publishedAppId, ulong publishedFileId)
        {
            var details = await Steam3.GetPublishedFileDetails(publishedAppId, publishedFileId);

            if (!string.IsNullOrEmpty(details?.file_url))
            {
                await DownloadWebFile(appId, details.filename, details.file_url);
            }
            else if (details?.hcontent_file > 0)
            {
                if (Steam3 != null)
                    await Steam3.RequestAppInfoAsync(appId);

                var depotIdsFound = new List<uint>();
                var depotIdsExpected = new List<uint> { appId };
                var depots = GetSteam3AppSection(appId, EAppInfoSection.Depots);

                var depotManifestIds = new List<(uint depotId, ulong manifestId)> { (appId, details.hcontent_file) };

                var workshopDepot = depots["workshopdepot"].AsUnsignedInteger();
                if (workshopDepot != 0 && !depotIdsExpected.Contains(workshopDepot))
                {
                    depotIdsExpected.Add(workshopDepot);
                    depotManifestIds = depotManifestIds.Select(pair => (workshopDepot, pair.manifestId)).ToList();
                }

                depotIdsFound.AddRange(depotIdsExpected);

                var manifest = await DownloadRawManifestAsync(appId, depotManifestIds[0].depotId, depotManifestIds[0].manifestId, DEFAULT_BRANCH, null);
                var depotManifest = DepotManifest.Deserialize(manifest);

                var depotManifests = new List<(uint depotId, ulong manifestId, DepotManifest manifest)> { new (appId, details.hcontent_file, depotManifest) };

                await DownloadAppAsync(new CancellationTokenSource(), appId, depotManifestIds, DEFAULT_BRANCH, null, null, null, true, true);
            }
            else
            {
                //Program.Instance._logger.ErrorFormat("Unable to locate manifest ID for published file {0}", publishedFileId);
            }
        }

        public static async Task DownloadUGCAsync(uint appId, ulong ugcId)
        {
            SteamCloud.UGCDetailsCallback details = null;

            if (Steam3.steamUser.SteamID.AccountType != EAccountType.AnonUser)
            {
                details = await Steam3.GetUGCDetails(ugcId);
            }
            else
            {
                //Program.Instance._logger.Error($"Unable to query UGC details for {ugcId} from an anonymous account");
            }

            if (!string.IsNullOrEmpty(details?.URL))
            {
                await DownloadWebFile(appId, details.FileName, details.URL);
            }
            else
            {
                await DownloadAppAsync(new(), appId, new List<(uint, ulong)> { (appId, ugcId) }, DEFAULT_BRANCH, null, null, null, false, true);
            }
        }

        private static async Task DownloadWebFile(uint appId, string fileName, string url)
        {
            if (!CreateDirectories(appId, 0, out var installDir))
            {
                //Program.Instance._logger.Error("Error: Unable to create install directories!");
                return;
            }

            var stagingDir = Path.Combine(installDir, STAGING_DIR);
            var fileStagingPath = Path.Combine(stagingDir, fileName);
            var fileFinalPath = Path.Combine(installDir, fileName);

            Directory.CreateDirectory(Path.GetDirectoryName(fileFinalPath));
            Directory.CreateDirectory(Path.GetDirectoryName(fileStagingPath));

            using (var file = File.OpenWrite(fileStagingPath))
            using (var client = HttpClientFactory.CreateHttpClient(HttpClientPurpose.WebAPI))
            {
                //Program.Instance._logger.InfoFormat("Downloading {0}", fileName);
                var responseStream = await client.GetStreamAsync(url);
                await responseStream.CopyToAsync(file);
            }

            if (File.Exists(fileFinalPath))
            {
                File.Delete(fileFinalPath);
            }

            File.Move(fileStagingPath, fileFinalPath);
        }

        public static async Task DownloadAppAsync(CancellationTokenSource cts, uint appId, List<(uint depotId, ulong manifestId)> depotManifestIds, string branch, string os, string arch, string language, bool lv, bool isUgc)
        {
            // Load our configuration data containing the depots currently installed
            var configPath = Config.InstallDirectory;
            if (string.IsNullOrWhiteSpace(configPath))
            {
                configPath = DEFAULT_DOWNLOAD_DIR;
            }

            Directory.CreateDirectory(Path.Combine(configPath, CONFIG_DIR));
            DepotConfigStore.Instance = null;
            DepotConfigStore.LoadFromFile(Path.Combine(configPath, CONFIG_DIR, "depot.config"));

            if (Steam3 != null)
                await Steam3.RequestAppInfoAsync(appId);

            if (!await AccountHasAccessAsync(appId, appId))
            {
                if (await Steam3.RequestFreeAppLicense(appId))
                {
                    //Logger.Logger.Log.LogInformation("Obtained FreeOnDemand license for app {0}", appId);

                    // Fetch app info again in case we didn't get it fully without a license.
                    await Steam3.RequestAppInfoAsync(appId, true);
                }
                else
                {
                    var contentName = GetAppName(appId);
                    throw new ContentDownloaderException(string.Format("App {0} ({1}) is not available from this account.", appId, contentName));
                }
            }

            var hasSpecificDepots = depotManifestIds.Count > 0;
            var depotIdsFound = new List<uint>();
            var depotIdsExpected = depotManifestIds.Select(x => x.depotId).ToList();
            var depots = GetSteam3AppSection(appId, EAppInfoSection.Depots);

            if (isUgc)
            {
                var workshopDepot = depots["workshopdepot"].AsUnsignedInteger();
                if (workshopDepot != 0 && !depotIdsExpected.Contains(workshopDepot))
                {
                    depotIdsExpected.Add(workshopDepot);
                    depotManifestIds = depotManifestIds.Select(pair => (workshopDepot, pair.manifestId)).ToList();
                }

                depotIdsFound.AddRange(depotIdsExpected);
            }
            else
            {
                if (depots != null)
                {
                    foreach (var depotSection in depots.Children)
                    {
                        var id = INVALID_DEPOT_ID;
                        if (depotSection.Children.Count == 0)
                            continue;

                        if (!uint.TryParse(depotSection.Name, out id))
                            continue;

                        if (hasSpecificDepots && !depotIdsExpected.Contains(id))
                            continue;

                        if (!hasSpecificDepots)
                        {
                            var depotConfig = depotSection["config"];
                            if (depotConfig != KeyValue.Invalid)
                            {
                                if (!Config.DownloadAllPlatforms &&
                                    depotConfig["oslist"] != KeyValue.Invalid &&
                                    !string.IsNullOrWhiteSpace(depotConfig["oslist"].Value))
                                {
                                    var oslist = depotConfig["oslist"].Value.Split(',');
                                    if (Array.IndexOf(oslist, os ?? Util.GetSteamOS()) == -1)
                                        continue;
                                }

                                if (depotConfig["osarch"] != KeyValue.Invalid &&
                                    !string.IsNullOrWhiteSpace(depotConfig["osarch"].Value))
                                {
                                    var depotArch = depotConfig["osarch"].Value;
                                    if (depotArch != (arch ?? Util.GetSteamArch()))
                                        continue;
                                }

                                if (!Config.DownloadAllLanguages &&
                                    depotConfig["language"] != KeyValue.Invalid &&
                                    !string.IsNullOrWhiteSpace(depotConfig["language"].Value))
                                {
                                    var depotLang = depotConfig["language"].Value;
                                    if (depotLang != (language ?? "english"))
                                        continue;
                                }

                                if (!lv &&
                                    depotConfig["lowviolence"] != KeyValue.Invalid &&
                                    depotConfig["lowviolence"].AsBoolean())
                                    continue;
                            }
                        }

                        depotIdsFound.Add(id);

                        if (!hasSpecificDepots)
                            depotManifestIds.Add((id, INVALID_MANIFEST_ID));
                    }
                }

                if (depotManifestIds.Count == 0 && !hasSpecificDepots)
                {
                    throw new ContentDownloaderException(string.Format("Couldn't find any depots to download for app {0}", appId));
                }

                if (depotIdsFound.Count < depotIdsExpected.Count)
                {
                    var remainingDepotIds = depotIdsExpected.Except(depotIdsFound);
                    throw new ContentDownloaderException(string.Format("Depot {0} not listed for app {1}", string.Join(", ", remainingDepotIds), appId));
                }
            }

            var infos = new List<DepotDownloadInfo>();

            foreach (var (depotId, manifestId) in depotManifestIds)
            {
                var info = await GetDepotInfoAsync(depotId, appId, manifestId, branch);
                if (info != null)
                {
                    infos.Add(info);
                }
            }

            try
            {
                await DownloadSteam3Async(cts, infos.Select(e => new DepotDownloadInfoWithManifest(e.DepotId, e.AppId, e.ManifestId, e.Branch, e.InstallDir, e.DepotKey, null)).ToList(), null).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
        }

        static async Task<DepotDownloadInfo> GetDepotInfoAsync(uint depotId, uint appId, ulong manifestId, string branch)
        {
            if (Steam3 != null && appId != INVALID_APP_ID)
                await Steam3.RequestAppInfoAsync(appId);

            var contentName = GetAppName(appId);

            if (!await AccountHasAccessAsync(appId, depotId))
            {
                return null;
            }

            if (manifestId == INVALID_MANIFEST_ID)
            {
                manifestId = await GetSteam3DepotManifestAsync(depotId, appId, branch);
                if (manifestId == INVALID_MANIFEST_ID && !string.Equals(branch, DEFAULT_BRANCH, StringComparison.OrdinalIgnoreCase))
                {
                    branch = DEFAULT_BRANCH;
                    manifestId = await GetSteam3DepotManifestAsync(depotId, appId, branch);
                }

                if (manifestId == INVALID_MANIFEST_ID)
                {
                    return null;
                }
            }

            await Steam3.RequestDepotKeyAsync(depotId, appId);
            if (!Steam3.DepotKeys.TryGetValue(depotId, out var depotKey))
            {
                return null;
            }

            var uVersion = GetSteam3AppBuildNumber(appId, branch);

            if (!CreateDirectories(depotId, uVersion, out var installDir))
            {
                return null;
            }

            // For depots that are proxied through depotfromapp, we still need to resolve the proxy app id, unless the app is freetodownload
            var containingAppId = appId;
            var proxyAppId = GetSteam3DepotProxyAppId(depotId, appId);
            if (proxyAppId != INVALID_APP_ID)
            {
                var common = GetSteam3AppSection(appId, EAppInfoSection.Common);
                if (common == null || !common["FreeToDownload"].AsBoolean())
                {
                    containingAppId = proxyAppId;
                }
            }

            return new DepotDownloadInfo(depotId, appId, manifestId, branch, installDir, depotKey);
        }

        private class ChunkMatch(DepotManifest.ChunkData oldChunk, DepotManifest.ChunkData newChunk)
        {
            public DepotManifest.ChunkData OldChunk { get; } = oldChunk;
            public DepotManifest.ChunkData NewChunk { get; } = newChunk;
        }

        private class DepotFilesData
        {
            internal DepotDownloadInfo depotDownloadInfo;
            internal DepotDownloadCounter depotCounter;
            internal string stagingDir;
            internal DepotManifest manifest;
            internal DepotManifest previousManifest;
            internal List<DepotManifest.FileData> filteredFiles;
            internal HashSet<string> allFileNames;
        }

        private class FileStreamData
        {
            internal FileStream fileStream;
            internal SemaphoreSlim fileLock;
            internal int chunksToDownload;
        }

        internal class GlobalDownloadCounter
        {
            public ulong TotalDownloadBytesCompressed;
            public ulong TotalDownloadBytesUncompressed;

            public ulong CompleteDownloadSize;
            public ulong TotalBytesCompressedDownloaded;
            public ulong TotalBytesUncompressedDownloaded;
        }

        internal class DepotDownloadCounter
        {
            internal ulong CompleteDownloadSize;
            internal ulong SizeDownloaded;
            internal ulong DepotBytesCompressed;
            internal ulong DepotBytesUncompressed;
        }

        private static async Task DownloadSteam3Async(CancellationTokenSource cts, List<DepotDownloadInfoWithManifest> depots, Action<string, GlobalDownloadCounter> downloadReportCallback)
        {
            var downloadCounter = new GlobalDownloadCounter();
            var depotsToDownload = new List<DepotFilesData>(depots.Count);
            var allFileNamesAllDepots = new HashSet<string>();

            // First, fetch all the manifests for each depot (including previous manifests) and perform the initial setup
            foreach (var depot in depots)
            {
                var depotFileData = await ProcessDepotManifestAndFilesAsync(cts, depot, downloadCounter);

                if (depotFileData != null)
                {
                    depotsToDownload.Add(depotFileData);
                    allFileNamesAllDepots.UnionWith(depotFileData.allFileNames);
                }

                cts.Token.ThrowIfCancellationRequested();
            }


            // If we're about to write all the files to the same directory, we will need to first de-duplicate any files by path
            // This is in last-depot-wins order, from Steam or the list of depots supplied by the user
            if (!string.IsNullOrWhiteSpace(Config.InstallDirectory) && depotsToDownload.Count > 0)
            {
                var claimedFileNames = new HashSet<string>();
                var symlinkNames = new HashSet<string>();
                var regularFileNames = new HashSet<DepotManifest.FileData>();

                foreach (var depotFilesData in depotsToDownload)
                {
                    // For each depot, remove all files from the list that have been claimed by a later depot 
                    depotFilesData.filteredFiles.RemoveAll(file => claimedFileNames.Contains(file.FileName));

                    claimedFileNames.UnionWith(depotFilesData.allFileNames);

                    symlinkNames.UnionWith(depotFilesData.filteredFiles.Where(
                        f => f.Flags.HasFlag(EDepotFileFlag.Symlink))
                        .Select(f => f.FileName));
                    regularFileNames.UnionWith(depotFilesData.filteredFiles.Where(
                        f => !f.Flags.HasFlag(EDepotFileFlag.Symlink)));
                }
            }

            foreach (var depotFilesData in depotsToDownload)
            {
                foreach (var depotFile in depotFilesData.filteredFiles)
                {
                    foreach (var chunk in depotFile.Chunks)
                    {
                        downloadCounter.TotalDownloadBytesUncompressed += chunk.UncompressedLength;
                        downloadCounter.TotalDownloadBytesCompressed += chunk.CompressedLength;
                    }
                }
            }

            foreach (var depotFileData in depotsToDownload)
            {
                await DownloadSteam3AsyncDepotFiles(cts, downloadCounter, depotFileData, allFileNamesAllDepots, downloadReportCallback);
            }
        }

        private static async Task<DepotFilesData> ProcessDepotManifestAndFilesAsync(CancellationTokenSource cts, DepotDownloadInfoWithManifest depotWithManifest, GlobalDownloadCounter downloadCounter)
        {
            var depot = depotWithManifest.depotDownloadInfo;

            var depotCounter = new DepotDownloadCounter();

            DepotManifest oldManifest = null;
            DepotManifest newManifest = null;
            var configDir = Path.Combine(depot.InstallDir, CONFIG_DIR);

            var lastManifestId = INVALID_MANIFEST_ID;
            DepotConfigStore.Instance.InstalledManifestIDs.TryGetValue(depot.DepotId, out lastManifestId);

            // In case we have an early exit, this will force equiv of verifyall next run.
            DepotConfigStore.Instance.InstalledManifestIDs[depot.DepotId] = INVALID_MANIFEST_ID;
            DepotConfigStore.Save();

            if (lastManifestId != INVALID_MANIFEST_ID)
            {
                // We only have to show this warning if the old manifest ID was different
                var badHashWarning = (lastManifestId != depot.ManifestId);
                oldManifest = Util.LoadManifestFromFile(configDir, depot.DepotId, lastManifestId, badHashWarning);
            }

            if (lastManifestId == depot.ManifestId && oldManifest != null)
            {
                newManifest = oldManifest;
            }
            else
            {
                newManifest = depotWithManifest.depotManifest;
                if (newManifest == null)
                {
                    newManifest = Util.LoadManifestFromFile(configDir, depot.DepotId, depot.ManifestId, true);
                }

                if (newManifest != null)
                {
                }
                else
                {
                    DepotManifest depotManifest = null;
                    bool retryManifest;
                    do
                    {
                        cts.Token.ThrowIfCancellationRequested();

                        (depotManifest, retryManifest) = await DownloadManifestAsync(depot.AppId, depot.DepotId, depot.ManifestId, depot.DepotKey, depot.Branch, cts);
                    } while (depotManifest == null && retryManifest);

                    if (depotManifest == null)
                    {
                        cts.Cancel();
                    }

                    // Throw the cancellation exception if requested so that this task is marked failed 
                    cts.Token.ThrowIfCancellationRequested();

                    newManifest = depotManifest;
                }
            }

            if (Config.DownloadManifestOnly)
            {
                DumpManifestToTextFile(depot, newManifest);
                return null;
            }

            var stagingDir = Path.Combine(depot.InstallDir, STAGING_DIR);

            var filesAfterExclusions = newManifest.Files.AsParallel().Where(f => TestIsFileIncluded(f.FileName)).ToList();
            var allFileNames = new HashSet<string>(filesAfterExclusions.Count);

            // Pre-process
            filesAfterExclusions.ForEach(file =>
            {
                allFileNames.Add(file.FileName);

                var fileFinalPath = Path.Combine(depot.InstallDir, file.FileName);
                var fileStagingPath = Path.Combine(stagingDir, file.FileName);

                if (file.Flags.HasFlag(EDepotFileFlag.Directory))
                {
                    Directory.CreateDirectory(fileFinalPath);
                    Directory.CreateDirectory(fileStagingPath);
                }
                else
                {
                    // Some manifests don't explicitly include all necessary directories
                    Directory.CreateDirectory(Path.GetDirectoryName(fileFinalPath));
                    Directory.CreateDirectory(Path.GetDirectoryName(fileStagingPath));

                    //downloadCounter.CompleteDownloadSize += file.TotalSize;
                    depotCounter.CompleteDownloadSize += file.TotalSize;
                }
            });

            return new DepotFilesData
            {
                depotDownloadInfo = depot,
                depotCounter = depotCounter,
                stagingDir = stagingDir,
                manifest = newManifest,
                previousManifest = oldManifest,
                filteredFiles = filesAfterExclusions,
                allFileNames = allFileNames
            };
        }

        private static async Task DownloadSteam3AsyncDepotFiles(CancellationTokenSource cts,
            GlobalDownloadCounter downloadCounter, DepotFilesData depotFilesData, HashSet<string> allFileNamesAllDepots, Action<string, GlobalDownloadCounter> downloadReportCallback)
        {
            var depot = depotFilesData.depotDownloadInfo;
            var depotCounter = depotFilesData.depotCounter;

            var files = depotFilesData.filteredFiles.Where(f => !f.Flags.HasFlag(EDepotFileFlag.Directory)).ToArray();
            var networkChunkQueue = new ConcurrentQueue<(FileStreamData fileStreamData, DepotManifest.FileData fileData, DepotManifest.ChunkData chunk)>();

            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = Config.MaxDownloads,
                CancellationToken = cts.Token
            };

            await Parallel.ForEachAsync(files, parallelOptions, async (file, cancellationToken) =>
            {
                await Task.Yield();
                DownloadSteam3AsyncDepotFile(cts, downloadCounter, depotFilesData, file, networkChunkQueue, downloadReportCallback);
            });

            await Parallel.ForEachAsync(networkChunkQueue, parallelOptions, async (q, cancellationToken) =>
            {
                await DownloadSteam3AsyncDepotFileChunk(
                    cts, downloadCounter, depotFilesData,
                    q.fileData, q.fileStreamData, q.chunk,
                    downloadReportCallback
                );
            });

            // Check for deleted files if updating the depot.
            if (depotFilesData.previousManifest != null)
            {
                var previousFilteredFiles = depotFilesData.previousManifest.Files.AsParallel().Where(f => TestIsFileIncluded(f.FileName)).Select(f => f.FileName).ToHashSet();

                // Check if we are writing to a single output directory. If not, each depot folder is managed independently
                if (string.IsNullOrWhiteSpace(Config.InstallDirectory))
                {
                    // Of the list of files in the previous manifest, remove any file names that exist in the current set of all file names
                    previousFilteredFiles.ExceptWith(depotFilesData.allFileNames);
                }
                else
                {
                    // Of the list of files in the previous manifest, remove any file names that exist in the current set of all file names across all depots being downloaded
                    previousFilteredFiles.ExceptWith(allFileNamesAllDepots);
                }

                foreach (var existingFileName in previousFilteredFiles)
                {
                    var fileFinalPath = Path.Combine(depot.InstallDir, existingFileName);

                    if (!File.Exists(fileFinalPath))
                        continue;

                    File.Delete(fileFinalPath);
                }
            }

            DepotConfigStore.Instance.InstalledManifestIDs[depot.DepotId] = depot.ManifestId;
            DepotConfigStore.Save();

            // Nemirtingas: Fix MacOS/Linux depot download executable rights.
            foreach (var file in depotFilesData.filteredFiles.Where(f => f.Flags == 0 || f.Flags.HasFlag(EDepotFileFlag.Executable)))
            {
                string fileFinalPath = Path.Combine(depotFilesData.depotDownloadInfo.InstallDir, file.FileName);
                var fileIsExecutable = file.Flags.HasFlag(EDepotFileFlag.Executable);

                if (!fileIsExecutable)
                {
                    //HeaderReader.ExecutableHeaderReader reader = new HeaderReader.ElfHeaderReader();
                    //reader.Parse(fileFinalPath);
                    //if (reader.IsValidHeader)
                    //{
                    //    fileIsExecutable = reader.IsExecutable;
                    //}
                    //else
                    //{
                    //    reader = new HeaderReader.MachOHeaderReader();
                    //    reader.Parse(fileFinalPath);
                    //    if (reader.IsValidHeader)
                    //    {
                    //        fileIsExecutable = reader.IsExecutable;
                    //    }
                    //}
                }

                if (fileIsExecutable)
                {
                    PlatformUtilities.SetExecutable(fileFinalPath, true);
                }
            }
        }

        private static void DownloadSteam3AsyncDepotFile(
            CancellationTokenSource cts,
            GlobalDownloadCounter downloadCounter,
            DepotFilesData depotFilesData,
            DepotManifest.FileData file,
            ConcurrentQueue<(FileStreamData, DepotManifest.FileData, DepotManifest.ChunkData)> networkChunkQueue,
            Action<string, GlobalDownloadCounter> downloadReportCallback)
        {
            cts.Token.ThrowIfCancellationRequested();

            var depot = depotFilesData.depotDownloadInfo;
            var stagingDir = depotFilesData.stagingDir;
            var depotDownloadCounter = depotFilesData.depotCounter;
            var oldProtoManifest = depotFilesData.previousManifest;
            DepotManifest.FileData oldManifestFile = null;
            if (oldProtoManifest != null)
            {
                oldManifestFile = oldProtoManifest.Files.SingleOrDefault(f => f.FileName == file.FileName);
            }

            var fileFinalPath = Path.Combine(depot.InstallDir, file.FileName);
            var fileStagingPath = Path.Combine(stagingDir, file.FileName);

            // This may still exist if the previous run exited before cleanup
            if (File.Exists(fileStagingPath))
            {
                File.Delete(fileStagingPath);
            }

            List<DepotManifest.ChunkData> neededChunks;
            var fi = new FileInfo(fileFinalPath);
            var fileDidExist = fi.Exists;
            if (!fileDidExist)
            {
                // create new file. need all chunks
                using var fs = File.Create(fileFinalPath);
                try
                {
                    fs.SetLength((long)file.TotalSize);
                }
                catch (IOException ex)
                {
                    throw new ContentDownloaderException(string.Format("Failed to allocate file {0}: {1}", fileFinalPath, ex.Message));
                }

                neededChunks = new List<DepotManifest.ChunkData>(file.Chunks);
            }
            else
            {
                // open existing
                if (oldManifestFile != null)
                {
                    neededChunks = [];

                    var hashMatches = oldManifestFile.FileHash.SequenceEqual(file.FileHash);
                    if (Config.VerifyAll || !hashMatches)
                    {
                        // we have a version of this file, but it doesn't fully match what we want
                        if (Config.VerifyAll)
                        {
                        }

                        var matchingChunks = new List<ChunkMatch>();

                        foreach (var chunk in file.Chunks)
                        {
                            var oldChunk = oldManifestFile.Chunks.FirstOrDefault(c => c.ChunkID.SequenceEqual(chunk.ChunkID));
                            if (oldChunk != null)
                            {
                                matchingChunks.Add(new ChunkMatch(oldChunk, chunk));
                            }
                            else
                            {
                                neededChunks.Add(chunk);
                            }
                        }

                        var orderedChunks = matchingChunks.OrderBy(x => x.OldChunk.Offset);

                        var copyChunks = new List<ChunkMatch>();

                        using (var fsOld = File.Open(fileFinalPath, FileMode.Open))
                        {
                            foreach (var match in orderedChunks)
                            {
                                fsOld.Seek((long)match.OldChunk.Offset, SeekOrigin.Begin);

                                var adler = Util.AdlerHash(fsOld, (int)match.OldChunk.UncompressedLength);
                                if (!adler.SequenceEqual(BitConverter.GetBytes(match.OldChunk.Checksum)))
                                {
                                    neededChunks.Add(match.NewChunk);
                                }
                                else
                                {
                                    copyChunks.Add(match);
                                }
                            }
                        }

                        if (!hashMatches || neededChunks.Count > 0)
                        {
                            File.Move(fileFinalPath, fileStagingPath);

                            using (var fsOld = File.Open(fileStagingPath, FileMode.Open))
                            {
                                using var fs = File.Open(fileFinalPath, FileMode.Create);
                                try
                                {
                                    fs.SetLength((long)file.TotalSize);
                                }
                                catch (IOException ex)
                                {
                                    throw new ContentDownloaderException(string.Format("Failed to resize file to expected size {0}: {1}", fileFinalPath, ex.Message));
                                }

                                foreach (var match in copyChunks)
                                {
                                    fsOld.Seek((long)match.OldChunk.Offset, SeekOrigin.Begin);

                                    var tmp = new byte[match.OldChunk.UncompressedLength];
                                    fsOld.ReadExactly(tmp);

                                    fs.Seek((long)match.NewChunk.Offset, SeekOrigin.Begin);
                                    fs.Write(tmp, 0, tmp.Length);
                                }
                            }

                            File.Delete(fileStagingPath);
                        }
                    }
                }
                else
                {
                    // No old manifest or file not in old manifest. We must validate.

                    using (var fs = File.Open(fileFinalPath, FileMode.Open, FileAccess.ReadWrite))
                    {
                        if ((ulong)fi.Length != file.TotalSize)
                        {
                            try
                            {
                                fs.SetLength((long)file.TotalSize);
                            }
                            catch (IOException ex)
                            {
                                throw new ContentDownloaderException(string.Format("Failed to allocate file {0}: {1}", fileFinalPath, ex.Message));
                            }
                        }
                    }

                    neededChunks = Util.ValidateSteam3FileChecksums(fileFinalPath, [.. file.Chunks.OrderBy(x => x.Offset)]);
                }

                if (neededChunks.Count == 0)
                {
                    lock (depotDownloadCounter)
                    {
                        depotDownloadCounter.SizeDownloaded += file.TotalSize;
                    }

                    lock (downloadCounter)
                    {
                        downloadCounter.CompleteDownloadSize += file.TotalSize;
                        if (downloadReportCallback != null)
                            downloadReportCallback(fileFinalPath, downloadCounter);
                    }

                    return;
                }

                var sizeOnDisk = (file.TotalSize - (ulong)neededChunks.Select(x => (long)x.UncompressedLength).Sum());
                lock (depotDownloadCounter)
                {
                    depotDownloadCounter.SizeDownloaded += sizeOnDisk;
                }

                lock (downloadCounter)
                {
                    downloadCounter.CompleteDownloadSize += sizeOnDisk;
                    if (downloadReportCallback != null)
                        downloadReportCallback(fileFinalPath, downloadCounter);
                }
            }

            var fileIsExecutable = file.Flags.HasFlag(EDepotFileFlag.Executable);
            if (fileIsExecutable && (!fileDidExist || oldManifestFile == null || !oldManifestFile.Flags.HasFlag(EDepotFileFlag.Executable)))
            {
                PlatformUtilities.SetExecutable(fileFinalPath, true);
            }
            else if (!fileIsExecutable && oldManifestFile != null && oldManifestFile.Flags.HasFlag(EDepotFileFlag.Executable))
            {
                PlatformUtilities.SetExecutable(fileFinalPath, false);
            }

            var fileStreamData = new FileStreamData
            {
                fileStream = null,
                fileLock = new SemaphoreSlim(1),
                chunksToDownload = neededChunks.Count
            };

            foreach (var chunk in neededChunks)
            {
                networkChunkQueue.Enqueue((fileStreamData, file, chunk));
            }
        }

        private static async Task DownloadSteam3AsyncDepotFileChunk(
            CancellationTokenSource cts,
            GlobalDownloadCounter downloadCounter,
            DepotFilesData depotFilesData,
            DepotManifest.FileData file,
            FileStreamData fileStreamData,
            DepotManifest.ChunkData chunk,
            Action<string, GlobalDownloadCounter> downloadReportCallback)
        {
            cts.Token.ThrowIfCancellationRequested();

            var depot = depotFilesData.depotDownloadInfo;
            var depotDownloadCounter = depotFilesData.depotCounter;

            var chunkID = Convert.ToHexString(chunk.ChunkID).ToLowerInvariant();

            var written = 0;
            var chunkBuffer = ArrayPool<byte>.Shared.Rent((int)chunk.UncompressedLength);

            try
            {
                do
                {
                    cts.Token.ThrowIfCancellationRequested();

                    Server connection = null;

                    try
                    {
                        connection = CDNPool.GetConnection(depot.AppId);

                        string cdnToken = null;
                        if (Steam3.CDNAuthTokens.TryGetValue((depot.DepotId, connection.Host), out var authTokenCallbackPromise))
                        {
                            var result = await authTokenCallbackPromise.Task;
                            cdnToken = result.Token;
                        }

                        written = await CDNPool.CDNClient.DownloadDepotChunkAsync(
                            depot.DepotId,
                            chunk,
                            connection,
                            chunkBuffer,
                            depot.DepotKey,
                            CDNPool.ProxyServer,
                            cdnToken).ConfigureAwait(false);

                        CDNPool.ReturnConnection(connection);

                        break;
                    }
                    catch (TaskCanceledException)
                    {
                        CDNPool.ReturnBrokenConnection(connection);
                    }
                    catch (SteamKitWebRequestException e)
                    {
                        // If the CDN returned 403, attempt to get a cdn auth if we didn't yet,
                        // if auth task already exists, make sure it didn't complete yet, so that it gets awaited above
                        if (e.StatusCode == HttpStatusCode.Forbidden &&
                            (!Steam3.CDNAuthTokens.TryGetValue((depot.DepotId, connection.Host), out var authTokenCallbackPromise) || !authTokenCallbackPromise.Task.IsCompleted))
                        {
                            await Steam3.RequestCDNAuthToken(depot.AppId, depot.DepotId, connection);

                            CDNPool.ReturnConnection(connection);

                            continue;
                        }

                        CDNPool.ReturnBrokenConnection(connection);

                        if (e.StatusCode == HttpStatusCode.Unauthorized || e.StatusCode == HttpStatusCode.Forbidden)
                        {
                            break;
                        }

                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception e)
                    {
                        CDNPool.ReturnBrokenConnection(connection);
                    }
                } while (written == 0);

                if (written == 0)
                {
                    cts.Cancel();
                }

                // Throw the cancellation exception if requested so that this task is marked failed
                cts.Token.ThrowIfCancellationRequested();

                try
                {
                    await fileStreamData.fileLock.WaitAsync().ConfigureAwait(false);

                    if (fileStreamData.fileStream == null)
                    {
                        var fileFinalPath = Path.Combine(depot.InstallDir, file.FileName);
                        fileStreamData.fileStream = File.Open(fileFinalPath, FileMode.Open);
                    }

                    fileStreamData.fileStream.Seek((long)chunk.Offset, SeekOrigin.Begin);
                    await fileStreamData.fileStream.WriteAsync(chunkBuffer.AsMemory(0, written), cts.Token);
                }
                finally
                {
                    fileStreamData.fileLock.Release();
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(chunkBuffer);
            }

            var remainingChunks = Interlocked.Decrement(ref fileStreamData.chunksToDownload);
            if (remainingChunks == 0)
            {
                fileStreamData.fileStream?.Dispose();
                fileStreamData.fileLock.Dispose();
            }

            ulong sizeDownloaded = 0;
            lock (depotDownloadCounter)
            {
                sizeDownloaded = depotDownloadCounter.SizeDownloaded + (ulong)written;
                depotDownloadCounter.SizeDownloaded = sizeDownloaded;
                depotDownloadCounter.DepotBytesCompressed += chunk.CompressedLength;
                depotDownloadCounter.DepotBytesUncompressed += chunk.UncompressedLength;
            }

            lock (downloadCounter)
            {
                var fileFinalPath = Path.Combine(depot.InstallDir, file.FileName);
                downloadCounter.TotalBytesCompressedDownloaded += chunk.CompressedLength;
                downloadCounter.TotalBytesUncompressedDownloaded += chunk.UncompressedLength;
                if (downloadReportCallback != null)
                    downloadReportCallback(fileFinalPath, downloadCounter);
            }

            if (remainingChunks == 0)
            {
                var fileFinalPath = Path.Combine(depot.InstallDir, file.FileName);
            }
        }

        class ChunkIdComparer : IEqualityComparer<byte[]>
        {
            public bool Equals(byte[] x, byte[] y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (x == null || y == null) return false;
                return x.SequenceEqual(y);
            }

            public int GetHashCode(byte[] obj)
            {
                ArgumentNullException.ThrowIfNull(obj);

                // ChunkID is SHA-1, so we can just use the first 4 bytes
                return BitConverter.ToInt32(obj, 0);
            }
        }

        static void DumpManifestToTextFile(DepotDownloadInfo depot, DepotManifest manifest)
        {
            var txtManifest = Path.Combine(depot.InstallDir, $"manifest_{depot.DepotId}_{depot.ManifestId}.txt");
            using var sw = new StreamWriter(txtManifest);

            sw.WriteLine($"Content Manifest for Depot {depot.DepotId} ");
            sw.WriteLine();
            sw.WriteLine($"Manifest ID / date     : {depot.ManifestId} / {manifest.CreationTime} ");

            var uniqueChunks = new HashSet<byte[]>(new ChunkIdComparer());

            foreach (var file in manifest.Files)
            {
                foreach (var chunk in file.Chunks)
                {
                    uniqueChunks.Add(chunk.ChunkID);
                }
            }

            sw.WriteLine($"Total number of files  : {manifest.Files.Count} ");
            sw.WriteLine($"Total number of chunks : {uniqueChunks.Count} ");
            sw.WriteLine($"Total bytes on disk    : {manifest.TotalUncompressedSize} ");
            sw.WriteLine($"Total bytes compressed : {manifest.TotalCompressedSize} ");
            sw.WriteLine();
            sw.WriteLine();
            sw.WriteLine("          Size Chunks File SHA                                 Flags Name");

            foreach (var file in manifest.Files)
            {
                var sha1Hash = Convert.ToHexString(file.FileHash).ToLower();
                sw.WriteLine($"{file.TotalSize,14:d} {file.Chunks.Count,6:d} {sha1Hash} {(int)file.Flags,5:x} {file.FileName}");
            }
        }

        public static async Task<(DepotManifest, bool)> DownloadManifestAsync(uint appId, uint depotId, ulong manifestId, byte[] depotKey, string branch, CancellationTokenSource cts)
        {
            var manifestRequestCode = await _GenerateManifestCodeAsync(appId, depotId, manifestId, branch, cts).ConfigureAwait(false);
            var (rawManifest, retry) = await _DownloadRawManifestAsync(appId, depotId, manifestId, branch, manifestRequestCode, cts).ConfigureAwait(false);
            var depotManifest = default(DepotManifest);
            if (rawManifest != null)
            {
                depotManifest = DepotManifest.Deserialize(rawManifest);
                if (depotKey != null)
                    depotManifest.DecryptFilenames(depotKey);
            }

            return (depotManifest, retry);
        }

        public static async Task<Stream> DownloadRawManifestAsync(uint appId, uint depotId, ulong manifestId, string branch, CancellationTokenSource cts)
        {
            var manifestRequestCode = await _GenerateManifestCodeAsync(appId, depotId, manifestId, branch, cts).ConfigureAwait(false);
            return (await _DownloadRawManifestAsync(appId, depotId, manifestId, branch, manifestRequestCode, cts).ConfigureAwait(false)).manifest;
        }

        internal static async Task<ulong> _GenerateManifestCodeAsync(uint appId, uint depotId, ulong manifestId, string branch, CancellationTokenSource cts)
        {
            var now = DateTime.Now;
            var manifestRequestCodeExpiration = DateTime.MinValue;
            ulong manifestRequestCode = 0;

            // In order to download this manifest, we need the current manifest request code
            // The manifest request code is only valid for a specific period in time
            if (manifestRequestCode == 0 || now >= manifestRequestCodeExpiration)
            {
                manifestRequestCode = await Steam3.GetDepotManifestRequestCodeAsync(
                    depotId,
                    appId,
                    manifestId,
                    branch).ConfigureAwait(false);
                // This code will hopefully be valid for one period following the issuing period
                manifestRequestCodeExpiration = now.Add(TimeSpan.FromMinutes(5));

                // If we could not get the manifest code, this is a fatal error
                if (manifestRequestCode == 0)
                {
                    cts.Cancel();
                }
            }

            return manifestRequestCode;
        }

        internal static async Task<(Stream manifest, bool canRetry)> _DownloadRawManifestAsync(uint appId, uint depotId, ulong manifestId, string branch, ulong manifestRequestCode, CancellationTokenSource cts)
        {
            Server connection = null;
            Stream depotManifest = null;

            try
            {
                connection = CDNPool.GetConnection(appId);

                string cdnToken = null;
                if (Steam3.CDNAuthTokens.TryGetValue((depotId, connection.Host), out var authTokenCallbackPromise))
                {
                    var result = await authTokenCallbackPromise.Task;
                    cdnToken = result.Token;
                }

                depotManifest = await CDNPool.CDNClient.DownloadRawManifestAsync(
                    depotId,
                    manifestId,
                    manifestRequestCode,
                    connection,
                    CDNPool.ProxyServer,
                    cdnToken).ConfigureAwait(false);

                CDNPool.ReturnConnection(connection);
            }
            catch (TaskCanceledException)
            {
                //Logger.Logger.Log.LogError("Connection timeout downloading depot manifest {0} {1}. Retrying.", depotId, manifestId);
            }
            catch (SteamKitWebRequestException e)
            {
                // If the CDN returned 403, attempt to get a cdn auth if we didn't yet 
                if (e.StatusCode == HttpStatusCode.Forbidden && !Steam3.CDNAuthTokens.ContainsKey((depotId, connection.Host)))
                {
                    await Steam3.RequestCDNAuthToken(appId, depotId, connection);

                    CDNPool.ReturnConnection(connection);

                    return (null, true);
                }

                CDNPool.ReturnBrokenConnection(connection);

                if (e.StatusCode == HttpStatusCode.Unauthorized || e.StatusCode == HttpStatusCode.Forbidden)
                {
                    //Logger.Logger.Log.LogError("Encountered 401 for depot manifest {0} {1}. Aborting.", depotId, manifestId);
                    return (null, false);
                }

                if (e.StatusCode == HttpStatusCode.NotFound)
                {
                    //Logger.Logger.Log.LogError("Encountered 404 for depot manifest {0} {1}. Aborting.", depotId, manifestId);
                    return (null, false);
                }

                //Logger.Logger.Log.LogError("Encountered error downloading depot manifest {0} {1}: {2}", depotId, manifestId, e.StatusCode);
            }
            catch (OperationCanceledException)
            {
                return (null, false);
            }
            catch (Exception e)
            {
                CDNPool.ReturnBrokenConnection(connection);
                //Logger.Logger.Log.LogError("Encountered error downloading manifest for depot {0} {1}: {2}", depotId, manifestId, e.Message);
            }
            return (depotManifest, true);
        }
    }
}