using CommandLine;
using EpicKit.WebAPI.Store.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace epic_retriever
{
    public class Options
    {
        [Option('i', "download-images", Required = false, HelpText = "Sets the flag to download games image. Images will not be downloaded if this flag is not set.")]
        public bool DownloadImages { get; set; } = false;

        [Option('f', "force", Required = false, HelpText = "Force to download game's infos (usefull if you want to refresh a game).")]
        public bool Force { get; set; } = false;

        [Option('o', "out", Required = false, HelpText = "Where to output your game definitions. By default it will output to 'epic' directory alongside the executable.")]
        public string OutDirectory { get; set; } = "epic";

        [Option('c', "cache-out", Required = false, HelpText = "Where to output the cache data. By default it will output to 'epic_cache' directory alongside the executable.")]
        public string OutCacheDirectory { get; set; } = "epic_cache";

        [Option('w', "from-web", Required = false, HelpText = "Try to deduce the catalog id from the app web page.")]
        public bool FromWeb { get; set; } = false;

        [Option("no-infos", Required = false, HelpText = "Don't retrieve games infos. (To retrieve only the achievements)")]
        public bool NoInfos { get; set; } = false;

        [Option('N', "namespace", Required = false, HelpText = "App namespace. (need -N AND -C)")]
        public string AppNamespace { get; set; } = string.Empty;
        
        [Option('C', "catalog-item", Required = false, HelpText = "App catalog item id. (need -N AND -C)")]
        public string AppCatalogItemId { get; set; } = string.Empty;

        [Option('U', "game-user", Required = false, HelpText = "Game user id. (need -U, -P, -D AND -A)")]
        public string GameUser { get; set; } = string.Empty;

        [Option('P', "game-password", Required = false, HelpText = "Game password. (need -U, -P, -D AND -A)")]
        public string GamePassword { get; set; } = string.Empty;

        [Option('D', "game-deployement-id", Required = false, HelpText = "Game deployement id. (need -U, -P, -D AND -A)")]
        public string GameDeployementId { get; set; } = string.Empty;

        [Option('A', "game-app-id", Required = false, HelpText = "Game app id. (need -U, -P, -D AND -A)")]
        public string GameAppId { get; set; } = string.Empty;

        [Option("games-credentials-directory", Required = false, HelpText = "")]
        public string GamesCredentialsDirectory { get; set; } = string.Empty;
    }

    public static class DictionaryExtensions
    {
        public static TValue GetOrCreate<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key)
        where TValue : new()
        {
            if (!dict.TryGetValue(key, out TValue val))
            {
                val = new TValue();
                dict.Add(key, val);
            }

            return val;
        }
    }

    class GameInfos
    {
        [JsonProperty("EOS_AUDIENCE")]
        public string GameUser { get; set; }
        [JsonProperty("EOS_SECRET_KEY")]
        public string GamePassword { get; set; }
        [JsonProperty("EOS_DEPLOYEMENT_ID")]
        public string GameDeployementId { get; set; }
        [JsonProperty("EOS_SANDBOX_ID")]
        public string GameNamespace { get; set; }
        [JsonProperty("AppId")]
        public string GameAppId { get; set; }
        public EpicKit.WebAPI.AuthorizationScopes[] GameScopes { get; set; }
    }

    class AppListEntry
    {
        [JsonIgnore]
        public ApplicationAsset Asset { get; set; }
    }

    class AppList
    {
        [JsonProperty(PropertyName = "Version")]
        public int Version { get; set; }

        [JsonProperty(PropertyName = "Namespaces")]
        public Dictionary<string, Dictionary<string, AppListEntry>> Namespaces { get; set; } = new Dictionary<string, Dictionary<string, AppListEntry>>();
    }

    class Program
    {
        static Options ProgramOptions { get; set; }

        static bool HasTargetApp => !string.IsNullOrWhiteSpace(ProgramOptions.AppNamespace) && !string.IsNullOrWhiteSpace(ProgramOptions.AppCatalogItemId);

        static EpicKit.WebApi EGSApi;

        static void SaveAppAsset(ApplicationAsset asset)
        {
            if (asset == null)
                return;

            try
            {
                string asset_path = Path.Combine(ProgramOptions.OutCacheDirectory, "assets", asset.Namespace);
                if (!Directory.Exists(asset_path))
                {
                    Directory.CreateDirectory(asset_path);
                }

                using (StreamWriter writer = new StreamWriter(new FileStream(Path.Combine(asset_path, asset.CatalogItemId + ".json"), FileMode.Create), new UTF8Encoding(false)))
                {
                    writer.Write(JsonConvert.SerializeObject(asset, Formatting.Indented));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to save app assets cache: ${e.Message}");
            }
        }

        static void SaveAppInfos(StoreApplicationInfos app)
        {
            try
            {
                string asset_path = Path.Combine(ProgramOptions.OutCacheDirectory, "app_infos", app.Namespace);
                if (!Directory.Exists(asset_path))
                {
                    Directory.CreateDirectory(asset_path);
                }

                using (StreamWriter writer = new StreamWriter(new FileStream(Path.Combine(asset_path, app.Id + ".json"), FileMode.Create), new UTF8Encoding(false)))
                {
                    writer.Write(JsonConvert.SerializeObject(app, Formatting.Indented));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to save app infos cache: ${e.Message}");
            }
        }

        static bool CachedDataChanged(ApplicationAsset asset)
        {
            if (ProgramOptions.Force || asset == null)
                return true;

            try
            {
                JObject cached_asset;
                string asset_path = Path.Combine(ProgramOptions.OutCacheDirectory, "assets", asset.Namespace, asset.CatalogItemId + ".json");

                using (StreamReader reader = new StreamReader(new FileStream(asset_path, FileMode.Open), Encoding.UTF8))
                {
                    cached_asset = JObject.Parse(reader.ReadToEnd());
                }

                return (string)cached_asset["buildVersion"] != asset.BuildVersion;
            }
            catch (Exception)
            {
                return true;
            }
        }

        static StoreApplicationInfos GetCachedAppInfos(string namespace_, string catalog_item_id)
        {
            StoreApplicationInfos app = new StoreApplicationInfos();
            string app_infos_path = Path.Combine(ProgramOptions.OutCacheDirectory, "app_infos", namespace_, catalog_item_id + ".json");
            if (!File.Exists(app_infos_path))
                return app;

            using (StreamReader reader = new StreamReader(new FileStream(app_infos_path, FileMode.Open), Encoding.UTF8))
            {
                app = JObject.Parse(reader.ReadToEnd()).ToObject<StoreApplicationInfos>();
            }

            return app;
        }

        static async Task<StoreApplicationInfos> GetApp(string namespace_, string catalog_item_id)
        {
            StoreApplicationInfos app = null;

            //if (CachedDataChanged())
            {
                //SaveAppAsset();

                app = await EGSApi.GetGameInfos(namespace_, catalog_item_id);
                if (app != null)
                {
                    SaveAppInfos(app);
                }
            }
            //else
            //{
            //    app = GetCachedAppInfos(namespace_, catalog_item_id);
            //}

            return app;
        }

        static string FindBestImage(StoreApplicationInfos app)
        {
            string result = string.Empty;
            int best_match = 0;
            foreach (StoreApplicationKeyImage img in app.KeyImages)
            {
                if (img.Type == "DieselGameBox")
                {
                    return img.Url;
                }
                else
                {
                    switch (img.Type)
                    {
                        case "DieselGameBoxTall":
                            if (best_match < 3)
                            {
                                result = img.Url;
                                best_match = 3;
                            }
                            break;

                        case "DieselGameBoxLogo":
                            if (best_match < 2)
                            {
                                result = img.Url;
                                best_match = 2;
                            }
                            break;

                        case "Thumbnail":
                            if (best_match < 1)
                            {
                                result = img.Url;
                                best_match = 2;
                            }
                            break;
                    }
                }
            }
            return result;
        }

        static string BuildSaveGameInfosDirectoryPath(string namespace_, string appId) =>
            Path.Combine(ProgramOptions.OutDirectory, namespace_, appId);

        static string BuildSaveGameInfosDirectoryPath(AppInfoModel game_infos) =>
            BuildSaveGameInfosDirectoryPath(game_infos.Namespace, game_infos.AppId);

        static string BuildSaveGameInfosPath(AppInfoModel game_infos) =>
            Path.Combine(BuildSaveGameInfosDirectoryPath(game_infos), game_infos.AppId + ".json");

        static async Task SaveGameInfos(AppInfoModel game_infos, bool merge_existing_infos)
        {
            try
            {
                string infos_path = BuildSaveGameInfosDirectoryPath(game_infos);
                if (!Directory.Exists(infos_path))
                    Directory.CreateDirectory(infos_path);

                string image_path = Path.Combine(infos_path, game_infos.AppId + "_background.jpg");
                if (ProgramOptions.DownloadImages && !File.Exists(image_path))
                {
                    try
                    {
                        await (await DownloadAchievementIconAsync(new Uri(game_infos.ImageUrl))).CopyToAsync(new FileStream(image_path, FileMode.Create, FileAccess.Write));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Failed to download app image: ${e.Message}.");
                    }
                }

                infos_path = BuildSaveGameInfosPath(game_infos);

                using (StreamWriter writer = new StreamWriter(new FileStream(infos_path, FileMode.Create), new UTF8Encoding(false)))
                {
                    writer.Write(JsonConvert.SerializeObject(game_infos, Formatting.Indented));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to save game infos: ${e.Message}.");
            }
        }

        static async Task<MemoryStream> DownloadAchievementIconAsync(Uri uri)
        {
            using var webClient = new HttpClient();

            webClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:88.0) Gecko/20100101 Firefox/88.0");
            webClient.DefaultRequestHeaders.TryAddWithoutValidation("Referer", "https://www.epicgames.com/store/en/");
            webClient.DefaultRequestHeaders.TryAddWithoutValidation("Origin", "https://www.epicgames.com");

            var ms = new MemoryStream();
            await (await webClient.GetStreamAsync(uri)).CopyToAsync(ms);
            ms.Position = 0;
            return ms;
        }

        static async Task<AppList> DownloadAppList()
        {
            var app_list = new AppList();

            var wcli = new HttpClient(new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.All,
            });
            wcli.DefaultRequestHeaders.TryAddWithoutValidation("UserAgent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:98.0) Gecko/20100101 Firefox/98.0");
            string applist_path = Path.Combine(ProgramOptions.OutCacheDirectory, "applist.json");

            
            try
            {
                using (StreamReader reader = new StreamReader(new FileStream(applist_path, FileMode.Open), Encoding.UTF8))
                {
                    JObject json = JObject.Parse(reader.ReadToEnd());
                    switch((int)json["Version"])
                    {
                        case 1: app_list = json.ToObject<AppList>(); break;
                    }
                }

                if (HasTargetApp)
                {
                    app_list.Namespaces.GetOrCreate(ProgramOptions.AppNamespace)[ProgramOptions.AppCatalogItemId] = new AppListEntry { };
                }
            }
            catch (Exception)
            { }

            app_list.Version = 1;

            Console.WriteLine("Downloading assets...");
            var assets = await EGSApi.GetApplicationsAssets();
            foreach (var asset in assets)
            {
                AppListEntry entry;
                if(!app_list.Namespaces.GetOrCreate(asset.Namespace).TryGetValue(asset.CatalogItemId, out entry))
                {
                    app_list.Namespaces.GetOrCreate(asset.Namespace)[asset.CatalogItemId] = new AppListEntry { Asset = asset };
                }
                else
                {
                    entry.Asset = asset;
                }
            }

            if (!HasTargetApp && ProgramOptions.FromWeb)
            {
                try
                {
                    var all_namespaces = JObject.Parse(await new StreamReader((await wcli.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://store-content.ak.epicgames.com/api/content/productmapping"))).Content.ReadAsStream()).ReadToEndAsync());

                    int count = 0;
                    int total = all_namespaces.Count;
                    foreach (var item in all_namespaces)
                    {
                        Console.WriteLine($"Getting web namespace {++count}/{total}: {item.Key}");
                        try
                        {
                            var productResult = await new StreamReader((await wcli.SendAsync(new HttpRequestMessage(HttpMethod.Get, $"https://store-content-ipv4.ak.epicgames.com/api/en-US/content/products/{item.Value}"))).Content.ReadAsStream()).ReadToEndAsync();
                            foreach (var page in JObject.Parse(productResult)["pages"])
                            {
                                try
                                {
                                    JObject json = (JObject)page["item"];
                                    if (json != null)
                                    {
                                        string catalog_item_id = (string)json["catalogId"];
                                        if (!string.IsNullOrWhiteSpace(catalog_item_id))
                                        {
                                            app_list.Namespaces.GetOrCreate(item.Key)[catalog_item_id.Trim()] = new AppListEntry { };
                                        }
                                    }
                                }
                                catch(Exception)
                                { }
                            }
                        }
                        catch (Exception)
                        { }
                    }
                }
                catch (Exception)
                { }
            }

            try
            {
                using (StreamWriter writer = new StreamWriter(new FileStream(applist_path, FileMode.Create), new UTF8Encoding(false)))
                {
                    writer.Write(JObject.FromObject(app_list).ToString());
                }
            }
            catch (Exception)
            { }

            return app_list;
        }

        static async Task<CatalogModel> GetAppCatalogInfos(string namespace_)
        {
            // Disabled
            return null;
            try
            {
                var url = $"https://www.epicgames.com/graphql?query={{Catalog{{catalogOffers(namespace:\"{namespace_}\" params:{{count:500}}){{elements{{id title offerType items{{id title}}}}}}}}}}";
                var webClient = new HttpClient(new HttpClientHandler
                {
                    AutomaticDecompression = DecompressionMethods.All,
                });

                using (var reader = new StreamReader((await webClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, url))).Content.ReadAsStream()))
                {
                    var x = await reader.ReadToEndAsync();
                    return JObject.Parse(x).ToObject<CatalogModel>();
                }
            }
            catch(Exception e)
            {
                Console.WriteLine($"Error while getting catalog infos: {e.Message}");
                return null;
            }
        }

        static async Task GetAppInfos(string namespace_, AppList app_list, string catalog_id)
        {
            var app = await GetApp(namespace_, catalog_id);

            AppListEntry catalog_entry = null;
            try
            {
                catalog_entry = app_list.Namespaces[namespace_][catalog_id];
            }
            catch(Exception)
            {}

            if (catalog_entry == null)
                return;

            if (app != null && !app.IsDlc && (app.ReleaseInfo.Count > 0 || catalog_entry.Asset != null))
            {
                var game_infos = new AppInfoModel();
                string app_id = catalog_entry.Asset != null ? catalog_entry.Asset.AppName : app.ReleaseInfo[0].AppId;
                Console.WriteLine("App {0}, AppId {1}, Namespace {2}, ItemId {3}", app.Title, app_id, app.Namespace, app.Id);
                int title_length = 0;
                int id_length = 0;
                int namespace_length = 0;
                int catalog_id_length = 0;

                // Loop for pretty print
                foreach (var dlc in app.DlcItemList)
                {
                    string dlc_id;
                    Dictionary<string, AppListEntry> namespace_apps;
                    AppListEntry dlc_meta;
                    
                    if (app_list.Namespaces.TryGetValue(dlc.Namespace, out namespace_apps) && namespace_apps.TryGetValue(dlc.Id, out dlc_meta) && dlc_meta != null && dlc_meta.Asset != null)
                    {
                        dlc_id = dlc_meta.Asset.AppName;
                    }
                    else
                    {
                        if (dlc.ReleaseInfo.Count > 0)
                        {
                            dlc_id = dlc.ReleaseInfo[0].AppId;
                        }
                        else
                        {
                            dlc_id = dlc.Title;
                        }
                    }

                    if (title_length < dlc.Title.Length)
                        title_length = dlc.Title.Length;
                    if (id_length < dlc_id.Length)
                        id_length = dlc_id.Length;
                    if (namespace_length < dlc.Namespace.Length)
                        namespace_length = dlc.Namespace.Length;
                    if (catalog_id_length < dlc.Id.Length)
                        catalog_id_length = dlc.Id.Length;
                }

                game_infos.Dlcs = new List<DlcInfoModel>();
                game_infos.Name = app.Title;
                game_infos.AppId = app.ReleaseInfo[0].AppId;
                game_infos.Namespace = app.Namespace;
                game_infos.ItemId = app.Id;
                game_infos.ImageUrl = FindBestImage(app);
                game_infos.Releases = new List<string>();

                if (app.DlcItemList.Count > 0)
                {
                    var catalogModel = await GetAppCatalogInfos(namespace_);

                    foreach (var dlc in app.DlcItemList)
                    {
                        Console.WriteLine($"   Dlc {{0, -{title_length}}}, Namespace {{1, -{namespace_length}}}, ItemId {{2, -{catalog_id_length}}}", dlc.Title, dlc.Namespace, dlc.Id);

                        var catalogElement = catalogModel?.Data.Catalog.CatalogOffers.Elements.Find(e => e.Title == dlc.Title || e.Items.Find(i => i.Id == dlc.Id) != null);

                        game_infos.Dlcs.Add(new DlcInfoModel
                        {
                            Name = dlc.Title,
                            EntitlementId = dlc.Id,
                            ItemId = catalogElement?.Id,
                            Type = catalogElement?.OfferType
                        });
                    }

                    var existing_infos = default(AppInfoModel);
                    try
                    {
                        var infos_path = BuildSaveGameInfosPath(game_infos);

                        using (var sr = new StreamReader(new FileStream(infos_path, FileMode.Open), new UTF8Encoding(false)))
                        {
                            existing_infos = JObject.Parse(await sr.ReadToEndAsync()).ToObject<AppInfoModel>();
                        }
                        foreach (var dlc in game_infos.Dlcs)
                        {
                            if (dlc.Type == null || dlc.ItemId == null)
                            {
                                var existing_dlc = existing_infos.Dlcs.Find(e => e.EntitlementId == dlc.EntitlementId);
                                if (existing_dlc != null)
                                {
                                    dlc.Type = existing_dlc.Type;
                                    dlc.ItemId = existing_dlc.ItemId;
                                }
                            }
                        }
                    }
                    catch
                    { }
                }

                Console.WriteLine();

                foreach (var releaseInfo in app.ReleaseInfo)
                {
                    foreach(var pf in releaseInfo.Platform)
                    {
                        if (!game_infos.Releases.Contains(pf))
                        {
                            game_infos.Releases.Add(pf);
                        }
                    }
                }

                await SaveGameInfos(game_infos, true);
            }
        }

        static async Task PrepareManifestDownload(string namespace_, string catalogId, string appId)
        {
            var result = await EGSApi.GetManifestDownloadInfos(namespace_, catalogId, appId);

            var manifestCacheDir = Path.Combine(ProgramOptions.OutCacheDirectory, "depots", appId);
            if (!Directory.Exists(manifestCacheDir))
                Directory.CreateDirectory(manifestCacheDir);

            using (var file = new FileStream(Path.Combine(manifestCacheDir, $"{appId}.manifest"), FileMode.Create))
            {
                await file.WriteAsync(result.ManifestData);
            }

            using (var writer = new StreamWriter(new FileStream(Path.Combine(manifestCacheDir, "download_infos.json"), FileMode.Create), new UTF8Encoding(false)))
            {
                var json = new JObject {
                    { "BaseUrls", new JArray(result.BaseUrls) }
                };

                await writer.WriteAsync(json.ToString());
            }
        }
        static Task<JObject> LoginAnonymous()
        {
            Console.WriteLine("Will now try to login anonymously...");
            return EGSApi.LoginAnonymous();
        }

        static Task<JObject> LoginWithAuthcode()
        {
            Console.WriteLine("Will now try to login with authorization code...");
            Console.WriteLine("EGL authcode (get it at: https://www.epicgames.com/id/api/redirect?clientId=34a02cf8f4414e29b15921876da36f9a&responseType=code): ");
            return EGSApi.LoginAuthCode(Console.ReadLine().Trim());
        }

        static Task<JObject> LoginWithSID()
        {
            Console.WriteLine("Will now try to login with SID...");
            Console.WriteLine("EGL sid (get it at: https://www.epicgames.com/id/login?redirectUrl=https://www.epicgames.com/id/api/redirect): ");
            return EGSApi.LoginSID(Console.ReadLine().Trim());
        }

        static async Task<bool> InteractiveContinuationAsync(string deployement_id, string user_id, string password, string continuationToken)
        {
            var endpoints = await EGSApi.GetDefaultApiEndpointsAsync();
            var url = (string)endpoints["client"]["AuthClient"]["AuthorizeContinuationEndpoint"];
            url = url.Replace("`continuation`", continuationToken);
            url = url.Replace("`continuation", continuationToken);

            if (url.Contains("`"))
                throw new NotImplementedException(url);

            Console.WriteLine($"Consent is required, please head to '{url}'.");

            var retries = 0;
            while (retries < 30)
            {
                try
                {
                    await EGSApi.RunContinuationToken(continuationToken, deployement_id, user_id, password);
                    return true;
                }
                catch (EpicKit.WebApiOAuthScopeConsentRequiredException)
                {
                    await Task.Delay(TimeSpan.FromSeconds(2));
                }
                ++retries;
            }

            return false;
        }

        static async Task<string> GetGameConnectionTokenAsync(string deployement_id, string user_id, string password, EpicKit.WebAPI.AuthorizationScopes[] scopes, bool autoAcceptScopes = true)
        {
            while (true)
            {
                try
                {
                    return await EGSApi.GetAppRefreshTokenFromExchangeCode(await EGSApi.GetAppExchangeCodeAsync(), deployement_id, user_id, password, scopes);
                }
                catch (EpicKit.WebApiOAuthScopeConsentRequiredException ex)
                {
                    try
                    {
                        await EGSApi.RunContinuationToken(ex.ContinuationToken, deployement_id, user_id, password);
                    }
                    catch(EpicKit.WebApiOAuthScopeConsentRequiredException)
                    {
                        if (autoAcceptScopes)
                        {
                            await EGSApi.AutoAcceptContinuationAsync(deployement_id, user_id, password, ex.ContinuationToken, scopes);
                        }
                        else
                        {
                            if (!await InteractiveContinuationAsync(deployement_id, user_id, password, ex.ContinuationToken))
                                return null;
                        }
                    }
                }
            }
        }

        static async Task AsyncMain(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args).WithParsed(options => {
                ProgramOptions = options;
            }).WithNotParsed(e => {
                Environment.Exit(0);
            });

            EGSApi = new EpicKit.WebApi();
            JObject oauth_infos = new JObject();
            bool login_with_sid = false;
            bool login_with_authcode = false;
            string oauth_path = Path.Combine(ProgramOptions.OutCacheDirectory, "oauth_cache.json");

            try
            {
                using (StreamReader reader = new StreamReader(new FileStream(oauth_path, FileMode.Open), Encoding.UTF8))
                {
                    oauth_infos = JObject.Parse(reader.ReadToEnd());
                }

                oauth_infos = await EGSApi.Login(oauth_infos);
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to login with the cached infos: {0};", e.Message);
                login_with_sid = false;
                login_with_authcode = true;
            }

            //oauth_infos = await LoginAnonymous();
            //if (oauth_infos == null)
            //{
            //    Console.WriteLine("Failed to login anonymously.");
            //    Console.WriteLine("Press a key to exit...");
            //    Console.ReadKey(true);
            //    return;
            //}

            if (login_with_authcode)
            {
                try
                {
                    oauth_infos = await LoginWithAuthcode();
                }
                catch(Exception e)
                {
                    Console.WriteLine($"Failed to login with authorization code: {e.Message}");
                    Console.WriteLine("Press a key to exit...");
                    Console.ReadKey(true);
                    return;
                }
            }

            if (login_with_sid)
            {
                try
                {
                    oauth_infos = await LoginWithSID();
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Failed to login with SID: {e.Message}");
                    Console.WriteLine("Press a key to exit...");
                    Console.ReadKey(true);
                    return;
                }
            }

            try
            {
                if (!Directory.Exists(Path.Combine(ProgramOptions.OutCacheDirectory)))
                    Directory.CreateDirectory(Path.Combine(ProgramOptions.OutCacheDirectory));
            }
            catch (Exception)
            {
                Console.WriteLine($"Failed to create {ProgramOptions.OutCacheDirectory}");
                return;
            }

            Console.WriteLine("Successfuly logged in !");
            using (StreamWriter writer = new StreamWriter(new FileStream(oauth_path, FileMode.Create), new UTF8Encoding(false)))
            {
                writer.Write(oauth_infos.ToString());
            }

            if (!ProgramOptions.NoInfos)
            {
                AppList app_list = await DownloadAppList();
                if (HasTargetApp)
                {
                    await GetAppInfos(ProgramOptions.AppNamespace, app_list, ProgramOptions.AppCatalogItemId);
                }
                else
                {
                    foreach (var namespace_entry in app_list.Namespaces)
                    {
                        foreach (var catalog_entry in namespace_entry.Value)
                        {
                            await GetAppInfos(namespace_entry.Key, app_list, catalog_entry.Key);
                        }
                    }
                }
            }

            var gamesInfos = new List<GameInfos>();
            if (!string.IsNullOrWhiteSpace(ProgramOptions.GamesCredentialsDirectory))
            {
                foreach (var applicationPath in Directory.EnumerateFiles(ProgramOptions.GamesCredentialsDirectory))
                {
                    var fileInfos = new FileInfo(applicationPath);
                    if (fileInfos.Attributes.HasFlag(FileAttributes.Hidden))
                        continue;

                    try
                    {
                        using (var fileStream = new StreamReader(new FileStream(fileInfos.FullName, FileMode.Open, FileAccess.Read)))
                        {
                            var gameInfos = JObject.Parse(await fileStream.ReadToEndAsync()).ToObject<GameInfos>();
                            if (string.IsNullOrWhiteSpace(gameInfos.GameNamespace) ||
                                string.IsNullOrWhiteSpace(gameInfos.GameUser) ||
                                string.IsNullOrWhiteSpace(gameInfos.GamePassword) ||
                                string.IsNullOrWhiteSpace(gameInfos.GameDeployementId) ||
                                string.IsNullOrWhiteSpace(gameInfos.GameAppId))
                            {
                                Console.WriteLine($"A required credential property is missing: {fileInfos.FullName}");
                                continue;
                            }

                            gamesInfos.Add(gameInfos);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to open file {applicationPath}: {ex.Message}");
                    }
                }
            }
            else if (!string.IsNullOrWhiteSpace(ProgramOptions.GameUser) &&
                     !string.IsNullOrWhiteSpace(ProgramOptions.GamePassword) &&
                     !string.IsNullOrWhiteSpace(ProgramOptions.GameDeployementId) &&
                     !string.IsNullOrWhiteSpace(ProgramOptions.GameAppId) &&
                     !string.IsNullOrWhiteSpace(ProgramOptions.AppNamespace))
            {
                gamesInfos.Add(new GameInfos
                {
                    GameUser = ProgramOptions.GameUser,
                    GamePassword = ProgramOptions.GamePassword,
                    GameDeployementId = ProgramOptions.GameDeployementId,
                    GameNamespace = ProgramOptions.AppNamespace,
                    GameAppId = ProgramOptions.GameAppId
                });
            }

            foreach (var gameInfos in gamesInfos)
            {
                try
                {
                    var token = await GetGameConnectionTokenAsync(gameInfos.GameDeployementId, gameInfos.GameUser, gameInfos.GamePassword, gameInfos.GameScopes, true);
                    if (string.IsNullOrWhiteSpace(token))
                    {
                        Console.WriteLine($"Failed to get game connection token: {gameInfos.GameNamespace}:{gameInfos.GameAppId}");
                        continue;
                    }

                    var gc = new EpicKit.WebAPI.Game.GameConnection();
                    await gc.GameLoginWithRefreshTokenAsync(gameInfos.GameDeployementId, gameInfos.GameUser, gameInfos.GamePassword, token);

                    Console.WriteLine($"Generating {gameInfos.GameNamespace}:{gameInfos.GameAppId} achievements...");

                    //var x = await gc.QueryOffersAsync();

                    var achievements = await gc.GetAchievementsSchemaAsync();
                    var achievementsDirectory = Path.Combine(BuildSaveGameInfosDirectoryPath(gameInfos.GameNamespace, gameInfos.GameAppId),
                        "achievements");
                    var achievementsDatabasePath = Path.Combine(achievementsDirectory, "achievements_db.json");
                    var achievementsImagesPath = Path.Combine(achievementsDirectory, "achievements_images");

                    if (achievements.Count <= 0)
                    {
                        Console.WriteLine($"No achievements for {gameInfos.GameNamespace}:{gameInfos.GameAppId}");
                        continue;
                    }

                    if (!Directory.Exists(achievementsImagesPath))
                        Directory.CreateDirectory(achievementsImagesPath);

                    foreach (var achievement in achievements)
                    {
                        if (ProgramOptions.DownloadImages)
                        {
                            var lockedImagePath = Path.Combine(achievementsImagesPath, $"{achievement.AchievementId}_locked");
                            if (!File.Exists(lockedImagePath))
                            {
                                var icon = await DownloadAchievementIconAsync(new Uri(achievement.LockedIconUrl));
                                using (var fs = new FileStream(lockedImagePath, FileMode.Create, FileAccess.Write))
                                {
                                    await icon.CopyToAsync(fs);
                                }
                            }

                            var unlockedImagePath = Path.Combine(achievementsImagesPath, achievement.AchievementId);
                            if (!File.Exists(unlockedImagePath))
                            {
                                var icon = await DownloadAchievementIconAsync(new Uri(achievement.UnlockedIconUrl));
                                using (var fs = new FileStream(unlockedImagePath, FileMode.Create, FileAccess.Write))
                                {
                                    await icon.CopyToAsync(fs);
                                }
                            }
                        }

                        achievement.LockedIconUrl = $"{achievement.AchievementId}_locked";
                        achievement.UnlockedIconUrl = achievement.AchievementId;
                    }

                    using (var achievementsDatabaseStream = new StreamWriter(new FileStream(achievementsDatabasePath, FileMode.Create, FileAccess.Write), new UTF8Encoding(false)))
                    {
                        await achievementsDatabaseStream.WriteAsync(JsonConvert.SerializeObject(achievements, Formatting.Indented));
                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine($"Failed to get {gameInfos.GameNamespace}:{gameInfos.GameAppId} achievements: {ex.Message}");
                    Console.WriteLine(ex.StackTrace);
                }
            }
        }

        static void Main(string[] args) =>
            AsyncMain(args).GetAwaiter().GetResult();
    }
}
