using CommandLine;
using EpicKit.WebAPI.Models;
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
using System.Threading;
using System.Threading.Tasks;

namespace epic_retriever
{
    public class Options
    {
        [Option('i', "download_images", Required = false, HelpText = "Sets the flag to download games image. Images will not be downloaded if this flag is not set.")]
        public bool DownloadImages { get; set; } = false;

        [Option('f', "force", Required = false, HelpText = "Force to download game's infos (usefull if you want to refresh a game).")]
        public bool Force { get; set; } = false;

        [Option('o', "out", Required = false, HelpText = "Where to output your game definitions. By default it will output to 'epic' directory alongside the executable.")]
        public string OutDirectory { get; set; } = "epic";

        [Option('c', "cache_out", Required = false, HelpText = "Where to output the cache datas. By default it will output to 'epic_cache' directory alongside the executable.")]
        public string OutCacheDirectory { get; set; } = "epic_cache";

        [Option('w', "fromweb", Required = false, HelpText = "Try to deduce the catalog id from the app web page.")]
        public bool FromWeb { get; set; } = false;
        
        [Option('N', "namespace", Required = false, HelpText = "App namespace. (need -N AND -C)")]
        public string AppNamespace { get; set; } = string.Empty;
        
        [Option('C', "catalog_item", Required = false, HelpText = "App catalog item id. (need -N AND -C)")]
        public string AppCatalogItemId { get; set; } = string.Empty;
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

    class AppListEntry
    {
        [JsonIgnore]
        public AppAsset Asset { get; set; }
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
        static string OutputDir { get; set; }
        static string OutCacheDirectory { get; set; }
        static bool DownloadImages { get; set; }
        static bool Force { get; set; }
        static bool FromWeb { get; set; }
        static string AppNamespace { get; set; }
        static string AppCatalogItemId { get; set; }
        static bool HasTargetApp => !string.IsNullOrWhiteSpace(AppNamespace) && !string.IsNullOrWhiteSpace(AppCatalogItemId);

        static EpicKit.WebApi EGSApi;

        static void SaveAppAsset(AppAsset asset)
        {
            if (asset == null)
                return;

            try
            {
                string asset_path = Path.Combine(OutCacheDirectory, "assets", asset.Namespace);
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

        static void SaveAppInfos(AppInfos app)
        {
            try
            {
                string asset_path = Path.Combine(OutCacheDirectory, "app_infos", app.Namespace);
                if (!Directory.Exists(asset_path))
                {
                    Directory.CreateDirectory(asset_path);
                }

                using (StreamWriter writer = new StreamWriter(new FileStream(Path.Combine(asset_path, app.Id + ".json"), FileMode.Create), Encoding.UTF8))
                {
                    writer.Write(JsonConvert.SerializeObject(app, Formatting.Indented));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to save app infos cache: ${e.Message}");
            }
        }

        static bool CachedDatasChanged(AppAsset asset)
        {
            if (Force || asset == null)
                return true;

            try
            {
                JObject cached_asset;
                string asset_path = Path.Combine(OutCacheDirectory, "assets", asset.Namespace, asset.CatalogItemId + ".json");

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

        static AppInfos GetCachedAppInfos(string namespace_, string catalog_item_id)
        {
            AppInfos app = new AppInfos();
            string app_infos_path = Path.Combine(OutCacheDirectory, "app_infos", namespace_, catalog_item_id + ".json");
            if (!File.Exists(app_infos_path))
                return app;

            using (StreamReader reader = new StreamReader(new FileStream(app_infos_path, FileMode.Open), Encoding.UTF8))
            {
                app = JObject.Parse(reader.ReadToEnd()).ToObject<AppInfos>();
            }

            return app;
        }

        static async Task<AppInfos> GetApp(string namespace_, string catalog_item_id)
        {
            AppInfos app = null;

            //if (CachedDatasChanged())
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

        static string FindBestImage(AppInfos app)
        {
            string result = string.Empty;
            int best_match = 0;
            foreach (KeyImage img in app.KeyImages)
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

        static string BuildSaveGameInfosDirectoryPath(AppInfoModel game_infos)
        {
            return Path.Combine(OutputDir, game_infos.Namespace, game_infos.AppId);
        }

        static string BuildSaveGameInfosPath(AppInfoModel game_infos)
        {
            return Path.Combine(BuildSaveGameInfosDirectoryPath(game_infos), game_infos.AppId + ".json");
        }

        static void SaveGameInfos(AppInfoModel game_infos, bool merge_existing_infos)
        {
            try
            {
                string infos_path = BuildSaveGameInfosDirectoryPath(game_infos);
                if (!Directory.Exists(infos_path))
                    Directory.CreateDirectory(infos_path);

                string image_path = Path.Combine(infos_path, game_infos.AppId + "_background.jpg");
                if (DownloadImages && !File.Exists(image_path))
                {
                    try
                    {
                        Uri uri = new Uri(game_infos.ImageUrl);

                        WebClient wcli = new WebClient();

                        wcli.Headers["User-Agent"] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:88.0) Gecko/20100101 Firefox/88.0";
                        wcli.Headers["Referer"] = "https://www.epicgames.com/store/en/";
                        wcli.Headers["Origin"] = "https://www.epicgames.com";

                        wcli.DownloadFile(uri, image_path);
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

        static async Task<AppList> DownloadAppList()
        {
            var app_list = new AppList();

            var wcli = new HttpClient(new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.All,
            });
            wcli.DefaultRequestHeaders.TryAddWithoutValidation("UserAgent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:98.0) Gecko/20100101 Firefox/98.0");
            string applist_path = Path.Combine(OutCacheDirectory, "applist.json");

            
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
                    app_list.Namespaces.GetOrCreate(AppNamespace)[AppCatalogItemId] = new AppListEntry { };
                }
            }
            catch (Exception)
            { }

            app_list.Version = 1;

            Console.WriteLine("Downloading assets...");
            var assets = await EGSApi.GetGamesAssets();
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

            if (!HasTargetApp && FromWeb)
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
            try
            {
                var url = $"https://www.epicgames.com/graphql?query={{Catalog{{catalogOffers(namespace:\"{namespace_}\" params:{{count:500}}){{elements{{id title offerType items{{id title}}}}}}}}}}";
                var webClient = new HttpClient(new HttpClientHandler
                {
                    AutomaticDecompression = DecompressionMethods.All,
                });

                using (var reader = new StreamReader((await webClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, url))).Content.ReadAsStream()))
                {
                    return JObject.Parse(await reader.ReadToEndAsync()).ToObject<CatalogModel>();
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

                SaveGameInfos(game_infos, true);
            }
        }

        static async Task PrepareManifestDownload(string namespace_, string catalogId, string appId)
        {
            var result = await EGSApi.GetManifestDownloadInfos(namespace_, catalogId, appId);

            var manifestCacheDir = Path.Combine(OutCacheDirectory, "depots", appId);
            if (!Directory.Exists(manifestCacheDir))
                Directory.CreateDirectory(manifestCacheDir);

            using (var file = new FileStream(Path.Combine(manifestCacheDir, $"{appId}.manifest"), FileMode.Create))
            {
                await file.WriteAsync(result.ManifestData);
            }

            using (var writer = new StreamWriter(new FileStream(Path.Combine(manifestCacheDir, "download_infos.json"), FileMode.Create)))
            {
                var json = new JObject {
                    { "BaseUrls", new JArray(result.BaseUrls) }
                };

                await writer.WriteAsync(json.ToString());
            }
        }
        static async Task<JObject> LoginAnonymous()
        {
            Console.WriteLine("Will now try to login anonymously...");
            return await EGSApi.LoginAnonymous();
        }

        static async Task<JObject> LoginWithAuthcode()
        {
            Console.WriteLine("Will now try to login with authorization code...");
            Console.WriteLine("EGL authcode (get it at: https://www.epicgames.com/id/api/redirect?clientId=34a02cf8f4414e29b15921876da36f9a&responseType=code): ");
            return await EGSApi.LoginAuthCode(Console.ReadLine().Trim());
        }

        static async Task<JObject> LoginWithSID()
        {
            Console.WriteLine("Will now try to login with SID...");
            Console.WriteLine("EGL sid (get it at: https://www.epicgames.com/id/login?redirectUrl=https://www.epicgames.com/id/api/redirect): ");
            return await EGSApi.LoginSID(Console.ReadLine().Trim());
        }

        static async Task<string> GetGameConnectionToken(string deployement_id, string user_id, string password)
        {
            try
            {
                return await EGSApi.GetAppRefreshTokenFromExchangeCode(await EGSApi.GetAppExchangeCode(), deployement_id, user_id, password);
            }
            catch (EpicKit.WebApiOAuthScopeConsentRequiredException ex)
            {
                int retries = 0;
                while (true)
                {
                    try
                    {
                        return await EGSApi.RunContinuationToken(ex.ContinuationToken, deployement_id, user_id, password);
                    }
                    catch (Exception)
                    {
                        if (retries == 0)
                        {
                            var url = $"https://www.epicgames.com/id/authorize?continuation={ex.ContinuationToken}&client_id={user_id}&scope=openid%20basic_profile%20friends_list%20presence&prompt=skip_merge%20skip_upgrade";
                            Console.WriteLine($"Consent is required, please head to '{url}'.");
                        }
                        ++retries;
                        if (retries == 30)
                            return null;

                        Thread.Sleep(TimeSpan.FromSeconds(2));
                    }
                }
            }
        }

        static async Task AsyncMain(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args).WithParsed(options => {
                OutputDir = options.OutDirectory;
                DownloadImages = options.DownloadImages;
                Force = options.Force;
                AppNamespace = options.AppNamespace;
                AppCatalogItemId = options.AppCatalogItemId;
                FromWeb = options.FromWeb;
                OutCacheDirectory = options.OutCacheDirectory;
            }).WithNotParsed(e => {
                Environment.Exit(0);
            });

            EGSApi = new EpicKit.WebApi();
            JObject oauth_infos = new JObject();
            bool login_with_sid = false;
            bool login_with_authcode = false;
            string oauth_path = Path.Combine(OutCacheDirectory, "oauth_cache.json");

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
                if (!Directory.Exists(Path.Combine(OutCacheDirectory)))
                    Directory.CreateDirectory(Path.Combine(OutCacheDirectory));
            }
            catch (Exception)
            {
                Console.WriteLine($"Failed to create {OutCacheDirectory}");
                return;
            }

            Console.WriteLine("Successfuly logged in !");
            using (StreamWriter writer = new StreamWriter(new FileStream(oauth_path, FileMode.Create), new UTF8Encoding(false)))
            {
                writer.Write(oauth_infos.ToString());
            }

            AppList app_list = await DownloadAppList();
            if (HasTargetApp)
            {
                await GetAppInfos(AppNamespace, app_list, AppCatalogItemId);
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

        static void Main(string[] args)
        {
            var t = AsyncMain(args);
            t.Wait();
        }
    }
}
