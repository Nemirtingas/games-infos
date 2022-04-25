/*
 * Copyright (C) Nemirtingas
 * This file is part of the Nemirtingas's Epic Emulator
 *
 * The Nemirtingas's Epic Emulator is free software; you can redistribute it
 * and/or modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * The Nemirtingas's Epic Emulator is distributed in the hope that it will be
 * useful, but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with the Nemirtingas's Epic Emulator; if not, see
 * <http://www.gnu.org/licenses/>.
 */

#include "os_funcs.h"

static void* hmodule;

void shared_library_load(void* hmodule)
{
    ::hmodule = hmodule;
}

void shared_library_unload(void* hmodule)
{

}

std::chrono::microseconds get_uptime()
{
    return std::chrono::duration_cast<std::chrono::microseconds>(std::chrono::system_clock::now() - get_boottime());
}

#if defined(UTILS_OS_WINDOWS)
#include <Windows.h>
#include <shellapi.h>
#include <shlobj.h>   // (shell32.lib) Infos about current user folders
#include <PathCch.h>  // (pathcch.lib)  Canonicalize path

std::string utf16_2_utf8(const std::wstring& wstr)
{
    if (wstr.empty())
        return std::string();

    int utf8_size = WideCharToMultiByte(CP_UTF8, 0, &wstr[0], (int)wstr.size(), nullptr, 0, nullptr, nullptr);
    std::string str(utf8_size, '\0');
    WideCharToMultiByte(CP_UTF8, 0, &wstr[0], (int)wstr.size(), &str[0], utf8_size, nullptr, nullptr);
    return str;
}

std::wstring utf8_2_utf16(const std::string& str)
{
    if (str.empty())
        return std::wstring();

    int utf16_size = MultiByteToWideChar(CP_UTF8, 0, &str[0], (int)str.size(), nullptr, 0);
    std::wstring wstr(utf16_size, L'\0');
    MultiByteToWideChar(CP_UTF8, 0, &str[0], (int)str.size(), &wstr[0], utf16_size);
    return wstr;
}

std::chrono::system_clock::time_point get_boottime()
{
    static std::chrono::system_clock::time_point boottime(std::chrono::system_clock::now() - std::chrono::milliseconds(GetTickCount64()));
    return boottime;
}

std::vector<std::string> get_proc_argv()
{
    std::vector<std::string> res;

    LPWSTR* szArglist;
    int nArgs;

    szArglist = CommandLineToArgvW(GetCommandLineW(), &nArgs);

    res.reserve(nArgs);
    for (int i = 0; i < nArgs; ++i)
    {
        res.emplace_back(utf16_2_utf8(szArglist[i]));
    }

    LocalFree(szArglist);

    return res;
}

std::string get_env_var(std::string const& var)
{
    std::wstring wide(utf8_2_utf16(var));
    std::wstring wVar;

    DWORD size = GetEnvironmentVariableW(wide.c_str(), nullptr, 0);
    // Size can be 0, and the size includes the null char, so resize to size - 1
    if (size < 2)
        return std::string();

    wVar.resize(size - 1);
    GetEnvironmentVariableW(wide.c_str(), &wVar[0], size);

    return utf16_2_utf8(wVar);
}

std::string get_userdata_path()
{
    WCHAR szPath[4096] = {};
    HRESULT hr = SHGetFolderPathW(NULL, CSIDL_APPDATA, NULL, 0, szPath);

    if (FAILED(hr))
        return std::string();

    return utf16_2_utf8(std::wstring(szPath));
}

std::string get_executable_path()
{
    std::string path;
    std::wstring wpath(4096, L'\0');

    wpath.resize(GetModuleFileNameW(nullptr, &wpath[0], wpath.length()));
    return utf16_2_utf8(wpath);
}

std::string get_module_path()
{
    std::string path;
    std::wstring wpath(4096, L'\0');

    DWORD size = GetModuleFileNameW((HINSTANCE)hmodule, &wpath[0], wpath.length());
    return utf16_2_utf8(wpath);
}

void* get_module_handle(std::string const& name)
{
    std::wstring wname(utf8_2_utf16(name));
    return GetModuleHandleW(wname.c_str());
}

#elif defined(UTILS_OS_LINUX) || defined(UTILS_OS_APPLE)
#ifdef UTILS_OS_LINUX
#include <fstream>
#include <sys/sysinfo.h>
#include <sys/types.h>
#include <pwd.h>
#include <unistd.h>

std::chrono::system_clock::time_point get_boottime()
{
    static bool has_boottime = false;
    static std::chrono::system_clock::time_point boottime(std::chrono::seconds(0));
    if (!has_boottime)
    {
        std::ifstream uptime_file("/proc/uptime");

        double uptime;
        if (uptime_file)
        {// Get uptime (millisecond resolution)
            uptime_file >> uptime;
            uptime_file.close();
        }
        else
        {// If we can't open /proc/uptime, fallback to sysinfo (second resolution)
            struct sysinfo infos;
            if (sysinfo(&infos) != 0)
                return boottime;

            uptime = infos.uptime;
        }

        std::chrono::system_clock::time_point now_tp = std::chrono::system_clock::now();
        std::chrono::system_clock::time_point uptime_tp(std::chrono::milliseconds(static_cast<uint64_t>(uptime * 1000)));

        boottime = std::chrono::system_clock::time_point(now_tp - uptime_tp);
        has_boottime = true;
    }

    return boottime;
}

std::string get_executable_path()
{
    std::string exec_path("./");

    char link[2048] = {};
    if (readlink("/proc/self/exe", link, sizeof(link)) > 0)
    {
        exec_path = link;
    }

    return exec_path;
}

std::vector<std::string> get_proc_argv()
{
    std::vector<std::string> res;
    std::ifstream fcmdline("/proc/self/cmdline", std::ios::in | std::ios::binary);

    if (fcmdline)
    {
        for (std::string line; std::getline(fcmdline, line, '\0');)
        {
            res.emplace_back(std::move(line));
        }
    }

    return res;
}

#else

std::chrono::system_clock::time_point get_boottime()
{
    static bool has_boottime = false;
    static std::chrono::system_clock::time_point boottime(std::chrono::seconds(0));
    if (!has_boottime)
    {
        struct timeval boottime_tv;
        size_t len = sizeof(boottime_tv);
        int mib[2] = { CTL_KERN, KERN_BOOTTIME };
        if (sysctl(mib, sizeof(mib) / sizeof(*mib), &boottime_tv, &len, nullptr, 0) < 0)
            return boottime;

        boottime = std::chrono::system_clock::time_point(
            std::chrono::seconds(boottime_tv.tv_sec) +
            std::chrono::microseconds(boottime_tv.tv_usec));
        has_boottime = true;
    }

    return boottime;
}

std::string get_executable_path()
{
    std::string exec_path("./");

    task_dyld_info dyld_info;
    task_t t;
    pid_t pid = getpid();
    task_for_pid(mach_task_self(), pid, &t);
    mach_msg_type_number_t count = TASK_DYLD_INFO_COUNT;

    if (task_info(t, TASK_DYLD_INFO, reinterpret_cast<task_info_t>(&dyld_info), &count) == KERN_SUCCESS)
    {
        dyld_all_image_infos* dyld_img_infos = reinterpret_cast<dyld_all_image_infos*>(dyld_info.all_image_info_addr);
        for (int i = 0; i < dyld_img_infos->infoArrayCount; ++i)
        {// For now I don't know how to be sure to get the executable path
         // but looks like the 1st entry is the executable path
            exec_path = dyld_img_infos->infoArray[i].imageFilePath;
            break;
        }
    }

    return exec_path;
}

std::vector<std::string> get_proc_argv()
{
    std::vector<std::string> res;
    int mib[3];
    int argmax;
    size_t size;
    int nargs;

    mib[0] = CTL_KERN;
    mib[1] = KERN_ARGMAX;

    size = sizeof(argmax);
    if (sysctl(mib, 2, &argmax, &size, NULL, 0) == -1)
    {
        return res;
    }

    std::unique_ptr<char[]> procargs = std::make_unique<char[]>(argmax);
    if (procargs == nullptr)
    {
        return res;
    }

    mib[0] = CTL_KERN;
    mib[1] = KERN_PROCARGS2;
    mib[2] = getpid();

    size = (size_t)argmax;
    if (sysctl(mib, 3, procargs.get(), &size, NULL, 0) == -1)
    {
        return res;
    }

    memcpy(&nargs, procargs.get(), sizeof(nargs));
    if (nargs <= 0)
    {
        return res;
    }

    char* args_end = procargs.get() + size;
    char* arg_iterator = procargs.get() + sizeof(nargs);
    // Skip saved exec path
    while (*arg_iterator != '\0' && arg_iterator < args_end)
    {
        ++arg_iterator;
    }
    // Skip trailing(s) '\0'
    while (*arg_iterator == '\0' && arg_iterator < args_end)
    {
        ++arg_iterator;
    }

    res.reserve(nargs);
    char* arg = arg_iterator;
    for (int i = 0; i < nargs && arg_iterator < args_end; ++arg_iterator)
    {
        if (*arg_iterator == '\0')
        {
            ++i;
            res.emplace_back(arg);
            arg = arg_iterator + 1;
        }
    }

    return res;
}

#endif

std::string get_userdata_path()
{
    std::string user_appdata_path;
    /*
    ~/Library/Application Support/<application name>
    ~/Library/Preferences/<application name>
    ~/Library/<application name>/
    */

    struct passwd* user_entry = getpwuid(getuid());
    if (user_entry == nullptr || user_entry->pw_dir == nullptr)
    {
        char* env_var = getenv("HOME");
        if (env_var != nullptr)
        {
            user_appdata_path = env_var;
        }
    }
    else
    {
        user_appdata_path = user_entry->pw_dir;
    }

    if (!user_appdata_path.empty())
    {
        if(*user_appdata_path.rbegin() != '/')
          user_appdata_path += '/';

        user_appdata_path += ".config";
    }

    return user_appdata_path;
}

std::string get_env_var(std::string const& var)
{
    char* env = getenv(var.c_str());
    if (env == nullptr)
        return std::string();

    return env;
}

#endif
