using System.Diagnostics;
using System.Runtime.InteropServices;

namespace EGS
{
    static class Shared
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
        public const string EGS_DEV_HOST         = "api.epicgames.dev";

        public static Process OpenUrl(string url)
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

            return null;
        }
    }
}