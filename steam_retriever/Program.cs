using System;
using System.Net;
using System.Text;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Threading;

using Newtonsoft.Json.Linq;
using CommandLine;
using Newtonsoft.Json;

namespace steam_db
{
    class Program
    {
        static string out_dir;
    static string webapi_key;
        static string language;
        static bool download_images;
        static bool force;

        static JObject games_infos = new JObject();
        static HashSet<string> appids = new HashSet<string>();
        static HashSet<string> done_appids = new HashSet<string>();

        static void GenerateAchievements(string appid)
        {
            if(string.IsNullOrEmpty(webapi_key))
            {
                Console.WriteLine("  + WebApi key is missing, the tool will not generate achievements list.");
                return;
            }

            try
            {
                int done = 0;
                string url = string.Format("http://api.steampowered.com/ISteamUserStats/GetSchemaForGame/v2/?l={0}&key={1}&appid={2}", language, webapi_key, appid);
                Console.WriteLine("  + Trying to retrieve achievements and stats...");

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Headers.Add("Accept-encoding:gzip, deflate, br");
                request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
                using (HttpWebResponse achievements_response = (HttpWebResponse)request.GetResponse())
                {
                    if (achievements_response.StatusCode == HttpStatusCode.OK)
                    {
                        using (Stream achievements_sresult = achievements_response.GetResponseStream())
                        {
                            using (StreamReader achievements_reader = new StreamReader(achievements_sresult))
                            {
                                string buffer = achievements_reader.ReadToEnd();

                                JObject achievements_json = JObject.Parse(buffer);

                                if (((JObject)achievements_json["game"]).ContainsKey("availableGameStats"))
                                {
                                    if(((JObject)achievements_json["game"]["availableGameStats"]).ContainsKey("achievements"))
                                    {
                                        Directory.CreateDirectory(Path.Combine(out_dir, appid));
    
                                        if(download_images)
                                        {
                                            Directory.CreateDirectory(Path.Combine(out_dir, appid, "achievements_images"));
                                            foreach (var achievement in (achievements_json["game"]["availableGameStats"]["achievements"]))
                                            {
                                                using (WebClient wc = new WebClient())
                                                {
                                                    string name = (string)achievement["name"];
                                                    string image_path = Path.Combine(out_dir, appid, "achievements_images", name + ".jpg");
                                                    if(!File.Exists(image_path))
                                                    {
                                                        Console.WriteLine(string.Format("   + Downloading achievement {0} unlocked icon...", name));
                                                        wc.DownloadFile((string)achievement["icon"], image_path);
                                                    }
                                                    image_path = Path.Combine(out_dir, appid, "achievements_images", name + "_gray.jpg");
                                                    if(!File.Exists(image_path))
                                                    {
                                                        Console.WriteLine(string.Format("   + Downloading achievement {0} locked icon...", name));
                                                        wc.DownloadFile((string)achievement["icongray"], image_path);
                                                    }
                                                }
                                            }
                                        }
    
                                        Console.WriteLine("  + Writing Achievements {0}.db_achievements.json", language);
                                        string achievements_file = Path.Combine(out_dir, appid, string.Format("{0}.db_achievements.json", language));
                                        done |= SaveJson(achievements_file, achievements_json["game"]["availableGameStats"]["achievements"]) ? 1 : 0;
                                        if( (done & 1) != 1 )
                                        {
                                            Console.WriteLine("  + Failed to save achievements.");
                                        }
                                    }
                                    if(((JObject)achievements_json["game"]["availableGameStats"]).ContainsKey("stats"))
                                    {
                                        Directory.CreateDirectory(Path.Combine(out_dir, appid));

                                        Console.WriteLine("  + Writing stats stats.json.");
                                        string stats_file = Path.Combine(out_dir, appid, "stats.json");
                                        done |= SaveJson(stats_file, achievements_json["game"]["availableGameStats"]["stats"]) ? 2 : 0;
                                        if( (done & 2) != 2 )
                                        {
                                            Console.WriteLine("  + Failed to save stats.");
                                        }
                                    }
                                }
                            }//using (StreamReader streamReader = new StreamReader(sresult))
                        }//using (Stream sresult = response.GetResponseStream())
                    }
                }//using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                if(done == 0)
                {
                    Console.WriteLine("  + No achievements or stats available for this AppID.");
                }
                else
                {
                    Console.WriteLine("  + Achievements and stats were successfully generated!");
                }
            }
            catch(Exception e)
            {
                Console.WriteLine("  + Failed (no achievements or stats?): {0}", e.Message);
            }
        }

        static void GenerateItems(string appid)
        {
            if(string.IsNullOrEmpty(webapi_key))
            {
                Console.WriteLine("  + WebApi key is missing, the tool will not generate items definitions.");
                return;
            }

            try
            {
                bool done = false;
                string url = string.Format("https://api.steampowered.com/IInventoryService/GetItemDefMeta/v1?key={0}&appid={1}", webapi_key, appid);
                JObject items_json;

                Console.WriteLine("  + Trying to retrieve items...");
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Headers.Add("Accept-encoding:gzip, deflate, br");
                request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        Stream sresult = response.GetResponseStream();
                        using (StreamReader reader = new StreamReader(sresult))
                        {
                            items_json = JObject.Parse(reader.ReadToEnd());

                            if (!string.IsNullOrWhiteSpace((string)items_json["response"]["digest"]))
                            {
                                url = string.Format("https://api.steampowered.com/IGameInventory/GetItemDefArchive/v0001?appid={0}&digest={1}", appid, (string)items_json["response"]["digest"]);

                                HttpWebRequest items_request = (HttpWebRequest)WebRequest.Create(url);
                                items_request.Headers.Add("Accept-encoding:gzip, deflate, br");
                                items_request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;

                                using (HttpWebResponse items_response = (HttpWebResponse)items_request.GetResponse())
                                {
                                    if (items_response.StatusCode == HttpStatusCode.OK)
                                    {
                                        Console.WriteLine("  + Writing Items db_inventory.json.");
                                        Stream items_result = items_response.GetResponseStream();
                                        using (StreamReader items_reader = new StreamReader(items_result))
                                        {
                                            JArray items = JArray.Parse(items_reader.ReadToEnd());
                                            string items_file = Path.Combine(out_dir, appid, "db_inventory.json");
                                            done = SaveJson(items_file, items);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                if(done)
                {
                    Console.WriteLine("  + Items list successfully generated!");
                }
                else
                {
                    Console.WriteLine("  + No items exist for this AppID");
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(" failed (no items?): {0}", e.Message);
            }
        }
        
        static JObject ParseDlc(string appid, string type, JObject json, out string main_appid)
        {
            main_appid = (string)json[appid]["data"]["fullgame"]["appid"];
            string name = (string)json[appid]["data"]["name"];
            string header_image = (string)json[appid]["data"]["header_image"];

            JObject infos = new JObject();
            infos["Name"] = name;
            infos["ImageUrl"] = header_image;
            infos["Type"] = type;

            Console.WriteLine(string.Format("  \\ Type {0}, DLCID {1}, DLCName {2}, MainAppID {3}", type, appid, name, main_appid));

            return infos;
        }

        static JObject ParseGame(string appid, string type, JObject json)
        {
            string languages = (string)json[appid]["data"]["supported_languages"];
            string name = (string)json[appid]["data"]["name"];
            string header_image = (string)json[appid]["data"]["header_image"];
            JObject dlcs = new JObject();

            try
            {
                if (((JObject)json[appid]["data"]).ContainsKey("dlc"))
                {
                    foreach (string dlcid in (JArray)json[appid]["data"]["dlc"])
                    {
                        JObject dlc_infos = new JObject();
                        dlc_infos.Add("Name", "");
                        dlc_infos.Add("ImageUrl", "");
                        dlcs.Add(dlcid, dlc_infos);
                        if (!done_appids.Contains(dlcid))
                        {
                            appids.Add(dlcid);
                        }
                    }
                }
            }
            catch(Exception)
            { }

            if (!string.IsNullOrWhiteSpace(languages))
            {
                int end_index = languages.IndexOf("<br>");
                if (end_index != -1)
                {
                    languages = languages.Remove(end_index);
                }

                languages = languages.Replace("<strong>*</strong>", "");
                languages = languages.Replace(", ", ",");
                languages = languages.Replace("Portuguese - Brazil", "Portuguese");
                languages = languages.Replace("Spanish - Spain", "Spanish");
                languages = languages.Replace("Spanish - Latin America", "Latam");
                languages = languages.Replace("Traditional Chinese", "tchinese");
                languages = languages.Replace("Simplified Chinese", "schinese");
                languages = languages.ToLower();
                languages = languages.Trim();
            }
            else
            {
                languages = "english";
            }

            JObject platforms = (JObject)json[appid]["data"]["platforms"];

            if (!games_infos.ContainsKey(appid))
            {
                games_infos.Add(appid, new JObject());
            }

            using (WebClient wc = new WebClient())
            {
                string image_path = Path.Combine(out_dir, appid, "background.jpg");
                if(!File.Exists(image_path))
                {
                    Console.WriteLine(string.Format("   + Downloading background image"));
                    wc.DownloadFile(header_image, image_path);
                }
            }

            GenerateItems(appid);
            GenerateAchievements(appid);

            JObject infos = GetOrCreateApp(appid, false);
            infos["Name"] = name;
            infos["AppId"] = appid;
            infos["ImageUrl"] = header_image;
            infos["Languages"] = JArray.FromObject(languages.Split(","));
            infos["Platforms"] = platforms;
            infos["Dlcs"] = dlcs;

            Console.WriteLine(string.Format("  \\ Type {0}, AppID {1}, appName {2}", type, appid, name));

            return infos;
        }

        static JObject ParseOther(string appid, string type, JObject json, out string main_appid)
        {
            main_appid = string.Empty;
            if(((JObject)json[appid]["data"]).ContainsKey("fullgame"))
            {
                if(((JObject)json[appid]["data"]["fullgame"]).ContainsKey("appid"))
                {
                    main_appid = (string)json[appid]["data"]["fullgame"]["appid"];
                }
            }
            string name = (string)json[appid]["data"]["name"];
            string header_image = (string)json[appid]["data"]["header_image"];

            JObject infos = new JObject();
            infos["Name"] = name;
            infos["ImageUrl"] = header_image;
            infos["Type"] = type;

            Console.WriteLine(string.Format("  \\ Type {0}, ID {1}, Name {2}", type, appid, name));

            return infos;
        }

        static JObject GetOrCreateApp(string appid, bool is_dlc)
        {
            if (!games_infos.ContainsKey(appid))
            {
                string infos_file = Path.Combine(out_dir, appid, appid + ".json");
                try
                {
                    using (StreamReader reader = new StreamReader(new FileStream(infos_file, FileMode.Open), new UTF8Encoding(false)))
                    {
                        games_infos.Add(appid, JObject.Parse(reader.ReadToEnd()));
                    }
                }
                catch(Exception)
                {
                    games_infos.Add(appid, new JObject());
                }
            }
            
            JObject app = (JObject)games_infos[appid];
            if(!is_dlc && !app.ContainsKey("Dlcs"))
            {
                app["Dlcs"] = new JObject();
            }
            return app;
        }

        static bool SaveJson(string file_path, JToken json)
        {
            try
            {
                string save_dir = Path.GetDirectoryName(file_path);
                if(!Directory.Exists(save_dir))
                {
                    Directory.CreateDirectory(save_dir);
                }
                using (StreamWriter streamWriter = new StreamWriter(new FileStream(file_path, FileMode.Create), new UTF8Encoding(false)))
                {
                    string buffer = Newtonsoft.Json.JsonConvert.SerializeObject(json, Newtonsoft.Json.Formatting.Indented);
                    streamWriter.Write(buffer);
                }
                return true;
            }
            catch(Exception e)
            {
                Console.WriteLine("Failed to save json: {0}", e.Message);
            }
            return false;
        }

        static void Main(string[] args)
        {
            JObject json;

            Parser.Default.ParseArguments<Options>(args).WithParsed(options => {
                out_dir = options.OutDirectory;
                webapi_key = options.ApiKey;
                language = options.Language;
                download_images = options.DownloadImages;
                force = options.Force;

                ulong test;
                foreach ( var appid in options.AppIds )
                {
                    if(ulong.TryParse(appid, out test))
                    {
                        appids.Add(appid);
                    }
                }
            }).WithNotParsed(e => {
                Environment.Exit(0);
            });

            try
            {
                HttpWebRequest request;

                if(appids.Count == 0)
                {
                    Console.WriteLine("No AppIDs specified, trying to dump all games from Steam! If that's not what you intended, stop the script right now!");

                    string url = "https://api.steampowered.com/ISteamApps/GetAppList/v2/";
                    request = (HttpWebRequest)WebRequest.Create(url);
                    request.Headers.Add("Accept-encoding:gzip, deflate, br");
                    request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    {
                        Stream sresult = response.GetResponseStream();
                        using (StreamReader streamReader = new StreamReader(sresult))
                        {
                            json = JObject.Parse(streamReader.ReadToEnd());
                            foreach( JObject app in (JArray)json["applist"]["apps"] )
                            {
                                appids.Add(((long)app["appid"]).ToString());
                            }
                        }
                    }
                }
                
                Console.WriteLine(string.Format("Got {0} AppIDs to check", appids.Count));

                int error_count = 0;
                const int max_error_count = 5;
                string appid;
                while (appids.Count > 0)
                {
                    appid = appids.Last();

                    if (File.Exists(Path.Combine(out_dir, appid, appid + ".json")) && !force)
                    {
                        Console.WriteLine(string.Format(" + {0} already present, skip (no -f).", appid));
                        appids.Remove(appid);
                        done_appids.Add(appid);
                        error_count = 0;

                        continue;
                    }

                    // Always sleep for some time before retrieving any game or dlc or Steam will lock you up for some time.
                    Thread.Sleep(1500);

                    try 
                    {
                        Console.WriteLine(string.Format(" + Trying to get infos on {0}...", appid));
                        request = (HttpWebRequest)WebRequest.Create(string.Format("https://store.steampowered.com/api/appdetails/?appids={0}&l=english", appid));
                        request.Headers.Add("Accept-encoding:gzip, deflate, br");
                        request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
                        using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                        {
                            Stream sresult = response.GetResponseStream();
                            using (StreamReader streamReader = new StreamReader(sresult))
                            {
                                try
                                {
                                    JObject app_json = JObject.Parse(streamReader.ReadToEnd());
                                    if (!(bool)app_json[appid]["success"])
                                    {
                                        Console.WriteLine(" + Failed (success == false)");
                                        appids.Remove(appid);
                                        done_appids.Add(appid);
                                        error_count = 0;
                                        continue;
                                    }

                                    string type = (string)app_json[appid]["data"]["type"];
                                    if (!Directory.Exists(Path.Combine(out_dir, appid)))
                                    {
                                        Directory.CreateDirectory(Path.Combine(out_dir, appid));
                                    }
    
                                    switch (type)
                                    {
                                        case "music":
                                        case "dlc":
                                        {
                                            string main_appid;
                                            JObject dlc = ParseDlc(appid, type, app_json, out main_appid);
                                            JObject app = GetOrCreateApp(main_appid, false);
    
                                            if(!done_appids.Contains(main_appid))
                                            {
                                                 appids.Add(main_appid);
                                            }
                                        
                                            // Add the dlc to the games_infos
                                            games_infos[appid] = dlc;
                                            // Add the dlc to its main game
                                            app["Dlcs"][appid] = dlc;
    
                                            SaveJson(Path.Combine(out_dir, appid, appid + ".json"), dlc);
                                            SaveJson(Path.Combine(out_dir, main_appid, main_appid + ".json"), app);
                                        }
                                        break;
    
                                        case "game":
                                        {
                                            JObject game = ParseGame(appid, type, app_json);
                                            games_infos[appid] = game;

                                            SaveJson(Path.Combine(out_dir, appid, appid + ".json"), game);
                                        }
                                        break;
    
                                        default:
                                        {
                                            string main_appid;
                                            JObject other = ParseOther(appid, type, app_json, out main_appid);
                                            games_infos[appid] = other;
    
                                            if (!string.IsNullOrWhiteSpace(main_appid))
                                            {
                                                JObject app = GetOrCreateApp(main_appid, false);
                                                ((JObject)app["Dlcs"]).Remove(appid);
                                                SaveJson(Path.Combine(out_dir, main_appid, main_appid + ".json"), app);
                                            }
    
                                            SaveJson(Path.Combine(out_dir, appid, appid + ".json"), other);
                                        }
                                        break;
                                    }
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine("Error while parsing AppID {0}: {1}", appid, e.Message);
                                }
                                appids.Remove(appid);
                                done_appids.Add(appid);
                                error_count = 0;
                            }
                        }
                    }
                    catch(Exception e)
                    {
                        if (File.Exists(Path.Combine(out_dir, appid, appid + ".json")))
                            File.Delete(Path.Combine(out_dir, appid, appid + ".json"));

                        Console.WriteLine("Error {0}", e.ToString());
                        if (error_count++ >= max_error_count)
                        {
                            break;
                        }
                        Thread.Sleep(5);
                    }
                }

            }
            catch(Exception e)
            {
                Console.WriteLine("Error {0}", e.Message);
            }
        }
    }

    public class Options
    {
        [Option('l', "language", Required = false, HelpText = "Sets the output language (if available). Default value is english.")]
        public string Language { get; set; } = "english";

        [Option('k', "apikey", Required = false, HelpText = "Sets the WebAPI key. Used to access achievements and items definitions.")]
        public string ApiKey { get; set; }

        [Option('i', "download_images", Required = false, HelpText = "Sets the flag to download achievements images (can take a lot of time, depending on how many achievements are available for the game). Images will not be downloaded if this flag is not specified.")]
        public bool DownloadImages { get; set; } = false;

        [Option('f', "force", Required = false, HelpText = "Force to download game's infos (usefull if you want to refresh a game).")]
        public bool Force { get; set; } = false;

        [Option('o', "out", Required = false, HelpText = "Where to output your game definitions. By default it will output to 'steam' directory alongside the executable.")]
        public string OutDirectory { get; set; } = "steam";

        [Value(0, Required = false, HelpText = "Any number of AppID to get their infos. Separate values by Space. If you don't pass any AppID, it will try to retrieve infos for all available Steam games.")]
        public IEnumerable<string> AppIds { get; set; }
    }
}
