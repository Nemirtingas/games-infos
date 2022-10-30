
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace EGS
{
    class WebApi : IDisposable
    {
        CookieContainer _WebCookies;

        HttpClient _WebHttpClient;

        string _SessionID = string.Empty;
        JObject _OAuthInfos = new JObject();
        bool _LoggedIn = false;

        public WebApi()
        {
            _WebCookies = new CookieContainer();

            _WebHttpClient = new HttpClient(new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.All,
                CookieContainer = _WebCookies,
            });
        }

        public void Dispose()
        {
            _WebHttpClient.Dispose();
        }

        string _NameValueCollectionToQueryString(System.Collections.Specialized.NameValueCollection collection)
        {
            return string.Join("&", collection.AllKeys.Select(a => a + "=" + HttpUtility.UrlEncode(collection[a])));
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

        void _ResetOAuth()
        {
            _OAuthInfos = new JObject();
            _LoggedIn = false;
        }

        void _UpdateOAuth(JObject oauth_infos)
        {
            _OAuthInfos["access_token"] = oauth_infos["token"];
            _OAuthInfos["expires_in"] = oauth_infos["expires_in"];
            _OAuthInfos["expires_at"] = oauth_infos["expires_at"];
            _OAuthInfos["token_type"] = oauth_infos["token_type"];
            _OAuthInfos["account_id"] = oauth_infos["account_id"];
            _OAuthInfos["client_id"] = oauth_infos["client_id"];
            _OAuthInfos["internal_client"] = oauth_infos["internal_client"];
            _OAuthInfos["client_service"] = oauth_infos["client_service"];
            _OAuthInfos["display_name"] = oauth_infos["display_name"];
            _OAuthInfos["app"] = oauth_infos["app"];
            _OAuthInfos["in_app_id"] = oauth_infos["in_app_id"];
            _OAuthInfos["device_id"] = oauth_infos["device_id"];
            _SessionID = (string)oauth_infos["session_id"];
        }

        async Task<Error<string>> _GetXSRFToken()
        {
            Error<string> err = new Error<string> { ErrorCode = 0 };

            try
            {
                Uri uri = new Uri($"https://{Shared.EPIC_GAMES_HOST}/id/api/csrf");

                var response = await _WebRunGet(new HttpRequestMessage(HttpMethod.Get, uri), new Dictionary<string, string>
                {
                    { "User-Agent", Shared.EGS_OAUTH_UAGENT },
                });

                foreach (Cookie c in _WebCookies.GetCookies(uri))
                {
                    if (c.Name == "XSRF-TOKEN")
                    {
                        err.Result = c.Value;
                        return err;
                    }
                }

                err.Message = "XSRF-TOKEN cookie not found.";
                err.ErrorCode = Error.NotFound;
            }
            catch (Exception e)
            {
                err.FromError(Error.GetWebErrorFromException(e));
            }

            return err;
        }

        async Task<Error<string>> _GetExchangeCode(string xsrf_token)
        {
            Error<string> err = new Error<string> { ErrorCode = 0 };

            try
            {
                Uri uri = new Uri($"https://{Shared.EPIC_GAMES_HOST}/id/api/exchange/generate");

                JObject response = JObject.Parse(await _WebRunPost(uri, new StringContent("", Encoding.UTF8), new Dictionary<string, string>
                {
                    { "X-XSRF-TOKEN", xsrf_token },
                    { "User-Agent", Shared.EGS_OAUTH_UAGENT },
                }));

                err.Result = (string)response["code"];
                if (string.IsNullOrEmpty(err.Result))
                {
                    err.Message = (string)response["message"];
                    err.ErrorCode = Error.ErrorCodeFromString((string)response["errorCode"]);
                }
            }
            catch (Exception e)
            {
                err.FromError(Error.GetWebErrorFromException(e));
            }

            return err;
        }

        async Task<Error> _ResumeSession(string access_token)
        {
            Error err = new Error { ErrorCode = 0 };
            Uri uri = new Uri($"https://{Shared.EGS_OAUTH_HOST}/account/api/oauth/verify");

            try
            {
                _WebCookies.GetCookies(uri).Clear();
                JObject oauth = JObject.Parse(await _WebRunGet(new HttpRequestMessage(HttpMethod.Get, uri), new Dictionary<string, string>
                {
                    { "User-Agent", Shared.EGS_OAUTH_UAGENT },
                    { "Authorization", access_token },
                }));
                _UpdateOAuth(oauth);
            }
            catch (Exception e)
            {
                err = Error.GetWebErrorFromException(e);
            }

            return err;
        }

        async Task<Error<JObject>> _StartSession(AuthToken token)
        {
            Error<JObject> err = new Error<JObject> { ErrorCode = 0 };
            FormUrlEncodedContent post_data;
            switch (token.Type)
            {
                case AuthToken.TokenType.ExchangeCode:
                    post_data = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>( "grant_type"   , "exchange_code" ),
                        new KeyValuePair<string, string>( "exchange_code", token.Token ),
                        new KeyValuePair<string, string>( "token_type"   , "eg1"),
                    });
                    break;

                case AuthToken.TokenType.RefreshToken:
                    post_data = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>( "grant_type"   , "refresh_token" ),
                        new KeyValuePair<string, string>( "refresh_token", token.Token ),
                        new KeyValuePair<string, string>( "token_type"   , "eg1"),
                    });
                    break;

                case AuthToken.TokenType.AuthorizationCode:
                    post_data = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>( "grant_type"   , "authorization_code" ),
                        new KeyValuePair<string, string>( "code"         , token.Token ),
                        new KeyValuePair<string, string>( "token_type"   , "eg1"),
                    });
                    break;

                case AuthToken.TokenType.ClientCredentials:
                    post_data = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>( "grant_type"   , "client_credentials" ),
                        new KeyValuePair<string, string>( "deployment_id", token.Token ),
                    });
                    break;

                default:
                    err.Message = "Invalid token type.";
                    err.ErrorCode = Error.InvalidParam;
                    return err;
            }

            Uri uri = new Uri($"https://{Shared.EGS_OAUTH_HOST}/account/api/oauth/token");

            _WebCookies.GetCookies(uri).Clear();

            try
            {
                err.Result = JObject.Parse(await _WebRunPost(uri, post_data, new Dictionary<string, string>
                {
                    { "User-Agent", Shared.EGS_OAUTH_UAGENT },
                    { "Authorization", string.Format("Basic {0}", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Shared.EGS_USER}:{Shared.EGS_PASS}"))) }
                }));

                if (err.Result.ContainsKey("errorCode"))
                {
                    err.FromError(Error.GetErrorFromJson(err.Result));
                }
            }
            catch (Exception e)
            {
                err.FromError(Error.GetWebErrorFromException(e));
            }

            return err;
        }

        public async Task<Error<JObject>> LoginSID(string sid)
        {
            Error<JObject> err = new Error<JObject>();

            _ResetOAuth();

            Uri uri = new Uri($"https://{Shared.EPIC_GAMES_HOST}/id/api/set-sid?sid={sid}");

            _WebCookies.GetCookies(uri).Clear();

            try
            {
                string response = await _WebRunGet(new HttpRequestMessage(HttpMethod.Get, uri), new Dictionary<string, string>
                {
                    { "User-Agent"           , Shared.EGL_UAGENT },
                    { "X-Epic-Event-Action"  , "login" },
                    { "X-Epic-Event-Category", "login" },
                    { "X-Requested-With"     , "XMLHttpRequest" },
                    { "Authorization"        , string.Format("Basic {0}", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Shared.EGS_USER}:{Shared.EGS_PASS}"))) },
                });

                string xsrf_token;
                {
                    Error<string> x = await _GetXSRFToken();
                    if (x.ErrorCode != Error.OK)
                    {
                        err.ErrorCode = x.ErrorCode;
                        err.Message = x.Message;
                        return err;
                    }
                    xsrf_token = x.Result;
                }

                string exchange_code;
                {
                    Error<string> x = await _GetExchangeCode(xsrf_token);
                    if (x.ErrorCode != Error.OK)
                    {
                        err.ErrorCode = x.ErrorCode;
                        err.Message = x.Message;
                        return err;
                    }
                    exchange_code = x.Result;
                }

                AuthToken auth_token = new AuthToken { Token = exchange_code, Type = AuthToken.TokenType.ExchangeCode };
                {
                    Error<JObject> x = await _StartSession(auth_token);
                    if (x.ErrorCode != Error.OK)
                    {
                        err.ErrorCode = x.ErrorCode;
                        err.Message = x.Message;
                        return err;
                    }
                    _OAuthInfos = x.Result;
                }

                string access_token = $"bearer {(string)_OAuthInfos["access_token"]}";
                {
                    Error x = await _ResumeSession(access_token);
                    if (x.ErrorCode != Error.OK)
                    {
                        err.ErrorCode = x.ErrorCode;
                        err.Message = x.Message;
                        return err;
                    }
                    // Don't share our internal oauth infos with the user returned oauth.
                    err.Result = (JObject)_OAuthInfos.DeepClone();
                    err.ErrorCode = Error.OK;
                    _LoggedIn = true;
                }
            }
            catch (Exception e)
            {
                err.FromError(Error.GetWebErrorFromException(e));
            }

            return err;
        }

        public async Task<Error<JObject>> LoginAuthCode(string auth_code)
        {
            Error<JObject> err = new Error<JObject>();
            _ResetOAuth();

            Uri uri = new Uri($"https://{Shared.EPIC_GAMES_HOST}");

            _WebCookies.GetCookies(uri).Clear();

            try
            {
                AuthToken auth_token = new AuthToken { Token = auth_code, Type = AuthToken.TokenType.AuthorizationCode };
                {
                    Error<JObject> x = await _StartSession(auth_token);
                    if (x.ErrorCode != Error.OK)
                    {
                        err.ErrorCode = x.ErrorCode;
                        err.Message = x.Message;
                        return err;
                    }
                    _OAuthInfos = x.Result;
                }

                string access_token = $"bearer {(string)_OAuthInfos["access_token"]}";
                {
                    Error x = await _ResumeSession(access_token);
                    if (x.ErrorCode != Error.OK)
                    {
                        err.ErrorCode = x.ErrorCode;
                        err.Message = x.Message;
                        return err;
                    }
                    // Don't share our internal oauth infos with the user returned oauth.
                    err.Result = (JObject)_OAuthInfos.DeepClone();
                    err.ErrorCode = Error.OK;
                    _LoggedIn = true;
                }
            }
            catch (Exception e)
            {
                err.FromError(Error.GetWebErrorFromException(e));
            }

            return err;
        }

        public async Task<Error<JObject>> Login(JObject oauth_infos)
        {
            Error<JObject> err = new Error<JObject> { ErrorCode = Error.InvalidParam, Message = "Invalid cached oauth infos." };

            _ResetOAuth();

            try
            {
                if (!oauth_infos.ContainsKey("access_token") || !oauth_infos.ContainsKey("expires_at"))
                {
                    err.ErrorCode = Error.InvalidParam;
                    err.Message = string.Format("OAuth credentials is missing datas.");
                    return err;
                }

                DateTime dt = new DateTime();
                {
                    JToken date_token = oauth_infos["expires_at"];
                    if (date_token.Type == JTokenType.String)
                    {
                        dt = DateTimeOffset.ParseExact(
                          (string)oauth_infos["expires_at"],
                          new string[] { "yyyy-MM-dd'T'HH:mm:ss.FFFK" },
                          CultureInfo.InvariantCulture,
                          DateTimeStyles.None).UtcDateTime;
                    }
                    else if (date_token.Type == JTokenType.Date)
                    {
                        dt = (DateTime)date_token;
                    }
                    else
                    {
                        err.ErrorCode = Error.InvalidParam;
                        err.Message = string.Format("OAuth credentials 'expires_at' is not an ISO8601 date.");
                        return err;
                    }
                }

                if (dt > DateTime.Now && (dt - DateTime.Now) > TimeSpan.FromMinutes(10))
                {
                    string access_token = string.Format("bearer {0}", (string)oauth_infos["access_token"]);
                    _OAuthInfos = (JObject)oauth_infos.DeepClone();
                    Error x = await _ResumeSession(access_token);
                    if (x.ErrorCode == Error.OK)
                    {
                        // Don't share our internal oauth infos with the user returned oauth.
                        err.Result = (JObject)_OAuthInfos.DeepClone();
                        err.ErrorCode = Error.OK;
                        _LoggedIn = true;
                        return err;
                    }
                }

                AuthToken token = new AuthToken { Token = (string)oauth_infos["refresh_token"], Type = AuthToken.TokenType.RefreshToken };
                {
                    Error<JObject> x = await _StartSession(token);
                    if (x.ErrorCode != Error.OK)
                    {
                        err.Message = x.Message;
                        err.ErrorCode = x.ErrorCode;
                        return err;
                    }
                    _OAuthInfos = x.Result;
                }
                {
                    string access_token = string.Format("bearer {0}", (string)_OAuthInfos["access_token"]);
                    Error x = await _ResumeSession(access_token);
                    if (x.ErrorCode != Error.OK)
                    {
                        err.Message = x.Message;
                        err.ErrorCode = x.ErrorCode;
                        return err;
                    }

                    // Don't share our internal oauth infos with the user returned oauth.
                    err.Result = (JObject)_OAuthInfos.DeepClone();
                    err.ErrorCode = Error.OK;
                    _LoggedIn = true;
                }
            }
            catch (Exception e)
            {
                err.FromError(Error.GetWebErrorFromException(e));
            }

            return err;
        }

        public async Task<Error> Logout()
        {
            Error err = new Error { ErrorCode = Error.OK };
            if (_LoggedIn)
            {
                try
                {
                    Uri uri = new Uri($"https://{Shared.EGS_OAUTH_HOST}/account/api/oauth/sessions/kill/{(string)_OAuthInfos["access_token"]}");
                    //_WebCli.Headers["Authorization"] = string.Format("bearer {0}", (string)_OAuthInfos["access_token"]);

                    await _WebHttpClient.DeleteAsync(uri);
                }
                catch (Exception e)
                {
                    err = Error.GetWebErrorFromException(e);
                }

                _ResetOAuth();
            }

            return err;
        }

        public async Task<Error<string>> GetAppExchangeCode()
        {
            Error<string> err = new Error<string>();
            if (!_LoggedIn)
            {
                err.ErrorCode = Error.NotLoggedIn;
                err.Message = "User is not logged in.";
                return err;
            }

            try
            {
                Uri uri = new Uri($"https://{Shared.EGS_OAUTH_HOST}/account/api/oauth/exchange");

                JObject response = JObject.Parse(await _WebRunGet(new HttpRequestMessage(HttpMethod.Get, uri), new Dictionary<string, string>
                {
                    { "User-Agent", Shared.EGL_UAGENT },
                    { "Authorization", $"bearer {(string)_OAuthInfos["access_token"]}" },
                }));
                err.Result = (string)response["code"];
                err.ErrorCode = Error.OK;
            }
            catch (Exception e)
            {
                err.FromError(Error.GetWebErrorFromException(e));
            }

            return err;
        }

        /// <summary>
        /// Get the refresh token that can be used to start a game.
        /// </summary>
        /// <param name="exchange_code">Exchange code generated by GetAppExchangeCode.</param>
        /// <param name="deployement_id">Application DeploymentId.</param>
        /// <param name="user_id">Application ClientId.</param>
        /// <param name="password">Application ClientSecret.</param>
        /// <returns></returns>
        public async Task<Error<string>> GetAppRefreshTokenFromExchangeCode(string exchange_code, string deployement_id, string user_id, string password)
        {
            Error<string> err = new Error<string>();
            if (!_LoggedIn)
            {
                err.ErrorCode = Error.NotLoggedIn;
                err.Message = "User is not logged in.";
                return err;
            }

            try
            {
                Uri uri = new Uri($"https://{Shared.EGS_DEV_HOST}/epic/oauth/v1/token");

                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>( "grant_type", "exchange_code" ),
                    new KeyValuePair<string, string>( "exchange_code", exchange_code ),
                    new KeyValuePair<string, string>( "scope", "basic_profile friends_list presence" ),
                    new KeyValuePair<string, string>( "deployment_id", deployement_id ),
                });

                JObject response = JObject.Parse(await _WebRunPost(uri, content, new Dictionary<string, string>
                {
                    { "Authorization", string.Format("Basic {0}", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user_id}:{password}"))) },
                }));

                if (response.ContainsKey("errorCode"))
                {
                    err.FromError(Error.GetErrorFromJson(response));
                    if (response.ContainsKey("continuation") && err.ErrorCode == Error.OAuthScopeConsentRequired)
                    {
                        string continuation_token = (string)response["continuation"];
                        string consent_url = $"https://epicgames.com/id/login?continuation={continuation_token}&prompt=skip_merge skip_upgrade";

                        Console.WriteLine($"Consent is required, please head to '{consent_url}'.");

                        Process p = Shared.OpenUrl(consent_url);
                        if (p == null)
                            return err;

                        await p.WaitForExitAsync();

                        content = new FormUrlEncodedContent(new[]
                        {
                            new KeyValuePair<string, string>( "grant_type"        , "continuation_token" ),
                            new KeyValuePair<string, string>( "continuation_token", continuation_token ),
                            new KeyValuePair<string, string>( "deployment_id"     , deployement_id ),
                        });

                        int retries = 0;
                        while (true)
                        {
                            response = JObject.Parse(await _WebRunPost(uri, content, new Dictionary<string, string>
                            {
                                { "Authorization", string.Format("Basic {0}", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user_id}:{password}"))) },
                                { "User-Agent"   , Shared.EGL_UAGENT },
                            }));

                            if (!response.ContainsKey("errorCode"))
                                break;

                            if (retries >= 30)
                            {// We waited for 1 minute, exit now...
                                return err;
                            }

                            ++retries;
                            Thread.Sleep(TimeSpan.FromSeconds(2));
                        }
                    }

                    err.FromError(Error.GetErrorFromJson(response));
                    return err;
                }

                err.Result = (string)response["refresh_token"];
                err.ErrorCode = Error.OK;
            }
            catch (Exception e)
            {
                err.FromError(Error.GetWebErrorFromException(e));
            }

            return err;
        }

        public Error<string> GetGameExchangeCodeCommandLine(string exchange_code, string appid)
        {
            Error<string> err = new Error<string>();

            if (!_LoggedIn)
            {
                err.ErrorCode = Error.NotLoggedIn;
                err.Message = "User is not logged in.";
                return err;
            }

            if (!_OAuthInfos.ContainsKey("display_name"))
            {
                err.ErrorCode = Error.NotFound;
                err.Message = "OAuth infos doesn't contain 'display_name'.";
                return err;
            }

            if (!_OAuthInfos.ContainsKey("account_id"))
            {
                err.ErrorCode = Error.NotFound;
                err.Message = "OAuth infos doesn't contain 'account_id'.";
                return err;
            }

            try
            {
                err.Result = string.Format("-AUTH_LOGIN=unused -AUTH_PASSWORD={0} -AUTH_TYPE=exchangecode -epicapp={1} -epicenv=Prod -EpicPortal -epicusername={2} -epicuserid={3} -epiclocal=en", exchange_code, appid, (string)_OAuthInfos["display_name"], (string)_OAuthInfos["account_id"]);
            }
            catch (Exception e)
            {
                err.ErrorCode = Error.InvalidParam;
                err.Message = e.Message;
            }

            return err;
        }

        public Error<string> GetGameTokenCommandLine(string refresh_token, string appid)
        {
            Error<string> err = new Error<string>();

            if (!_LoggedIn)
            {
                err.ErrorCode = Error.NotLoggedIn;
                err.Message = "User is not logged in.";
                return err;
            }

            if (!_OAuthInfos.ContainsKey("display_name"))
            {
                err.ErrorCode = Error.NotFound;
                err.Message = "OAuth infos doesn't contain 'display_name'.";
                return err;
            }

            if (!_OAuthInfos.ContainsKey("account_id"))
            {
                err.ErrorCode = Error.NotFound;
                err.Message = "OAuth infos doesn't contain 'account_id'.";
                return err;
            }

            try
            {
                err.Result = string.Format("-AUTH_LOGIN=unused -AUTH_PASSWORD={0} -AUTH_TYPE=refreshtoken -epicapp={1} -epicenv=Prod -EpicPortal -epicusername={2} -epicuserid={3} -epiclocal=en", refresh_token, appid, (string)_OAuthInfos["display_name"], (string)_OAuthInfos["account_id"]);
            }
            catch (Exception e)
            {
                err.ErrorCode = Error.InvalidParam;
                err.Message = e.Message;
            }

            return err;
        }

        public async Task<Error<List<AppAsset>>> GetGamesAssets(string platform = "Windows", string label = "Live")
        {
            Error<List<AppAsset>> err = new Error<List<AppAsset>>();

            if (!_LoggedIn)
            {
                err.ErrorCode = Error.NotLoggedIn;
                err.Message = "User is not logged in.";
                return err;
            }

            try
            {
                System.Collections.Specialized.NameValueCollection get_datas = new System.Collections.Specialized.NameValueCollection()
                {
                    { "label", label },
                };
                string q = _NameValueCollectionToQueryString(get_datas);
                Uri uri = new Uri($"https://{Shared.EGS_LAUNCHER_HOST}/launcher/api/public/assets/{platform}?{q}");

                JArray response = JArray.Parse(await _WebRunGet(new HttpRequestMessage(HttpMethod.Get, uri), new Dictionary<string, string>
                {
                    { "Authorization", $"bearer {(string)_OAuthInfos["access_token"]}" },
                }));

                List<AppAsset> app_assets = new List<AppAsset>();
                
                foreach (JObject asset in response)
                {
                    app_assets.Add(asset.ToObject<AppAsset>());
                }
                
                err.Result = app_assets;
                err.ErrorCode = Error.OK;
            }
            catch (Exception e)
            {
                err.FromError(Error.GetWebErrorFromException(e));
            }

            return err;
        }

        public async Task<Error<JObject>> GetGameManifest(string game_namespace, string catalog_id, string app_name, string platform = "Windows", string label = "Live")
        {
            Error<JObject> err = new Error<JObject>();

            if (!_LoggedIn)
            {
                err.ErrorCode = Error.NotLoggedIn;
                err.Message = "User is not logged in.";
                return err;
            }

            try
            {
                Uri uri = new Uri($"https://{Shared.EGS_LAUNCHER_HOST}/launcher/api/public/assets/v2/platform/{platform}/namespace/{game_namespace}/catalogItem/{catalog_id}/app/{app_name}/label/{label}");

                err.Result = JObject.Parse(await _WebRunGet(new HttpRequestMessage(), new Dictionary<string, string>
                {
                    { "Authorization", $"bearer {(string)_OAuthInfos["access_token"]}" },
                }));
                err.ErrorCode = Error.OK;
            }
            catch (Exception e)
            {
                err.FromError(Error.GetWebErrorFromException(e));
            }

            return err;
        }

        public async Task<Error<List<Entitlement>>> GetUserEntitlements(uint start = 0, uint count = 5000)
        {
            Error<List<Entitlement>> err = new Error<List<Entitlement>>();

            if (!_LoggedIn)
            {
                err.ErrorCode = Error.NotLoggedIn;
                err.Message = "User is not logged in.";
                return err;
            }

            try
            {
                System.Collections.Specialized.NameValueCollection get_datas = new System.Collections.Specialized.NameValueCollection()
                {
                    { "start", start.ToString() },
                    { "count", count.ToString() },
                };
                string q = _NameValueCollectionToQueryString(get_datas);
                Uri uri = new Uri($"https://{Shared.EGS_ENTITLEMENT_HOST}/entitlement/api/account/{(string)_OAuthInfos["account_id"]}/entitlements?{q}");

                JArray response = JArray.Parse(await _WebRunGet(new HttpRequestMessage(HttpMethod.Get, uri), new Dictionary<string, string>
                {
                    { "Authorization", $"bearer {(string)_OAuthInfos["access_token"]}" },
                }));

                List<Entitlement> entitlements = new List<Entitlement>();
                foreach (JObject entitlement in response)
                {
                    entitlements.Add(entitlement.ToObject<EGS.Entitlement>());
                }

                err.Result = entitlements;
                err.ErrorCode = Error.OK;
            }
            catch (Exception e)
            {
                err.FromError(Error.GetWebErrorFromException(e));
            }

            return err;
        }

        public async Task<Error<AppInfos>> GetGameInfos(string game_namespace, string catalog_item_id, bool include_dlcs = true)
        {
            Error<AppInfos> err = new Error<AppInfos>();

            if (!_LoggedIn)
            {
                err.ErrorCode = Error.NotLoggedIn;
                err.Message = "User is not logged in.";
                return err;
            }

            try
            {
                System.Collections.Specialized.NameValueCollection get_datas = new System.Collections.Specialized.NameValueCollection()
                {
                    { "id", catalog_item_id },
                    { "includeDLCDetails", include_dlcs.ToString() },
                    { "includeMainGameDetails", "true" },
                    { "country", "US" },
                    { "locale", "en" }
                };
                string q = _NameValueCollectionToQueryString(get_datas);

                Uri uri = new Uri($"https://{Shared.EGS_CATALOG_HOST}/catalog/api/shared/namespace/{game_namespace}/bulk/items?{q}");
                
                JObject response = JObject.Parse(await _WebRunGet(new HttpRequestMessage(HttpMethod.Get, uri), new Dictionary<string, string>
                {
                    { "Authorization", $"bearer {(string)_OAuthInfos["access_token"]}" },
                }));

                foreach (KeyValuePair<string, JToken> v in response)
                {
                    err.Result = ((JObject)v.Value).ToObject<AppInfos>();
                }

                err.ErrorCode = Error.OK;
            }
            catch (Exception e)
            {
                err.FromError(Error.GetWebErrorFromException(e));
            }

            return err;
        }
    }
}
