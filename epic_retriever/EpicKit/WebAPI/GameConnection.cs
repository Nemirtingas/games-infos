using Newtonsoft.Json.Linq;
using System.Net;
using System.Text;

namespace EpicKit
{
    [Flags]
    public enum GameFeatures : uint
    {
        None         = 0x00000000,
        Achievements = 0x00000001,
        AntiCheat    = 0x00000002,
        Connect      = 0x00000004,
        Ecom         = 0x00000008,
    }

    public class GameConnection : IDisposable
    {
        HttpClient _WebHttpClient;

        public enum ApiVersion
        {
            v1_0_0,
            v1_1_0,
            v1_2_0,
            v1_3_0,
            v1_3_1,
            v1_5_0,
            v1_6_0,
            v1_6_1,
            v1_6_2,
            v1_7_0,
            v1_7_1,
            v1_8_0,
            v1_8_1,
            v1_9_0,
            v1_10_0,
            v1_10_1,
            v1_10_2,
            v1_10_3,
            v1_11_0,
            v1_12_0,
            v1_13_0,
            v1_13_1,
            v1_14_0,
            v1_14_1,
            v1_14_2,
            v1_15_0,
            v1_15_1,
            v1_15_2,
            v1_15_3,
        }

        JObject _Json1;
        JObject _Json2;
        JObject _Json3;

        string _ApiVersion;
        string _UserAgent;

        public string _GameUserId { get; private set; }
        public string _GamePassword { get; private set; }
        public string _DeploymentId { get; private set; }
        string _Nonce;

        public string AccountId { get; private set; }
        public string ProductUserId { get; private set; }

        public GameFeatures GameFeatures { get; private set; }

        bool _LoggedIn;

        public GameConnection()
        {
            _WebHttpClient = new HttpClient(new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.All,
            });
            _Json1 = new JObject();
            _Json2 = new JObject();
            _Json3 = new JObject();

            _ApiVersion = string.Empty;

            _GameUserId = string.Empty;
            _GamePassword = string.Empty;
            _DeploymentId = string.Empty;
            _Nonce = string.Empty;

            _LoggedIn = false;
        }

        public void Dispose()
        {
            _WebHttpClient.Dispose();
        }

        public string ApiVersionToString(ApiVersion v)
        {
            switch(v)
            {
                case ApiVersion.v1_0_0 : return "1.0.0-5464091";
                case ApiVersion.v1_1_0 : return "1.1.0-6537116";
                case ApiVersion.v1_2_0 : return "1.2.0-9765216";
                case ApiVersion.v1_3_0 : return "1.3.0-11034880";
                case ApiVersion.v1_3_1 : return "1.3.1-11123224";
                case ApiVersion.v1_5_0 : return "1.5.0-12496671";
                case ApiVersion.v1_6_0 : return "1.6.0-13289764";
                case ApiVersion.v1_6_1 : return "1.6.1-13568552";
                case ApiVersion.v1_6_2 : return "1.6.2-13619780";
                case ApiVersion.v1_7_0 : return "1.7.0-13812567";
                case ApiVersion.v1_7_1 : return "1.7.1-13992660";
                case ApiVersion.v1_8_0 : return "1.8.0-14316386";
                case ApiVersion.v1_8_1 : return "1.8.1-14507409";
                case ApiVersion.v1_9_0 : return "1.9.0-14547226";
                case ApiVersion.v1_10_0: return "1.10.0-14778275";
                case ApiVersion.v1_10_1: return "1.10.1-14934259";
                case ApiVersion.v1_10_2: return "1.10.2-15217776";
                case ApiVersion.v1_10_3: return "1.10.3-15571429";
                case ApiVersion.v1_11_0: return "1.11.0-15929945";
                case ApiVersion.v1_12_0: return "1.12.0-16488214";
                case ApiVersion.v1_13_0: return "1.13.0-16697186";
                case ApiVersion.v1_13_1: return "1.13.1-16972539";
                case ApiVersion.v1_14_0: return "1.14.0-17607641";
                case ApiVersion.v1_14_1: return "1.14.1-18153445";
                case ApiVersion.v1_14_2: return "1.14.2-18950192";
                case ApiVersion.v1_15_0: return "1.15.0-20662730";
                case ApiVersion.v1_15_1: return "1.15.1-20662730";
                case ApiVersion.v1_15_2: return "1.15.2-21689671";
                case ApiVersion.v1_15_3: return "1.15.3-21924193";
            }

            return "1.15.3-21924193";
        }

        void _MakeNonce(int length)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            Random random = new Random();

            _Nonce = new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private async Task _GameLogin(string deployement_id, string user_id, string password, AuthToken token, ApiVersion api_version)
        {
            try
            {
                _ApiVersion = ApiVersionToString(api_version);
                _UserAgent = $"EOS-SDK/{_ApiVersion} (Linux/) Unreal/1.0.0";

                _GameUserId = user_id;
                _GamePassword = password;
                _DeploymentId = deployement_id;

                Uri auth_uri = new Uri($"https://{Shared.EGS_DEV_HOST}/auth/v1/oauth/token");
                Uri epic_uri = new Uri($"https://{Shared.EGS_DEV_HOST}/epic/oauth/v1/token");

                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>( "grant_type", "client_credentials" ),
                    new KeyValuePair<string, string>( "deployment_id", _DeploymentId ),
                });

                _Json1 = JObject.Parse(await Shared.WebRunPost(_WebHttpClient, auth_uri, content, new Dictionary<string, string>
                {
                    { "Authorization", string.Format("Basic {0}", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_GameUserId}:{_GamePassword}"))) },
                    { "User-Agent"   , _UserAgent },
                    { "X-EOS-Version", _ApiVersion },
                }));

                if (_Json1.ContainsKey("errorCode"))
                    WebApiException.BuildErrorFromJson(_Json1);

                switch (token.Type)
                {
                    case AuthToken.TokenType.ExchangeCode:
                        content = new FormUrlEncodedContent(new[]
                        {
                            new KeyValuePair<string, string>( "grant_type"   , "exchange_code" ),
                            new KeyValuePair<string, string>( "scope"        , "openid" ),
                            new KeyValuePair<string, string>( "exchange_code", token.Token ),
                            new KeyValuePair<string, string>( "deployment_id", _DeploymentId ),
                        });
                        break;

                    case AuthToken.TokenType.RefreshToken:
                        content = new FormUrlEncodedContent(new[]
                        {
                            new KeyValuePair<string, string>( "grant_type"   , "refresh_token" ),
                            new KeyValuePair<string, string>( "scope"        , "openid" ),
                            new KeyValuePair<string, string>( "refresh_token", token.Token ),
                            new KeyValuePair<string, string>( "deployment_id", _DeploymentId ),
                        });
                        break;
                }

                _Json2 = JObject.Parse(await Shared.WebRunPost(_WebHttpClient, epic_uri, content, new Dictionary<string, string>
                {
                    { "Authorization", string.Format("Basic {0}", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_GameUserId}:{_GamePassword}"))) },
                    { "User-Agent"   , _UserAgent },
                    { "X-EOS-Version", _ApiVersion },
                }));

                if (_Json2.ContainsKey("errorCode"))
                {
                    try
                    {
                        WebApiException.BuildErrorFromJson(_Json2);
                    }
                    catch (WebApiException e)
                    {
                        if (_Json2.ContainsKey("continuation") && e.ErrorCode == WebApiException.OAuthScopeConsentRequired)
                        {
                            var ex = new WebApiException((string)_Json2["continuation"], WebApiException.OAuthScopeConsentRequired);
                            throw ex;
                        }

                        throw;
                    }
                }

                _MakeNonce(22);
                content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>( "grant_type", "external_auth" ),
                    new KeyValuePair<string, string>( "external_auth_type", "epicgames_access_token" ),
                    new KeyValuePair<string, string>( "external_auth_token", (string)_Json2["access_token"] ),
                    new KeyValuePair<string, string>( "deployment_id", _DeploymentId ),
                    new KeyValuePair<string, string>( "nonce", _Nonce ),
                });

                _Json3 = JObject.Parse(await Shared.WebRunPost(_WebHttpClient, new Uri($"https://{Shared.EGS_DEV_HOST}/auth/v1/oauth/token"), content, new Dictionary<string, string>
                {
                    { "Authorization", string.Format("Basic {0}", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_GameUserId}:{_GamePassword}"))) },
                    { "User-Agent"   , _UserAgent },
                    { "X-EOS-Version", _ApiVersion },
                }));

                if(_Json3.ContainsKey("errorCode"))
                    WebApiException.BuildErrorFromJson(_Json3);

                AccountId = (string)_Json2["account_id"];
                ProductUserId = (string)_Json3["product_user_id"];

                if (_Json1.ContainsKey("features"))
                {
                    GameFeatures = GameFeatures.None;
                    foreach (var feature in new GameFeatures[] { GameFeatures.Achievements, GameFeatures.AntiCheat, GameFeatures.Connect, GameFeatures.Ecom })
                    {
                        foreach (var jtoken in (JArray)_Json1["features"])
                        {
                            if ((string)jtoken == feature.ToString())
                                GameFeatures |= feature;
                        }
                    }
                }

                _LoggedIn = true;
            }
            catch (Exception e)
            {
                WebApiException.BuildExceptionFromWebException(e);
            }
        }

        public async Task<string> RunContinuationToken(string continuation_token, string deployement_id, string user_id, string password)
        {
            return await Shared.RunContinuationToken(_WebHttpClient, continuation_token, deployement_id, user_id, password);
        }

        public async Task GameLoginWithExchangeCodeAsync(string deployement_id, string user_id, string password, string exchange_code, ApiVersion api_version = ApiVersion.v1_15_3)
        {
            await _GameLogin(deployement_id, user_id, password, new AuthToken { Token = exchange_code, Type = AuthToken.TokenType.ExchangeCode }, api_version);
        }

        public async Task GameLoginWithRefreshTokenAsync(string deployement_id, string user_id, string password, string game_token, ApiVersion api_version = ApiVersion.v1_15_3)
        {
            await _GameLogin(deployement_id, user_id, password, new AuthToken { Token = game_token, Type = AuthToken.TokenType.RefreshToken }, api_version);
        }

        public async Task<JArray> GetAchievementsSchemaAsync(string locale = "")
        {
            if (!_LoggedIn)
                throw new WebApiException("User is not logged in.", WebApiException.NotLoggedIn);

            var result = new JArray();

            if (!GameFeatures.HasFlag(GameFeatures.Achievements))
                return result;

            try
            {
                JArray json;
                List<string> locales;

                if (string.IsNullOrEmpty(locale))
                {
                    locales = new List<string>
                    {
                        "en",//  English
                        "ar",//  Arabic
                        "cs",//  Czech
                        "de",//  German
                        "es-ES", // Spanish - Spain
                        "es-MX", // Spanish - Mexico
                        "fr",//  French
                        "it",
                        "ja",//  Japanese
                        "ko",//  Korean
                        "pl",
                        "pt-BR", // Portugues - Brasil
                        "ru",
                        "tr",
                        "th",
                        "zh",
                        "zh-Hant"
                    };
                }
                else
                {
                    locales = new List<string> { locale };
                }

                string get_entry_value(JObject json, string key, string locale, out bool is_default)
                {
                    JObject tmp;
                    if ((tmp = (JObject)json[key]) != null)
                    {
                        if ((tmp = (JObject)tmp["data"]) != null && tmp.ContainsKey(locale))
                        {
                            is_default = false;
                            return (string)tmp[locale];
                        }
                        else if (((JObject)json[key]).ContainsKey("default"))
                        {
                            is_default = true;
                            return (string)json[key]["default"];
                        }
                    }

                    is_default = true;
                    return string.Empty;
                }

                foreach (string current_locale in locales)
                {
                    json = JArray.Parse(await Shared.WebRunGet(_WebHttpClient, new HttpRequestMessage(HttpMethod.Get, $"https://{Shared.EGS_DEV_HOST}/stats/v2/{_DeploymentId}/definitions/achievements?locale={current_locale}&iconLinks=true"), new Dictionary<string, string> {
                        { "Authorization", $"Bearer {(string)_Json3["access_token"]}" },
                        { "User-Agent"   , _UserAgent },
                        { "X-EOS-Version", _ApiVersion },
                    }));

                    foreach (JObject ach in json)
                    {
                        JObject jach = (JObject)ach["achievement"];
                        JObject jicons = (JObject)ach["iconLinks"];
                        JArray jthresholds = new JArray();

                        string id = (string)jach["name"];

                        bool default_unlocked_display_name;
                        string unlocked_display_name = get_entry_value(jach, "unlockedDisplayName", current_locale, out default_unlocked_display_name);

                        bool default_unlocked_description;
                        string unlocked_description = get_entry_value(jach, "unlockedDescription", current_locale, out default_unlocked_description);

                        bool default_locked_display_name;
                        string locked_display_name = get_entry_value(jach, "lockedDisplayName", current_locale, out default_locked_display_name);

                        bool default_locked_description;
                        string locked_description = get_entry_value(jach, "lockedDescription", current_locale, out default_locked_description);

                        bool default_flavor_text;
                        string flavor_text = get_entry_value(jach, "flavorText", current_locale, out default_flavor_text);

                        bool found = false;
                        foreach (JObject def_ach in result)
                        {
                            if ((string)def_ach["AchievementId"] == id)
                            {
                                if (!default_unlocked_display_name)
                                    def_ach["UnlockedDisplayName"][current_locale] = unlocked_display_name;

                                if(!default_unlocked_description)
                                    def_ach["UnlockedDescription"][current_locale] = unlocked_description;

                                if(!default_locked_display_name)
                                    def_ach["LockedDisplayName"][current_locale] = locked_display_name;

                                if (!default_locked_description)
                                {
                                    def_ach["LockedDescription"][current_locale] = locked_description;
                                    def_ach["HiddenDescription"][current_locale] = locked_description;
                                }

                                if(!default_flavor_text)
                                    def_ach["FlavorText"][current_locale] = flavor_text;
                            }
                        }

                        if (!found)
                        {
                            bool x;
                            string unlocked_icon_id = get_entry_value(jach, "unlockedIconId", current_locale, out x);
                            string locked_icon_id = get_entry_value(jach, "lockedIconId", current_locale, out x);

                            if (jach.ContainsKey("statThresholds"))
                            {
                                foreach (JProperty threshold in jach["statThresholds"])
                                {
                                    jthresholds.Add(new JObject
                                    {
                                        { "Name"     , threshold.Name },
                                        { "Threshold", (long)threshold.Value },
                                    });
                                }
                            }

                            if (jicons.ContainsKey(locked_icon_id))
                            {
                                Uri icon_url = new Uri((string)jicons[locked_icon_id]["readLink"]);
                            }
                            if (jicons.ContainsKey(unlocked_icon_id))
                            {
                                Uri icon_url = new Uri((string)jicons[unlocked_icon_id]["readLink"]);
                            }

                            result.Add(new JObject
                            {
                                { "AchievementId"        , id },
                                { "UnlockedDisplayName"  , new JObject{ { current_locale, unlocked_display_name } } },
                                { "UnlockedDescription"  , new JObject{ { current_locale, unlocked_description } } },
                                { "LockedDisplayName"    , new JObject{ { current_locale, locked_display_name } } },
                                { "LockedDescription"    , new JObject{ { current_locale, locked_description } } },
                                { "HiddenDescription"    , new JObject{ { current_locale, locked_description } } },
                                { "FlavorText"           , new JObject{ { current_locale, flavor_text } } },
                                { "CompletionDescription", new JObject{ { current_locale, "" } } },
                                { "UnlockedIconUrl"      , id },
                                { "LockedIconUrl"        , $"{id}_locked" },
                                { "IsHidden"             , (bool)jach["hidden"] },
                                { "StatsThresholds"      , jthresholds }
                            });
                        }
                    }
                }
            }
            catch(Exception)
            {
            }

            return result;
        }
    }
}