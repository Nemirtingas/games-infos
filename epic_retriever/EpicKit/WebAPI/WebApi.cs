using EpicKit.WebAPI.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System.Net;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;

namespace EpicKit
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum AuthorizationScopes
    {
        [EnumMember(Value = "basic_profile")]
        BasicProfile,
        [EnumMember(Value = "openid")]
        OpenId,
        [EnumMember(Value = "friends_list")]
        FriendsList,
        [EnumMember(Value = "presence")]
        Presence,
        [EnumMember(Value = "offline_access")]
        OfflineAccess,
        [EnumMember(Value = "friends_management")]
        FriendsManagement,
        [EnumMember(Value = "library")]
        Library,
        [EnumMember(Value = "country")]
        Country,
        [EnumMember(Value = "relevant_cosmetics")]
        RelevantCosmetics
    }

    public static class AuthorizationScopesExtensions
    {
        public static string ToApiString(this AuthorizationScopes scope)
        {
            try
            {
                var enumType = typeof(AuthorizationScopes);
                var memberInfos = enumType.GetMember(scope.ToString());
                var enumValueMemberInfo = memberInfos.FirstOrDefault(m => m.DeclaringType == enumType);
                var valueAttributes = enumValueMemberInfo.GetCustomAttributes(typeof(EnumMemberAttribute), false);
                return ((EnumMemberAttribute)valueAttributes[0]).Value;
            }
            catch
            {
                return scope.ToString();
            }
        }

        public static string JoinWithValue(this AuthorizationScopes[] scopes, string separator)
        {
            return string.Join(separator, scopes.Select(scope => scope.ToApiString()));
        }
    }

    public class WebApi : IDisposable
    {
        CookieContainer _WebCookies;
        CookieContainer _UnauthWebCookies;

        HttpClient _WebHttpClient;
        HttpClient _UnauthWebHttpClient;

        string _SessionID = string.Empty;
        JObject _OAuthInfos = new JObject();
        bool _LoggedIn = false;

        public WebApi()
        {
            _WebCookies = new CookieContainer();
            _UnauthWebCookies = new CookieContainer();

            _WebHttpClient = new HttpClient(new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.All,
                CookieContainer = _WebCookies,
            });

            _UnauthWebHttpClient = new HttpClient(new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.All,
                CookieContainer = _UnauthWebCookies,
            });

            _UnauthWebHttpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "UELauncher/11.0.1-14907503+++Portal+Release-Live Windows/10.0.19041.1.256.64bit");
        }

        public void Dispose()
        {
            _WebHttpClient.Dispose();
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

        async Task<string> _GetXSRFToken()
        {
            try
            {
                Uri uri = new Uri($"https://{Shared.EPIC_GAMES_HOST}/id/api/csrf");

                var response = await Shared.WebRunGet(_WebHttpClient, new HttpRequestMessage(HttpMethod.Get, uri), new Dictionary<string, string>
                {
                    { "User-Agent", Shared.EGS_OAUTH_UAGENT },
                });

                foreach (Cookie c in _WebCookies.GetCookies(uri))
                {
                    if (c.Name == "XSRF-TOKEN")
                        return c.Value;
                }

                throw new WebApiException("XSRF-TOKEN cookie not found.", WebApiException.NotFound);
            }
            catch (Exception e)
            {
                WebApiException.BuildExceptionFromWebException(e);
            }

            return string.Empty;
        }

        async Task<string> _GetExchangeCode(string xsrf_token)
        {
            try
            {
                Uri uri = new Uri($"https://{Shared.EPIC_GAMES_HOST}/id/api/exchange/generate");

                JObject response = JObject.Parse(await Shared.WebRunPost(_WebHttpClient, uri, new StringContent("", Encoding.UTF8), new Dictionary<string, string>
                {
                    { "X-XSRF-TOKEN", xsrf_token },
                    { "User-Agent", Shared.EGS_OAUTH_UAGENT },
                }));

                if (!string.IsNullOrEmpty((string)response["code"]))
                    return (string)response["code"];

                throw new WebApiException((string)response["message"], WebApiException.ErrorCodeFromString((string)response["errorCode"]));
            }
            catch (Exception e)
            {
                WebApiException.BuildExceptionFromWebException(e);
            }

            return string.Empty;
        }

        async Task _ResumeSession(string access_token)
        {
            Uri uri = new Uri($"https://{Shared.EGS_OAUTH_HOST}/account/api/oauth/verify");

            try
            {
                _WebCookies.GetCookies(uri).Clear();
                JObject oauth = JObject.Parse(await Shared.WebRunGet(_WebHttpClient, new HttpRequestMessage(HttpMethod.Get, uri), new Dictionary<string, string>
                {
                    { "User-Agent", Shared.EGS_OAUTH_UAGENT },
                    { "Authorization", access_token },
                }));
                _UpdateOAuth(oauth);
            }
            catch (Exception e)
            {
                WebApiException.BuildExceptionFromWebException(e);
            }
        }

        async Task<JObject> _StartSession(AuthToken token)
        {
            FormUrlEncodedContent post_data;
            var json = default(JObject);

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
                        new KeyValuePair<string, string>( "token_type"   , "eg1"),
                    });
                    break;

                default:
                    throw new WebApiException("Invalid token type.", WebApiException.InvalidParam);
            }

            Uri uri = new Uri($"https://{Shared.EGS_OAUTH_HOST}/account/api/oauth/token");

            _WebCookies.GetCookies(uri).Clear();

            try
            {
                json = JObject.Parse(await Shared.WebRunPost(_WebHttpClient, uri, post_data, new Dictionary<string, string>
                {
                    { "User-Agent", Shared.EGS_OAUTH_UAGENT },
                    { "Authorization", string.Format("Basic {0}", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Shared.EGS_USER}:{Shared.EGS_PASS}"))) }
                }));

                if (json.ContainsKey("errorCode"))
                    WebApiException.BuildErrorFromJson(json);
            }
            catch (Exception e)
            {
                WebApiException.BuildExceptionFromWebException(e);
            }

            return json;
        }

        public async Task<JObject> LoginAnonymous()
        {
            _ResetOAuth();

            _OAuthInfos = await _StartSession(new AuthToken { Type = AuthToken.TokenType.ClientCredentials });

            string access_token = $"bearer {(string)_OAuthInfos["access_token"]}";
            await _ResumeSession(access_token);

            var json = (JObject)_OAuthInfos.DeepClone();
            _LoggedIn = true;

            return json;
        }

        public async Task<JObject> LoginSID(string sid)
        {
            _ResetOAuth();

            Uri uri = new Uri($"https://{Shared.EPIC_GAMES_HOST}/id/api/set-sid?sid={sid}");

            _WebCookies.GetCookies(uri).Clear();

            try
            {
                string response = await Shared.WebRunGet(_WebHttpClient, new HttpRequestMessage(HttpMethod.Get, uri), new Dictionary<string, string>
                {
                    { "User-Agent"           , Shared.EGL_UAGENT },
                    { "X-Epic-Event-Action"  , "login" },
                    { "X-Epic-Event-Category", "login" },
                    { "X-Requested-With"     , "XMLHttpRequest" },
                    { "Authorization"        , string.Format("Basic {0}", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Shared.EGS_USER}:{Shared.EGS_PASS}"))) },
                });

                string exchange_code = await _GetExchangeCode(await _GetXSRFToken());

                _OAuthInfos = await _StartSession(new AuthToken { Token = exchange_code, Type = AuthToken.TokenType.ExchangeCode });

                string access_token = $"bearer {(string)_OAuthInfos["access_token"]}";
                await _ResumeSession(access_token);
                var json = (JObject)_OAuthInfos.DeepClone();
                _LoggedIn = true;
                return json;
            }
            catch (Exception e)
            {
                WebApiException.BuildExceptionFromWebException(e);
            }

            return null;
        }

        public async Task<JObject> LoginAuthCode(string auth_code)
        {
            _ResetOAuth();

            Uri uri = new Uri($"https://{Shared.EPIC_GAMES_HOST}");

            _WebCookies.GetCookies(uri).Clear();

            try
            {
                _OAuthInfos = await _StartSession(new AuthToken { Token = auth_code, Type = AuthToken.TokenType.AuthorizationCode });

                string access_token = $"bearer {(string)_OAuthInfos["access_token"]}";
                await _ResumeSession(access_token);
                // Don't share our internal oauth infos with the user returned oauth.
                var json = (JObject)_OAuthInfos.DeepClone();
                _LoggedIn = true;
                return json;
            }
            catch (Exception e)
            {
                WebApiException.BuildExceptionFromWebException(e);
            }

            return null;
        }

        public async Task<JObject> Login(JObject oauth_infos)
        {
            _ResetOAuth();

            try
            {
                if (!oauth_infos.ContainsKey("access_token") || !oauth_infos.ContainsKey("expires_at"))
                    throw new WebApiException("OAuth credentials is missing datas.", WebApiException.InvalidParam);

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
                        throw new WebApiException("OAuth credentials 'expires_at' is not an ISO8601 date.", WebApiException.InvalidParam);
                }

                if (dt > DateTime.Now && (dt - DateTime.Now) > TimeSpan.FromMinutes(10))
                {
                    string access_token = string.Format("bearer {0}", (string)oauth_infos["access_token"]);
                    _OAuthInfos = (JObject)oauth_infos.DeepClone();
                    await _ResumeSession(access_token);
                    // Don't share our internal oauth infos with the user returned oauth.
                    var json = (JObject)_OAuthInfos.DeepClone();
                    _LoggedIn = true;
                    return json;
                }
                else
                {
                    _OAuthInfos = await _StartSession(new AuthToken { Token = (string)oauth_infos["refresh_token"], Type = AuthToken.TokenType.RefreshToken });
                    await _ResumeSession(string.Format("bearer {0}", (string)_OAuthInfos["access_token"]));
                    // Don't share our internal oauth infos with the user returned oauth.
                    var json = (JObject)_OAuthInfos.DeepClone();
                    _LoggedIn = true;
                    return json;
                }
            }
            catch (Exception e)
            {
                WebApiException.BuildExceptionFromWebException(e);
            }

            return null;
        }

        public async Task Logout()
        {
            if (_LoggedIn)
            {
                try
                {
                    Uri uri = new Uri($"https://{Shared.EGS_OAUTH_HOST}/account/api/oauth/sessions/kill/{(string)_OAuthInfos["access_token"]}");

                    await _WebHttpClient.DeleteAsync(uri);
                }
                catch (Exception e)
                {
                    WebApiException.BuildExceptionFromWebException(e);
                }

                _ResetOAuth();
            }
        }

        public async Task<string> GetArtifactServiceTicket(string sandbox_id, string artifact_id, string label = "Live", string platform = "Windows")
        {
            if (!_LoggedIn)
                throw new WebApiException("User is not logged in.", WebApiException.NotLoggedIn);

            try
            {
                Uri uri = new Uri($"https://{Shared.EGS_ARTIFACT_HOST}/artifact-service/api/public/v1/dependency/sandbox/{sandbox_id}/artifact/{artifact_id}/ticket");

                JObject json = new JObject
                {
                    { "label"           , label },
                    { "expiresInSeconds", 300 },
                    { "platform"        , platform },
                };
                StringContent content = new StringContent(json.ToString(), Encoding.UTF8, "application/json");

                JObject response = JObject.Parse(await Shared.WebRunPost(_WebHttpClient, uri, content , new Dictionary<string, string>
                {
                    { "User-Agent"  , Shared.EGL_UAGENT },
                }));

                if (response.ContainsKey("errorCode"))
                {
                    WebApiException.BuildErrorFromJson(response);
                }
                else
                {
                    return (string)response["code"];
                }
            }
            catch (Exception e)
            {
                WebApiException.BuildExceptionFromWebException(e);
            }

            return string.Empty;

            // Only works when logged in anonymously.
            // sandbox_id is the same as the namespace, artifact_id is the same as the app name


            //r = self.session.post(f'https://{self._artifact_service_host}/artifact-service/api/public/v1/dependency/'
            //                      f'sandbox/{sandbox_id}/artifact/{artifact_id}/ticket',
            //                      json = dict(label = label, expiresInSeconds = 300, platform = platform),
            //                      params= dict(useSandboxAwareLabel = 'false'),
            //                      timeout = self.request_timeout)
            //r.raise_for_status()
            //return r.json()
        }

        //def get_game_manifest_by_ticket(self, artifact_id: str, signed_ticket: str, label= 'Live', platform= 'Windows') :
        //    # Based on EOS Helper Windows service implementation.
        //    r = self.session.post(f'https://{self._launcher_host}/launcher/api/public/assets/v2/'
        //                          f'by-ticket/app/{artifact_id}',
        //                          json=dict(platform= platform, label= label, signedTicket= signed_ticket),
        //                          timeout=self.request_timeout)
        //    r.raise_for_status()
        //    return r.json()
        public async Task<string> GetAppExchangeCodeAsync()
        {
            if (!_LoggedIn)
                throw new WebApiException("User is not logged in.", WebApiException.NotLoggedIn);

            try
            {
                Uri uri = new Uri($"https://{Shared.EGS_OAUTH_HOST}/account/api/oauth/exchange");

                JObject response = JObject.Parse(await Shared.WebRunGet(_WebHttpClient, new HttpRequestMessage(HttpMethod.Get, uri), new Dictionary<string, string>
                {
                    { "User-Agent", Shared.EGL_UAGENT },
                    { "Authorization", $"bearer {(string)_OAuthInfos["access_token"]}" },
                }));

                if (response.ContainsKey("errorCode"))
                    WebApiException.BuildErrorFromJson(response);

                return (string)response["code"];
            }
            catch (Exception e)
            {
                WebApiException.BuildExceptionFromWebException(e);
            }

            return null;
        }

        /// <summary>
        /// Get the refresh token that can be used to start a game.
        /// </summary>
        /// <param name="exchange_code">Exchange code generated by GetAppExchangeCode.</param>
        /// <param name="deployement_id">Application DeploymentId.</param>
        /// <param name="user_id">Application ClientId.</param>
        /// <param name="password">Application ClientSecret.</param>
        /// <returns></returns>
        public async Task<string> GetAppRefreshTokenFromExchangeCode(string exchange_code, string deployement_id, string user_id, string password, AuthorizationScopes[] scopes )
        {
            if (!_LoggedIn)
                throw new WebApiException("User is not logged in.", WebApiException.NotLoggedIn);

            JObject response;
            try
            {
                Uri uri = new Uri($"https://{Shared.EGS_DEV_HOST}/epic/oauth/v1/token");

                HttpContent content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>( "grant_type", "exchange_code" ),
                    new KeyValuePair<string, string>( "exchange_code", exchange_code ),
                    new KeyValuePair<string, string>( "scope", scopes.JoinWithValue(" ") ),
                    new KeyValuePair<string, string>( "deployment_id", deployement_id ),
                });

                response = JObject.Parse(await Shared.WebRunPost(_WebHttpClient, uri, content, new Dictionary<string, string>
                {
                    { "Authorization", string.Format("Basic {0}", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user_id}:{password}"))) },
                }));
            }
            catch (Exception e)
            {
                WebApiException.BuildExceptionFromWebException(e);
                return null;
            }

            if (response.ContainsKey("errorCode"))
            {
                try
                {
                    WebApiException.BuildErrorFromJson(response);
                }
                catch(WebApiException e)
                {
                    if (response.ContainsKey("continuation") && e.ErrorCode == WebApiException.OAuthScopeConsentRequired)
                        throw new WebApiOAuthScopeConsentRequiredException(e.Message) { ContinuationToken = (string)response["continuation"] };

                    throw;
                }
            }

            return (string)response["refresh_token"];
        }

        public async Task<string> RunContinuationToken(string continuation_token, string deployement_id, string user_id, string password)
        {
            return await Shared.RunContinuationToken(_WebHttpClient, continuation_token, deployement_id, user_id, password);
        }

        private string _GetGameCommandLine(AuthToken token, string appid)
        {
            if (!_LoggedIn)
                throw new WebApiException("User is not logged in.", WebApiException.NotLoggedIn);

            if (!_OAuthInfos.ContainsKey("display_name"))
                throw new WebApiException("OAuth infos doesn't contain 'display_name'.", WebApiException.NotFound);

            if (!_OAuthInfos.ContainsKey("account_id"))
                throw new WebApiException("OAuth infos doesn't contain 'account_id'.", WebApiException.NotFound);

            string auth_type = string.Empty;
            switch(token.Type)
            {
                case AuthToken.TokenType.ExchangeCode:
                    auth_type = "exchangecode";
                    break;

                case AuthToken.TokenType.RefreshToken:
                    auth_type = "refreshtoken";
                    break;
            }

            try
            {
                return string.Format("-AUTH_LOGIN=unused -AUTH_PASSWORD={0} -AUTH_TYPE={1} -epicapp={2} -epicenv=Prod -EpicPortal -epicusername={3} -epicuserid={4} -epiclocal=en", token.Token, auth_type, appid, (string)_OAuthInfos["display_name"], (string)_OAuthInfos["account_id"]);
            }
            catch (Exception e)
            {
                throw new WebApiException(e.Message, WebApiException.InvalidParam);
            }
        }

        public string GetGameExchangeCodeCommandLine(string exchange_code, string appid)
        {
            return _GetGameCommandLine(new AuthToken { Token = exchange_code, Type = AuthToken.TokenType.ExchangeCode }, appid);
        }

        public string GetGameTokenCommandLine(string refresh_token, string appid)
        {
            return _GetGameCommandLine(new AuthToken { Token = refresh_token, Type = AuthToken.TokenType.RefreshToken }, appid);
        }

        public async Task<List<AppAsset>> GetGamesAssets(string platform = "Windows", string label = "Live")
        {
            if (!_LoggedIn)
                throw new WebApiException("User is not logged in.", WebApiException.NotLoggedIn);

            try
            {
                System.Collections.Specialized.NameValueCollection get_datas = new System.Collections.Specialized.NameValueCollection()
                {
                    { "label", label },
                };
                string q = Shared.NameValueCollectionToQueryString(get_datas);
                Uri uri = new Uri($"https://{Shared.EGS_LAUNCHER_HOST}/launcher/api/public/assets/{platform}?{q}");

                JArray response = JArray.Parse(await Shared.WebRunGet(_WebHttpClient, new HttpRequestMessage(HttpMethod.Get, uri), new Dictionary<string, string>
                {
                    { "Authorization", $"bearer {(string)_OAuthInfos["access_token"]}" },
                }));

                List<AppAsset> app_assets = new List<AppAsset>();
                
                foreach (JObject asset in response)
                {
                    app_assets.Add(asset.ToObject<AppAsset>());
                }
                
                return app_assets;
            }
            catch (Exception e)
            {
                WebApiException.BuildExceptionFromWebException(e);
            }

            return null;
        }

        public async Task<JObject> GetGameManifest(string game_namespace, string catalog_id, string app_name, string platform = "Windows", string label = "Live")
        {
            if (!_LoggedIn)
                throw new WebApiException("User is not logged in.", WebApiException.NotLoggedIn);

            try
            {
                Uri uri = new Uri($"https://{Shared.EGS_LAUNCHER_HOST}/launcher/api/public/assets/v2/platform/{platform}/namespace/{game_namespace}/catalogItem/{catalog_id}/app/{app_name}/label/{label}");

                var json = JObject.Parse(await Shared.WebRunGet(_WebHttpClient, new HttpRequestMessage(HttpMethod.Get, uri), new Dictionary<string, string>
                {
                    { "Authorization", $"bearer {(string)_OAuthInfos["access_token"]}" },
                }));

                if (json.ContainsKey("errorCode"))
                    WebApiException.BuildErrorFromJson(json);

                return json;
            }
            catch (Exception e)
            {
                WebApiException.BuildExceptionFromWebException(e);
            }

            return null;
        }

        public async Task<ManifestDownloadInfos> GetManifestDownloadInfos(string game_namespace, string catalog_id, string app_name, string platform = "Windows", string label = "Live")
        {
            var manifestResult = await GetGameManifest(game_namespace, catalog_id, app_name, platform, label);

            var result = new ManifestDownloadInfos();

            result.ManifestHash = (string)manifestResult["elements"][0]["hash"];

            foreach (JObject manifest in manifestResult["elements"][0]["manifests"])
            {
                var manifestUrl = (string)manifest["uri"];
                string baseUrl = manifestUrl.Substring(0, manifestUrl.LastIndexOf('/'));
                if (!result.BaseUrls.Contains(baseUrl))
                    result.BaseUrls.Add(baseUrl);

                var queryParams = new List<string>();
                if (manifest.ContainsKey("queryParams"))
                {
                    foreach (JObject param in manifest["queryParams"])
                    {
                        queryParams.Add($"{param["name"]}={param["value"]}");
                    }
                }

                if (queryParams.Count > 0)
                {
                    manifestUrl = $"{manifestUrl}?{string.Join("&", queryParams)}";
                }

                if (manifestUrl.Contains(".akamaized.net/"))
                {
                    result.ManifestUrls.Insert(0, manifestUrl);
                }
                else
                {
                    result.ManifestUrls.Add(manifestUrl);
                }
            }

            if (result.BaseUrls.Count <= 0)
                throw new WebApiException("Couldn't find base urls.", WebApiException.NotFound);

            if (result.ManifestUrls.Count <= 0)
                throw new WebApiException("Couldn't find manifest urls.", WebApiException.NotFound);

            foreach (var manifestUrl in result.ManifestUrls)
            {
                using (var response = await _UnauthWebHttpClient.GetAsync(manifestUrl))
                {
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        using (var stream = response.Content.ReadAsStream())
                        using (MemoryStream ms = new MemoryStream())
                        {
                            stream.CopyTo(ms);
                            result.ManifestData = ms.ToArray();

                            var manifestHash = SHA1.HashData(result.ManifestData).Aggregate(new StringBuilder(), (sb, v) => sb.Append(v.ToString("x2"))).ToString();

                            if (result.ManifestHash != manifestHash)
                                throw new WebApiException("Manifest hash didn't match", WebApiException.InvalidData);

                            break;
                        }
                    }
                    else
                    {
                        //Console.WriteLine($"Failed to download manifest on url: {manifestUrl.Split("?")[0]}");
                    }
                }
            }
            if (result.ManifestData.Length <= 0)
                throw new WebApiException("Couldn't download manifest.", WebApiException.InvalidData);

            return result;
        }

        public async Task<List<EntitlementModel>> GetUserEntitlements(uint start = 0, uint count = 5000)
        {
            if (!_LoggedIn)
                throw new WebApiException("User is not logged in.", WebApiException.NotLoggedIn);

            try
            {
                System.Collections.Specialized.NameValueCollection get_datas = new System.Collections.Specialized.NameValueCollection()
                {
                    { "start", start.ToString() },
                    { "count", count.ToString() },
                };
                string q = Shared.NameValueCollectionToQueryString(get_datas);
                Uri uri = new Uri($"https://{Shared.EGS_ENTITLEMENT_HOST}/entitlement/api/account/{(string)_OAuthInfos["account_id"]}/entitlements?{q}");

                JArray response = JArray.Parse(await Shared.WebRunGet(_WebHttpClient, new HttpRequestMessage(HttpMethod.Get, uri), new Dictionary<string, string>
                {
                    { "Authorization", $"bearer {(string)_OAuthInfos["access_token"]}" },
                }));

                List<EntitlementModel> entitlements = new List<EntitlementModel>();
                foreach (JObject entitlement in response)
                {
                    entitlements.Add(entitlement.ToObject<EntitlementModel>());
                }

                return entitlements;
            }
            catch (Exception e)
            {
                WebApiException.BuildExceptionFromWebException(e);
            }

            return null;
        }

        public async Task<AppInfos> GetGameInfos(string game_namespace, string catalog_item_id, bool include_dlcs = true)
        {
            if (!_LoggedIn)
                throw new WebApiException("User is not logged in.", WebApiException.NotLoggedIn);

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
                string q = Shared.NameValueCollectionToQueryString(get_datas);

                Uri uri = new Uri($"https://{Shared.EGS_CATALOG_HOST}/catalog/api/shared/namespace/{game_namespace}/bulk/items?{q}");
                
                JObject response = JObject.Parse(await Shared.WebRunGet(_WebHttpClient, new HttpRequestMessage(HttpMethod.Get, uri), new Dictionary<string, string>
                {
                    { "Authorization", $"bearer {(string)_OAuthInfos["access_token"]}" },
                }));

                AppInfos appInfos = null;
                foreach (KeyValuePair<string, JToken> v in response)
                {
                    appInfos = ((JObject)v.Value).ToObject<AppInfos>();
                }

                return appInfos;
            }
            catch (Exception e)
            {
                WebApiException.BuildExceptionFromWebException(e);
            }

            return null;
        }

        public async Task<JObject> GetDefaultApiEndpointsAsync(string platformId = "LNX")
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"https://{Shared.EGS_DEV_HOST}/sdk/v1/default?platformId={platformId}");

            var t = await (await _UnauthWebHttpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead)).Content.ReadAsStringAsync();
            if (t.Contains("errorCode"))
                EpicKit.WebApiException.BuildErrorFromJson(JObject.Parse(t));

            return JObject.Parse(t);
        }

        //public async Task<JObject> GetProductApiEndpointsAsync(string productId, string deployementId, string platformId = "LNX")
        //{
        //    var request = new HttpRequestMessage(HttpMethod.Get, $"https://{Shared.EGS_DEV_HOST}/sdk/v1/product/{productId}?platformId={platformId}&deploymentId={deployementId}");
        //
        //    var t = await (await _WebHttpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead)).Content.ReadAsStringAsync();
        //    if (t.Contains("errorCode"))
        //        EpicKit.WebApiException.BuildErrorFromJson(JObject.Parse(t));
        //
        //    return JObject.Parse(t);
        //}
    }
}
