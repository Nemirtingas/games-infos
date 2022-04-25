#pragma once

#include <string>
#include <vector>

class CurlEasy
{
    class CurlEasyImpl* _impl;

public:
    CurlEasy();
    ~CurlEasy();

    bool Init();

    std::string GetError(int c) const;

    int SetVerifyPeer(int verify = 1);

    int SetVerifyHost(int verify = 1);

    int SetConnectOnly(int connect_only = 0);

    void SetHeader(std::string const& key, std::string const& value);

    void SetData(std::string const& key, std::string const& value);

    int PerformGet(std::string const& url);

    int PerformPost(std::string const& url);

    int Recv(void* buffer, size_t buflen, size_t* read_len);

    int GetHTMLCode(long& code);

    int SetCookiesFile(std::string const& cookies_file);

    int SetCookies(std::string const& cookies);

    int SetEncoding(std::string const& encoding);

    int ResolveIPv4Only();

    int ResolveIPv6Only();

    int ResolveAny();

    void ResetQueryDatas();

    void ResetHeaders();

    void ResetCookies();

    void Reset();

    std::vector<std::string> GetCookies();

    std::string const& GetAnswer() const;
};