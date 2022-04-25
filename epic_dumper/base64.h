#include <string>

// From boost implementation

namespace base64 {

inline std::size_t constexpr encoded_size(std::size_t n)
{
    return 4 * ((n + 2) / 3);
}

inline std::size_t constexpr decoded_size(std::size_t n)
{
    return n * 3 / 4;
}

std::size_t encode(void* dest, void const* src, std::size_t len, bool padding = true);

std::pair<std::size_t, std::size_t> decode(void* dest, char const* src, std::size_t len);

inline std::string encode(std::uint8_t const* data, std::size_t len, bool padding = true)
{
    std::string dest;
    dest.resize(base64::encoded_size(len));
    dest.resize(base64::encode(&dest[0], data, len, padding));
    return dest;
}

inline std::string encode(std::string const& s, bool padding = true)
{
    return encode(reinterpret_cast <std::uint8_t const*>(s.data()), s.size(), padding);
}

inline std::string base64_decode(std::string const& data)
{
    std::string dest;
    dest.resize(base64::decoded_size(data.size()));
    auto const result = base64::decode(&dest[0], data.data(), data.size());
    dest.resize(result.first);
    return dest;
}

} // base64