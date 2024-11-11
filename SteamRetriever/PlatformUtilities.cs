using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using static Windows.Win32.PInvoke;

namespace SteamRetriever
{
    static class PlatformUtilities
    {
        public static void SetExecutable(string path, bool value)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }

            const UnixFileMode ModeExecute = UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute;

            var mode = File.GetUnixFileMode(path);
            var hasExecuteMask = (mode & ModeExecute) == ModeExecute;
            if (hasExecuteMask != value)
            {
                File.SetUnixFileMode(path, value
                    ? mode | ModeExecute
                    : mode & ~ModeExecute);
            }
        }

        [SupportedOSPlatform("windows5.0")]
        public static void VerifyConsoleLaunch()
        {
            // Reference: https://devblogs.microsoft.com/oldnewthing/20160125-00/?p=92922
            var processList = new uint[2];
            var processCount = GetConsoleProcessList(processList);

            if (processCount != 1)
            {
                return;
            }

            _ = MessageBox(
                Windows.Win32.Foundation.HWND.Null,
                "Steam retriever is a console application; there is no GUI.\n\nIf you do not pass any command line parameters, it prints usage info and exits.\n\nYou must use this from a terminal/console.",
                "Steam retriever",
                Windows.Win32.UI.WindowsAndMessaging.MESSAGEBOX_STYLE.MB_OK | Windows.Win32.UI.WindowsAndMessaging.MESSAGEBOX_STYLE.MB_ICONWARNING
            );
        }
    }
}