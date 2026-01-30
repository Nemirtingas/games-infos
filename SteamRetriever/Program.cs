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
using log4net;
using log4net.Layout;
using SteamRetriever.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;

namespace SteamRetriever;

class StoreServiceApplicationModel
{
    [JsonProperty("appid")]
    public uint Appid { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("last_modified")]
    public ulong LastModified { get; set; }

    [JsonProperty("price_change_number")]
    public ulong PriceChangeNumber { get; set; }
}

class StoreServiceApplicationResponseModel
{
    [JsonProperty("apps")]
    public List<StoreServiceApplicationModel> Applications { get; set; }

    [JsonProperty("have_more_results")]
    public bool? HaveMoreResults { get; set; }

    [JsonProperty("last_appid")]
    public uint? LastAppid { get; set; }
}

class StoreServiceResponseModel
{
    [JsonProperty("response")]
    public StoreServiceApplicationResponseModel Response { get; set; }
}

enum SchemaStatType
{
    Int = 1,
    Float = 2,
    AvgRate = 3,
    Bits = 4, // Achievements
}

enum AppType
{
    [EnumMember(Value = "Other")]
    Other = 0,
    [EnumMember(Value = "Game")]
    Game = 1,
    [EnumMember(Value = "Dlc")]
    Dlc = 2,
    [EnumMember(Value = "Application")]
    Application = 3,
    [EnumMember(Value = "Tool")]
    Tool = 4,
    [EnumMember(Value = "Demo")]
    Demo = 5,
    [EnumMember(Value = "Beta")]
    Beta = 6,
}

class GameInfoControllerModel
{
    [JsonProperty("Type")]
    public string Type { get; set; }
}

class GameInfoApplicationModel
{
    [JsonProperty("Name")]
    public string Name { get; set; }

    [JsonProperty("AppId")]
    public uint AppId { get; set; }

    [JsonProperty("Type")]
    [JsonConverter(typeof(StringEnumConverter))]
    public AppType Type { get; set; }

    [JsonProperty("ImageUrl")]
    public string ImageUrl { get; set; }

    [JsonProperty("MainAppId")]
    public uint? MainAppId { get; set; }

    [JsonProperty("Languages")]
    public List<string> Languages { get; set; }

    public Dictionary<ulong, GameInfoControllerModel> ControllerConfigurations { get; set; }

    [JsonProperty("Dlcs")]
    public Dictionary<uint, GameInfoApplicationModel> Dlcs { get; set; }
}

class Program
{
    readonly object ExitCondVar = new object();

    public static Program Instance { get; private set; }
    public ILog _logger;

    ProgramOptions Options;

    Dictionary<uint, GameInfoApplicationModel> GamesInfos = new();
    Dictionary<uint, KeyValue> AppIds = new Dictionary<uint, KeyValue>();
    HashSet<uint> DoneAppIds = new HashSet<uint>();
    
    HttpClient WebHttpClient = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.All });

    MetadataDatabase MetadataDatabase = new MetadataDatabase();

    dynamic WebSteamUser = null;
    bool IsAnonymous = true;
    DateTime LastWebRequestTime = new DateTime();

    async Task<HttpResponseMessage> LimitSteamWebApiGET(HttpClient http_client, HttpRequestMessage http_request, CancellationTokenSource cts = null)
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

    async Task DownloadAchievementIcon(string appid, string ach_name, Uri url)
    {
        string image_path = Path.Combine(Options.OutDirectory, appid, "achievements_images", ach_name + ".jpg");

        Directory.CreateDirectory(Path.Combine(Options.OutDirectory, appid, "achievements_images"));

        if (!File.Exists(image_path))
        {
            _logger.Info($"   + Downloading achievement {ach_name} icon...");
            var response = await LimitSteamWebApiGET(WebHttpClient, new HttpRequestMessage(HttpMethod.Get, url));

            using (BinaryWriter streamWriter = new BinaryWriter(new FileStream(image_path, FileMode.Create), new UTF8Encoding(false)))
            {
                streamWriter.Write(await response.Content.ReadAsByteArrayAsync());
            }
        }
    }

    async Task GenerateAchievementsFromWebAPI(string appid)
    {
        if (string.IsNullOrEmpty(Options.WebApiKey))
        {
            _logger.Info("  + WebApi key is missing, the tool will not generate achievements list.");
            return;
        }

        try
        {
            int done = 0;
            _logger.Info("  + Trying to retrieve achievements and stats...");

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
                                await DownloadAchievementIcon(appid, (string)achievement["name"], new Uri((string)achievement["icon"]));
                                await DownloadAchievementIcon(appid, (string)achievement["name"] + "_locked", new Uri((string)achievement["icongray"]));
                            }
                        }

                        done |= await SaveAchievementsToFileAsync(appid, achievements) ? 1 : 0;
                        if ((done & 1) != 1)
                        {
                            _logger.Info("  + Failed to save achievements.");
                        }
                    }
                    if (((JObject)achievements_json["game"]["availableGameStats"]).ContainsKey("stats"))
                    {
                        Directory.CreateDirectory(Path.Combine(Options.OutDirectory, appid));

                        done |= await SaveStatsToFileAsync(appid, achievements_json["game"]["availableGameStats"]["stats"]) ? 2 : 0;
                        if ((done & 2) != 2)
                        {
                            _logger.Info("  + Failed to save stats.");
                        }
                    }
                }
            }//using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            if (done == 0)
            {
                _logger.Info("  + No achievements or stats available for this AppID.");
            }
            else
            {
                _logger.Info("  + Achievements and stats were successfully generated!");
            }
        }
        catch (Exception e)
        {
            _logger.Error($"  + Failed (no achievements or stats?): {e.Message}");
        }
    }

    async Task<bool> GenerateAchievementsFromKeyValue(KeyValue schema, uint appid)
    {
        JArray achievements_array = new JArray();
        JArray stats_array = new JArray();

        string str_appid = appid.ToString();

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
                                    _logger.Error($"Unknown operation: {kv.Value}");
                                }
                            }
                            else if (kv.Name.Contains("operand"))
                            {
                                if (stats_thresholds.ContainsKey("stat_name"))
                                {
                                    _logger.Error($"Error, multiple operands in the progress object: {kv.Name}:{kv.Value}");
                                }
                                else
                                {
                                    stats_thresholds["stat_name"] = kv.Value;
                                }
                            }
                            else
                            {
                                _logger.Info($"Unhandled progression Key {kv.Name}");
                            }
                        }

                        if (add_threshold)
                        {
                            achievement_json["stats_thresholds"] = stats_thresholds;
                        }
                    }

                    if (Options.DownloadImages)
                    {
                        await DownloadAchievementIcon(str_appid, (string)achievement_json["name"], new Uri((string)achievement_json["icon"]));
                        await DownloadAchievementIcon(str_appid, (string)achievement_json["name"] + "_locked", new Uri((string)achievement_json["icongray"]));
                    }

                    achievements_array.Add(achievement_json);
                }
            }
            else
            {// Parse stats
                string display_name = string.Empty;

                var v = stats_object["display"]["name"];
                if (v != KeyValue.Invalid && !string.IsNullOrWhiteSpace(v.Value))
                {
                    display_name = v.Value;
                }

                JObject stat_obj = new JObject
                {
                    { "name"         , stats_object["name"].Value },
                    { "displayName"  , display_name },
                    { "type"         , ((SchemaStatType)stats_object["type"].AsLong()).ToString().ToLower() },
                    { "default"      , 0 },
                    { "incrementonly", false },
                    { "aggregated"   , false },
                };

                v = stats_object["incrementonly"];
                if(v != KeyValue.Invalid && ulong.Parse(v.Value) == 1)
                {
                    stat_obj["incrementonly"] = true;
                }

                v = stats_object["aggregated"];
                if (v != KeyValue.Invalid && ulong.Parse(v.Value) == 1)
                {
                    stat_obj["aggregated"] = true;
                }

                switch ((SchemaStatType)stats_object["type"].AsLong())
                {
                    case SchemaStatType.Int:
                        v = stats_object["maxchange"];
                        if (v != KeyValue.Invalid)
                        {
                            if (ulong.TryParse(v.Value, out var ulValue))
                            {
                                stat_obj["maxchange"] = ulValue;
                            }
                            else if (long.TryParse(v.Value, out var lValue))
                            {
                                stat_obj["maxchange"] = lValue;
                            }
                            else if (double.TryParse(v.Value, CultureInfo.InvariantCulture, out var dValue))
                            {
                                stat_obj["maxchange"] = (long)dValue;
                            }
                        }

                        v = stats_object["min"];
                        if (v != KeyValue.Invalid)
                        {
                            if (ulong.TryParse(v.Value, out var ulValue))
                            {
                                stat_obj["min"] = ulValue;
                            }
                            else if(long.TryParse(v.Value, out var lValue))
                            {
                                stat_obj["min"] = lValue;
                            }
                            else if(double.TryParse(v.Value, CultureInfo.InvariantCulture, out var dValue))
                            {
                                stat_obj["min"] = (long)dValue;
                            }
                        }

                        v = stats_object["max"];
                        if (v != KeyValue.Invalid)
                        {
                            if (ulong.TryParse(v.Value, out var ulValue))
                            {
                                stat_obj["max"] = ulValue;
                            }
                            else if (long.TryParse(v.Value, out var lValue))
                            {
                                stat_obj["max"] = lValue;
                            }
                            else if (double.TryParse(v.Value, CultureInfo.InvariantCulture, out var dValue))
                            {
                                stat_obj["max"] = (long)dValue;
                            }
                        }

                        v = stats_object["default"];
                        if (v != KeyValue.Invalid)
                        {
                            if (ulong.TryParse(v.Value, out var ulValue))
                            {
                                stat_obj["default"] = ulValue;
                            }
                            else if (long.TryParse(v.Value, out var lValue))
                            {
                                stat_obj["default"] = lValue;
                            }
                            else if (double.TryParse(v.Value, CultureInfo.InvariantCulture, out var dValue))
                            {
                                stat_obj["default"] = (long)dValue;
                            }
                        }
                        break;

                    case SchemaStatType.Float:
                        v = stats_object["maxchange"];
                        if (v != KeyValue.Invalid)
                        {
                            if (ulong.TryParse(v.Value, out var ulValue))
                            {
                                stat_obj["maxchange"] = (double)ulValue;
                            }
                            else if (long.TryParse(v.Value, out var lValue))
                            {
                                stat_obj["maxchange"] = (double)lValue;
                            }
                            else if (double.TryParse(v.Value, CultureInfo.InvariantCulture, out var dValue))
                            {
                                stat_obj["maxchange"] = dValue;
                            }
                        }

                        v = stats_object["min"];
                        if (v != KeyValue.Invalid)
                        {
                            if (ulong.TryParse(v.Value, out var ulValue))
                            {
                                stat_obj["min"] = (double)ulValue;
                            }
                            else if (long.TryParse(v.Value, out var lValue))
                            {
                                stat_obj["min"] = (double)lValue;
                            }
                            else if (double.TryParse(v.Value, CultureInfo.InvariantCulture, out var dValue))
                            {
                                stat_obj["min"] = dValue;
                            }
                        }

                        v = stats_object["max"];
                        if (v != KeyValue.Invalid)
                        {
                            if (ulong.TryParse(v.Value, out var ulValue))
                            {
                                stat_obj["max"] = ulValue;
                            }
                            else if (long.TryParse(v.Value, out var lValue))
                            {
                                stat_obj["max"] = lValue;
                            }
                            else if (double.TryParse(v.Value, CultureInfo.InvariantCulture, out var dValue))
                            {
                                stat_obj["max"] = dValue;
                            }
                        }

                        v = stats_object["default"];
                        if (v != KeyValue.Invalid)
                        {
                            if (ulong.TryParse(v.Value, out var ulValue))
                            {
                                stat_obj["default"] = (double)ulValue;
                            }
                            else if (long.TryParse(v.Value, out var lValue))
                            {
                                stat_obj["default"] = (double)lValue;
                            }
                            else if (double.TryParse(v.Value, CultureInfo.InvariantCulture, out var dValue))
                            {
                                stat_obj["default"] = dValue;
                            }
                        }
                        break;

                    case SchemaStatType.AvgRate:
                        v = stats_object["default"];
                        if (v != KeyValue.Invalid)
                        {
                            if (ulong.TryParse(v.Value, out var ulValue))
                            {
                                stat_obj["default"] = (double)ulValue;
                            }
                            else if (long.TryParse(v.Value, out var lValue))
                            {
                                stat_obj["default"] = (double)lValue;
                            }
                            else if (double.TryParse(v.Value, CultureInfo.InvariantCulture, out var dValue))
                            {
                                stat_obj["default"] = dValue;
                            }
                        }

                        v = stats_object["maxchange"];
                        if (v != KeyValue.Invalid)
                        {
                            if (ulong.TryParse(v.Value, out var ulValue))
                            {
                                stat_obj["maxchange"] = (double)ulValue;
                            }
                            else if (long.TryParse(v.Value, out var lValue))
                            {
                                stat_obj["maxchange"] = (double)lValue;
                            }
                            else if (double.TryParse(v.Value, CultureInfo.InvariantCulture, out var dValue))
                            {
                                stat_obj["maxchange"] = dValue;
                            }
                        }

                        v = stats_object["min"];
                        if (v != KeyValue.Invalid)
                        {
                            if (ulong.TryParse(v.Value, out var ulValue))
                            {
                                stat_obj["min"] = (double)ulValue;
                            }
                            else if (long.TryParse(v.Value, out var lValue))
                            {
                                stat_obj["min"] = (double)lValue;
                            }
                            else if (double.TryParse(v.Value, CultureInfo.InvariantCulture, out var dValue))
                            {
                                stat_obj["min"] = dValue;
                            }
                        }

                        v = stats_object["max"];
                        if (v != KeyValue.Invalid)
                        {
                            if (ulong.TryParse(v.Value, out var ulValue))
                            {
                                stat_obj["max"] = ulValue;
                            }
                            else if (long.TryParse(v.Value, out var lValue))
                            {
                                stat_obj["max"] = lValue;
                            }
                            else if (double.TryParse(v.Value, CultureInfo.InvariantCulture, out var dValue))
                            {
                                stat_obj["max"] = dValue;
                            }
                        }

                        v = stats_object["windowsize"];
                        if (v != KeyValue.Invalid)
                        {
                            if (ulong.TryParse(v.Value, out var ulValue))
                            {
                                stat_obj["windowsize"] = (double)ulValue;
                            }
                            else if (long.TryParse(v.Value, out var lValue))
                            {
                                stat_obj["windowsize"] = (double)lValue;
                            }
                            else if (double.TryParse(v.Value, CultureInfo.InvariantCulture, out var dValue))
                            {
                                stat_obj["windowsize"] = dValue;
                            }
                        }
                        break;
                }

                stats_array.Add(stat_obj);
            }
        }

        await SaveAchievementsToFileAsync(appid.ToString(), achievements_array);
        await SaveStatsToFileAsync(appid.ToString(), stats_array);

        return true;
    }

    async Task<bool> GenerateAchievementsFromSteamNetwork(uint appid)
    {
        if (IsAnonymous)
            return false;

        var reviewersIds = await GetAppPublicSteamIDs(appid, 15);

        if (reviewersIds.Count == 0)
            return false;

        foreach (var steamId in reviewersIds)
        {
            var result = await ContentDownloader.steam3.GetUserStats(appid, steamId);
            try
            {
                if (result.Result == EResult.OK && result.Schema != null)
                {
                    using(MemoryStream ms = new MemoryStream())
                    {
                        result.Schema.SaveToStream(ms, false);
                        ms.Position = 0;

                        UpdateMetadataDatabaseStatsSteamId(appid, steamId);

                        using (var sr = new StreamReader(ms))
                        {
                            await SaveFileAsync(Path.Combine(Options.CacheOutDirectory, appid.ToString(), "stats_schema.vdf"), sr.ReadToEnd());
                        }
                    }

                    return await GenerateAchievementsFromKeyValue(result.Schema, appid);
                }
            }
            catch (Exception)
            { }
        }

        _logger.Info("  + No achievements or stats available for this AppID.");
        return false;
    }

    async Task<bool> GetItemsDef(uint appid, string digest)
    {
        var response = await LimitSteamWebApiGET(
            WebHttpClient,
            new HttpRequestMessage(HttpMethod.Get, $"https://api.steampowered.com/IGameInventory/GetItemDefArchive/v0001?appid={appid}&digest={digest}"));

        if (response.StatusCode == HttpStatusCode.OK)
        {
            JArray items = JArray.Parse(await response.Content.ReadAsStringAsync());
            if (items.Count > 0)
            {
                await SaveJsonAsync(Path.Combine(Options.CacheOutDirectory, appid.ToString(), "inventory_db.json"), items);
                return await SaveJsonAsync(Path.Combine(Options.OutDirectory, appid.ToString(), "inventory_db.json"), items);
            }
        }

        return false;
    }

    async Task<bool> GenerateItemsFromSteamNetwork(uint appid)
    {
        try
        {
            string digest = await ContentDownloader.steam3.GetInventoryDigest(appid);
            if (!string.IsNullOrWhiteSpace(digest))
            {
                if (await GetItemsDef(appid, digest))
                {
                    _logger.Info("  + Items list successfully generated!");
                }
                else
                {
                    _logger.Info("  + No items exist for this AppID");
                }
                return true;
            }
        }
        catch (TaskCanceledException)
        {
            throw;
        }
        catch (Exception e)
        {
            _logger.Error($" failed (no items?): {e.Message}");
        }

        return false;
    }

    async Task<List<ulong>> GetAppPublicSteamIDs(long appid, int maxIdCount)
    {
        var publicSteamIds = new List<ulong>
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
            if (MetadataDatabase.ApplicationDetails.TryGetValue(appid, out var application) && application.PublicSteamIdForStats != null)
            {
                KeyValue kvUser = WebSteamUser.GetPlayerSummaries2(steamids: application.PublicSteamIdForStats.Value);

                foreach (KeyValue v in kvUser["players"].Children)
                {
                    if (v["communityvisibilitystate"].AsLong() == 3)
                        return new List<ulong> {application.PublicSteamIdForStats.Value };
                }
            }

            var response = await LimitSteamWebApiGET(
                WebHttpClient,
                new HttpRequestMessage(HttpMethod.Get, $"https://store.steampowered.com/appreviews/{appid}?json=1&cursor=*&start_date=-1&end_date=-1date_range_type=all&filter=summary&language=all&review_type=all&purchase_type=all&playtime_filter_min=0&playtime_filter_max=0"));

            var reviews = JObject.Parse(await response.Content.ReadAsStringAsync());

            if (reviews["success"] != null && (int)reviews["success"] == 1 && reviews["reviews"] != null)
            {
                foreach (var entry in reviews["reviews"])
                {
                    if (publicSteamIds.Count >= maxIdCount)
                        break;

                    string userSteamId = (string)entry["author"]["steamid"];
                    KeyValue kvUser = WebSteamUser.GetPlayerSummaries2(steamids: userSteamId);

                    foreach (KeyValue v in kvUser["players"].Children)
                    {
                        if (v["communityvisibilitystate"].AsLong() == 3)
                        {
                            publicSteamIds.Add(ulong.Parse(userSteamId));
                        }
                    }
                }
            }
        }
        catch(Exception)
        { }

        return publicSteamIds;
    }

    async Task<bool> SaveAchievementsToFileAsync(string appid, JToken json)
    {
        if (((JArray)json).Count <= 0)
        {
            _logger.Info("  + No Achievements for this app");
            return true;
        }

        _logger.Info("  + Writing Achievements achievements_db.json");
        return await SaveJsonAsync(Path.Combine(Options.OutDirectory, appid, "achievements_db.json"), json);
    }

    async Task<bool> SaveStatsToFileAsync(string appid, JToken json)
    {
        if (((JArray)json).Count <= 0)
        {
            _logger.Info("  + No Stats for this app");
            return true;
        }
        _logger.Info("  + Writing stats stats_db.json.");
        return await SaveJsonAsync(Path.Combine(Options.OutDirectory, appid, "stats_db.json"), json);
    }

    GameInfoApplicationModel GetOrCreateApp(uint appid, AppType appType)
    {
        if (!GamesInfos.ContainsKey(appid))
        {
            string infos_file = Path.Combine(Options.OutDirectory, $"{appid}", $"{appid}.json");
            try
            {
                using (StreamReader reader = new StreamReader(new FileStream(infos_file, FileMode.Open), new UTF8Encoding(false)))
                {
                    GamesInfos.Add(appid, JsonConvert.DeserializeObject<GameInfoApplicationModel>(reader.ReadToEnd()));
                }
            }
            catch (Exception)
            {
                GamesInfos.Add(appid, new()
                {
                    AppId = appid,
                    Type = appType,
                });
            }
        }

        var app = GamesInfos[appid];
        if (appType != AppType.Dlc && app.Dlcs == null)
            app.Dlcs = new();

        return app;
    }

    async Task<bool> SaveJsonAsync(string file_path, object data)
    {
        return await SaveFileAsync(file_path, Newtonsoft.Json.JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented));
    }

    async Task<bool> SaveFileAsync(string file_path, string data)
    {
        try
        {
            var saveDirectory = Path.GetDirectoryName(file_path);
            if (!string.IsNullOrWhiteSpace(saveDirectory) && !Directory.Exists(saveDirectory))
            {
                Directory.CreateDirectory(saveDirectory);
            }
            using (StreamWriter streamWriter = new StreamWriter(new FileStream(file_path, FileMode.Create), new UTF8Encoding(false)))
            {
                await streamWriter.WriteAsync(data);
            }
            return true;
        }
        catch (Exception e)
        {
            _logger.Error($"Failed to save json: {e.Message}");
        }
        return false;
    }

    bool ParseCommonDetails(GameInfoApplicationModel infos, uint appid, KeyValue app, AppType type)
    {
        string app_name = app["common"]["name"].Value;

        infos.Name = app_name;
        infos.AppId = appid;
        infos.Type = type;

        foreach (var image in app["common"]["header_image"].Children)
        {
            if (image.Name == "english")
            {
                infos.ImageUrl = $"https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/{appid}/{image.Value}";
                break;
            }

            infos.ImageUrl = $"https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/{appid}/{image.Value}";
        }

        return true;
    }

    async Task<bool> ParseGameDetails(GameInfoApplicationModel infos, uint appid, KeyValue app)
    {
        string str_appid = appid.ToString();

        if (app["common"]["supported_languages"].Children.Count > 0)
        {
            infos.Languages = new List<string>();
            foreach (var language in app["common"]["supported_languages"].Children)
            {
                try
                {
                    bool b;
                    int i;
                    if ((bool.TryParse(language["supported"].AsString(), out b) && b) || (int.TryParse(language["supported"].AsString(), out i) && i != 0))
                    {
                        infos.Languages.Add(language.Name.Trim().ToLower());
                    }
                }
                catch (Exception)
                { }
            }
        }

        if (app["config"]["steamcontrollerconfigdetails"].Children.Count > 0)
        {
            infos.ControllerConfigurations = new();
            foreach (var controller_config in app["config"]["steamcontrollerconfigdetails"].Children)
            {
                string published_id = "0";
                try
                {
                    published_id = controller_config.Name.Trim();

                    infos.ControllerConfigurations.Add(ulong.Parse(published_id), new GameInfoControllerModel{
                        Type = controller_config["controller_type"].Value.Trim().ToLower()
                    });

                    if (!Options.CacheOnly && Options.DownloadControllerConfigurations)
                    {
                        var file_details = await ContentDownloader.steam3.GetPublishedFileDetails(null, ulong.Parse(published_id));
                        if (!string.IsNullOrWhiteSpace(file_details.filename) && !string.IsNullOrWhiteSpace(file_details.file_url))
                        {
                            CancellationTokenSource cts = new CancellationTokenSource();

                            string controller_path = Path.Combine(Options.CacheOutDirectory, str_appid, file_details.filename);
                            _logger.Info($"  + Saved controller file {controller_path}");

                            await SaveFileAsync(
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
                                File.Move(f.Path, Path.Combine(Options.CacheOutDirectory, str_appid, $"{published_id}_{f.FileName}"));
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.Error($"Failed to download controller config {published_id}: {e.Message}");
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

                await GenerateAchievementsFromKeyValue(schema, appid);
            }
            catch (Exception)
            { }
        }

        return true;
    }

    async Task<bool> ParseDlcDetailsAsync(GameInfoApplicationModel infos, uint appid, KeyValue app)
    {
        if (app["common"]["parent"].Value != null)
        {
            string str_main_appid = app["common"]["parent"].Value.Trim();
            uint main_appid = uint.Parse(str_main_appid);

            var main_app = GetOrCreateApp(main_appid, AppType.Game);

            if (!AppIds.ContainsKey(main_appid) && !DoneAppIds.Contains(main_appid))
            {
                AppIds.Add(main_appid, null);
            }

            infos.MainAppId = main_appid;
            // Add the dlc to its main game
            main_app.Dlcs[appid] = infos;

            // Only update main app if it already exists
            if (File.Exists(Path.Combine(Options.OutDirectory, str_main_appid, str_main_appid + ".json")))
                await SaveJsonAsync(Path.Combine(Options.OutDirectory, str_main_appid, str_main_appid + ".json"), main_app);
        }

        return true;
    }

    ulong GetProductCachedChangeNumber(uint appId)
    {
        if (!MetadataDatabase.ApplicationDetails.TryGetValue(appId, out var result))
            return 0;

        return result.ChangeNumber;
    }

    async Task<bool> GetAppDetailsFromSteamNetwork(uint appId, KeyValue productInfoKeyValues, ApplicationMetadata applicationMetadata)
    {
        string appIdString = appId.ToString();

        if (productInfoKeyValues == null)
            return false;

        string appOutputPath = Path.Combine(Options.OutDirectory, appIdString, $"{appIdString}.json");
        if (!Options.Force && File.Exists(appOutputPath) && applicationMetadata.ChangeNumber == GetProductCachedChangeNumber(appId))
        {
            _logger.Info($"Skipping {appId}");
            return true;
        }

        _logger.Info($"Parsing {appId}");

        if (!Options.CacheOnly)
        {
            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    productInfoKeyValues.SaveToStream(ms, false);
                    ms.Position = 0;

                    using (var sr = new StreamReader(ms))
                    {
                        await SaveFileAsync(Path.Combine(Options.CacheOutDirectory, appId.ToString(), "app_infos.vdf"), sr.ReadToEnd());
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Error($"Failed to save app_infos.vdf: {e.Message}");
            }
        }

        try
        {
            if (productInfoKeyValues["common"] != KeyValue.Invalid)
            {
                string appType = productInfoKeyValues["common"]["type"].Value.Trim().ToLower();
                AppType type;

                switch (appType)
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

                var infos = GetOrCreateApp(appId, type);

                ParseCommonDetails(infos, appId, productInfoKeyValues, type);

                switch (type)
                {
                    //case AppType.Other: ParseOtherDetails(); break;
                    case AppType.Dlc: await ParseDlcDetailsAsync(infos, appId, productInfoKeyValues); break;
                    case AppType.Application:
                    case AppType.Tool:
                    case AppType.Demo:
                    case AppType.Beta:
                    case AppType.Game: await ParseGameDetails(infos, appId, productInfoKeyValues); break;
                }

                applicationMetadata.Name = infos.Name;
                UpdateMetadataDatabase(appId, applicationMetadata);
                await SaveJsonAsync(appOutputPath, infos);

                _logger.Info($"  \\ Type {appType}, AppID {appId}, appName {infos.Name}");
            }
        }
        catch (TaskCanceledException)
        {
            throw;
        }
        catch(Exception e)
        {
            _logger.Error($"## Exception while parsing {appId}: {e.Message} ##", e);
        }

        return true;
    }

    async Task<JObject> GetWebAppDetails(string appid)
    {
        var response = await LimitSteamWebApiGET(WebHttpClient, new HttpRequestMessage(HttpMethod.Get, $"https://store.steampowered.com/api/appdetails/?appids={appid}&l=english"));

        return JObject.Parse(await response.Content.ReadAsStringAsync());
    }

    async Task GetSteamAppidsFromStoreServiceAsync()
    {
        uint lastAppid = 0;
        uint pageSize = 50000;
        // First request sometimes returns nothing
        // {"response":{}}
        // {"response":{"apps":[{"appid":10,"name":"Counter-Strike","last_modified":1745368572,"price_change_number":29993100}],"have_more_results":true,"last_appid":1533720}}
        for (int i = 0; i < 10; ++i)
        {
            var response = await LimitSteamWebApiGET(WebHttpClient, new HttpRequestMessage(HttpMethod.Get, $"https://api.steampowered.com/IStoreService/GetAppList/v1/?key={Options.WebApiKey}&include_games=true&max_results={pageSize}&last_appid={lastAppid}"));
            var apiResponse = JsonConvert.DeserializeObject<StoreServiceResponseModel>(await response.Content.ReadAsStringAsync());
            if (apiResponse?.Response?.Applications != null)
            {
                i = 0;

                foreach (var application in apiResponse.Response.Applications)
                {
                    AppIds.Add(application.Appid, null);
                }

                if (apiResponse.Response.Applications.Count != pageSize)
                    break;

                if (apiResponse.Response.LastAppid == null)
                    break;

                lastAppid = apiResponse.Response.LastAppid.Value;
            }
        }
    }

    async Task GetSteamAppidsFromSteamAppsAsync()
    {
        var response = await LimitSteamWebApiGET(WebHttpClient, new HttpRequestMessage(HttpMethod.Get, "https://api.steampowered.com/ISteamApps/GetAppList/v2/"));

        var json = JObject.Parse(await response.Content.ReadAsStringAsync());

        foreach (JObject app in (JArray)json["applist"]["apps"])
        {
            uint appid = (uint)app["appid"];
            if (!AppIds.ContainsKey(appid))
            {
                AppIds.Add(appid, null);
            }
        }
    }

    async Task GetSteamAppidsAsync()
    {
        if (Options.CacheOnly)
        {
            _logger.Info("No AppIDs specified but cache_only is set, reading only cached apps.");

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
            _logger.Info("No AppIDs specified, trying to dump all games from Steam! If that's not what you intended, stop the script right now!");

            if (string.IsNullOrWhiteSpace(Options.WebApiKey))
            {
                await GetSteamAppidsFromSteamAppsAsync();
            }
            else
            {
                await GetSteamAppidsFromStoreServiceAsync();
            }
        }
    }

    private void InitializeLogger()
    {
        PatternLayout patternLayout = new PatternLayout();
        patternLayout.ConversionPattern = "%date [%thread] %-5level %logger - %message%newline";
        patternLayout.ActivateOptions();

        var fileAppender = new log4net.Appender.FileAppender
        {
            AppendToFile = true,
            File = Path.Combine(".", "log.txt"),
            Encoding = System.Text.Encoding.UTF8,
            ImmediateFlush = true,
            Name = "DefaultFileAppender",
            Layout = patternLayout
        };
        fileAppender.ActivateOptions();

        var rootLogger = (log4net.Repository.Hierarchy.Hierarchy)log4net.LogManager.GetRepository();
        rootLogger.Root.AddAppender(fileAppender);
        var colorConsole = new log4net.Appender.ManagedColoredConsoleAppender
        {
            Layout = patternLayout,
            Target = "Console.Out"
        };
        colorConsole.AddMapping(new log4net.Appender.ManagedColoredConsoleAppender.LevelColors { Level = log4net.Core.Level.Info, ForeColor = ConsoleColor.White });
        colorConsole.AddMapping(new log4net.Appender.ManagedColoredConsoleAppender.LevelColors { Level = log4net.Core.Level.Debug, ForeColor = ConsoleColor.DarkGray });
        colorConsole.AddMapping(new log4net.Appender.ManagedColoredConsoleAppender.LevelColors { Level = log4net.Core.Level.Warn, ForeColor = ConsoleColor.Yellow });
        colorConsole.AddMapping(new log4net.Appender.ManagedColoredConsoleAppender.LevelColors { Level = log4net.Core.Level.Error, ForeColor = ConsoleColor.Red });
        colorConsole.AddMapping(new log4net.Appender.ManagedColoredConsoleAppender.LevelColors { Level = log4net.Core.Level.Fatal, ForeColor = ConsoleColor.Black, BackColor = ConsoleColor.DarkRed });
        colorConsole.ActivateOptions();
        rootLogger.Root.AddAppender(colorConsole);

        rootLogger.Root.Level = log4net.Core.Level.Debug;
        rootLogger.Configured = true;
        _logger = log4net.LogManager.GetLogger(typeof(Program));
    }

    ApplicationMetadata ApplicationMetadataFromPICSKeyValue(uint appId, KeyValue productInfo, ulong changeNumber)
    {
        var name = string.Empty;

        if (productInfo != null && productInfo != KeyValue.Invalid &&
            productInfo["common"] != null && productInfo["common"] != KeyValue.Invalid &&
            productInfo["common"]["name"] != null && productInfo["common"]["name"] != KeyValue.Invalid)
        {
            name = productInfo["common"]["name"].AsString();
        }
        else
        {
            name = $"Unknown {appId}";
        }

        return new ApplicationMetadata
        {
            Name = name,
            ChangeNumber = changeNumber,
            LastUpdateTimestamp = DateTime.UtcNow,
        };
    }

    ApplicationMetadata ApplicationMetadataFromPICS(SteamApps.PICSProductInfoCallback.PICSProductInfo productInfo)
    {
        return ApplicationMetadataFromPICSKeyValue(productInfo.ID, productInfo.KeyValues, productInfo.ChangeNumber);
    }

    async Task LoadMetadataDatabaseAsync()
    {
        var filePath = Path.Combine(Path.GetDirectoryName(Options.OutDirectory), "steam_metadata.json");
        MetadataDatabase = new MetadataDatabase();
        if (File.Exists(filePath))
        {
            try
            {
                using (StreamReader reader = new StreamReader(new FileStream(filePath, FileMode.Open), new UTF8Encoding(false)))
                {
                    MetadataDatabase = Newtonsoft.Json.JsonConvert.DeserializeObject<MetadataDatabase>(await reader.ReadToEndAsync());
                    await MetadataDatabase.UpdateMetadataDatabaseAsync();
                }
            }
            catch (Exception)
            {

            }
        }
    }

    async Task SaveMetadataDatabaseAsync()
    {
        var filePath = Path.Combine(Path.GetDirectoryName(Options.OutDirectory), "steam_metadata.json");
        await SaveFileAsync(filePath, Newtonsoft.Json.JsonConvert.SerializeObject(MetadataDatabase, new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        }));
    }

    void UpdateMetadataDatabaseStatsSteamId(uint appId, ulong steamID)
    {
        if (!MetadataDatabase.ApplicationDetails.TryGetValue(appId, out var appMetadata))
        {
            appMetadata = new ApplicationMetadata();
            appMetadata.Name = $"Unknown {steamID}";
            appMetadata.PublicSteamIdForStats = steamID;
            MetadataDatabase.ApplicationDetails[appId] = appMetadata;
        }
        else
        {
            if (appMetadata.PublicSteamIdForStats != steamID)
            {
                appMetadata.PublicSteamIdForStats = steamID;
            }
        }
    }

    void UpdateMetadataDatabase(uint appId, ApplicationMetadata applicationMetadata)
    {
        if (!MetadataDatabase.ApplicationDetails.TryGetValue(appId, out var appMetadata))
        {
            MetadataDatabase.ApplicationDetails[appId] = applicationMetadata;
            appMetadata = applicationMetadata;
        }
        else
        {
            appMetadata.Name = applicationMetadata.Name;
            appMetadata.ChangeNumber = applicationMetadata.ChangeNumber;
        }

        appMetadata.LastUpdateTimestamp = DateTime.UtcNow;
    }

    bool RestartSteamConnection()
    {
        try
        {
            if (WebSteamUser != null)
            {
                WebSteamUser.Dispose();
            }

            ContentDownloader.ShutdownSteam3();

            var result = ContentDownloader.InitializeSteam3(Options.Username, Options.UserPassword);
            if (result)
                WebSteamUser = WebAPI.GetInterface("ISteamUser", Options.WebApiKey);

            return result;
        }
        catch(Exception e)
        {
            _logger.Error($"Failed to restart steam: {e.Message}", e);
            return false;
        }
    }

    async Task MainFunc(string[] args)
    {
        InitializeLogger();

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

            if (!string.IsNullOrWhiteSpace(Options.Username) && !string.IsNullOrWhiteSpace(Options.UserPassword))
            {
                AccountSettingsStore.Instance.LoadFromFile("steam_retriever_cred.store");
                ContentDownloader.Config.RememberPassword = Options.RememberPassword;
                ContentDownloader.Config.LoginID = Options.LoginId;
                if (Options.CacheOnly || ContentDownloader.InitializeSteam3(Options.Username, Options.UserPassword))
                {
                    ContentDownloader.Config.MaxDownloads = 50;
                    ContentDownloader.Config.InstallDirectory = Path.Combine(Options.CacheOutDirectory, "depots");
                    if (!Options.CacheOnly)
                    {
                        IsAnonymous = ContentDownloader.steam3.steamClient.SteamID.AccountType != EAccountType.Individual;
                        WebSteamUser = WebAPI.GetInterface("ISteamUser", Options.WebApiKey);
                    }

                    await LoadMetadataDatabaseAsync();
                    var applicationsMetadata = new Dictionary<uint, ApplicationMetadata>();

                    var changes = default(SteamApps.PICSChangesCallback);
                    if (AppIds.Count == 0)
                    {
                        if (!Options.CacheOnly)
                        {
                            changes = await ContentDownloader.steam3.RequestAppDiffAsync((uint)MetadataDatabase.LastChangeNumber);
                            foreach (var appId in changes.AppChanges.Select(e => e.Value.ID))
                            {
                                AppIds.Add(appId, null);
                            }
                        }
                        else
                        {
                            foreach (var appId in MetadataDatabase.ApplicationDetails.Select(e => e.Key))
                            {
                                AppIds.Add((uint)appId, null);
                            }
                        }
                    }

                    var advancement = 0;

                    while (AppIds.Count > 0)
                    {
                        var chunks = AppIds.Select(e => e.Key).Chunk(1000);

                        _logger.Info($"Got {AppIds.Count} AppIDs to check");
                        foreach (var chunk in chunks)
                        {
                            if (!Options.CacheOnly)
                            {
                                await ContentDownloader.steam3.RequestAppsInfoAsync(chunk, false);
                                foreach (var appid in chunk)
                                {
                                    if (ContentDownloader.steam3.AppInfo.ContainsKey(appid) && ContentDownloader.steam3.AppInfo[appid]?.KeyValues != null)
                                    {
                                        AppIds[appid] = ContentDownloader.steam3.AppInfo[appid]?.KeyValues;
                                        applicationsMetadata[appid] = ApplicationMetadataFromPICS(ContentDownloader.steam3.AppInfo[appid]);
                                    }
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
                                            if (appinfos.ReadAsText(fs))
                                            {
                                                // No need to lock on read-only
                                                if (MetadataDatabase.ApplicationDetails?.TryGetValue(appid, out var metadata) != true)
                                                    metadata = null;

                                                AppIds[appid] = appinfos;
                                                applicationsMetadata[appid] = ApplicationMetadataFromPICSKeyValue(appid, appinfos, metadata?.ChangeNumber ?? 0);
                                            }
                                        }
                                    }
                                    catch (Exception)
                                    { }
                                }
                            }

                            foreach (var appid in chunk)
                            {
                                if (AppIds.TryGetValue(appid, out var appIdKeyValue) && applicationsMetadata.TryGetValue(appid, out var metadata))
                                {
                                    while (true)
                                    {
                                        try
                                        {
                                            await GetAppDetailsFromSteamNetwork(appid, appIdKeyValue, metadata);

                                            if (!Options.CacheOnly)
                                                ContentDownloader.steam3.AppInfo.Remove(appid);

                                            break;
                                        }
                                        catch (TaskCanceledException e)
                                        {
                                            _logger.Error($"Failed to handle app {appid}: {e.Message}", e);
                                            int i;
                                            for (i = 0; i < 5; ++i)
                                            {
                                                if (RestartSteamConnection())
                                                    break;

                                                _logger.Info("Failed to restart steam connection, waiting 5s...");
                                                await Task.Delay(TimeSpan.FromSeconds(5));
                                            }
                                            if (i == 5)
                                                throw;
                                        }
                                    }
                                }
                                else
                                {
                                    _logger.Info($"Skipped {appid}: missing keyValue and/or metadata...");
                                }

                                AppIds.Remove(appid);
                                DoneAppIds.Add(appid);

                                var newAdvancement = (int)((DoneAppIds.Count * 100.0) / (DoneAppIds.Count + AppIds.Count));
                                if (newAdvancement > advancement)
                                {
                                    advancement = newAdvancement;
                                    _logger.Info($"{DoneAppIds.Count}/{DoneAppIds.Count + AppIds.Count} ({advancement}%) done");
                                    await SaveMetadataDatabaseAsync();
                                }
                            }
                        }
                    }

                    if (changes != null)
                    {
                        MetadataDatabase.LastChangeNumber = changes.CurrentChangeNumber;
                        await SaveMetadataDatabaseAsync();
                    }
                }
            }
        }
        catch (Exception e)
        {
            _logger.Error($"Error {e.Message}", e);
        }

        if (WebSteamUser != null)
        {
            WebSteamUser.Dispose();
        }

        ContentDownloader.ShutdownSteam3();
    }

    static async Task Main(string[] args)
    {
        Instance = new Program();

        await Instance.MainFunc(args);

        Program.Instance._logger.Info("Work done, exiting now.");
    }
}



public class ProgramOptions
{
    [Option('u', "username", Required = false, HelpText = "The steam user.")]
    public string Username { get; set; } = string.Empty;

    [Option('p', "password", Required = false, HelpText = "The steam user password.")]
    public string UserPassword { get; set; } = string.Empty;
    
    [Option('r', "remember-password", Required = false, HelpText = "Save password for future login.")]
    public bool RememberPassword { get; set; } = false;

    [Option("login-id", Required = false, HelpText = "Steam loginId.")]
    public uint? LoginId { get; set; } = null;

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

    [Option("cache-only", Required = false, HelpText = "Use the cached data only, don't query Steam.")]
    public bool CacheOnly { get; set; } = false;

    [Option("cache-out", Required = false, HelpText = "Where to output the metadata cache.")]
    public string CacheOutDirectory { get; set; } = "steam_cache";

    [Option('o', "out", Required = false, HelpText = "Where to output your game definitions. By default it will output to 'steam' directory alongside the executable.")]
    public string OutDirectory { get; set; } = "steam";

    [Value(0, Required = false, HelpText = "Any number of AppID to get their infos. Separate values by Space. If you don't pass any AppID, it will try to retrieve infos for all available Steam games.")]
    public IEnumerable<string> AppIds { get; set; }
}