#pragma once

#include <cstdint>
#include <string>

constexpr uint64_t _hash(const char* input)
{
    return (*input ? static_cast<uint64_t>(*input) + 33 * _hash(input + 1) : 5381);
}

constexpr uint64_t _hash(const char* input, size_t len)
{
    return (len > 0 ? static_cast<uint64_t>(*input) + 33 * _hash(input + 1, len - 1) : 5381);
}

inline uint64_t _hash(const std::string& input)
{
    return _hash(input.c_str());
}

#define switchstr(x) switch( _hash(x) )
#define casestr(x) case _hash(x)

#define switchstrn(x, s) switch( _hash(x, s) )
#define casestrn(x, s) case _hash(x, s)