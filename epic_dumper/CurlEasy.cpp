#include "CurlEasy.h"

#include <fmt/format.h>
#include <curl/curl.h>
#include <curl/easy.h>

#include <map>

class CurlEasyImpl
{
    CURL* _me;
    bool _init;
    std::string _buffer;
    std::map<std::string, std::string> _headers;
    std::map<std::string, std::string> _query_datas;

    static int _Writer(char* data, size_t size, size_t nmemb,
        CurlEasyImpl* _this)
    {
        if (_this == nullptr)
            return 0;

        _this->_buffer.append(data, size * nmemb);

        return size * nmemb;
    }

    void _Cleanup()
    {
        if (_init)
        {
            curl_easy_cleanup(_me);
        }
    }

    std::string _MakeUrlDatas()
    {
        if (_query_datas.empty())
            return "";

        std::string datas;

        auto it = _query_datas.begin();

        datas = fmt::format("{}={}", it->first, it->second);

        ++it;
        for (; it != _query_datas.end(); ++it)
        {
            datas.append(fmt::format("&{}={}", it->first, it->second));
        }

        return datas;
    }

    CURLcode _SetUrl(std::string const& url)
    {
        return curl_easy_setopt(_me, CURLOPT_URL, url.c_str());
    }

    CURLcode _Perform()
    {
        CURLcode c;
        c = curl_easy_perform(_me);

        _query_datas.clear();

        return c;
    }

public:
    CurlEasyImpl() :_me(nullptr), _init(false) {}
    ~CurlEasyImpl() { _Cleanup(); }

    bool Init()
    {
        _init = (_me = curl_easy_init()) != nullptr;
        if (_init)
        {
            if (curl_easy_setopt(_me, CURLOPT_WRITEFUNCTION, &CurlEasyImpl::_Writer) != CURLE_OK)
            {
                _Cleanup();
                return false;
            }

            if (curl_easy_setopt(_me, CURLOPT_WRITEDATA, this) != CURLE_OK)
            {
                _Cleanup();
                return false;
            }
        }
        return _init;
    }

    std::string GetError(CURLcode c) const
    {
        return curl_easy_strerror(c);
    }

    CURLcode SetVerifyPeer(int verify)
    {
        return curl_easy_setopt(_me, CURLOPT_SSL_VERIFYPEER, verify);
    }

    CURLcode SetVerifyHost(int verify)
    {
        return curl_easy_setopt(_me, CURLOPT_SSL_VERIFYHOST, verify);
    }

    CURLcode SetConnectOnly(int connect_only = 0)
    {
        return curl_easy_setopt(_me, CURLOPT_CONNECT_ONLY, connect_only);
    }

    void SetHeader(std::string const& key, std::string const& value)
    {
        _headers[key] = value;
    }

    void SetData(std::string const& key, std::string const& value)
    {
        _query_datas[key] = value;
    }

    CURLcode PerformGet(std::string const& url)
    {
        std::string datas = _MakeUrlDatas();
        CURLcode c;
        _buffer.clear();

        struct curl_slist* header_list = nullptr;

        for (auto const& header : _headers)
            header_list = curl_slist_append(header_list, fmt::format("{}: {}", header.first, header.second).c_str());

        curl_easy_setopt(_me, CURLOPT_HTTPHEADER, header_list);

        curl_easy_setopt(_me, CURLOPT_HTTPGET, 1ul);

        if (datas.empty())
        {
            _SetUrl(url);
        }
        else
        {
            _SetUrl(fmt::format("{}?{}", url, datas));
        }

        c = _Perform();

        curl_slist_free_all(header_list);

        return c;
    }

    CURLcode PerformPost(std::string const& url)
    {
        std::string datas = _MakeUrlDatas();
        CURLcode c;
        _buffer.clear();

        struct curl_slist* header_list = nullptr;

        for (auto const& header : _headers)
            header_list = curl_slist_append(header_list, fmt::format("{}: {}", header.first, header.second).c_str());

        curl_easy_setopt(_me, CURLOPT_HTTPHEADER, header_list);

        curl_easy_setopt(_me, CURLOPT_POSTFIELDS, datas.c_str());
        _SetUrl(url);

        c = _Perform();

        curl_slist_free_all(header_list);

        return c;
    }

    CURLcode Recv(void* buffer, size_t buflen, size_t* read_len)
    {
        return curl_easy_recv(_me, buffer, buflen, read_len);
    }

    CURLcode GetHTMLCode(long& code)
    {
        return curl_easy_getinfo(_me, CURLINFO_RESPONSE_CODE, &code);
    }

    CURLcode SetCookiesFile(std::string const& cookies_file)
    {
        return curl_easy_setopt(_me, CURLOPT_COOKIEFILE, cookies_file.c_str());
    }

    CURLcode SetCookies(std::string const& cookies)
    {
        return curl_easy_setopt(_me, CURLOPT_COOKIELIST, cookies.c_str());
    }

    CURLcode SetEncoding(std::string const& encoding)
    {
        return curl_easy_setopt(_me, CURLOPT_ACCEPT_ENCODING, encoding.c_str());
    }

    CURLcode ResolveIPv4Only()
    {
        return curl_easy_setopt(_me, CURLOPT_IPRESOLVE, CURL_IPRESOLVE_V4);
    }

    CURLcode ResolveIPv6Only()
    {
        return curl_easy_setopt(_me, CURLOPT_IPRESOLVE, CURL_IPRESOLVE_V6);
    }

    CURLcode ResolveAny()
    {
        return curl_easy_setopt(_me, CURLOPT_IPRESOLVE, CURL_IPRESOLVE_WHATEVER);
    }

    void ResetQueryDatas()
    {
        _query_datas.clear();
    }

    void ResetHeaders()
    {
        _headers.clear();
    }

    void ResetCookies()
    {
        SetCookies(std::string());
    }

    void Reset()
    {
        _buffer.clear();

        ResetQueryDatas();
        ResetHeaders();
        ResetCookies();
    }

    std::vector<std::string> GetCookies()
    {
        std::vector<std::string> cookies;
        CURLcode c;
        struct curl_slist* cookie_list = nullptr;

        c = curl_easy_getinfo(_me, CURLINFO_COOKIELIST, &cookie_list);
        if (c == CURLE_OK)
        {
            for (struct curl_slist* cookie = cookie_list; cookie != nullptr; cookie = cookie->next)
            {
                cookies.emplace_back(cookie->data);
            }

            curl_slist_free_all(cookie_list);
        }
        return cookies;
    }

    std::string const& GetAnswer() const
    {
        return _buffer;
    }
};





CurlEasy::CurlEasy():_impl(new CurlEasyImpl) { }

CurlEasy::~CurlEasy() { delete _impl; _impl = nullptr; }

bool CurlEasy::Init() { return _impl->Init(); }

std::string CurlEasy::GetError(int c) const { return _impl->GetError((CURLcode)c); }

int CurlEasy::SetVerifyPeer(int verify) { return (int)_impl->SetVerifyPeer(verify); }

int CurlEasy::SetVerifyHost(int verify) { return (int)_impl->SetVerifyHost(verify); }

int CurlEasy::SetConnectOnly(int connect_only) { return (int)_impl->SetConnectOnly(connect_only); }

void CurlEasy::SetHeader(std::string const& key, std::string const& value) { _impl->SetHeader(key, value); }

void CurlEasy::SetData(std::string const& key, std::string const& value) { _impl->SetData(key, value); }

int CurlEasy::PerformGet(std::string const& url) { return (int)_impl->PerformGet(url); }

int CurlEasy::PerformPost(std::string const& url) { return (int)_impl->PerformPost(url); }

int CurlEasy::Recv(void* buffer, size_t buflen, size_t* read_len) { return (int)_impl->Recv(buffer, buflen, read_len); }

int CurlEasy::GetHTMLCode(long& code) { return (int)_impl->GetHTMLCode(code); }

int CurlEasy::SetCookiesFile(std::string const& cookies_file) { return (int)_impl->SetCookiesFile(cookies_file); }

int CurlEasy::SetCookies(std::string const& cookies) { return (int)_impl->SetCookies(cookies); }

int CurlEasy::SetEncoding(std::string const& encoding) { return (int)_impl->SetEncoding(encoding); }

int CurlEasy::ResolveIPv4Only() { return (int)_impl->ResolveIPv4Only(); }

int CurlEasy::ResolveIPv6Only() { return (int)_impl->ResolveIPv6Only(); }

int CurlEasy::ResolveAny() { return (int)_impl->ResolveAny(); }

void CurlEasy::ResetQueryDatas() { _impl->ResetQueryDatas(); }

void CurlEasy::ResetHeaders() { _impl->ResetHeaders(); }

void CurlEasy::ResetCookies() { _impl->ResetCookies(); }

void CurlEasy::Reset() { _impl->Reset(); }

std::vector<std::string> CurlEasy::GetCookies() { return _impl->GetCookies(); }

std::string const& CurlEasy::GetAnswer() const { return _impl->GetAnswer(); }
