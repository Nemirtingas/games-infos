#pragma once

#include <chrono>
#include <vector>
#include <string>

#if defined(WIN64) || defined(_WIN64) || defined(__MINGW64__)
    #define UTILS_OS_WINDOWS
    #define UTILS_ARCH_X64
#elif defined(WIN32) || defined(_WIN32) || defined(__MINGW32__)
    #define UTILS_OS_WINDOWS
    #define UTILS_ARCH_X86
#elif defined(__linux__) || defined(linux)
    #if defined(__x86_64__)
        #define UTILS_OS_LINUX
        #define UTILS_ARCH_X64
    #else
        #define UTILS_OS_LINUX
        #define UTILS_ARCH_X86
    #endif
#elif defined(__APPLE__)
    #if defined(__x86_64__)
        #define UTILS_OS_APPLE
        #define UTILS_ARCH_X64
    #else
        #define UTILS_OS_APPLE
        #define UTILS_ARCH_X86
    #endif
#else
    //#error "Unknown OS"
#endif

std::chrono::system_clock::time_point get_boottime();
std::chrono::microseconds get_uptime();

// Try to disable all online networking
void disable_online_networking();
void enable_online_networking();

// Get the current process argv
std::vector<std::string> get_proc_argv();
// Get User env variable
std::string get_env_var(std::string const& var);
// User appdata full path
std::string get_userdata_path();
// Executable full path
std::string get_executable_path();
// .dll, .so or .dylib full path
std::string get_module_path();