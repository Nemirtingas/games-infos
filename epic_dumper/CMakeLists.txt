cmake_policy(SET CMP0091 NEW)
project(epic_dumper)
cmake_minimum_required(VERSION 3.15)

set(CMAKE_CXX_STANDARD 14)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

set(CMAKE_ARCHIVE_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/lib)
set(CMAKE_LIBRARY_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/lib)
set(CMAKE_RUNTIME_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/bin)

set(CMAKE_BUILD_TYPE "Release" CACHE STRING "Debug or Release")

find_package(Threads REQUIRED)
find_package(CURL REQUIRED)
find_package(OpenSSL REQUIRED)

add_executable(${CMAKE_PROJECT_NAME}
  epic_dumper.cpp
  base64.cpp
  CurlEasy.cpp
  EGS_Api.cpp
  library.cpp
  os_funcs.cpp
)

set_target_properties(${CMAKE_PROJECT_NAME} PROPERTIES
  MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>"
  C_VISIBILITY_PRESET hidden
  CXX_VISIBILITY_PRESET hidden
  VISIBILITY_INLINES_HIDDEN ON
  #MACHO_COMPATIBILITY_VERSION 4.26.0
  #MACHO_CURRENT_VERSION 1454.72.26
)

target_include_directories(${CMAKE_PROJECT_NAME}
  PRIVATE
  ${CURL_INCLUDE_DIRS}
  "${CMAKE_CURRENT_SOURCE_DIR}/deps"
  "${CMAKE_CURRENT_SOURCE_DIR}/deps/eos_sdk"
)

target_link_libraries(${CMAKE_PROJECT_NAME}
  PRIVATE
  Threads::Threads
  CURL::libcurl
  OpenSSL::SSL
  OpenSSL::Crypto
  $<$<BOOL:${WIN32}>:advapi32>
  $<$<BOOL:${WIN32}>:crypt32>
  $<$<BOOL:${UNIX}>:dl>
)

target_link_options(${CMAKE_PROJECT_NAME}
  PRIVATE
  $<$<AND:$<NOT:$<BOOL:${APPLE}>>,$<BOOL:${UNIX}>>:-Wl,--exclude-libs,ALL>
  $<$<AND:$<NOT:$<BOOL:${APPLE}>>,$<BOOL:${UNIX}>>:-Wl,--no-undefined>
  #$<$<BOOL:${UNIX}>:-static>
)
