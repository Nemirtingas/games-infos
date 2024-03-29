#include <iostream>
#include <iomanip>
#include <fstream>
#include <thread>
#include <atomic>
#include <mutex>
#include <condition_variable>

#include <eos_common.h>
#include <eos_sdk.h>
#include <eos_auth.h>
#include <eos_ecom.h>
#include <eos_logging.h>
#include <eos_presence.h>
#include <eos_sessions.h>
#include <eos_achievements.h>
#include <eos_leaderboards.h>
#include <eos_stats.h>
#include <eos_titlestorage.h>
#include <eos_mods.h>

#define SPDLOG_ACTIVE_LEVEL 0
#include <spdlog/spdlog.h>
#include <spdlog/sinks/dist_sink.h>
#include <spdlog/sinks/basic_file_sink.h>
#include <spdlog/sinks/stdout_color_sinks.h>
#include <nlohmann/json.hpp>

#include "CurlEasy.h"
#include "switchstr.hpp"
#include "base64.h"
#include "os_funcs.h"
#include "library.h"
#include "EGS_Api.h"
#include "function_traits.hpp"

#include "cxxopts.h"

#if defined(UTILS_OS_WINDOWS)
#if defined(UTILS_ARCH_X64)
#define EOS_LIBRARY_NAME "EOSSDK-Win64-Shipping.dll"
#elif defined(UTILS_ARCH_X86)
#define EOS_LIBRARY_NAME "EOSSDK-Win32-Shipping.dll"
#endif
#elif defined(UTILS_OS_LINUX)
#if defined(UTILS_ARCH_X64)
#define EOS_LIBRARY_NAME "libEOSSDK-Linux-Shipping.so"
#endif
#elif defined(UTILS_OS_APPLE)
#if defined(UTILS_ARCH_X64)
#define EOS_LIBRARY_NAME "libEOSSDK-Mac-Shipping.dylib"
#endif
#endif

#if defined(UTILS_OS_WINDOWS)
#define PATH_SEPARATOR '\\'

static std::string utf16_2_utf8(const std::wstring& wstr)
{
    if (wstr.empty())
        return std::string();

    int utf8_size = WideCharToMultiByte(CP_UTF8, 0, &wstr[0], (int)wstr.size(), nullptr, 0, nullptr, nullptr);
    std::string str(utf8_size, '\0');
    WideCharToMultiByte(CP_UTF8, 0, &wstr[0], (int)wstr.size(), &str[0], utf8_size, nullptr, nullptr);
    return str;
}

static std::wstring utf8_2_utf16(const std::string& str)
{
    if (str.empty())
        return std::wstring();

    int utf16_size = MultiByteToWideChar(CP_UTF8, 0, &str[0], (int)str.size(), nullptr, 0);
    std::wstring wstr(utf16_size, L'\0');
    MultiByteToWideChar(CP_UTF8, 0, &str[0], (int)str.size(), &wstr[0], utf16_size);
    return wstr;
}

static bool create_folder(std::string const& _folder)
{
    size_t pos = 0;
    struct _stat sb;

    std::wstring sub_dir;
    std::wstring folder;
    folder = utf8_2_utf16(_folder);
    if (folder.empty())
        return true;

    if (folder.length() >= 3 && folder[1] == ':' && (folder[2] == '\\' || folder[2] == '/'))
        pos = 3;

    do
    {
        pos = folder.find_first_of(L"\\/", pos + 1);
        sub_dir = std::move(folder.substr(0, pos));
        if (_wstat(sub_dir.c_str(), &sb) == 0)
        {
            if (!(sb.st_mode & _S_IFDIR))
            {// A subpath in the target is not a folder
                return false;
            }
            // Folder exists
        }
        else if (CreateDirectoryW(folder.substr(0, pos).c_str(), NULL))
        {// Failed to create folder (no permission?)
        }
    } while (pos != std::string::npos);

    return true;
}

static std::map<std::string, void*> original_exported_funcs;

#if defined(UTILS_ARCH_X86)
// Minimalistic 32bits library exported functions loader
void load_symbols(void* _mem_addr)
{
    uint8_t* mem_addr = (uint8_t*)_mem_addr;
    uint8_t* pAddr = mem_addr;

    original_exported_funcs.clear();

    IMAGE_DOS_HEADER   dos_header;
    IMAGE_NT_HEADERS32 nt_header;

    memcpy(&dos_header, pAddr, sizeof(dos_header));
    pAddr += dos_header.e_lfanew;
    memcpy(&nt_header, pAddr, sizeof(nt_header));

    // ----- Read the exported symbols (if any) ----- //
    if (nt_header.OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_EXPORT].VirtualAddress &&
        nt_header.OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_EXPORT].Size)
    {
        IMAGE_EXPORT_DIRECTORY exportDir;
        int rva = nt_header.OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_EXPORT].VirtualAddress;
        unsigned short funcOrdinal;
        // Goto export directory structure
        pAddr = mem_addr + rva;
        memcpy(&exportDir, pAddr, sizeof(IMAGE_EXPORT_DIRECTORY));
        // Goto DLL name
        pAddr = mem_addr + exportDir.Name;// +offset;

        // Read exported functions that have a name
        for (unsigned int i = 0; i < exportDir.NumberOfNames; ++i)
        {
            // Goto the funcOrdinals offset
            pAddr = mem_addr + exportDir.AddressOfNameOrdinals + sizeof(unsigned short) * i;
            // Read the current func ordinal
            memcpy(&funcOrdinal, pAddr, sizeof(unsigned short));
            // Goto the nameAddr rva
            pAddr = mem_addr + exportDir.AddressOfNames + sizeof(unsigned int) * i;
            auto& addr = original_exported_funcs[(const char*)(mem_addr + *(int32_t*)pAddr)];
            // Goto the funcAddr rva
            pAddr = mem_addr + exportDir.AddressOfFunctions + sizeof(unsigned int) * i;
            addr = (void*)(mem_addr + *(int32_t*)pAddr);
        }
    }
}

// Get the proc address from its name and not its exported name
// __stdcall convention adds an '_' as prefix and "@<parameter size>" as suffix
// __fastcall convention adds an '@' as prefix and "@<parameter size>" as suffix
void* get_proc_address(HMODULE hModule, LPCSTR procName)
{
    size_t proc_len = strlen(procName);
    for (auto& proc : original_exported_funcs)
    {
        auto pos = proc.first.find(procName);
        if (pos == 0 ||                                                              // __cdecl
            (pos == 1 && proc.first[0] == '_' && proc.first[proc_len + 1] == '@') || // __stdcall
            (pos == 1 && proc.first[0] == '@' && proc.first[proc_len + 1] == '@')    // __fastcall
            )
        {
            return proc.second;
        }
    }

    return nullptr;
}

#define GET_PROC_ADDRESS(hModule, procName) get_proc_address((HMODULE)hModule, procName)

#else

#define load_symbols(...)
#define GET_PROC_ADDRESS(MODULE, PROC) GetProcAddress((HMODULE)MODULE, PROC)

#endif

#elif defined(UTILS_OS_LINUX) || defined(UTILS_OS_APPLE)
#include <unistd.h>
#include <sys/types.h>
#include <sys/stat.h>
#include <dlfcn.h>

#define PATH_SEPARATOR '/'

static bool create_folder(std::string const& _folder)
{
    size_t pos = 0;
    struct stat sb;

    std::string sub_dir;
    std::string folder = _folder;

    do
    {
        pos = folder.find_first_of("\\/", pos + 1);
        sub_dir = std::move(folder.substr(0, pos));
        if (stat(sub_dir.c_str(), &sb) == 0)
        {
            if (!S_ISDIR(sb.st_mode))
            {// A subpath in the target is not a folder
                return false;
            }
            // Folder exists
        }
        else if (mkdir(sub_dir.c_str(), 0755) < 0)
        {// Failed to create folder (no permission?)
        }
    } while (pos != std::string::npos);

    return true;
}

#define load_symbols(...)
#define GET_PROC_ADDRESS(MODULE, PROC) dlsym((void*)MODULE, PROC)

#endif

static struct {
    bool DownloadIcons = false;
} AppArgs;

inline const char* str_or_empty(const char* str)
{
    return str == nullptr ? "" : str;
}

template<template<typename, typename, typename...> class ObjectType,
    template<typename, typename...> class ArrayType,
    class StringType, class BooleanType, class NumberIntegerType,
    class NumberUnsignedType, class NumberFloatType,
    template<typename> class AllocatorType,
    template<typename, typename = void> class JSONSerializer>
bool load_json(std::string const& path, nlohmann::basic_json<ObjectType, ArrayType, StringType, BooleanType, NumberIntegerType, NumberUnsignedType, NumberFloatType, AllocatorType, JSONSerializer>& json)
{
    std::ifstream file(path, std::ios::in | std::ios::binary);

    SPDLOG_INFO("Loading {}", path);
    if (file)
    {
        file.seekg(0, std::ios::end);
        size_t size = static_cast<size_t>(file.tellg());
        file.seekg(0, std::ios::beg);

        std::string buffer(size, '\0');

        file.read(&buffer[0], size);
        file.close();

        try
        {
            json = std::move(nlohmann::json::parse(buffer));

            return true;
        }
        catch (std::exception& e)
        {
            SPDLOG_ERROR("Error while parsing JSON {}: {}", path, e.what());
        }
    }
    else
    {
        SPDLOG_WARN("File not found: {}", path);
    }
    return false;
}

template<template<typename, typename, typename...> class ObjectType,
    template<typename, typename...> class ArrayType,
    class StringType, class BooleanType, class NumberIntegerType,
    class NumberUnsignedType, class NumberFloatType,
    template<typename> class AllocatorType,
    template<typename, typename = void> class JSONSerializer>
bool save_json(std::string const& file_path, nlohmann::basic_json<ObjectType, ArrayType, StringType, BooleanType, NumberIntegerType, NumberUnsignedType, NumberFloatType, AllocatorType, JSONSerializer> const& json)
{
    std::ofstream file(file_path, std::ios::trunc | std::ios::out);
    if (!file)
    {
        SPDLOG_ERROR("Failed to save: {}", file_path.c_str());
        return false;
    }
    SPDLOG_DEBUG("Saving {}", file_path.c_str());
    file << std::setw(2) << json;
    return true;
}

static void download_icon(std::string const& url, std::string filename)
{
    if (!AppArgs.DownloadIcons)
        return;

    CurlEasy webcli;
    int err;
    std::fstream f;

    size_t pos = filename.find_last_of("/\\");
    if (pos != std::string::npos)
    {
        create_folder(filename.substr(0, pos));
    }

    f.open(filename, std::ios::in | std::ios::binary);
    if (f.is_open())
        return;

    SPDLOG_INFO("Downloading icon -> {}", filename);

    if (!webcli.Init())
    {
        SPDLOG_ERROR("Failed to initialize curl.");
        return;
    }
    webcli.SetHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:95.0) Gecko/20100101 Firefox/95.0");
    webcli.SetHeader("Accept-Encoding", "gzip, deflate, br");
    if ((err = webcli.PerformGet(url)) != 0)
    {
        SPDLOG_ERROR("Error: {}", webcli.GetError(err));
        return;
    }

    f.open(filename, std::ios::out | std::ios::trunc | std::ios::binary);
    if (f.is_open())
    {
        f.write(webcli.GetAnswer().data(), webcli.GetAnswer().size());
    }
}


class EOSApi
{
    template<typename T>
    static void EOS_CALL shim_callback(T param)
    {
        std::function<void(T)>* f = reinterpret_cast<std::function<void(T)>*>(param->ClientData);
        const_cast<void*&>(param->ClientData) = nullptr;
        (*f)(param);
        delete f;
    }

    Library eos_api;

    // EOS_API
    decltype(EOS_Initialize)* _EOS_Initialize;
    decltype(EOS_Shutdown)* _EOS_Shutdown;
    decltype(EOS_EResult_ToString)* _EOS_EResult_ToString;
    decltype(EOS_EpicAccountId_IsValid)* _EOS_EpicAccountId_IsValid;
    decltype(EOS_EpicAccountId_ToString)* _EOS_EpicAccountId_ToString;
    decltype(EOS_EpicAccountId_FromString)* _EOS_EpicAccountId_FromString;
    decltype(EOS_ProductUserId_IsValid)* _EOS_ProductUserId_IsValid;
    decltype(EOS_ProductUserId_ToString)* _EOS_ProductUserId_ToString;
    decltype(EOS_ProductUserId_FromString)* _EOS_ProductUserId_FromString;

    // EOS_Platform
    decltype(EOS_Platform_Create)* _EOS_Platform_Create;
    decltype(EOS_Platform_Tick)* _EOS_Platform_Tick;
    decltype(EOS_Platform_GetAchievementsInterface)* _EOS_Platform_GetAchievementsInterface;
    decltype(EOS_Platform_GetAuthInterface)* _EOS_Platform_GetAuthInterface;
    decltype(EOS_Platform_GetConnectInterface)* _EOS_Platform_GetConnectInterface;
    decltype(EOS_Platform_GetEcomInterface)* _EOS_Platform_GetEcomInterface;
    decltype(EOS_Platform_GetStatsInterface)* _EOS_Platform_GetStatsInterface;
    decltype(EOS_Platform_GetLeaderboardsInterface)* _EOS_Platform_GetLeaderboardsInterface;
    decltype(EOS_Platform_GetTitleStorageInterface)* _EOS_Platform_GetTitleStorageInterface;
    decltype(EOS_Platform_SetOverrideLocaleCode)* _EOS_Platform_SetOverrideLocaleCode;

    // EOS_Auth
    decltype(EOS_Auth_Login)* _EOS_Auth_Login;
    decltype(EOS_Auth_GetLoggedInAccountByIndex)* _EOS_Auth_GetLoggedInAccountByIndex;
    decltype(EOS_Auth_CopyUserAuthToken)* _EOS_Auth_CopyUserAuthToken;

    // EOS_Connect
    decltype(EOS_Connect_Login)* _EOS_Connect_Login;
    decltype(EOS_Connect_CreateUser)* _EOS_Connect_CreateUser;
    decltype(EOS_Connect_GetLoggedInUserByIndex)* _EOS_Connect_GetLoggedInUserByIndex;
    decltype(EOS_Connect_GetProductUserIdMapping)* _EOS_Connect_GetProductUserIdMapping;
    decltype(EOS_Connect_GetExternalAccountMapping)* _EOS_Connect_GetExternalAccountMapping;

    // EOS_Ecom
    decltype(EOS_Ecom_QueryOffers)* _EOS_Ecom_QueryOffers;
    decltype(EOS_Ecom_GetOfferCount)* _EOS_Ecom_GetOfferCount;
    decltype(EOS_Ecom_CopyOfferByIndex)* _EOS_Ecom_CopyOfferByIndex;
    decltype(EOS_Ecom_GetOfferItemCount)* _EOS_Ecom_GetOfferItemCount;
    decltype(EOS_Ecom_CopyOfferItemByIndex)* _EOS_Ecom_CopyOfferItemByIndex;
    decltype(EOS_Ecom_QueryEntitlements)* _EOS_Ecom_QueryEntitlements;
    decltype(EOS_Ecom_GetEntitlementsCount)* _EOS_Ecom_GetEntitlementsCount;
    decltype(EOS_Ecom_CopyEntitlementByIndex)* _EOS_Ecom_CopyEntitlementByIndex;
    decltype(EOS_Ecom_Entitlement_Release)* _EOS_Ecom_Entitlement_Release;
    decltype(EOS_Ecom_CatalogOffer_Release)* _EOS_Ecom_CatalogOffer_Release;
    decltype(EOS_Ecom_CatalogItem_Release)* _EOS_Ecom_CatalogItem_Release;

    // EOS_Stats
    decltype(EOS_Stats_QueryStats)* _EOS_Stats_QueryStats;
    decltype(EOS_Stats_GetStatsCount)* _EOS_Stats_GetStatsCount;
    decltype(EOS_Stats_CopyStatByIndex)* _EOS_Stats_CopyStatByIndex;
    decltype(EOS_Stats_Stat_Release)* _EOS_Stats_Stat_Release;

    // EOS_TitleStorage
    decltype(EOS_TitleStorage_QueryFile)* _EOS_TitleStorage_QueryFile;
    decltype(EOS_TitleStorage_QueryFileList)* _EOS_TitleStorage_QueryFileList;
    decltype(EOS_TitleStorage_CopyFileMetadataByFilename)* _EOS_TitleStorage_CopyFileMetadataByFilename;
    decltype(EOS_TitleStorage_GetFileMetadataCount)* _EOS_TitleStorage_GetFileMetadataCount;
    decltype(EOS_TitleStorage_CopyFileMetadataAtIndex)* _EOS_TitleStorage_CopyFileMetadataAtIndex;
    decltype(EOS_TitleStorage_ReadFile)* _EOS_TitleStorage_ReadFile;
    decltype(EOS_TitleStorage_DeleteCache)* _EOS_TitleStorage_DeleteCache;

    // EOS_Leaderboards
    decltype(EOS_Leaderboards_QueryLeaderboardDefinitions)* _EOS_Leaderboards_QueryLeaderboardDefinitions;
    decltype(EOS_Leaderboards_GetLeaderboardDefinitionCount)* _EOS_Leaderboards_GetLeaderboardDefinitionCount;
    decltype(EOS_Leaderboards_CopyLeaderboardDefinitionByIndex)* _EOS_Leaderboards_CopyLeaderboardDefinitionByIndex;
    decltype(EOS_Leaderboards_Definition_Release)* _EOS_Leaderboards_Definition_Release;

    // EOS_Achievements
    decltype(EOS_Achievements_QueryDefinitions)* _EOS_Achievements_QueryDefinitions;
    decltype(EOS_Achievements_GetAchievementDefinitionCount)* _EOS_Achievements_GetAchievementDefinitionCount;
    decltype(EOS_Achievements_CopyAchievementDefinitionByIndex)* _EOS_Achievements_CopyAchievementDefinitionByIndex;
    decltype(EOS_Achievements_Definition_Release)* _EOS_Achievements_Definition_Release;
    decltype(EOS_Achievements_CopyAchievementDefinitionV2ByIndex)* _EOS_Achievements_CopyAchievementDefinitionV2ByIndex;
    decltype(EOS_Achievements_CopyAchievementDefinitionV2ByAchievementId)* _EOS_Achievements_CopyAchievementDefinitionV2ByAchievementId;
    decltype(EOS_Achievements_UnlockAchievements)* _EOS_Achievements_UnlockAchievements;
    decltype(EOS_Achievements_DefinitionV2_Release)* _EOS_Achievements_DefinitionV2_Release;

    // EOS_TitleStorageFileTransferRequest
    decltype(EOS_TitleStorageFileTransferRequest_GetFileRequestState)* _EOS_TitleStorageFileTransferRequest_GetFileRequestState;
    decltype(EOS_TitleStorageFileTransferRequest_GetFilename)* _EOS_TitleStorageFileTransferRequest_GetFilename;
    decltype(EOS_TitleStorageFileTransferRequest_CancelRequest)* _EOS_TitleStorageFileTransferRequest_CancelRequest;
    decltype(EOS_TitleStorageFileTransferRequest_Release)* _EOS_TitleStorageFileTransferRequest_Release;

    class CTitleStorageFileTransferRequest
    {
        friend class CTitleStorage;
        EOSApi* eos_api;
        EOS_HTitleStorageFileTransferRequest hIface = nullptr;

    public:
        CTitleStorageFileTransferRequest(EOS_HTitleStorageFileTransferRequest hIface) :
            hIface(hIface)
        {}
    };

    class CPlatform
    {
        friend EOSApi;
        EOSApi* _EOSApi;
        EOS_HPlatform hIface = nullptr;

    public:
        CPlatform(EOSApi* eos_api) :
            _EOSApi(eos_api)
        {}

        inline bool HasInterface() const { return hIface != nullptr; }

        void Create(EOS_Platform_Options const* options)
        {
            if ((hIface = _EOSApi->_EOS_Platform_Create(options)) == nullptr)
                throw std::runtime_error("Failed to create EOS_Platform.");

            if ((_EOSApi->Auth.hIface = _EOSApi->_EOS_Platform_GetAuthInterface(hIface)) == nullptr)
                throw std::runtime_error("Failed to get EOS_HAuth.");

            if ((_EOSApi->Connect.hIface = _EOSApi->_EOS_Platform_GetConnectInterface(hIface)) == nullptr)
                throw std::runtime_error("Failed to get EOS_HConnect.");

            if ((_EOSApi->Ecom.hIface = _EOSApi->_EOS_Platform_GetEcomInterface(hIface)) == nullptr)
                throw std::runtime_error("Failed to get EOS_HEcom.");

            // Optionnal interfaces
            if (_EOSApi->_EOS_Platform_GetAchievementsInterface != nullptr)
            {
                if ((_EOSApi->Achievements.hIface = _EOSApi->_EOS_Platform_GetAchievementsInterface(hIface)) == nullptr)
                    throw std::runtime_error("Failed to get EOS_HAchievements.");
            }
            if (_EOSApi->_EOS_Platform_GetStatsInterface != nullptr)
            {
                if ((_EOSApi->Stats.hIface = _EOSApi->_EOS_Platform_GetStatsInterface(hIface)) == nullptr)
                    throw std::runtime_error("Failed to get EOS_HStats.");
            }
            if (_EOSApi->_EOS_Platform_GetLeaderboardsInterface != nullptr)
            {
                if ((_EOSApi->Leaderboards.hIface = _EOSApi->_EOS_Platform_GetLeaderboardsInterface(hIface)) == nullptr)
                    throw std::runtime_error("Failed to get EOS_HLeaderboards.");
            }
            if (_EOSApi->_EOS_Platform_GetTitleStorageInterface != nullptr)
            {
                if ((_EOSApi->TitleStorage.hIface = _EOSApi->_EOS_Platform_GetTitleStorageInterface(hIface)) == nullptr)
                    throw std::runtime_error("Failed to get EOS_HTitleStorage.");
            }
        }

        void Tick() { _EOSApi->_EOS_Platform_Tick(hIface); }

        EOS_EResult SetOverrideLocaleCode(const char* local_code)
        {
            return _EOSApi->_EOS_Platform_SetOverrideLocaleCode(hIface, local_code);
        }
    };

    class CAchievements
    {
        friend EOSApi;
        friend CPlatform;
        EOSApi* _EOSApi;
        EOS_HAchievements hIface = nullptr;

    public:
        CAchievements(EOSApi* eos_api) :
            _EOSApi(eos_api)
        {}

        inline bool HasInterface() const { return hIface != nullptr; }

        inline bool HasOldAchievements() const { return hIface != nullptr && _EOSApi->_EOS_Achievements_CopyAchievementDefinitionByIndex != nullptr; }
        inline bool HasNewAchievements() const { return hIface != nullptr && _EOSApi->_EOS_Achievements_CopyAchievementDefinitionV2ByIndex != nullptr; }

        void QueryDefinitions(EOS_Achievements_QueryDefinitionsOptions const* options, std::function<std::remove_pointer_t<EOS_Achievements_OnQueryDefinitionsCompleteCallback>> completion_delegate)
        {
            _EOSApi->_EOS_Achievements_QueryDefinitions(hIface, options, new (decltype(completion_delegate))(std::move(completion_delegate)), &EOSApi::shim_callback<typename boost::function_traits<decltype(completion_delegate)>::arg1_type>);
        }

        uint32_t GetAchievementDefinitionCount(EOS_Achievements_GetAchievementDefinitionCountOptions const* options)
        {
            return _EOSApi->_EOS_Achievements_GetAchievementDefinitionCount(hIface, options);
        }

        EOS_EResult CopyAchievementDefinitionByIndex(EOS_Achievements_CopyAchievementDefinitionByIndexOptions const* options, EOS_Achievements_Definition** out_definition)
        {
            if (_EOSApi->_EOS_Achievements_CopyAchievementDefinitionByIndex == nullptr)
                throw std::runtime_error("EOS_Achievements_CopyAchievementDefinitionByIndex is not available");

            return _EOSApi->_EOS_Achievements_CopyAchievementDefinitionByIndex(hIface, options, out_definition);
        }

        void Definition_Release(EOS_Achievements_Definition* achievement_definition)
        {
            if (_EOSApi->_EOS_Achievements_Definition_Release == nullptr)
                throw std::runtime_error("EOS_Achievements_Definition_Release is not available");

            return _EOSApi->_EOS_Achievements_Definition_Release(achievement_definition);
        }

        EOS_EResult CopyAchievementDefinitionV2ByIndex(EOS_Achievements_CopyAchievementDefinitionV2ByIndexOptions const* options, EOS_Achievements_DefinitionV2** out_definition)
        {
            if (_EOSApi->_EOS_Achievements_CopyAchievementDefinitionV2ByIndex == nullptr)
                throw std::runtime_error("_EOS_Achievements_CopyAchievementDefinitionByIndex is not available");

            return _EOSApi->_EOS_Achievements_CopyAchievementDefinitionV2ByIndex(hIface, options, out_definition);
        }

        EOS_EResult CopyAchievementDefinitionV2ByAchievementId(EOS_Achievements_CopyAchievementDefinitionV2ByAchievementIdOptions const* options, EOS_Achievements_DefinitionV2** out_definition)
        {
            if (_EOSApi->_EOS_Achievements_CopyAchievementDefinitionV2ByAchievementId == nullptr)
                throw std::runtime_error("_EOS_Achievements_CopyAchievementDefinitionV2ByAchievementId is not available");

            return _EOSApi->_EOS_Achievements_CopyAchievementDefinitionV2ByAchievementId(hIface, options, out_definition);
        }

        void UnlockAchievements(const EOS_Achievements_UnlockAchievementsOptions* options, std::function<std::remove_pointer_t<EOS_Achievements_OnUnlockAchievementsCompleteCallback>> completion_delegate)
        {
            if (_EOSApi->_EOS_Achievements_UnlockAchievements == nullptr)
                throw std::runtime_error("_EOS_Achievements_CopyAchievementDefinitionV2ByAchievementId is not available");

            return _EOSApi->_EOS_Achievements_UnlockAchievements(hIface, options, new (decltype(completion_delegate))(std::move(completion_delegate)), &EOSApi::shim_callback<typename boost::function_traits<decltype(completion_delegate)>::arg1_type>);
        }

        void DefinitionV2_Release(EOS_Achievements_DefinitionV2* achievement_definition)
        {
            if (_EOSApi->_EOS_Achievements_DefinitionV2_Release == nullptr)
                throw std::runtime_error("EOS_Achievements_DefinitionV2_Release is not available");

            return _EOSApi->_EOS_Achievements_DefinitionV2_Release(achievement_definition);
        }
    };

    class CAuth
    {
        friend EOSApi;
        friend CPlatform;
        EOSApi* _EOSApi;
        EOS_HAuth hIface = nullptr;

    public:
        CAuth(EOSApi* eos_api) :
            _EOSApi(eos_api)
        {}

        inline bool HasInterface() const { return hIface != nullptr; }

        void Login(EOS_Auth_LoginOptions const* options, std::function<std::remove_pointer_t<EOS_Auth_OnLoginCallback>> completion_delegate)
        {
            _EOSApi->_EOS_Auth_Login(hIface, options, new (decltype(completion_delegate))(std::move(completion_delegate)), &EOSApi::shim_callback<typename boost::function_traits<decltype(completion_delegate)>::arg1_type>);
        }

        EOS_EpicAccountId GetLoggedInAccountByIndex(int32_t index)
        {
            return _EOSApi->_EOS_Auth_GetLoggedInAccountByIndex(hIface, index);
        }

        EOS_EResult CopyUserAuthToken(EOS_Auth_CopyUserAuthTokenOptions const* options, EOS_EpicAccountId local_user_id, EOS_Auth_Token** out_user_auth_token)
        {
            return _EOSApi->_EOS_Auth_CopyUserAuthToken(hIface, options, local_user_id, out_user_auth_token);
        }
    };

    class CConnect
    {
        friend EOSApi;
        friend CPlatform;
        EOSApi* _EOSApi;
        EOS_HConnect hIface = nullptr;

    public:
        CConnect(EOSApi* eos_api) :
            _EOSApi(eos_api)
        {}

        inline bool HasInterface() const { return hIface != nullptr; }

        void Login(EOS_Connect_LoginOptions const* options, std::function<std::remove_pointer_t<EOS_Connect_OnLoginCallback>> completion_delegate)
        {
            _EOSApi->_EOS_Connect_Login(hIface, options, new (decltype(completion_delegate))(std::move(completion_delegate)), &EOSApi::shim_callback<typename boost::function_traits<decltype(completion_delegate)>::arg1_type>);
        }

        void CreateUser(EOS_Connect_CreateUserOptions const* options, std::function<std::remove_pointer_t<EOS_Connect_OnCreateUserCallback>> completion_delegate)
        {
            _EOSApi->_EOS_Connect_CreateUser(hIface, options, new (decltype(completion_delegate))(std::move(completion_delegate)), &EOSApi::shim_callback<typename boost::function_traits<decltype(completion_delegate)>::arg1_type>);
        }

        EOS_ProductUserId GetLoggedInUserByIndex(int32_t index)
        {
            return _EOSApi->_EOS_Connect_GetLoggedInUserByIndex(hIface, index);
        }

        EOS_EResult GetProductUserIdMapping(const EOS_Connect_GetProductUserIdMappingOptions* Options, std::string &str)
        {
            std::string tmp(512, '\0');
            int32_t size = 512;

            EOS_EResult r = _EOSApi->_EOS_Connect_GetProductUserIdMapping(hIface, Options, &tmp[0], &size);
            if(r == EOS_EResult::EOS_Success)
            {
                tmp.resize(size - 1);
                str = tmp;
            }

            return r;
        }

        EOS_ProductUserId GetExternalAccountMapping(const EOS_Connect_GetExternalAccountMappingsOptions* Options)
        {
            return _EOSApi->_EOS_Connect_GetExternalAccountMapping(hIface, Options);
        }
    };

    class CEcom
    {
        friend EOSApi;
        friend CPlatform;
        EOSApi* _EOSApi;
        EOS_HEcom hIface = nullptr;

    public:
        CEcom(EOSApi* eos_api) :
            _EOSApi(eos_api)
        {}

        inline bool HasInterface() const { return hIface != nullptr; }

        void QueryOffers(EOS_Ecom_QueryOffersOptions const* options, std::function<std::remove_pointer_t<EOS_Ecom_OnQueryOffersCallback>> completion_delegate)
        {
            _EOSApi->_EOS_Ecom_QueryOffers(hIface, options, new (decltype(completion_delegate))(std::move(completion_delegate)), &EOSApi::shim_callback<typename boost::function_traits<decltype(completion_delegate)>::arg1_type>);
        }

        uint32_t GetOfferCount(EOS_Ecom_GetOfferCountOptions const* options)
        {
            return _EOSApi->_EOS_Ecom_GetOfferCount(hIface, options);
        }

        EOS_EResult CopyOfferByIndex(EOS_Ecom_CopyOfferByIndexOptions const* options, EOS_Ecom_CatalogOffer** out_offer)
        {
            return _EOSApi->_EOS_Ecom_CopyOfferByIndex(hIface, options, out_offer);
        }

        uint32_t GetOfferItemCount(const EOS_Ecom_GetOfferItemCountOptions* options)
        {
            return _EOSApi->_EOS_Ecom_GetOfferItemCount(hIface, options);
        }

        EOS_EResult CopyOfferItemByIndex(const EOS_Ecom_CopyOfferItemByIndexOptions* options, EOS_Ecom_CatalogItem** out_item)
        {
            return _EOSApi->_EOS_Ecom_CopyOfferItemByIndex(hIface, options, out_item);
        }

        void QueryEntitlements(const EOS_Ecom_QueryEntitlementsOptions* options, std::function<std::remove_pointer_t<EOS_Ecom_OnQueryEntitlementsCallback>> completion_delegate)
        {
            _EOSApi->_EOS_Ecom_QueryEntitlements(hIface, options, new (decltype(completion_delegate))(std::move(completion_delegate)), &EOSApi::shim_callback<typename boost::function_traits<decltype(completion_delegate)>::arg1_type>);
        }

        uint32_t GetEntitlementsCount(const EOS_Ecom_GetEntitlementsCountOptions* options)
        {
            return _EOSApi->_EOS_Ecom_GetEntitlementsCount(hIface, options);
        }

        EOS_EResult CopyEntitlementByIndex(const EOS_Ecom_CopyEntitlementByIndexOptions* options, EOS_Ecom_Entitlement** out_entitlement)
        {
            return _EOSApi->_EOS_Ecom_CopyEntitlementByIndex(hIface, options, out_entitlement);
        }

        void Entitlement_Release(EOS_Ecom_Entitlement* entitlement)
        {
            _EOSApi->_EOS_Ecom_Entitlement_Release(entitlement);
        }

        void CatalogOffer_Release(EOS_Ecom_CatalogOffer* offer)
        {
            _EOSApi->_EOS_Ecom_CatalogOffer_Release(offer);
        }

        void CatalogItem_Release(EOS_Ecom_CatalogItem* catalog_item)
        {
            _EOSApi->_EOS_Ecom_CatalogItem_Release(catalog_item);
        }
    };

    class CStats
    {
        friend EOSApi;
        friend CPlatform;
        EOSApi* _EOSApi;
        EOS_HStats hIface = nullptr;

    public:
        CStats(EOSApi* eos_api) :
            _EOSApi(eos_api)
        {}
        
        inline bool HasInterface() const { return hIface != nullptr; }

        void QueryStats(EOS_Stats_QueryStatsOptions const* options, std::function<std::remove_pointer_t<EOS_Stats_OnQueryStatsCompleteCallback>> completion_delegate)
        {
            if (_EOSApi->_EOS_Stats_QueryStats == nullptr)
                throw std::runtime_error("EOS_Stats_QueryStats is not available");

            _EOSApi->_EOS_Stats_QueryStats(hIface, options, new (decltype(completion_delegate))(std::move(completion_delegate)), &EOSApi::shim_callback<typename boost::function_traits<decltype(completion_delegate)>::arg1_type>);
        }

        uint32_t GetStatsCount(EOS_Stats_GetStatCountOptions const* options)
        {
            return _EOSApi->_EOS_Stats_GetStatsCount(hIface, options);
        }

        EOS_EResult CopyStatByIndex(EOS_Stats_CopyStatByIndexOptions const* options, EOS_Stats_Stat** out_stat)
        {
            return _EOSApi->_EOS_Stats_CopyStatByIndex(hIface, options, out_stat);
        }

        void Stat_Release(EOS_Stats_Stat* stat)
        {
            _EOSApi->_EOS_Stats_Stat_Release(stat);
        }
    };

    class CTitleStorage
    {
        friend EOSApi;
        friend CPlatform;
        EOSApi* _EOSApi;
        EOS_HTitleStorage hIface = nullptr;

    public:
        CTitleStorage(EOSApi* eos_api) :
            _EOSApi(eos_api)
        {}

        inline bool HasInterface() const { return hIface != nullptr; }

        void QueryFile(const EOS_TitleStorage_QueryFileOptions* options, std::function<std::remove_pointer_t<EOS_TitleStorage_OnQueryFileCompleteCallback>> completion_callback)
        {
            if (_EOSApi->_EOS_TitleStorage_QueryFile == nullptr)
                throw std::runtime_error("EOS_TitleStorage_QueryFile is not available");

            _EOSApi->_EOS_TitleStorage_QueryFile(hIface, options, new (decltype(completion_callback))(std::move(completion_callback)), &EOSApi::shim_callback<typename boost::function_traits<decltype(completion_callback)>::arg1_type>);
        }

        void QueryFileList(const EOS_TitleStorage_QueryFileListOptions* options, std::function<std::remove_pointer_t<EOS_TitleStorage_OnQueryFileListCompleteCallback>> completion_callback)
        {
            if (_EOSApi->_EOS_TitleStorage_QueryFileList == nullptr)
                throw std::runtime_error("EOS_TitleStorage_QueryFileList is not available");

            _EOSApi->_EOS_TitleStorage_QueryFileList(hIface, options, new (decltype(completion_callback))(std::move(completion_callback)), &EOSApi::shim_callback<typename boost::function_traits<decltype(completion_callback)>::arg1_type>);
        }

        EOS_HTitleStorageFileTransferRequest ReadFile(const EOS_TitleStorage_ReadFileOptions* options, std::function<std::remove_pointer_t<EOS_TitleStorage_OnReadFileCompleteCallback>> completion_callback)
        {
            if (_EOSApi->_EOS_TitleStorage_ReadFile == nullptr)
                throw std::runtime_error("EOS_TitleStorage_ReadFile is not available");

            return _EOSApi->_EOS_TitleStorage_ReadFile(hIface, options, new (decltype(completion_callback))(std::move(completion_callback)), &EOSApi::shim_callback<typename boost::function_traits<decltype(completion_callback)>::arg1_type>);
        }
    };

    class CLeaderboards
    {
        friend EOSApi;
        friend CPlatform;
        EOSApi* _EOSApi;
        EOS_HLeaderboards hIface = nullptr;

    public:
        CLeaderboards(EOSApi* eos_api) :
            _EOSApi(eos_api)
        {}

        inline bool HasInterface() const { return hIface != nullptr; }

        void QueryLeaderboardDefinitions(const EOS_Leaderboards_QueryLeaderboardDefinitionsOptions* options, std::function<std::remove_pointer_t<EOS_Leaderboards_OnQueryLeaderboardDefinitionsCompleteCallback>> completion_delegate)
        {
            if (_EOSApi->_EOS_Leaderboards_QueryLeaderboardDefinitions == nullptr)
                throw std::runtime_error("EOS_Leaderboards_QueryLeaderboardDefinitions is not available");

            _EOSApi->_EOS_Leaderboards_QueryLeaderboardDefinitions(hIface, options, new (decltype(completion_delegate))(std::move(completion_delegate)), &EOSApi::shim_callback<typename boost::function_traits<decltype(completion_delegate)>::arg1_type>);
        }

        uint32_t GetLeaderboardDefinitionCount(const EOS_Leaderboards_GetLeaderboardDefinitionCountOptions* options)
        {
            return _EOSApi->_EOS_Leaderboards_GetLeaderboardDefinitionCount(hIface, options);
        }

        EOS_EResult CopyLeaderboardDefinitionByIndex(const EOS_Leaderboards_CopyLeaderboardDefinitionByIndexOptions* options, EOS_Leaderboards_Definition** out_definition)
        {
            return _EOSApi->_EOS_Leaderboards_CopyLeaderboardDefinitionByIndex(hIface, options, out_definition);
        }

        void Definition_Release(EOS_Leaderboards_Definition* leaderboard_definition)
        {
            _EOSApi->_EOS_Leaderboards_Definition_Release(leaderboard_definition);
        }
    };

public:
    CPlatform Platform;
    CAchievements Achievements;
    CAuth Auth;
    CConnect Connect;
    CEcom Ecom;
    CStats Stats;
    CLeaderboards Leaderboards;
    CTitleStorage TitleStorage;

    EOSApi():
        Platform(this),
        Achievements(this),
        Auth(this),
        Connect(this),
        Ecom(this),
        Stats(this),
        Leaderboards(this),
        TitleStorage(this)
    {
        std::string lib_path = get_executable_path();
        lib_path = lib_path.substr(0, lib_path.rfind(PATH_SEPARATOR) + 1);
        lib_path += EOS_LIBRARY_NAME;
        if (!eos_api.load_library(lib_path, false))
        {
            throw std::runtime_error("Canno't load " EOS_LIBRARY_NAME);
        }

        load_symbols(eos_api.native_handle());

#define LOAD_API(_THIS, NAME) if( ((_THIS)->_##NAME = reinterpret_cast<decltype(::NAME)*>(GET_PROC_ADDRESS(eos_api.native_handle(), #NAME))) == nullptr) throw std::runtime_error("Cannot load " #NAME)
#define LOAD_OPTIONNAL(_THIS, NAME) (_THIS)->_##NAME = reinterpret_cast<decltype(::NAME)*>(GET_PROC_ADDRESS(eos_api.native_handle(), #NAME))

        // EOS API
        LOAD_API(this, EOS_Initialize);
        LOAD_API(this, EOS_Shutdown);
        LOAD_API(this, EOS_EResult_ToString);
        LOAD_API(this, EOS_EpicAccountId_IsValid);
        LOAD_API(this, EOS_EpicAccountId_ToString);
        LOAD_API(this, EOS_EpicAccountId_FromString);
        LOAD_API(this, EOS_ProductUserId_IsValid);
        LOAD_API(this, EOS_ProductUserId_ToString);
        LOAD_API(this, EOS_ProductUserId_FromString);

        // Platform
        LOAD_API(this, EOS_Platform_Create);
        LOAD_API(this, EOS_Platform_Tick);
        LOAD_API(this, EOS_Platform_SetOverrideLocaleCode);
        LOAD_API(this, EOS_Platform_GetAuthInterface);
        LOAD_API(this, EOS_Platform_GetConnectInterface);
        LOAD_API(this, EOS_Platform_GetEcomInterface);

        LOAD_OPTIONNAL(this, EOS_Platform_GetAchievementsInterface);
        LOAD_OPTIONNAL(this, EOS_Platform_GetStatsInterface);
        LOAD_OPTIONNAL(this, EOS_Platform_GetLeaderboardsInterface);
        LOAD_OPTIONNAL(this, EOS_Platform_GetTitleStorageInterface);

        // Achievements
        LOAD_OPTIONNAL(this, EOS_Achievements_QueryDefinitions);
        LOAD_OPTIONNAL(this, EOS_Achievements_GetAchievementDefinitionCount);
        LOAD_OPTIONNAL(this, EOS_Achievements_CopyAchievementDefinitionByIndex);
        LOAD_OPTIONNAL(this, EOS_Achievements_Definition_Release);
        LOAD_OPTIONNAL(this, EOS_Achievements_CopyAchievementDefinitionV2ByIndex);
        LOAD_OPTIONNAL(this, EOS_Achievements_CopyAchievementDefinitionV2ByAchievementId);
        LOAD_OPTIONNAL(this, EOS_Achievements_UnlockAchievements);
        LOAD_OPTIONNAL(this, EOS_Achievements_DefinitionV2_Release);

        // Auth
        LOAD_API(this, EOS_Auth_Login);
        LOAD_API(this, EOS_Auth_GetLoggedInAccountByIndex);
        LOAD_API(this, EOS_Auth_CopyUserAuthToken);

        // Connect
        LOAD_API(this, EOS_Connect_Login);
        LOAD_API(this, EOS_Connect_CreateUser);
        LOAD_API(this, EOS_Connect_GetLoggedInUserByIndex);
        LOAD_API(this, EOS_Connect_GetProductUserIdMapping);
        LOAD_API(this, EOS_Connect_GetExternalAccountMapping);

        // Ecom
        LOAD_API(this, EOS_Ecom_QueryOffers);
        LOAD_API(this, EOS_Ecom_GetOfferCount);
        LOAD_API(this, EOS_Ecom_CopyOfferByIndex);
        LOAD_API(this, EOS_Ecom_GetOfferItemCount);
        LOAD_API(this, EOS_Ecom_CopyOfferItemByIndex);
        LOAD_API(this, EOS_Ecom_QueryEntitlements);
        LOAD_API(this, EOS_Ecom_GetEntitlementsCount);
        LOAD_API(this, EOS_Ecom_CopyEntitlementByIndex);
        LOAD_API(this, EOS_Ecom_Entitlement_Release);
        LOAD_API(this, EOS_Ecom_CatalogOffer_Release);
        LOAD_API(this, EOS_Ecom_CatalogItem_Release);

        // Stats
        LOAD_OPTIONNAL(this, EOS_Stats_QueryStats);
        LOAD_OPTIONNAL(this, EOS_Stats_GetStatsCount);
        LOAD_OPTIONNAL(this, EOS_Stats_CopyStatByIndex);
        LOAD_OPTIONNAL(this, EOS_Stats_Stat_Release);

        // Leaderboards
        LOAD_OPTIONNAL(this, EOS_Leaderboards_QueryLeaderboardDefinitions);
        LOAD_OPTIONNAL(this, EOS_Leaderboards_GetLeaderboardDefinitionCount);
        LOAD_OPTIONNAL(this, EOS_Leaderboards_CopyLeaderboardDefinitionByIndex);
        LOAD_OPTIONNAL(this, EOS_Leaderboards_Definition_Release);

        // TitleStorage
        LOAD_OPTIONNAL(this, EOS_TitleStorage_QueryFile);
        LOAD_OPTIONNAL(this, EOS_TitleStorage_QueryFileList);
        LOAD_OPTIONNAL(this, EOS_TitleStorage_CopyFileMetadataByFilename);
        LOAD_OPTIONNAL(this, EOS_TitleStorage_GetFileMetadataCount);
        LOAD_OPTIONNAL(this, EOS_TitleStorage_CopyFileMetadataAtIndex);
        LOAD_OPTIONNAL(this, EOS_TitleStorage_ReadFile);
        LOAD_OPTIONNAL(this, EOS_TitleStorage_DeleteCache);

#undef LOAD_API
    }

    void Initialize(EOS_InitializeOptions const* options)
    {
        if (_EOS_Initialize(options) != EOS_EResult::EOS_Success)
            throw std::runtime_error("Failed to initialize EOS API.");
    }

    EOS_EResult Shutdown()
    {
        return _EOS_Shutdown();
    }

    std::string EResult_ToString(EOS_EResult result)
    {
        return _EOS_EResult_ToString(result);
    }

    EOS_Bool EpicAccountId_IsValid(EOS_EpicAccountId AccountId)
    {
        return _EOS_EpicAccountId_IsValid(AccountId);
    }

    std::string EpicAccountId_ToString(EOS_EpicAccountId AccountId)
    {
        std::string res(512, '\0');
        int32_t size = 512;

        if (_EOS_EpicAccountId_ToString(AccountId, &res[0], &size) == EOS_EResult::EOS_Success)
        {
            res.resize(size - 1);
        }
        else
        {
            res.clear();
        }

        return res;
    }

    EOS_EpicAccountId EpicAccountId_FromString(const char * str)
    {
        return _EOS_EpicAccountId_FromString(str);
    }

    EOS_Bool ProductUserId_IsValid(EOS_ProductUserId AccountId)
    {
        return _EOS_ProductUserId_IsValid(AccountId);
    }

    std::string ProductUserId_ToString(EOS_ProductUserId AccountId)
    {
        std::string res(512, '\0');
        int32_t size = 512;

        if (_EOS_ProductUserId_ToString(AccountId, &res[0], &size) == EOS_EResult::EOS_Success)
        {
            res.resize(size - 1);
        }
        else
        {
            res.clear();
        }

        return res;
    }

    EOS_ProductUserId ProductUserId_FromString(const char* str)
    {
        return _EOS_ProductUserId_FromString(str);
    }
};

static EOSApi eos_api;
static std::string dumper_root;

static void EOSApiInitialize(std::string product_name, std::string product_version, int32_t api_version = EOS_INITIALIZE_API_LATEST)
{
    EOS_InitializeOptions InitOptions{};
    InitOptions.ApiVersion = EOS_INITIALIZE_API_LATEST;
    InitOptions.ProductName = product_name.c_str();
    InitOptions.ProductVersion = product_version.c_str();

    SPDLOG_TRACE("Initalizing API: {}, {}", product_name, product_version);
    eos_api.Initialize(&InitOptions);
}

static void PlatformInitialize(std::string deployement_id, std::string product_id, std::string sandbox_id, std::string client_id, std::string client_secret, std::string cache_directory, std::string encryption_key, uint64_t flags = EOS_PF_DISABLE_OVERLAY, int32_t api_version = EOS_PLATFORM_OPTIONS_API_LATEST)
{
    EOS_Platform_Options PlatformOptions{};
    PlatformOptions.ApiVersion = api_version;
    PlatformOptions.DeploymentId = deployement_id.c_str();
    PlatformOptions.ProductId = product_id.c_str();
    PlatformOptions.SandboxId = sandbox_id.c_str();
    PlatformOptions.ClientCredentials.ClientId = client_id.c_str();
    PlatformOptions.ClientCredentials.ClientSecret = client_secret.c_str();
    PlatformOptions.CacheDirectory = cache_directory.c_str();
    PlatformOptions.EncryptionKey = encryption_key.empty() ? nullptr : encryption_key.c_str();

    PlatformOptions.Flags = flags;

    SPDLOG_TRACE("Creating Platform: ApiVersion({}), DeployementId({}), ProductId({}), SandboxId({}), ClientId({}), ClientSecret({}), EncryptionKey({})", api_version, deployement_id, product_id, sandbox_id, client_id, client_secret, encryption_key);
    try
    {
        eos_api.Platform.Create(&PlatformOptions);
    }
    catch (...)
    {
        if (api_version > 1)
        {
            PlatformInitialize(deployement_id, product_id, sandbox_id, client_id, client_secret, cache_directory, encryption_key, flags, api_version - 1);
        }
    }
}

static std::atomic<bool> DoTick;
static std::thread tick_thread;
static void StartApiTick()
{
    DoTick = true;
    tick_thread = std::thread([]()
    {
        while (DoTick)
        {
            eos_api.Platform.Tick();
            std::this_thread::sleep_for(std::chrono::milliseconds{ 100 });
        }
    });
}

static void AuthLogin(std::string auth_password, EOS_ELoginCredentialType auth_type, int32_t api_version = EOS_AUTH_LOGIN_API_LATEST)
{
    std::atomic<int> go;

    EOS_Auth_LoginOptions options{};
    EOS_Auth_Credentials credentials{};
    options.ApiVersion = api_version;
    options.ScopeFlags = EOS_EAuthScopeFlags::EOS_AS_NoFlags;
    //options.ScopeFlags = EOS_EAuthScopeFlags::EOS_AS_BasicProfile;

    credentials.ApiVersion = api_version;
    credentials.ExternalType = EOS_EExternalCredentialType::EOS_ECT_EPIC;
    credentials.Id = nullptr;
    credentials.Token = auth_password.c_str();
    credentials.Type = auth_type;

    options.Credentials = &credentials;

    SPDLOG_TRACE("Auth login...");

    go = -1;
    eos_api.Auth.Login(&options, [&go](EOS_Auth_LoginCallbackInfo const* infos)
    {
        go = (int)infos->ResultCode;
    });

    while (go == -1);

    if (go != (int)EOS_EResult::EOS_Success)
    {
        if (go == (int)EOS_EResult::EOS_IncompatibleVersion && api_version > 1)
        {
            AuthLogin(auth_password, auth_type, api_version - 1);
        }
        else
        {
            throw std::runtime_error(fmt::format("Failed to auth login: {}", eos_api.EResult_ToString((EOS_EResult)go.load())));
        }
    }
}

static void ConnectLogin(int32_t api_version = EOS_CONNECT_LOGIN_API_LATEST)
{
    std::atomic<int> go;
    EOS_ContinuanceToken token;

    EOS_Auth_CopyUserAuthTokenOptions auth_options{};
    auth_options.ApiVersion = EOS_AUTH_COPYUSERAUTHTOKEN_API_LATEST;

    EOS_Auth_Token* auth_token = nullptr;

    SPDLOG_TRACE("Getting auth token");
    if (eos_api.Auth.CopyUserAuthToken(&auth_options, eos_api.Auth.GetLoggedInAccountByIndex(0), &auth_token) != EOS_EResult::EOS_Success)
    {
        throw std::runtime_error("Failed to get Auth Token.");
    }

    EOS_Connect_LoginOptions options{};
    options.ApiVersion = api_version;

    EOS_Connect_Credentials credentials{};
    credentials.ApiVersion = EOS_CONNECT_CREDENTIALS_API_LATEST;
    credentials.Token = auth_token->AccessToken;
    credentials.Type = EOS_EExternalCredentialType::EOS_ECT_EPIC;

    options.Credentials = &credentials;

    SPDLOG_TRACE("Connect login...");

    go = -1;
    eos_api.Connect.Login(&options, [&](EOS_Connect_LoginCallbackInfo const* infos)
    {
        go = (int)infos->ResultCode;
        token = infos->ContinuanceToken;
    });

    while (go == -1);

    if (go == (int)EOS_EResult::EOS_InvalidUser && token != nullptr)
    {
        EOS_Connect_CreateUserOptions options{};
        options.ApiVersion = EOS_CONNECT_CREATEUSER_API_LATEST;
        options.ContinuanceToken = token;

        SPDLOG_TRACE("Connect create user...");

        go = -1;
        eos_api.Connect.CreateUser(&options, [&](EOS_Connect_CreateUserCallbackInfo const* infos)
        {
            go = (int)infos->ResultCode;
        });

        while (go == -1);
    }

    if (go != (int)EOS_EResult::EOS_Success)
    {
        if (go == (int)EOS_EResult::EOS_IncompatibleVersion && api_version > 1)
        {
            ConnectLogin(api_version - 1);
        }
        else
        {
            throw std::runtime_error(fmt::format("Failed to connect: {}", eos_api.EResult_ToString((EOS_EResult)go.load())));
        }
    }
}

static uint32_t GetAchievementsCount(int32_t api_version = EOS_ACHIEVEMENTS_QUERYDEFINITIONS_API_LATEST)
{
    if (!eos_api.Achievements.HasInterface())
        return 0;

    std::atomic<int> go;

    EOS_Achievements_QueryDefinitionsOptions query_options{};
    query_options.ApiVersion = api_version;
    query_options.EpicUserId_DEPRECATED = eos_api.Auth.GetLoggedInAccountByIndex(0);
    query_options.LocalUserId = eos_api.Connect.GetLoggedInUserByIndex(0);

    SPDLOG_TRACE("{}, {}", eos_api.EpicAccountId_ToString(query_options.EpicUserId_DEPRECATED), eos_api.ProductUserId_ToString(query_options.LocalUserId));

    go = -1;
    SPDLOG_TRACE("Querying achievements...");
    eos_api.Achievements.QueryDefinitions(&query_options, [&go](EOS_Achievements_OnQueryDefinitionsCompleteCallbackInfo const* infos)
    {
        go = (int)infos->ResultCode;
    });

    while (go == -1);

    if (go != (int)EOS_EResult::EOS_Success)
    {
        if (go == (int)EOS_EResult::EOS_IncompatibleVersion && api_version > 1)
        {
            return GetAchievementsCount(api_version - 1);
        }
        else
        {
            throw std::runtime_error(fmt::format("Failed to query achievements: {}", eos_api.EResult_ToString((EOS_EResult)go.load())));
        }
    }

    EOS_Achievements_GetAchievementDefinitionCountOptions options{};
    options.ApiVersion = EOS_ACHIEVEMENTS_GETACHIEVEMENTDEFINITIONCOUNT_API_LATEST;

    auto count = eos_api.Achievements.GetAchievementDefinitionCount(&options);
    SPDLOG_TRACE("Getting achievements count: {}", count);

    return count;
}

static void MakeAchievementsV1()
{
    if (!eos_api.Achievements.HasOldAchievements())
        return;

    uint32_t count = GetAchievementsCount();

    constexpr static char achievements_db_file[] = "achievements_db1.json";
    nlohmann::ordered_json achievements_db = nlohmann::json::array_t();

    for (uint32_t i = 0; i < count; ++i)
    {
        EOS_Achievements_CopyAchievementDefinitionByIndexOptions DefinitionOptions;
        DefinitionOptions.ApiVersion = EOS_ACHIEVEMENTS_COPYDEFINITIONBYINDEX_API_LATEST;
        DefinitionOptions.AchievementIndex = i;

        EOS_Achievements_Definition* OutDefinition;

        auto res = eos_api.Achievements.CopyAchievementDefinitionByIndex(&DefinitionOptions, &OutDefinition);
        if (res == EOS_EResult::EOS_Success)
        {
            std::string url(OutDefinition->AchievementId);
            std::string url_locked(std::string(OutDefinition->AchievementId) + "_locked");

            nlohmann::ordered_json entry = nlohmann::ordered_json{
                    {"AchievementId"        , str_or_empty(OutDefinition->AchievementId)},
                    {"UnlockedDisplayName"  , str_or_empty(OutDefinition->DisplayName)},
                    {"UnlockedDescription"  , str_or_empty(OutDefinition->Description)},
                    {"LockedDisplayName"    , str_or_empty(OutDefinition->LockedDisplayName)},
                    {"LockedDescription"    , str_or_empty(OutDefinition->LockedDescription)},
                    {"HiddenDescription"    , str_or_empty(OutDefinition->HiddenDescription)},
                    {"FlavorText"           , str_or_empty("")},
                    {"CompletionDescription", str_or_empty(OutDefinition->CompletionDescription)},
                    {"UnlockedIconUrl"      , url},
                    {"LockedIconUrl"        , url_locked},
                    {"IsHidden"             , (bool)OutDefinition->bIsHidden}
            };

            SPDLOG_INFO("locked icon: {}", OutDefinition->LockedIconId);
            //download_icon(OutDefinition->UnlockedIconURL, dumper_root + url);
            //download_icon(OutDefinition->LockedIconURL, dumper_root + url_locked);

            for (int i = 0; i < OutDefinition->StatThresholdsCount; ++i)
            {
                entry["StatsThresholds"].emplace_back(nlohmann::ordered_json{
                    {"Name"     , str_or_empty(OutDefinition->StatThresholds[i].Name)},
                    {"Threshold", OutDefinition->StatThresholds[i].Threshold}
                });
            }

            achievements_db.emplace_back(std::move(entry));
            eos_api.Achievements.Definition_Release(OutDefinition);
        }
        else
        {
            SPDLOG_ERROR("Failed to dump achievement {}: {}", i, eos_api.EResult_ToString(res));
        }
    }

    save_json(dumper_root + achievements_db_file, achievements_db);
}

static void MakeAchievementsV2()
{
    if (!eos_api.Achievements.HasNewAchievements())
        return;

    uint32_t count = GetAchievementsCount();

    constexpr static char achievements_db_file[] = "achievements_db2.json";
    nlohmann::ordered_json achievements_db = nlohmann::json::array_t();
    
    for (uint32_t i = 0; i < count; ++i)
    {
        EOS_Achievements_CopyAchievementDefinitionV2ByIndexOptions DefinitionOptions;
        DefinitionOptions.ApiVersion = EOS_ACHIEVEMENTS_COPYACHIEVEMENTDEFINITIONV2BYINDEX_API_002;
        DefinitionOptions.AchievementIndex = i;
    
        EOS_Achievements_DefinitionV2* OutDefinition;
    
        auto res = eos_api.Achievements.CopyAchievementDefinitionV2ByIndex(&DefinitionOptions, &OutDefinition);
        if (res == EOS_EResult::EOS_Success)
        {
            std::string url(OutDefinition->AchievementId);
            std::string url_locked(std::string(OutDefinition->AchievementId) + "_locked");
    
            nlohmann::ordered_json entry = nlohmann::ordered_json{
                    {"AchievementId"        , str_or_empty(OutDefinition->AchievementId)},
                    {"UnlockedDisplayName"  , str_or_empty(OutDefinition->UnlockedDisplayName)},
                    {"UnlockedDescription"  , str_or_empty(OutDefinition->UnlockedDescription)},
                    {"LockedDisplayName"    , str_or_empty(OutDefinition->LockedDisplayName)},
                    {"LockedDescription"    , str_or_empty(OutDefinition->LockedDescription)},
                    {"HiddenDescription"    , str_or_empty(OutDefinition->LockedDescription)},
                    {"FlavorText"           , str_or_empty(OutDefinition->FlavorText)},
                    {"CompletionDescription", str_or_empty(OutDefinition->UnlockedDescription)},
                    {"UnlockedIconUrl"      , url},
                    {"LockedIconUrl"        , url_locked},
                    {"IsHidden"             , (bool)OutDefinition->bIsHidden}
            };
    
            download_icon(OutDefinition->UnlockedIconURL, dumper_root + "achievements_images/" + url);
            download_icon(OutDefinition->LockedIconURL, dumper_root + "achievements_images/" + url_locked);
    
            for (int i = 0; i < OutDefinition->StatThresholdsCount; ++i)
            {
                entry["StatsThresholds"].emplace_back(nlohmann::ordered_json{
                    {"Name"     , str_or_empty(OutDefinition->StatThresholds[i].Name)},
                    {"Threshold", OutDefinition->StatThresholds[i].Threshold}
                });
            }
    
            achievements_db.emplace_back(std::move(entry));
            eos_api.Achievements.DefinitionV2_Release(OutDefinition);
        }
        else
        {
            SPDLOG_ERROR("Failed to dump achievement {}: {}", i, eos_api.EResult_ToString(res));
        }
    }

    save_json(dumper_root + achievements_db_file, achievements_db);
}

static uint32_t GetStatsCount()
{
    if (!eos_api.Stats.HasInterface())
        return 0;

    std::atomic<int> go;

    EOS_Stats_QueryStatsOptions query_options{};
    query_options.ApiVersion = EOS_STATS_QUERYSTATS_API_LATEST;
    query_options.UserId = eos_api.Connect.GetLoggedInUserByIndex(0);
    query_options.TargetUserId = eos_api.Connect.GetLoggedInUserByIndex(0);
    query_options.StartTime = EOS_STATS_TIME_UNDEFINED;
    query_options.EndTime = EOS_STATS_TIME_UNDEFINED;
    query_options.StatNames = nullptr;
    query_options.StatNamesCount = 0;

    SPDLOG_TRACE("{}", eos_api.ProductUserId_ToString(query_options.UserId));

    go = -1;
    SPDLOG_TRACE("Querying stats...");
    eos_api.Stats.QueryStats(&query_options, [&go](EOS_Stats_OnQueryStatsCompleteCallbackInfo const* infos)
    {
        go = (int)infos->ResultCode;
    });

    while (go == -1);

    if (go != (int)EOS_EResult::EOS_Success)
    {
        throw std::runtime_error(fmt::format("Failed to query stats: {}", eos_api.EResult_ToString((EOS_EResult)go.load())));
    }

    nlohmann::ordered_json stats = nlohmann::json::object_t();

    EOS_Stats_GetStatCountOptions count_options;
    count_options.ApiVersion = EOS_STATS_GETSTATSCOUNT_API_LATEST;
    count_options.UserId = query_options.UserId;

    uint32_t count = eos_api.Stats.GetStatsCount(&count_options);

    SPDLOG_TRACE("Getting stats count: {}", count);

    return count;
}

static void MakeStats()
{
    constexpr const char stats_file[] = "stats.json";

    nlohmann::ordered_json stats = nlohmann::json::object_t();

    uint32_t count = GetStatsCount();

    if (count > 0)
    {
        EOS_Stats_CopyStatByIndexOptions stat_options;
        stat_options.ApiVersion = EOS_STATS_COPYSTATBYINDEX_API_LATEST;
        stat_options.TargetUserId = eos_api.Connect.GetLoggedInUserByIndex(0);

        EOS_Stats_Stat* stat;

        for (int i = 0; i < count; ++i)
        {
            stat_options.StatIndex = i;

            if (eos_api.Stats.CopyStatByIndex(&stat_options, &stat) == EOS_EResult::EOS_Success)
            {
                stats[stat->Name] = nlohmann::json{
                    {"Value"    , stat->Value},
                    {"StartTime", stat->StartTime},
                    {"EndTime"  , stat->EndTime},
                };

                eos_api.Stats.Stat_Release(stat);
            }
        }

        save_json(dumper_root + stats_file, stats);
    }
}

static uint32_t GetEntitlementCount()
{
    std::atomic<int> go;

    EOS_Ecom_QueryEntitlementsOptions query_options;

    query_options.ApiVersion = EOS_ECOM_QUERYENTITLEMENTS_API_LATEST;
    query_options.LocalUserId = eos_api.Auth.GetLoggedInAccountByIndex(0);
    query_options.EntitlementNameCount = 0;
    query_options.EntitlementNames = nullptr;
    query_options.bIncludeRedeemed = EOS_TRUE;

    go = -1;
    SPDLOG_TRACE("Querying entitlements...");
    eos_api.Ecom.QueryEntitlements(&query_options, [&go](EOS_Ecom_QueryEntitlementsCallbackInfo const* infos)
    {
        go = (int)infos->ResultCode;
    });

    while (go == -1);

    if (go != (int)EOS_EResult::EOS_Success)
    {
        throw std::runtime_error(fmt::format("Failed to query entitlements: {}", eos_api.EResult_ToString((EOS_EResult)go.load())));
    }

    EOS_Ecom_GetEntitlementsCountOptions count_options;
    count_options.ApiVersion = EOS_ECOM_GETENTITLEMENTSCOUNT_API_LATEST;
    count_options.LocalUserId = eos_api.Auth.GetLoggedInAccountByIndex(0);

    int count = eos_api.Ecom.GetEntitlementsCount(&count_options);

    SPDLOG_TRACE("Getting entitlements count: {}", count);

    return count;
}

static void MakeEntitlements()
{
    uint32_t count = GetEntitlementCount();

    if (count > 0)
    {
        EOS_Ecom_CopyEntitlementByIndexOptions entitlement_opts;
        entitlement_opts.ApiVersion = EOS_ECOM_COPYENTITLEMENTBYINDEX_API_LATEST;
        entitlement_opts.LocalUserId = eos_api.Auth.GetLoggedInAccountByIndex(0);

        for (int i = 0; i < count; ++i)
        {
            entitlement_opts.EntitlementIndex = i;
            EOS_Ecom_Entitlement* entitlement;
            if (eos_api.Ecom.CopyEntitlementByIndex(&entitlement_opts, &entitlement) == EOS_EResult::EOS_Success && entitlement->EntitlementId != nullptr)
            {

                eos_api.Ecom.Entitlement_Release(entitlement);
            }
        }

        //save_json(dumper_root + catalog_file, catalog);
    }
}

static uint32_t GetCatalogCount()
{
    std::atomic<int> go;

    EOS_Ecom_QueryOffersOptions query_options;

    query_options.ApiVersion = EOS_ECOM_QUERYOFFERS_API_LATEST;
    query_options.LocalUserId = eos_api.Auth.GetLoggedInAccountByIndex(0);
    query_options.OverrideCatalogNamespace = nullptr;

    SPDLOG_TRACE("{}", eos_api.EpicAccountId_ToString(query_options.LocalUserId));

    go = -1;
    SPDLOG_TRACE("Querying offers...");
    eos_api.Ecom.QueryOffers(&query_options, [&go](EOS_Ecom_QueryOffersCallbackInfo const* infos)
    {
        go = (int)infos->ResultCode;
    });

    while (go == -1);

    if (go != (int)EOS_EResult::EOS_Success)
    {
        throw std::runtime_error(fmt::format("Failed to query offers: {}", eos_api.EResult_ToString((EOS_EResult)go.load())));
    }

    EOS_Ecom_GetOfferCountOptions count_options;
    count_options.ApiVersion = EOS_ECOM_GETOFFERCOUNT_API_LATEST;
    count_options.LocalUserId = eos_api.Auth.GetLoggedInAccountByIndex(0);

    int count = eos_api.Ecom.GetOfferCount(&count_options);

    SPDLOG_TRACE("Getting offers count: {}", count);

    return count;
}

static void MakeCatalog()
{
    constexpr const char catalog_file[] = "catalog.json";

    nlohmann::ordered_json catalog = nlohmann::json::object_t();

    uint32_t count = GetCatalogCount();

    if (count > 0)
    {
        EOS_Ecom_CopyOfferByIndexOptions offer_opts;
        offer_opts.ApiVersion = EOS_ECOM_COPYOFFERBYINDEX_API_LATEST;
        offer_opts.LocalUserId = eos_api.Auth.GetLoggedInAccountByIndex(0);

        for (int i = 0; i < count; ++i)
        {
            offer_opts.OfferIndex = i;
            EOS_Ecom_CatalogOffer* offer;
            if (eos_api.Ecom.CopyOfferByIndex(&offer_opts, &offer) == EOS_EResult::EOS_Success && offer->Id != nullptr)
            {
                // CatalogItemId (not the entitlement ID!)
                catalog[offer->Id] = nlohmann::ordered_json{
                    {"Name"     , str_or_empty(offer->TitleText)},
                    {"Namespace", str_or_empty(offer->CatalogNamespace)}, // plaftorm->SandboxID
                    {"Owned"    , true},
                };

                EOS_Ecom_GetOfferItemCountOptions offer_item_opts;
                offer_item_opts.ApiVersion = EOS_ECOM_GETOFFERITEMCOUNT_API_LATEST;
                offer_item_opts.LocalUserId = eos_api.Auth.GetLoggedInAccountByIndex(0);
                offer_item_opts.OfferId = offer->Id;

                uint32_t offer_item_count = eos_api.Ecom.GetOfferItemCount(&offer_item_opts);

                for (int i = 0; i < offer_item_count; ++i)
                {
                    EOS_Ecom_CopyOfferItemByIndexOptions options;
                    options.ApiVersion = EOS_ECOM_COPYOFFERITEMBYINDEX_API_LATEST;
                    options.ItemIndex = i;
                    options.LocalUserId = eos_api.Auth.GetLoggedInAccountByIndex(0);
                    options.OfferId = offer->Id;

                    EOS_Ecom_CatalogItem* catalog_item;

                    if (eos_api.Ecom.CopyOfferItemByIndex(&options, &catalog_item) == EOS_EResult::EOS_Success)
                    {
                        eos_api.Ecom.CatalogItem_Release(catalog_item);
                    }
                }

                eos_api.Ecom.CatalogOffer_Release(offer);
            }
        }

        save_json(dumper_root + catalog_file, catalog);
    }
}

static uint32_t GetLeaderboardsCount()
{
    std::atomic<int> go;

    EOS_Leaderboards_QueryLeaderboardDefinitionsOptions query_options;

    query_options.ApiVersion = EOS_LEADERBOARDS_QUERYLEADERBOARDDEFINITIONS_API_LATEST;
    query_options.LocalUserId = eos_api.Connect.GetLoggedInUserByIndex(0);
    query_options.StartTime = EOS_LEADERBOARDS_TIME_UNDEFINED;
    query_options.EndTime = EOS_LEADERBOARDS_TIME_UNDEFINED;

    go = -1;
    SPDLOG_TRACE("Querying leaderboards...");
    eos_api.Leaderboards.QueryLeaderboardDefinitions(&query_options, [&go](EOS_Leaderboards_OnQueryLeaderboardDefinitionsCompleteCallbackInfo const* infos)
    {
        go = (int)infos->ResultCode;
    });

    while (go == -1);

    if (go != (int)EOS_EResult::EOS_Success)
    {
        throw std::runtime_error(fmt::format("Failed to query leaderboards: {}", eos_api.EResult_ToString((EOS_EResult)go.load())));
    }

    EOS_Leaderboards_GetLeaderboardDefinitionCountOptions count_options;
    count_options.ApiVersion = EOS_LEADERBOARDS_GETLEADERBOARDDEFINITIONCOUNT_API_LATEST;

    int count = eos_api.Leaderboards.GetLeaderboardDefinitionCount(&count_options);

    SPDLOG_TRACE("Getting leaderboards count: {}", count);

    return count;
}

static void MakeLeaderboards()
{
    constexpr const char leaderboards_file[] = "leaderboards_db.json";

    nlohmann::ordered_json leaderboards = nlohmann::json::object_t();

    uint32_t count = GetLeaderboardsCount();

    if (count > 0)
    {
        EOS_Leaderboards_CopyLeaderboardDefinitionByIndexOptions leaderboard_opts;
        leaderboard_opts.ApiVersion = EOS_LEADERBOARDS_COPYLEADERBOARDDEFINITIONBYINDEX_API_LATEST;

        EOS_Leaderboards_Definition* leadeboard_definition = nullptr;
        for (int i = 0; i < count; ++i)
        {
            leaderboard_opts.LeaderboardIndex = i;
            if (eos_api.Leaderboards.CopyLeaderboardDefinitionByIndex(&leaderboard_opts, &leadeboard_definition) == EOS_EResult::EOS_Success && leadeboard_definition->LeaderboardId != nullptr)
            {
                leaderboards[leadeboard_definition->LeaderboardId] = nlohmann::ordered_json{
                    {"StatName"   , str_or_empty(leadeboard_definition->StatName)},
                    {"StartTime"  , leadeboard_definition->StartTime},
                    {"EndTime"    , leadeboard_definition->EndTime},
                    {"Aggregation" , (int)leadeboard_definition->Aggregation},
                };

                eos_api.Leaderboards.Definition_Release(leadeboard_definition);
            }
        }

        save_json(dumper_root + leaderboards_file, leaderboards);
    }
}

void DownloadTitleStorage(std::string filename)
{
    std::atomic<int> go;

    EOS_TitleStorage_QueryFileOptions query_options{};

    query_options.ApiVersion = EOS_TITLESTORAGE_QUERYFILEOPTIONS_API_LATEST;
    query_options.LocalUserId = eos_api.Connect.GetLoggedInUserByIndex(0);
    query_options.Filename = filename.c_str();

    go = -1;
    SPDLOG_TRACE("Querying title storage...");
    eos_api.TitleStorage.QueryFile(&query_options, [&go](EOS_TitleStorage_QueryFileCallbackInfo const* infos)
    {
        go = (int)infos->ResultCode;
    });

    while (go == -1);

    if (go != (int)EOS_EResult::EOS_Success)
    {
        throw std::runtime_error(fmt::format("Failed to query title storage: {}", eos_api.EResult_ToString((EOS_EResult)go.load())));
    }

    EOS_TitleStorage_ReadFileOptions read_options{};

    read_options.ApiVersion = EOS_TITLESTORAGE_READFILEOPTIONS_API_LATEST;
    read_options.LocalUserId = eos_api.Connect.GetLoggedInUserByIndex(0);
    read_options.Filename = filename.c_str();
    read_options.ReadChunkLengthBytes = 65536;
    read_options.ReadFileDataCallback = [](const EOS_TitleStorage_ReadFileDataCallbackInfo* Data) -> EOS_TitleStorage_EReadResult
    {
        std::string chunk((const char*)Data->DataChunk, (const char*)Data->DataChunk + Data->DataChunkLengthBytes);

        return EOS_TitleStorage_EReadResult::EOS_TS_RR_ContinueReading;
    };

    go = -1;
    eos_api.TitleStorage.ReadFile(&read_options, [&go](EOS_TitleStorage_ReadFileCallbackInfo const* infos)
    {
        go = (int)infos->ResultCode;
    });

    while (go == -1);

    if (go != (int)EOS_EResult::EOS_Success)
    {
        throw std::runtime_error(fmt::format("Failed to query title storage: {}", eos_api.EResult_ToString((EOS_EResult)go.load())));
    }
}

void ParseCmdLine()
{
    auto args = get_proc_argv();
    std::vector<const char*> c_args;

    for(auto& arg : args)
    {
        c_args.emplace_back(arg.c_str());
    }

    try
    {
        cxxopts::Options options("epic dumper", "App to dump some of the API datas.");
        options.add_options()
            ("i,icons", "Download achievements icons.", cxxopts::value<bool>()->default_value("false"));
        auto result = options.parse(c_args.size(), &c_args[0]);

        AppArgs.DownloadIcons = result["icons"].as<bool>();
    }
    catch(std::exception& e)
    {
        std::cerr << "Failed to parse command line: " << e.what() << std::endl;
        exit(1);
    }
}

EGS_Api::Error LoginWithSid(EGS_Api& egs_api)
{
    std::string user_input;

    std::cout << "EGL sid (get it at: https://www.epicgames.com/id/login?redirectUrl=https://www.epicgames.com/id/api/redirect): ";
    std::cin >> user_input;

    return egs_api.LoginSID(user_input);
}

EGS_Api::Error LoginWithAuthorizationCode(EGS_Api& egs_api)
{
    std::string user_input;

    std::cout << "EGL authorization code (get it at: https://www.epicgames.com/id/api/redirect?clientId=34a02cf8f4414e29b15921876da36f9a&responseType=code): ";
    std::cin >> user_input;

    return egs_api.LoginAuthorizationCode(user_input);
}

int main(int argc, char* argv[])
{
    std::vector<std::string> args;

    ParseCmdLine();

    {
        dumper_root = get_executable_path();
        dumper_root = dumper_root.substr(0, dumper_root.find_last_of("/\\") + 1) + "dumper/";

        create_folder(dumper_root);

        auto sinks = std::make_shared<spdlog::sinks::dist_sink_mt>();

        sinks->add_sink(std::make_shared<spdlog::sinks::stdout_color_sink_mt>());
        sinks->add_sink(std::make_shared<spdlog::sinks::basic_file_sink_mt>(dumper_root + "dumper.log", true));

        auto logger = std::make_shared<spdlog::logger>("emu_logger", sinks);

        spdlog::register_logger(logger);

        logger->set_level(spdlog::level::trace);
        // Format: [THREAD](LOG_LEVEL) - FUNC_NAME(FILE_LINE): LOG_MESSAGE
        logger->set_pattern("(%t)[%l] - %!{%#} - %v");
        logger->flush_on(spdlog::level::trace);
        spdlog::set_default_logger(logger);
    }

    EOS_ELoginCredentialType auth_type{};
    std::string auth_password;

    std::string product_name("Unreal");
    std::string product_version("1.0.0");
    std::string deployement_id;
    std::string product_id;
    std::string sandbox_id;
    std::string audience;
    std::string secret_key;
    std::string encryption_key;
    std::string cache_directory = get_executable_path();
    cache_directory = cache_directory.substr(0, cache_directory.rfind(PATH_SEPARATOR) + 1);

    {
        nlohmann::json json = nlohmann::json::object_t();
        if (!load_json(dumper_root + "dumper_params.txt", json))
        {
            SPDLOG_ERROR("Failed to load dumper_params.txt, dumper will miss parameters, exiting now.");
            return -1;
        }

        try { secret_key = json["EOS_SECRET_KEY"]; }
        catch (...)
        {
            SPDLOG_ERROR("Failed to read EOS_SECRET_KEY from dumper_params.txt, dumper will miss parameters, exiting now.");
            return -1;
        }

        try { product_name = json["EOS_PRODUCT_NAME"]; }
        catch (...) {}
        try { product_version = json["EOS_PRODUCT_VERSION"]; }
        catch (...) {}
        try { deployement_id = json["EOS_DEPLOYEMENT_ID"]; }
        catch (...) {}
        try { product_id = json["EOS_PRODUCT_ID"]; }
        catch (...) {}
        try { sandbox_id = json["EOS_SANDBOX_ID"]; }
        catch (...) {}
        try { audience = json["EOS_AUDIENCE"]; }
        catch (...) {}
        try { encryption_key = json["EOS_ENCRYPTIONKEY"]; }
        catch(...) {}
    }

    EGS_Api egs_api;
    EGS_Api::Error err;
    std::string game_refresh_token;

    nlohmann::json oauth;
    if (load_json(dumper_root + "dumper_oauth.json", oauth))
    {
        err = egs_api.Login(oauth);
    }
    else
    {
        std::cerr << "Failed to read oauth infos from " << dumper_root + "dumper_oauth.json" << std::endl;
        err.error = EGS_Api::ErrorType::NotLoggedIn;
    }

    if (err.error != EGS_Api::ErrorType::OK)
    {
        SPDLOG_ERROR("Failed to login: {}", err.message);
        
        //err = LoginWithSid(egs_api);
        err = LoginWithAuthorizationCode(egs_api);

        if (err.error != EGS_Api::ErrorType::OK)
            return -1;
    }

    egs_api.GetUserOAuth(oauth);
    save_json(dumper_root + "dumper_oauth.json", oauth);

    {
        std::string game_exchange_code;
        if (egs_api.GetGameExchangeCode(game_exchange_code).error != EGS_Api::ErrorType::OK)
        {
            return -1;
        }

        if (egs_api.GetGameRefreshToken(game_exchange_code, deployement_id, audience, secret_key, game_refresh_token).error == EGS_Api::ErrorType::OK && !game_refresh_token.empty())
        {
            args.emplace_back("-AUTH_TYPE=refreshtoken");
            args.emplace_back("-AUTH_PASSWORD=" + game_refresh_token);
        }
        else
        {
            if (egs_api.GetGameExchangeCode(game_exchange_code).error != EGS_Api::ErrorType::OK)
            {
                return -1;
            }
            args.emplace_back("-AUTH_TYPE=exchangecode");
            args.emplace_back("-AUTH_PASSWORD=" + game_exchange_code);
        }
    }

    for (auto& arg : args)
    {
        if (arg.find("-AUTH_TYPE=") == 0)
        {
            switchstr(&arg[11])
            {
                casestr("refreshtoken") : auth_type = EOS_ELoginCredentialType::EOS_LCT_RefreshToken; break;
                casestr("exchangecode") : auth_type = EOS_ELoginCredentialType::EOS_LCT_ExchangeCode; break;
            }
            break;
        }
    }

    for (auto& arg : args)
    {
        if (arg.find("-AUTH_PASSWORD=") == 0)
        {
            auth_password = &arg[15];
            if (auth_type == EOS_ELoginCredentialType::EOS_LCT_RefreshToken)
            {
                auto pos = auth_password.find(".");
                auto jwt = base64::base64_decode(auth_password.substr(pos + 1, auth_password.find(".", pos + 1) - pos - 1));
                try
                {
                    nlohmann::json json = nlohmann::json::parse(jwt);

                    deployement_id = json["pfdid"];
                    product_id = json["pfpid"];
                    sandbox_id = json["pfsid"];
                    audience = json["aud"];
                }
                catch (...)
                {
                }
            }
            break;
        }
    }

    if (product_name.empty())
    {
        SPDLOG_ERROR("Dumper param missing: product_name");
        exit(-1);
    }
    if (product_version.empty())
    {
        SPDLOG_ERROR("Dumper param missing: product_version");
        exit(-1);
    }
    if (deployement_id.empty())
    {
        SPDLOG_ERROR("Dumper param missing: deployement_id");
        exit(-1);
    }
    if (product_id.empty())
    {
        SPDLOG_ERROR("Dumper param missing: product_id");
        exit(-1);
    }
    if (sandbox_id.empty())
    {
        SPDLOG_ERROR("Dumper param missing: sandbox_id");
        exit(-1);
    }
    if (sandbox_id.empty())
    {
        SPDLOG_ERROR("Dumper param missing: sandbox_id");
        exit(-1);
    }
    if (audience.empty())
    {
        SPDLOG_ERROR("Dumper param missing: audience");
        exit(-1);
    }
    if (secret_key.empty())
    {
        SPDLOG_ERROR("Dumper param missing: secret_key");
        exit(-1);
    }

    try
    {
        EOSApiInitialize(product_name, product_version, EOS_INITIALIZE_API_LATEST);
        PlatformInitialize(
            deployement_id,
            product_id,
            sandbox_id, // Namespace
            audience,
            secret_key,
            cache_directory,
            encryption_key,
            EOS_PF_DISABLE_OVERLAY);

        eos_api.Platform.SetOverrideLocaleCode("en");

        StartApiTick();
        AuthLogin(auth_password, auth_type);
        ConnectLogin();

        //try { MakeAchievementsV1(); }
        //catch (std::runtime_error& e) { SPDLOG_TRACE("{}", e.what()); }
        try { MakeAchievementsV2(); }
        catch (std::runtime_error& e) { SPDLOG_TRACE("{}", e.what()); }
        try { MakeStats(); }
        catch (std::runtime_error& e) { SPDLOG_TRACE("{}", e.what()); }
        try { MakeCatalog(); }
        catch (std::runtime_error& e) { SPDLOG_TRACE("{}", e.what()); }
        try { MakeEntitlements(); }
        catch (std::runtime_error& e) { SPDLOG_TRACE("{}", e.what()); }
        try { MakeLeaderboards(); }
        catch (std::runtime_error& e) { SPDLOG_TRACE("{}", e.what()); }
        //for (auto x : { "game_config.scr", "titlestorage_manifest.json" })
        //{
        //    try { DownloadTitleStorage(x); }
        //    catch (std::runtime_error& e) { SPDLOG_TRACE("{}", e.what()); }
        //}
    }
    catch (std::runtime_error& e)
    {
        SPDLOG_TRACE("{}", e.what());
    }

    DoTick = false;
    if (tick_thread.joinable())
        tick_thread.join();

    eos_api.Shutdown();

    return 0;
}