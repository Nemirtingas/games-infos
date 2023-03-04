using System.Collections.Generic;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Web;
using System.Text;
using Newtonsoft.Json.Linq;

namespace EpicKit
{
    internal static class Shared
    {
        public const string EPIC_GAMES_HOST      = "www.epicgames.com";
        public const string EGL_UAGENT           = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) EpicGamesLauncher/12.2.4-16388143+++Portal+Release-Live UnrealEngine/4.23.0-14907503+++Portal+Release-Live Chrome/84.0.4147.38 Safari/537.36";
        public const string EGS_OAUTH_UAGENT     = "EpicGamesLauncher/12.2.4-16388143+++Portal+Release-Live Windows/10.0.19041.1.256.64bit";
        public const string EGS_USER             = "34a02cf8f4414e29b15921876da36f9a";
        public const string EGS_PASS             = "daafbccc737745039dffe53d94fc76cf";
        public const string EGS_OAUTH_HOST       = "account-public-service-prod03.ol.epicgames.com";
        public const string EGS_LAUNCHER_HOST    = "launcher-public-service-prod06.ol.epicgames.com";
        public const string EGS_ENTITLEMENT_HOST = "entitlement-public-service-prod08.ol.epicgames.com";
        public const string EGS_CATALOG_HOST     = "catalog-public-service-prod06.ol.epicgames.com";
        public const string EGS_ARTIFACT_HOST    = "artifact-public-service-prod.beee.live.use1a.on.epicgames.com";
        public const string EGS_DEV_HOST         = "api.epicgames.dev";

        internal static Process OpenUrl(string url)
        {
            try
            {
                return Process.Start(url);
            }
            catch
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return Process.Start(new ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true,
                    });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    return Process.Start("xdg-open", url);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    return Process.Start("open", url);
                }
                else
                {
                    throw;
                }
            }
        }

        internal static string NameValueCollectionToQueryString(System.Collections.Specialized.NameValueCollection collection)
        {
            return string.Join("&", collection.AllKeys.Select(a => a + "=" + HttpUtility.UrlEncode(collection[a])));
        }

        internal static async Task<string> WebRunGet(HttpClient client, HttpRequestMessage request, Dictionary<string, string> headers)
        {
            Dictionary<string, string> added_headers = new Dictionary<string, string>();
            foreach (var item in headers)
            {
                if (client.DefaultRequestHeaders.TryAddWithoutValidation(item.Key, item.Value))
                {
                    added_headers.Add(item.Key, item.Value);
                }
            }

            var t = await (await client.SendAsync(request, HttpCompletionOption.ResponseContentRead)).Content.ReadAsStringAsync();

            foreach (var item in added_headers)
            {
                client.DefaultRequestHeaders.Remove(item.Key);
            }

            return t;
        }

        internal static async Task<string> WebRunPost(HttpClient client, Uri uri, HttpContent request, Dictionary<string, string> headers)
        {
            foreach (var item in headers)
            {
                client.DefaultRequestHeaders.TryAddWithoutValidation(item.Key, item.Value);
            }

            var t = await (await client.PostAsync(uri, request)).Content.ReadAsStringAsync();

            foreach (var item in headers)
            {
                client.DefaultRequestHeaders.Remove(item.Key);
            }

            return t;
        }

        internal static async Task<string> RunContinuationToken(HttpClient client, string continuation_token, string deployement_id, string user_id, string password)
        {
            string consent_url = $"https://epicgames.com/id/login?continuation={continuation_token}&prompt=skip_merge skip_upgrade";

            Uri uri = new Uri($"https://{EGS_DEV_HOST}/epic/oauth/v1/token");

            // Start web browser.
            //Process p = Shared.OpenUrl(consent_url);
            //if (p == null)
            //    return err;
            //
            //await p.WaitForExitAsync();

            HttpContent content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>( "grant_type"        , "continuation_token" ),
                new KeyValuePair<string, string>( "continuation_token", continuation_token ),
                new KeyValuePair<string, string>( "deployment_id"     , deployement_id ),
            });

            JObject response = JObject.Parse(await WebRunPost(client, uri, content, new Dictionary<string, string>
            {
                { "Authorization", string.Format("Basic {0}", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user_id}:{password}"))) },
                { "User-Agent"   , EGL_UAGENT },
            }));

            if (response.ContainsKey("errorCode"))
            {
                try
                {
                    WebApiException.BuildErrorFromJson(response);
                }
                catch (WebApiException e)
                {
                    if (e.ErrorCode == WebApiException.OAuthScopeConsentRequired)
                    {
                        var ex = new WebApiException(consent_url, WebApiException.OAuthScopeConsentRequired);
                        throw ex;
                    }

                    throw;
                }
            }

            return (string)response["refresh_token"];
        }
    }
}