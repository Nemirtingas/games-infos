using System;
using System.Net;
using System.Text;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Globalization;

using Newtonsoft.Json.Linq;
using CommandLine;
using System.Threading.Tasks;
using System.Net.Http;
using SteamKit2;

namespace steam_retriever
{
    class Program
    {
        enum RunningStatus
        {
            NotRunning = 0,
            WaitingCallback = 1,
            LoggedIn = 2,
        };

        enum SchemaStatType
        {
            Int = 1,
            Float = 2,
            AvgRate = 3,
            Bits = 4, // Achievements
        }

        enum AppType
        {
            Other = 0,
            Game = 1,
            Dlc = 2,
            Application = 3,
            Tool = 4,
            Demo = 5,
            Beta = 6,
        }

        static ProgramOptions Options;

        static JObject GamesInfos = new JObject();
        static Dictionary<uint, KeyValue> AppIds = new Dictionary<uint, KeyValue>();
        static HashSet<uint> DoneAppIds = new HashSet<uint>();
        
        static HttpClient WebHttpClient = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.All });

        static dynamic WebSteamUser = null;
        static bool IsAnonymous = true;
        static DateTime LastWebRequestTime = new DateTime();

        static async Task<HttpResponseMessage> LimitSteamWebApiGET(HttpClient http_client, HttpRequestMessage http_request, CancellationTokenSource cts = null)
        {// Steam has a limit of 300 requests every 5 minutes (1 request per second).
            if ((DateTime.Now - LastWebRequestTime) < TimeSpan.FromSeconds(1))
                Thread.Sleep(TimeSpan.FromSeconds(1));

            LastWebRequestTime = DateTime.Now;

            if (cts == null)
            {
                cts = new CancellationTokenSource();
            }

            return await http_client.SendAsync(http_request, HttpCompletionOption.ResponseContentRead, cts.Token);
        }

        static async Task GenerateAchievementsFromWebAPI(string appid)
        {
            if (string.IsNullOrEmpty(Options.WebApiKey))
            {
                Console.WriteLine("  + WebApi key is missing, the tool will not generate achievements list.");
                return;
            }

            try
            {
                int done = 0;
                Console.WriteLine("  + Trying to retrieve achievements and stats...");

                var response = await LimitSteamWebApiGET(
                    WebHttpClient,
                    new HttpRequestMessage(HttpMethod.Get, $"http://api.steampowered.com/ISteamUserStats/GetSchemaForGame/v2/?l={Options.Language}&key={Options.WebApiKey}&appid={appid}"));

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    JObject achievements_json = JObject.Parse(await response.Content.ReadAsStringAsync());

                    if (((JObject)achievements_json["game"]).ContainsKey("availableGameStats"))
                    {
                        if (((JObject)achievements_json["game"]["availableGameStats"]).ContainsKey("achievements"))
                        {
                            JArray achievements = (JArray)achievements_json["game"]["availableGameStats"]["achievements"];

                            Directory.CreateDirectory(Path.Combine(Options.OutDirectory, appid));

                            foreach (var achievement in achievements)
                            {
                                achievement["displayName"] = new JObject { { Options.Language, achievement["displayName"] } };
                                achievement["description"] = new JObject { { Options.Language, achievement["description"] } };

                                if (Options.DownloadImages)
                                {
                                    Directory.CreateDirectory(Path.Combine(Options.OutDirectory, appid, "achievements_images"));

                                    string name = (string)achievement["name"];
                                    string image_path = Path.Combine(Options.OutDirectory, appid, "achievements_images", name + ".jpg");
                                    if (!File.Exists(image_path))
                                    {
                                        Console.WriteLine(string.Format("   + Downloading achievement {0} unlocked icon...", name));
                                        response = await LimitSteamWebApiGET(WebHttpClient, new HttpRequestMessage(HttpMethod.Get, (string)achievement["icon"]));

                                        using (BinaryWriter streamWriter = new BinaryWriter(new FileStream(image_path, FileMode.Create), new UTF8Encoding(false)))
                                        {
                                            streamWriter.Write(await response.Content.ReadAsByteArrayAsync());
                                        }
                                    }
                                    image_path = Path.Combine(Options.OutDirectory, appid, "achievements_images", name + "_gray.jpg");
                                    if (!File.Exists(image_path))
                                    {
                                        Console.WriteLine(string.Format("   + Downloading achievement {0} locked icon...", name));

                                        response = await LimitSteamWebApiGET(WebHttpClient, new HttpRequestMessage(HttpMethod.Get, (string)achievement["icongray"]));
                                        using (BinaryWriter streamWriter = new BinaryWriter(new FileStream(image_path, FileMode.Create), new UTF8Encoding(false)))
                                        {
                                            streamWriter.Write(await response.Content.ReadAsByteArrayAsync());
                                        }
                                    }
                                }
                            }

                            done |= SaveAchievementsToFile(appid, achievements) ? 1 : 0;
                            if ((done & 1) != 1)
                            {
                                Console.WriteLine("  + Failed to save achievements.");
                            }
                        }
                        if (((JObject)achievements_json["game"]["availableGameStats"]).ContainsKey("stats"))
                        {
                            Directory.CreateDirectory(Path.Combine(Options.OutDirectory, appid));

                            done |= SaveStatsToFile(appid, achievements_json["game"]["availableGameStats"]["stats"]) ? 2 : 0;
                            if ((done & 2) != 2)
                            {
                                Console.WriteLine("  + Failed to save stats.");
                            }
                        }
                    }
                }//using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                if (done == 0)
                {
                    Console.WriteLine("  + No achievements or stats available for this AppID.");
                }
                else
                {
                    Console.WriteLine("  + Achievements and stats were successfully generated!");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("  + Failed (no achievements or stats?): {0}", e.Message);
            }
        }

        static bool GenerateAchievementsFromKeyValue(KeyValue schema, uint appid)
        {
            JArray achievements_array = new JArray();
            JArray stats_array = new JArray();

            foreach (KeyValue stats_object in schema["stats"].Children)
            {
                if (stats_object["type"].AsLong() == (int)SchemaStatType.Bits)
                {// Parse achievements
                    foreach (KeyValue achievement_definition in stats_object["bits"].Children)
                    {
                        KeyValue display_object = achievement_definition["display"];
                        KeyValue progress_object = achievement_definition["progress"];

                        JObject localized_names = new JObject();
                        JObject localized_descriptions = new JObject();

                        KeyValue v = display_object["hidden"];
                        long hidden = v == null ? 0 : v.AsLong();

                        foreach (KeyValue item in display_object["name"].Children)
                        {
                            localized_names[item.Name.ToLower()] = item.Value;
                        }
                        foreach (KeyValue item in display_object["desc"].Children)
                        {
                            localized_descriptions[item.Name.ToLower()] = item.Value;
                        }

                        JObject achievement_json = new JObject
                        {
                            { "name"       , achievement_definition["name"].Value },
                            { "hidden"     , hidden },
                            { "icon"       , $"https://steamcdn-a.akamaihd.net/steamcommunity/public/images/apps/{appid}/{display_object["icon"].AsString()}" },
                            { "icongray"   , $"https://steamcdn-a.akamaihd.net/steamcommunity/public/images/apps/{appid}/{display_object["icon_gray"].AsString()}" },
                            { "displayName", localized_names },
                            { "description", localized_descriptions },
                        };

                        if (progress_object != null && progress_object["value"] != null)
                        {
                            JObject stats_thresholds = new JObject();
                            stats_thresholds["min_val"] = progress_object["min_val"].AsLong();
                            stats_thresholds["max_val"] = progress_object["max_val"].AsLong();

                            bool add_threshold = false;

                            foreach (KeyValue kv in progress_object["value"].Children)
                            {
                                if (kv.Name == "operation")
                                {
                                    if (kv.Value == "statvalue")
                                    {
                                        add_threshold = true;
                                    }
                                    else
                                    {
                                        Console.Write("Unknown operation: {0}", kv.Value);
                                    }
                                }
                                else if (kv.Name.Contains("operand"))
                                {
                                    if (stats_thresholds.ContainsKey("stat_name"))
                                    {
                                        Console.Write("Error, multiple operands in the progress object: {0}:{1}", kv.Name, kv.Value);
                                    }
                                    else
                                    {
                                        stats_thresholds["stat_name"] = kv.Value;
                                    }
                                }
                                else
                                {
                                    Console.Write("Unhandled progression Key {0}", kv.Name);
                                }
                            }

                            if (add_threshold)
                            {
                                achievement_json["stats_thresholds"] = stats_thresholds;
                            }
                        }

                        achievements_array.Add(achievement_json);
                    }
                }
                else
                {// Parse stats
                    KeyValue v = stats_object["default"];
                    long default_long_value = 0;
                    float default_float_value = 0.0f;
                    string display_name = string.Empty;

                    v = stats_object["display"]["name"];
                    if (v != KeyValue.Invalid && !string.IsNullOrWhiteSpace(v.Value))
                    {
                        display_name = v.Value;
                    }

                    JObject stat_obj = new JObject
                    {
                        { "name"         , stats_object["name"].Value },
                        { "displayName"  , display_name },
                        { "type"         , ((SchemaStatType)stats_object["type"].AsLong()).ToString().ToLower() },
                        { "incrementonly", false },
                    };

                    v = stats_object["incrementonly"];
                    if(v != KeyValue.Invalid && ulong.Parse(v.Value) == 1)
                    {
                        stat_obj["incrementonly"] = true;
                    }

                    v = stats_object["default"];
                    switch ((SchemaStatType)stats_object["type"].AsLong())
                    {
                        case SchemaStatType.Int:
                            if (v != KeyValue.Invalid)
                            {
                                try
                                {
                                    default_long_value = (long)ulong.Parse(v.Value);
                                }
                                catch (Exception)
                                {
                                    try
                                    {
                                        default_long_value = long.Parse(v.Value);
                                    }
                                    catch (Exception)
                                    {
                                        try
                                        {
                                            default_long_value = (long)float.Parse(v.Value, CultureInfo.InvariantCulture);
                                        }
                                        catch (Exception)
                                        {
                                        }
                                    }
                                }
                            }
                            stat_obj["defaultvalue"] = default_long_value;
                            break;

                        case SchemaStatType.Float:
                            if (v != KeyValue.Invalid)
                            {
                                try
                                {
                                    default_float_value = ulong.Parse(v.Value);
                                }
                                catch (Exception)
                                {
                                    try
                                    {
                                        default_float_value = long.Parse(v.Value);
                                    }
                                    catch (Exception)
                                    {
                                        try
                                        {
                                            default_float_value = float.Parse(v.Value, CultureInfo.InvariantCulture);
                                        }
                                        catch (Exception)
                                        {
                                        }
                                    }
                                }
                            }
                            stat_obj["defaultvalue"] = default_float_value;
                            break;
                    }

                    stats_array.Add(stat_obj);
                }
            }

            SaveAchievementsToFile(appid.ToString(), achievements_array);
            SaveStatsToFile(appid.ToString(), stats_array);

            return true;
        }

        static async Task<bool> GenerateAchievementsFromSteamNetwork(uint appid)
        {
            if (IsAnonymous)
                return false;

            List<SteamID> reviewers_ids;

            reviewers_ids = await GetAppPublicSteamIDs(appid, 15);

            if (reviewers_ids.Count == 0)
                return false;

            foreach (SteamID steam_id in reviewers_ids)
            {
                var result = ContentDownloader.steam3.GetUserStats(appid, steam_id.ConvertToUInt64());

                try
                {
                    if (result.Result == EResult.OK && result.Schema != null)
                    {
                        using(MemoryStream ms = new MemoryStream())
                        {
                            result.Schema.SaveToStream(ms, false);
                            ms.Position = 0;
                            
                            using(var sr = new StreamReader(ms))
                            {
                                SaveFile(Path.Combine(Options.CacheOutDirectory, appid.ToString(), "stats_schema.vdf"), sr.ReadToEnd());
                            }
                        }

                        return GenerateAchievementsFromKeyValue(result.Schema, appid);
                    }
                }
                catch (Exception)
                { }
            }

            Console.WriteLine("  + No achievements or stats available for this AppID.");
            return false;
        }

        static async Task<bool> GetItemsDef(uint appid, string digest)
        {
            var response = await LimitSteamWebApiGET(
                WebHttpClient,
                new HttpRequestMessage(HttpMethod.Get, $"https://api.steampowered.com/IGameInventory/GetItemDefArchive/v0001?appid={appid}&digest={digest}"));

            if (response.StatusCode == HttpStatusCode.OK)
            {
                JArray items = JArray.Parse(await response.Content.ReadAsStringAsync());
                if (items.Count > 0)
                {
                    SaveJson(Path.Combine(Options.CacheOutDirectory, appid.ToString(), "inventory_db.json"), items);
                    return SaveJson(Path.Combine(Options.OutDirectory, appid.ToString(), "inventory_db.json"), items);
                }
            }

            return false;
        }

        static async Task GenerateItemsFromSteamNetwork(uint appid)
        {
            try
            {
                string digest = ContentDownloader.steam3.GetInventoryDigest(appid);
                if (digest != null)
                {
                    if (await GetItemsDef(appid, digest))
                    {
                        Console.WriteLine("  + Items list successfully generated!");
                    }
                    else
                    {
                        Console.WriteLine("  + No items exist for this AppID");
                    }
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(" failed (no items?): {0}", e.Message);
            }
        }

        static async Task<List<SteamID>> GetAppPublicSteamIDs(ulong appid, int max_id_count)
        {
            List<SteamID> public_steamids = new List<SteamID>
            {
                76561198028121353,
                76561198001237877,
                76561198355625888,
                76561198001678750,
                76561198237402290,
                //76561197979911851,
                //76561198152618007,
                //76561197969050296,
                //76561198213148949,
                //76561198037867621,
                //76561198108581917,
            };

            try
            {
                var response = await LimitSteamWebApiGET(
                    WebHttpClient,
                    new HttpRequestMessage(HttpMethod.Get, $"https://store.steampowered.com/appreviews/{appid}?json=1&cursor=*&start_date=-1&end_date=-1date_range_type=all&filter=summary&language=all&review_type=all&purchase_type=all&playtime_filter_min=0&playtime_filter_max=0"));

                JObject reviews = JObject.Parse(await response.Content.ReadAsStringAsync());

                if (reviews["success"] != null && (int)reviews["success"] == 1 && reviews["reviews"] != null)
                {
                    foreach (var entry in reviews["reviews"])
                    {
                        if (public_steamids.Count >= max_id_count)
                            break;

                        string user_steamid = (string)entry["author"]["steamid"];
                        KeyValue kvUser = WebSteamUser.GetPlayerSummaries2(steamids: user_steamid);

                        foreach (KeyValue v in kvUser["players"].Children)
                        {
                            if (v["communityvisibilitystate"].AsLong() == 3)
                            {
                                public_steamids.Add(new SteamID(ulong.Parse(user_steamid)));
                            }
                        }
                    }
                }
            }
            catch(Exception)
            { }

            return public_steamids;
        }

        static bool SaveAchievementsToFile(string appid, JToken json)
        {
            if (((JArray)json).Count <= 0)
            {
                Console.WriteLine("  + No Achievements for this app");
                return true;
            }

            Console.WriteLine("  + Writing Achievements achievements_db.json");
            return SaveJson(Path.Combine(Options.OutDirectory, appid, "achievements_db.json"), json);
        }

        static bool SaveStatsToFile(string appid, JToken json)
        {
            if (((JArray)json).Count <= 0)
            {
                Console.WriteLine("  + No Stats for this app");
                return true;
            }
            Console.WriteLine("  + Writing stats stats_db.json.");
            return SaveJson(Path.Combine(Options.OutDirectory, appid, "stats_db.json"), json);
        }

        static JObject GetOrCreateApp(string appid, bool is_dlc)
        {
            if (!GamesInfos.ContainsKey(appid))
            {
                string infos_file = Path.Combine(Options.OutDirectory, appid, appid + ".json");
                try
                {
                    using (StreamReader reader = new StreamReader(new FileStream(infos_file, FileMode.Open), new UTF8Encoding(false)))
                    {
                        GamesInfos.Add(appid, JObject.Parse(reader.ReadToEnd()));
                    }
                }
                catch (Exception)
                {
                    GamesInfos.Add(appid, new JObject());
                }
            }

            JObject app = (JObject)GamesInfos[appid];
            if (!is_dlc && !app.ContainsKey("Dlcs"))
            {
                app["Dlcs"] = new JObject();
            }
            return app;
        }

        static bool SaveJson(string file_path, JToken json)
        {
            return SaveFile(file_path, Newtonsoft.Json.JsonConvert.SerializeObject(json, Newtonsoft.Json.Formatting.Indented));
        }

        static bool SaveFile(string file_path, string data)
        {
            try
            {
                string save_dir = Path.GetDirectoryName(file_path);
                if (!Directory.Exists(save_dir))
                {
                    Directory.CreateDirectory(save_dir);
                }
                using (StreamWriter streamWriter = new StreamWriter(new FileStream(file_path, FileMode.Create), new UTF8Encoding(false)))
                {
                    streamWriter.Write(data);
                }
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to save json: {0}", e.Message);
            }
            return false;
        }

        static bool ParseCommonDetails(JObject infos, uint appid, KeyValue app, string type)
        {
            string app_name = app["common"]["name"].Value;

            infos["Name"] = app_name;
            infos["AppId"] = appid;
            infos["Type"] = type;

            foreach (var image in app["common"]["header_image"].Children)
            {
                if (image.Name == "english")
                {
                    infos["ImageUrl"] = $"https://cdn.akamai.steamstatic.com/steam/apps/{appid}/{image.Value}";
                    break;
                }

                infos["ImageUrl"] = $"https://cdn.akamai.steamstatic.com/steam/apps/{appid}/{image.Value}";
            }

            return true;
        }

        static async Task<bool> ParseGameDetails(JObject infos, uint appid, KeyValue app)
        {
            string str_appid = appid.ToString();

            if (app["common"]["supported_languages"].Children.Count > 0)
            {
                infos["Languages"] = new JArray();
                foreach (var language in app["common"]["supported_languages"].Children)
                {
                    try
                    {
                        if (bool.Parse(language["supported"].AsString()))
                        {
                            ((JArray)infos["Languages"]).Add(language.Name.Trim().ToLower());
                        }
                    }
                    catch (Exception)
                    { }
                }
            }

            if (app["config"]["steamcontrollerconfigdetails"].Children.Count > 0)
            {
                infos["ControllerConfigurations"] = new JObject();
                foreach (var controller_config in app["config"]["steamcontrollerconfigdetails"].Children)
                {
                    string published_id = "0";
                    try
                    {
                        published_id = controller_config.Name.Trim();

                        ((JObject)infos["ControllerConfigurations"])[published_id] = new JObject{
                            { "Type", controller_config["controller_type"].Value.Trim().ToLower() }
                        };

                        if (!Options.CacheOnly && Options.DownloadControllerConfigurations)
                        {
                            var file_details = ContentDownloader.steam3.GetPublishedFileDetails(null, ulong.Parse(published_id));
                            if (!string.IsNullOrWhiteSpace(file_details.filename) && !string.IsNullOrWhiteSpace(file_details.file_url))
                            {
                                CancellationTokenSource cts = new CancellationTokenSource();

                                string controller_path = Path.Combine(Options.CacheOutDirectory, str_appid, file_details.filename);
                                Console.WriteLine($"  + Saved controller file {controller_path}");

                                SaveFile(
                                    controller_path,
                                    await(await WebHttpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, file_details.file_url), HttpCompletionOption.ResponseContentRead, cts.Token)).Content.ReadAsStringAsync()
                                );
                            }
                            else if(file_details.hcontent_file != 0)
                            {// TODO: Try something else.
                                await ContentDownloader.DownloadAppAsync(file_details.consumer_appid, new List<(uint depotId, ulong manifestId)> { new(file_details.consumer_appid, file_details.hcontent_file) }, "public", null, null, null, false, true);
                                if (ContentDownloader.UGCFilesDownloaded.ContainsKey(file_details.hcontent_file))
                                {
                                    var f = ContentDownloader.UGCFilesDownloaded[file_details.hcontent_file];
                                    File.Move(f.path, Path.Combine(Options.CacheOutDirectory, str_appid, $"{published_id}_{f.filename}"));
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Failed to download controller config {published_id}: {e.Message}");
                    }
                }
            }

            if (!Options.CacheOnly && !Options.ExcludeDlcs)
            {
                List<uint> dlc_list = new List<uint>();

                try
                {
                    JObject app_json = await GetWebAppDetails(appid.ToString());
                    app_json = (JObject)app_json[appid.ToString()];
                    if ((bool)app_json["success"])
                    {
                        if (((JObject)app_json["data"]).ContainsKey("dlc"))
                        {
                            foreach (string dlcid in (JArray)app_json["data"]["dlc"])
                            {
                                dlc_list.Add(uint.Parse(dlcid));
                            }
                        }
                    }
                }
                catch (Exception)
                { }

                if (app["extended"] != KeyValue.Invalid && app["extended"]["listofdlc"] != KeyValue.Invalid)
                {
                    foreach (string str_dlc_id in app["extended"]["listofdlc"].Value.Split(","))
                    {
                        uint dlc_id = uint.Parse(str_dlc_id);
                        if (!dlc_list.Contains(dlc_id))
                        {
                            dlc_list.Add(dlc_id);
                        }
                    }
                }

                foreach (uint dlc_id in dlc_list)
                {
                    if (!AppIds.ContainsKey(dlc_id) && !DoneAppIds.Contains(dlc_id))
                    {
                        AppIds.Add(dlc_id, null);
                        //if (!infos["Dlcs"].Contains(dlcid))
                        //{
                        //    infos["Dlcs"][dlcid] = new JObject
                        //    {
                        //        { "Name"    , $"Dlc{dlcid}" },
                        //        { "AppId"   , ulong.Parse(dlcid) },
                        //        { "ImageUrl", $"https://cdn.akamai.steamstatic.com/steam/apps/{dlcid}/header.jpg" },
                        //    };
                        //}
                    }
                }
            }

            if (!Options.CacheOnly)
            {
                await GenerateItemsFromSteamNetwork(appid);

                if (!await GenerateAchievementsFromSteamNetwork(appid))
                {
                    await GenerateAchievementsFromWebAPI(str_appid);
                }
            }
            else
            {
                try
                {
                    File.Copy(
                        Path.Combine(Options.CacheOutDirectory, appid.ToString(), "inventory_db.json"),
                        Path.Combine(Options.OutDirectory, appid.ToString(), "inventory_db.json")
                    );
                }
                catch (Exception)
                { }

                try
                {
                    KeyValue schema = new KeyValue();

                    using (FileStream fs = new FileStream(Path.Combine(Options.CacheOutDirectory, $"{appid}", "stats_schema.vdf"), FileMode.Open))
                    {
                        schema.ReadAsText(fs);
                    }

                    GenerateAchievementsFromKeyValue(schema, appid);
                }
                catch (Exception)
                { }
            }

            return true;
        }

        static bool ParseDlcDetails(JObject infos, uint appid, KeyValue app)
        {
            if (app["common"]["parent"].Value != null)
            {
                string str_main_appid = app["common"]["parent"].Value.Trim();
                uint main_appid = uint.Parse(str_main_appid);

                JObject main_app = GetOrCreateApp(str_main_appid, false);

                if (!AppIds.ContainsKey(main_appid) && !DoneAppIds.Contains(main_appid))
                {
                    AppIds.Add(main_appid, null);
                }

                infos["MainAppId"] = main_appid;
                // Add the dlc to its main game
                main_app["Dlcs"][appid.ToString()] = infos;

                // Only update main app if it already exists
                if (File.Exists(Path.Combine(Options.OutDirectory, str_main_appid, str_main_appid + ".json")))
                    SaveJson(Path.Combine(Options.OutDirectory, str_main_appid, str_main_appid + ".json"), main_app);
            }

            return true;
        }

        static async Task<bool> GetAppDetailsFromSteamNetwork(uint appid, KeyValue app)
        {
            string str_appid = appid.ToString();

            if (app == null)
                return false;

            string app_output_path = Path.Combine(Options.OutDirectory, str_appid, $"{str_appid}.json");
            if (!Options.Force && File.Exists(app_output_path))
            {
                Console.WriteLine($"Skipping {appid}");
                return true;
            }

            Console.WriteLine($"Parsing {appid}");

            if (!Options.CacheOnly)
            {
                try
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        app.SaveToStream(ms, false);
                        ms.Position = 0;

                        using (var sr = new StreamReader(ms))
                        {
                            SaveFile(Path.Combine(Options.CacheOutDirectory, appid.ToString(), "app_infos.vdf"), sr.ReadToEnd());
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Failed to save app_infos.vdf: {e.Message}");
                }
            }
            
            if (app["common"] != KeyValue.Invalid)
            {
                string app_type = app["common"]["type"].Value.Trim().ToLower();
                AppType type;
            
                switch (app_type)
                {
                    case "game": type = AppType.Game; break;
                    case "dlc": type = AppType.Dlc; break;
                    case "demo": type = AppType.Demo; break;
                    case "beta": type = AppType.Beta; break;
                    case "application": type = AppType.Application; break;
                    case "tool": type = AppType.Tool; break;
                    default: type = AppType.Other; break;
                        //Console.WriteLine($"Skipping app_type {app_type}");
                        //return true;
                }
            
                JObject infos = GetOrCreateApp(appid.ToString(), type == AppType.Dlc);

                ParseCommonDetails(infos, appid, app, app_type);
            
                switch(type)
                {
                    //case AppType.Other: ParseOtherDetails(); break;
                    case AppType.Dlc  : ParseDlcDetails(infos, appid, app); break;
                    case AppType.Application:
                    case AppType.Tool:
                    case AppType.Demo:
                    case AppType.Beta:
                    case AppType.Game : await ParseGameDetails(infos, appid, app); break;
                }
            
                SaveJson(app_output_path, infos);
            
                Console.WriteLine($"  \\ Type {app_type}, AppID {appid}, appName {(string)infos["Name"]}");
            }

            return true;
        }

        static async Task<JObject> GetWebAppDetails(string appid)
        {
            var response = await LimitSteamWebApiGET(WebHttpClient, new HttpRequestMessage(HttpMethod.Get, $"https://store.steampowered.com/api/appdetails/?appids={appid}&l=english"));

            return JObject.Parse(await response.Content.ReadAsStringAsync());
        }

        static async Task GetSteamAppids()
        {
            JObject json;

            if (Options.CacheOnly)
            {
                Console.WriteLine("No AppIDs specified but cache_only is set, reading only cached apps.");

                foreach(var entry in Directory.EnumerateDirectories(Options.CacheOutDirectory))
                {
                    try
                    {
                        AppIds.Add(uint.Parse(entry.Replace(Options.CacheOutDirectory + Path.DirectorySeparatorChar, null)), null);
                    }
                    catch(Exception)
                    { }
                }
            }
            else
            {
                Console.WriteLine("No AppIDs specified, trying to dump all games from Steam! If that's not what you intended, stop the script right now!");

                var response = await LimitSteamWebApiGET(WebHttpClient, new HttpRequestMessage(HttpMethod.Get, "https://api.steampowered.com/ISteamApps/GetAppList/v2/"));

                json = JObject.Parse(await response.Content.ReadAsStringAsync());

                foreach (JObject app in (JArray)json["applist"]["apps"])
                {
                    AppIds.Add((uint)app["appid"], null);
                }
            }
        }

        static async Task Main(string[] args)
        {
            Parser.Default.ParseArguments<ProgramOptions>(args).WithParsed(options => {
                Options = options;

                uint appid;
                foreach (var str_appid in options.AppIds)
                {
                    if (uint.TryParse(str_appid, out appid))
                    {
                        AppIds.Add(appid, null);
                    }
                }
            }).WithNotParsed(e => {
                Environment.Exit(0);
            });

            try
            {
                WebHttpClient.DefaultRequestHeaders.Clear();
                WebHttpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:102.0) Gecko/20100101 Firefox/102.0");
                WebHttpClient.DefaultRequestHeaders.TryAddWithoutValidation("Referer", "https://store.steampowered.com/");
                WebHttpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept-encoding", "gzip, deflate, br");

                if (AppIds.Count == 0)
                {
                    await GetSteamAppids();
                }

                Console.WriteLine(string.Format("Got {0} AppIDs to check", AppIds.Count));

                if (AppIds.Count > 0 && !string.IsNullOrWhiteSpace(Options.Username) && !string.IsNullOrWhiteSpace(Options.UserPassword))
                {
                    AccountSettingsStore.LoadFromFile("steam_retriever_cred.store");
                    if (Options.CacheOnly || ContentDownloader.InitializeSteam3(Options.Username, Options.UserPassword))
                    {
                        ContentDownloader.Config.MaxDownloads = 50;
                        ContentDownloader.Config.InstallDirectory = Path.Combine(Options.CacheOutDirectory, "depots");
                        if (!Options.CacheOnly)
                        {
                            IsAnonymous = ContentDownloader.steam3.steamClient.SteamID.AccountType != EAccountType.Individual;
                            WebSteamUser = WebAPI.GetInterface("ISteamUser", Options.WebApiKey);
                        }

                        while (AppIds.Count > 0)
                        {
                            var chunk = AppIds.Keys.Chunk(1000).First();
                            if (!Options.CacheOnly)
                            {
                                ContentDownloader.steam3.RequestAppsInfo(chunk, false);
                                foreach (var appid in chunk)
                                {
                                    AppIds[appid] = ContentDownloader.steam3.AppInfo[appid]?.KeyValues;
                                }
                            }
                            else
                            {
                                foreach (var appid in chunk)
                                {
                                    try
                                    {
                                        using (FileStream fs = new FileStream(Path.Combine(Options.CacheOutDirectory, $"{appid}", "app_infos.vdf"), FileMode.Open))
                                        {
                                            KeyValue appinfos = new KeyValue();
                                            if(appinfos.ReadAsText(fs))
                                                AppIds[appid] = appinfos;
                                        }
                                    }
                                    catch(Exception)
                                    { }
                                }
                            }

                            foreach (var appid in chunk)
                            {
                                await GetAppDetailsFromSteamNetwork(appid, AppIds[appid]);

                                if(!Options.CacheOnly)
                                    ContentDownloader.steam3.AppInfo.Remove(appid);

                                AppIds.Remove(appid);
                                DoneAppIds.Add(appid);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error {0}", e.Message);
            }

            if (WebSteamUser != null)
            {
                WebSteamUser.Dispose();
            }

            ContentDownloader.ShutdownSteam3();

            Console.WriteLine("Work done, exiting now.");
        }
    }

    

    public class ProgramOptions
    {
        [Option('u', "username", Required = false, HelpText = "The steam user.")]
        public string Username { get; set; } = string.Empty;

        [Option('p', "password", Required = false, HelpText = "The steam user password.")]
        public string UserPassword { get; set; } = string.Empty;

        [Option('l', "language", Required = false, HelpText = "Sets the output language (if available). Default value is english.")]
        public string Language { get; set; } = "english";

        [Option('k', "apikey", Required = false, HelpText = "Sets the WebAPI key. Used to access achievements and items definitions.")]
        public string WebApiKey { get; set; }

        [Option('i', "download-images", Required = false, HelpText = "Sets the flag to download achievements images (can take a lot of time, depending on how many achievements are available for the game). Images will not be downloaded if this flag is not specified.")]
        public bool DownloadImages { get; set; } = false;
        
        [Option('c', "controller-config", Required = false, HelpText = "Download all the configured controller configurations.")]
        public bool DownloadControllerConfigurations { get; set; } = false;

        [Option('d', "dlcs", Required = false, HelpText = "Do NOT retrieve dlcs when querying apps (Only match the appids in parameters).")]
        public bool ExcludeDlcs { get; set; } = false;

        [Option('f', "force", Required = false, HelpText = "Force to download game's infos (usefull if you want to refresh a game).")]
        public bool Force { get; set; } = false;

        [Option("cache-only", Required = false, HelpText = "Use the cached datas only, don't query Steam.")]
        public bool CacheOnly { get; set; } = false;

        [Option("cache-out", Required = false, HelpText = "Where to output the metadatas cache.")]
        public string CacheOutDirectory { get; set; } = "steam_cache";

        [Option('o', "out", Required = false, HelpText = "Where to output your game definitions. By default it will output to 'steam' directory alongside the executable.")]
        public string OutDirectory { get; set; } = "steam";

        [Value(0, Required = false, HelpText = "Any number of AppID to get their infos. Separate values by Space. If you don't pass any AppID, it will try to retrieve infos for all available Steam games.")]
        public IEnumerable<string> AppIds { get; set; }
    }
}