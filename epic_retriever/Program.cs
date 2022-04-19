using CommandLine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace epic_retriever
{
    public class Options
    {
        [Option('i', "download_images", Required = false, HelpText = "Sets the flag to download games image. Images will not be downloaded if this flag is not specified.")]
        public bool DownloadImages { get; set; } = false;

        [Option('f', "force", Required = false, HelpText = "Force to download game's infos (usefull if you want to refresh a game).")]
        public bool Force { get; set; } = false;

        [Option('o', "out", Required = false, HelpText = "Where to output your game definitions. By default it will output to 'steam' directory alongside the executable.")]
        public string OutDirectory { get; set; } = "epic";

        [Option('s', "sessionid", Required = false, HelpText = "The SID, allows you to remove the need of interactivity.")]
        public string SessionID { get; set; } = string.Empty;

        [Option('w', "fromweb", Required = false, HelpText = "Try to deduce the catalog id from the app web page. (Slow and not reliable but can help to find more games)")]
        public string FromWeb { get; set; } = string.Empty;
        
        [Option('N', "namespace", Required = false, HelpText = "App namespace. (need -N AND -C)")]
        public string AppNamespace { get; set; } = string.Empty;
        
        [Option('C', "catalog_item", Required = false, HelpText = "App catalog item id. (need -N AND -C)")]
        public string AppCatalogItemId { get; set; } = string.Empty;
    }

    class AppListEntry
    {
        [JsonProperty(PropertyName = "Namespace")]
        public string Namespace { get; set; }

        [JsonProperty(PropertyName = "CatalogItemId")]
        public string CatalogItemId { get; set; }

        [JsonIgnore]
        public EGS.AppAsset Asset { get; set; }
    }

    class Program
    {
        static string OutputDir { get; set; }
        static bool DownloadImages { get; set; }
        static bool Force { get; set; }
        static string SessionID { get; set; }
        static bool FromWeb { get; set; }
        static string AppNamespace { get; set; }
        static string AppCatalogItemId { get; set; }
        static bool HasTargetApp => !string.IsNullOrWhiteSpace(AppNamespace) && !string.IsNullOrWhiteSpace(AppCatalogItemId);

        static EGS.WebApi EGSApi;

        static void SaveAppAsset(EGS.AppAsset asset)
        {
            if (asset == null)
                return;

            try
            {
                string asset_path = Path.Combine(OutputDir, "cache", "assets", asset.Namespace);
                if (!Directory.Exists(asset_path))
                {
                    Directory.CreateDirectory(asset_path);
                }

                using (StreamWriter writer = new StreamWriter(new FileStream(Path.Combine(asset_path, asset.CatalogItemId + ".json"), FileMode.Create), Encoding.UTF8))
                {
                    writer.Write(JsonConvert.SerializeObject(asset, Formatting.Indented));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to save app assets cache: ${e.Message}");
            }
        }

        static void SaveAppInfos(EGS.AppInfos app)
        {
            try
            {
                string asset_path = Path.Combine(OutputDir, "cache", "app_infos", app.Namespace);
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

        static bool CachedDatasChanged(EGS.AppAsset asset)
        {
            if (Force || asset == null)
                return true;

            try
            {
                JObject cached_asset;
                string asset_path = Path.Combine(OutputDir, "cache", "assets", asset.Namespace, asset.CatalogItemId + ".json");

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

        static EGS.AppInfos GetCachedAppInfos(string namespace_, string catalog_item_id)
        {
            EGS.AppInfos app = new EGS.AppInfos();
            string app_infos_path = Path.Combine(OutputDir, "cache", "app_infos", namespace_, catalog_item_id + ".json");
            if (!File.Exists(app_infos_path))
                return app;

            using (StreamReader reader = new StreamReader(new FileStream(app_infos_path, FileMode.Open), Encoding.UTF8))
            {
                app = JObject.Parse(reader.ReadToEnd()).ToObject<EGS.AppInfos>();
            }

            return app;
        }

        static List<EGS.AppAsset> GetAssets()
        {
            var t1 = EGSApi.GetGamesAssets();
            t1.Wait();
            if (t1.Result.ErrorCode == EGS.Error.OK)
                return t1.Result.Result;

            return new List<EGS.AppAsset>();
        }

        static EGS.AppInfos GetApp(string namespace_, string catalog_item_id)
        {
            EGS.AppInfos app = null;

            //if (CachedDatasChanged())
            {
                //SaveAppAsset();

                var t2 = EGSApi.GetGameInfos(namespace_, catalog_item_id);
                t2.Wait();
                if (t2.Result.ErrorCode == EGS.Error.OK)
                {
                    app = t2.Result.Result;
                    SaveAppInfos(app);
                }
            }
            //else
            //{
            //    app = GetCachedAppInfos(namespace_, catalog_item_id);
            //}

            return app;
        }

        static string FindBestImage(EGS.AppInfos app)
        {
            string result = string.Empty;
            int best_match = 0;
            foreach (EGS.KeyImage img in app.KeyImages)
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

        static void SaveGameInfos(JObject game_infos)
        {
            try
            {
                string infos_path = Path.Combine(OutputDir, "game_infos", (string)game_infos["Namespace"]);
                if (!Directory.Exists(infos_path))
                    Directory.CreateDirectory(infos_path);

                string app_id = (string)game_infos["AppId"];
                string image_path = Path.Combine(infos_path, app_id + "_background.jpg");
                if (DownloadImages && !File.Exists(image_path))
                {
                    try
                    {
                        Uri uri = new Uri((string)game_infos["ImageUrl"]);

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

                infos_path = Path.Combine(infos_path, app_id + ".json");
                using (StreamWriter writer = new StreamWriter(new FileStream(infos_path, FileMode.Create), Encoding.UTF8))
                {
                    writer.Write(game_infos.ToString());
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to save game infos: ${e.Message}.");
            }
        }

        static List<AppListEntry> DownloadAppList()
        {
            List<AppListEntry> app_list = new List<AppListEntry>();

            WebClient wcli = new WebClient();
            string applist_path = Path.Combine(OutputDir, "cache", "applist.json");

            if (HasTargetApp)
            {
                app_list.Add(new AppListEntry{ Namespace = AppNamespace, CatalogItemId = AppCatalogItemId });
            }
            else
            {
                try
                {
                    using (StreamReader reader = new StreamReader(new FileStream(applist_path, FileMode.Open), Encoding.UTF8))
                    {
                        app_list = JArray.Parse(reader.ReadToEnd()).ToObject<List<AppListEntry>>();
                    }
                }
                catch (Exception)
                { }
            }

            Console.WriteLine("Downloading assets...");
            List<EGS.AppAsset> assets = GetAssets();
            foreach (EGS.AppAsset asset in assets)
            {
                var entry = app_list.Find(x => x.Namespace == asset.Namespace && x.CatalogItemId == asset.CatalogItemId);
                if (entry == null)
                {
                    if (!HasTargetApp)
                    {
                        app_list.Add(new AppListEntry { Namespace = asset.Namespace, CatalogItemId = asset.CatalogItemId, Asset = asset });
                    }
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
                    wcli.Headers[HttpRequestHeader.UserAgent] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:98.0) Gecko/20100101 Firefox/98.0";
                    var t = wcli.DownloadStringTaskAsync("https://store-content.ak.epicgames.com/api/content/productmapping");
                    t.Wait();
                    foreach (var item in JObject.Parse(t.Result))
                    {
                        if (app_list.Find(x => x.Namespace == item.Key) == null)
                        {
                            try
                            {
                                var t2 = wcli.DownloadStringTaskAsync($"https://www.epicgames.com/store/en-US/p/{item.Value}");
                                t2.Wait();
                                int start = t2.Result.IndexOf("\"item\":{\"catalogId\":\"");
                                if (start != -1)
                                {
                                    start += 21; // length of "item":{"catalogId":"
                                    string catalog_item_id = t2.Result.Substring(start, t2.Result.IndexOf("\"", start) - start);
                                    if (catalog_item_id != string.Empty)
                                    {
                                        app_list.Add(new AppListEntry { Namespace = item.Key, CatalogItemId = catalog_item_id });
                                    }
                                }
                            }
                            catch (Exception)
                            { }
                        }
                    }
                }
                catch (Exception)
                { }
            }

            if (!HasTargetApp)
            {
                try
                {
                    using (StreamWriter writer = new StreamWriter(new FileStream(applist_path, FileMode.Create), Encoding.UTF8))
                    {
                        writer.Write(JArray.FromObject(app_list).ToString());
                    }
                }
                catch (Exception)
                { }
            }

            return app_list;
        }

        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args).WithParsed(options => {
                OutputDir = options.OutDirectory;
                DownloadImages = options.DownloadImages;
                Force = options.Force;
                SessionID = options.SessionID;
                AppNamespace = options.AppNamespace;
                AppCatalogItemId = options.AppCatalogItemId;
            }).WithNotParsed(e => {
                Environment.Exit(0);
            });

            EGSApi = new EGS.WebApi();
            JObject oauth_infos = new JObject();
            bool login_sid = false;
            string oauth_path = Path.Combine(OutputDir, "cache", "oauth_cache.json");

            try
            {
                using (StreamReader reader = new StreamReader(new FileStream(oauth_path, FileMode.Open), Encoding.UTF8))
                {
                    oauth_infos = JObject.Parse(reader.ReadToEnd());
                }

                var t = EGSApi.Login(oauth_infos);
                t.Wait();
                EGS.Error<JObject> result = t.Result;
                if (result.ErrorCode != EGS.Error.OK)
                {
                    Console.WriteLine("Failed to login with the cached infos: {0};", result.Message);
                    login_sid = true;
                }
                else
                {
                    oauth_infos = result.Result;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to login with the cached infos: {0};", e.Message);
                login_sid = true;
            }

            if (login_sid)
            {
                Console.WriteLine("Will now try to login with SID...");
                if (string.IsNullOrWhiteSpace(SessionID))
                {
                    Console.WriteLine("EGL sid (get it at: https://www.epicgames.com/id/login?redirectUrl=https://www.epicgames.com/id/api/redirect): ");
                    SessionID = Console.ReadLine();
                }

                var t = EGSApi.LoginSID(SessionID);
                t.Wait();
                EGS.Error<JObject> result = t.Result;
                if (result.ErrorCode != EGS.Error.OK)
                {
                    Console.WriteLine("Failed to login with the SID: {0}", result.Message);
                    Console.WriteLine("Press a key to exit...");
                    Console.ReadKey(true);
                    return;
                }

                oauth_infos = result.Result;
            }

            try
            {
                if(!Directory.Exists(Path.Combine(OutputDir, "cache")))
                    Directory.CreateDirectory(Path.Combine(OutputDir, "cache"));
            }
            catch(Exception)
            {
                Console.WriteLine($"Failed to create {Path.Combine(OutputDir, "cache")}");
                return;
            }

            Console.WriteLine("Successfuly logged in !");
            using (StreamWriter writer = new StreamWriter(new FileStream(oauth_path, FileMode.Create), Encoding.UTF8))
            {
                writer.Write(oauth_infos.ToString());
            }

            List<AppListEntry> app_list = DownloadAppList();

            foreach (AppListEntry app_meta in app_list)
            {
                EGS.AppInfos app = GetApp(app_meta.Namespace, app_meta.CatalogItemId);

                if (app != null && !app.IsDlc && (app.ReleaseInfo.Count > 0 || app_meta.Asset != null))
                {
                    string app_id = app_meta.Asset != null ? app_meta.Asset.AppName : app.ReleaseInfo[0].AppId;
                    Console.WriteLine("App {0}, AppId {1}, Namespace {2}, ItemId {3}", app.Title, app_id, app.Namespace, app.Id);
                    int title_length = 0;
                    int id_length = 0;
                    int namespace_length = 0;
                    int catalog_id_length = 0;

                    foreach (EGS.AppInfos dlc in app.DlcItemList)
                    {
                        string dlc_id;
                        var dlc_meta = app_list.Find(a => a.CatalogItemId == dlc.Id);
                        if (dlc_meta != null && dlc_meta.Asset != null)
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

                    JObject dlcs = new JObject();

                    foreach (EGS.AppInfos dlc in app.DlcItemList)
                    {
                        string dlc_id;
                        var dlc_meta = app_list.Find(a => a.CatalogItemId == dlc.Id);
                        if (dlc_meta != null && dlc_meta.Asset != null)
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
                        Console.WriteLine($"   Dlc {{0, -{title_length}}}, DlcId {{1, -{id_length}}}, Namespace {{2, -{namespace_length}}}, ItemId {{3, -{catalog_id_length}}}", dlc.Title, dlc_id, dlc.Namespace, dlc.Id);

                        dlcs[dlc_id] = new JObject();
                        dlcs[dlc_id]["Name"] = dlc.Title;
                        dlcs[dlc_id]["ItemId"] = dlc.Id;
                    }

                    Console.WriteLine();

                    JObject game_infos = new JObject();
                    game_infos["Name"] = app.Title;
                    game_infos["AppId"] = app.ReleaseInfo[0].AppId;
                    game_infos["Namespace"] = app.Namespace;
                    game_infos["ItemId"] = app.Id;
                    game_infos["ImageUrl"] = FindBestImage(app);
                    foreach (EGS.ReleaseInfo releaseInfo in app.ReleaseInfo)
                    {
                        game_infos["Releases"] = JArray.FromObject(releaseInfo.Platform);
                    }
                    game_infos["Dlcs"] = dlcs;

                    SaveGameInfos(game_infos);
                }
            }
        }
    }
}

