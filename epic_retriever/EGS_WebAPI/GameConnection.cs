

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using static EGS.GameConnection;

namespace EGS
{
    class GameConnection : IDisposable
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
            //v1_8_1,
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
        string _UserAgent => $"EOS-SDK/{_ApiVersion} (Linux/) Unreal/1.0.0";

        string _UserId;
        string _Password;
        string _DeploymentId;
        string _Nonce;

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

            _UserId = string.Empty;
            _Password = string.Empty;
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
                //case ApiVersion.v1_8_1 : return "1.8.1-00000000";
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

        async Task<string> _WebRunGet(HttpRequestMessage request, Dictionary<string, string> headers)
        {
            Dictionary<string, string> added_headers = new Dictionary<string, string>();
            foreach (var item in headers)
            {
                if (_WebHttpClient.DefaultRequestHeaders.TryAddWithoutValidation(item.Key, item.Value))
                {
                    added_headers.Add(item.Key, item.Value);
                }
            }

            var t = await (await _WebHttpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead)).Content.ReadAsStringAsync();

            foreach (var item in added_headers)
            {
                _WebHttpClient.DefaultRequestHeaders.Remove(item.Key);
            }

            return t;
        }

        async Task<string> _WebRunPost(Uri uri, HttpContent request, Dictionary<string, string> headers)
        {
            foreach (var item in headers)
            {
                _WebHttpClient.DefaultRequestHeaders.TryAddWithoutValidation(item.Key, item.Value);
            }

            var t = await (await _WebHttpClient.PostAsync(uri, request)).Content.ReadAsStringAsync();

            foreach (var item in headers)
            {
                _WebHttpClient.DefaultRequestHeaders.Remove(item.Key);
            }

            return t;
        }

        public async Task<Error> GameLogin(string deployement_id, string user_id, string password, string game_token, ApiVersion api_version = ApiVersion.v1_15_3)
        {
            Error err = new Error();

            try
            {
                _ApiVersion = ApiVersionToString(api_version);
                _UserId = user_id;
                _Password = password;
                _DeploymentId = deployement_id;

                Uri auth_uri = new Uri($"https://{Shared.EGS_DEV_HOST}/auth/v1/oauth/token");
                Uri epic_uri = new Uri($"https://{Shared.EGS_DEV_HOST}/epic/oauth/v1/token");

                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>( "grant_type", "client_credentials" ),
                    new KeyValuePair<string, string>( "deployment_id", _DeploymentId ),
                });

                _Json1 = JObject.Parse(await _WebRunPost(auth_uri, content, new Dictionary<string, string>
                {
                    { "Authorization", string.Format("Basic {0}", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_UserId}:{_Password}"))) },
                    { "User-Agent"   , _UserAgent },
                    { "X-EOS-Version", _ApiVersion },
                }));

                if (_Json1.ContainsKey("errorCode"))
                {
                    return Error.GetErrorFromJson(_Json3);
                }

                content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>( "grant_type"   , "refresh_token" ),
                    new KeyValuePair<string, string>( "scope"        , "openid" ),
                    new KeyValuePair<string, string>( "refresh_token", game_token ),
                    new KeyValuePair<string, string>( "deployment_id", _DeploymentId ),
                });

                _Json2 = JObject.Parse(await _WebRunPost(epic_uri, content, new Dictionary<string, string>
                {
                    { "Authorization", string.Format("Basic {0}", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_UserId}:{_Password}"))) },
                    { "User-Agent"   , _UserAgent },
                    { "X-EOS-Version", _ApiVersion },
                }));

                if (_Json2.ContainsKey("errorCode"))
                {
                    err = Error.GetErrorFromJson(_Json3);
                    if(_Json3.ContainsKey("continuation") && err.ErrorCode == Error.OAuthScopeConsentRequired)
                    {
                        string continuation_token = (string)_Json3["continuation"];
                        Console.WriteLine($"Consent is required, please head to '{continuation_token}'.");

                        Shared.OpenUrl($"https://epicgames.com/id/login?continuation={continuation_token}&prompt=skip_merge%20skip_upgrade");

                        content = new FormUrlEncodedContent(new[]
                        {
                            new KeyValuePair<string, string>( "grant_type"        , "continuation_token" ),
                            new KeyValuePair<string, string>( "continuation_token", continuation_token ),
                            new KeyValuePair<string, string>( "deployment_id", _DeploymentId ),
                        });

                        _Json2 = JObject.Parse(await _WebRunPost(epic_uri, content, new Dictionary<string, string>
                        {
                            { "Authorization", string.Format("Basic {0}", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_UserId}:{_Password}"))) },
                            { "User-Agent"   , _UserAgent },
                            { "X-EOS-Version", _ApiVersion },
                        }));
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

                _Json3 = JObject.Parse(await _WebRunPost(new Uri($"https://{Shared.EGS_DEV_HOST}/auth/v1/oauth/token"), content, new Dictionary<string, string>
                {
                    { "Authorization", string.Format("Basic {0}", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_UserId}:{_Password}"))) },
                    { "User-Agent"   , _UserAgent },
                    { "X-EOS-Version", _ApiVersion },
                }));

                if(_Json3.ContainsKey("errorCode"))
                {
                    return Error.GetErrorFromJson(_Json3);
                }

                err.ErrorCode = 0;
                _LoggedIn = true;
            }
            catch (Exception e)
            {
                err = Error.GetWebErrorFromException(e);
            }

            return err;
        }

        public async Task<Error<JArray>> GetAchievementsSchema(string locale = "")
        {
            Error<JArray> result = new Error<JArray>();

            if (!_LoggedIn)
            {
                result.ErrorCode = Error.NotLoggedIn;
                return result;
            }

            try
            {
                result.Result = new JArray();

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
                    string r = await _WebRunGet(new HttpRequestMessage(HttpMethod.Get, $"https://{Shared.EGS_DEV_HOST}/stats/v2/{_DeploymentId}/definitions/achievements?locale={current_locale}&iconLinks=true"), new Dictionary<string, string> {
                        { "Authorization", $"Bearer {(string)_Json3["access_token"]}" },
                        { "User-Agent"   , _UserAgent },
                        { "X-EOS-Version", _ApiVersion },
                    });

                    json = JArray.Parse(r);

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
                        foreach (JObject def_ach in result.Result)
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

                            result.Result.Add(new JObject
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