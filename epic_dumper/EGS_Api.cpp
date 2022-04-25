#define NOMINMAX

#include "EGS_Api.h"

#include <vector>
#include <fstream>

#include <fmt/format.h>

#include <spdlog/spdlog.h>

#include "base64.h"
#include "switchstr.hpp"
#include "CurlEasy.h"

using namespace std::chrono_literals;

#define EPIC_GAMES_HOST  "www.epicgames.com"
#define EGL_UAGENT       "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) EpicGamesLauncher/11.0.1-14907503+++Portal+Release-Live UnrealEngine/4.23.0-14907503+++Portal+Release-Live Chrome/84.0.4147.38 Safari/537.36"
#define EGS_OAUTH_HOST   "account-public-service-prod03.ol.epicgames.com"
#define EGS_OAUTH_UAGENT "EpicGamesLauncher/12.2.4-16388143+++Portal+Release-Live Windows/10.0.19041.1.256.64bit"
#define EGS_USER         "34a02cf8f4414e29b15921876da36f9a"
#define EGS_PASS         "daafbccc737745039dffe53d94fc76cf"

#define EGS_DEV_HOST     "api.epicgames.dev"

class EGS_ApiImpl
{
    CurlEasy _Curl;
    bool _IsLoggedIn;
    nlohmann::json _OAuthInfos;

    EGS_Api::Error _HasError(std::string const& str)
    {
        try
        {
            nlohmann::json json = nlohmann::json::parse(str);
            auto it = json.find("errorCode");
            if (it != json.end())
            {
                std::string message = json.value("errorMessage", std::string());
                if (message.empty())
                {
                    message = json.value("message", std::string());
                }

                switchstr(it.value().get_ref<std::string const&>())
                {
                    casestr("errors.com.epicgames.common.method_not_allowed")                : return EGS_Api::Error{ EGS_Api::ErrorType::MethodNotAllowed    , message };
                    casestr("errors.com.epicgames.accountportal.csrf_token_invalid")         : return EGS_Api::Error{ EGS_Api::ErrorType::CSRFTokenInvalid    , message };
                    casestr("errors.com.epicgames.accountportal.validation")                 : return EGS_Api::Error{ EGS_Api::ErrorType::Validation          , message };
                    casestr("errors.com.epicgames.account.oauth.exchange_code_not_found")    : return EGS_Api::Error{ EGS_Api::ErrorType::ExchangeCodeNotFound, message };
                    casestr("errors.com.epicgames.common.oauth.invalid_token")               : return EGS_Api::Error{ EGS_Api::ErrorType::InvalidToken        , message };
                    casestr("errors.com.epicgames.account.auth_token.invalid_refresh_token") : return EGS_Api::Error{ EGS_Api::ErrorType::InvalidRefreshToken , message };
                }

                return EGS_Api::Error{ EGS_Api::ErrorType::Unknown, it.value().get_ref<std::string const&>() + ": " + message };
            }
        }
        catch (...)
        {
        }

        return EGS_Api::Error{ EGS_Api::ErrorType::OK, std::string() };
    }

    void _UpdateOAuth(nlohmann::json const& oauth_infos)
    {
        _OAuthInfos["access_token"] = oauth_infos["token"];
        _OAuthInfos["expires_in"] = oauth_infos["expires_in"];
        _OAuthInfos["expires_at"] = oauth_infos["expires_at"];
        _OAuthInfos["token_type"] = oauth_infos["token_type"];
        _OAuthInfos["account_id"] = oauth_infos["account_id"];
        _OAuthInfos["client_id"] = oauth_infos["client_id"];
        _OAuthInfos["internal_client"] = oauth_infos["internal_client"];
        _OAuthInfos["client_service"] = oauth_infos["client_service"];
        _OAuthInfos["display_name"] = oauth_infos["display_name"];
        _OAuthInfos["app"] = oauth_infos["app"];
        _OAuthInfos["in_app_id"] = oauth_infos["in_app_id"];
        _OAuthInfos["device_id"] = oauth_infos["device_id"];
    }

    EGS_Api::Error _GetXSRFToken(std::string& xsrf_token)
    {
        int c;

        _Curl.SetHeader("User-Agent", EGL_UAGENT);
        if ((c = _Curl.PerformGet("https://" EPIC_GAMES_HOST "/id/api/csrf")) != 0)
            return EGS_Api::Error{ EGS_Api::ErrorType::CURLError, _Curl.GetError(c) };

        auto err = _HasError(_Curl.GetAnswer());
        if (err.error != EGS_Api::ErrorType::OK)
            return err;

        auto cookies = _Curl.GetCookies();
        for (auto& cookie : cookies)
        {
            if (cookie.find("XSRF-TOKEN") != std::string::npos)
            {
                auto pos = cookie.rfind('\t');
                xsrf_token = cookie.substr(pos + 1);
            }
        }

        return EGS_Api::Error{ EGS_Api::ErrorType::OK, "" };
    }

    EGS_Api::Error _GetExchangeCode(std::string const& xsrf_token, std::string& exchange_code)
    {
        int c;

        _Curl.SetHeader("User-Agent", EGL_UAGENT);
        _Curl.SetHeader("X-XSRF-TOKEN", xsrf_token);

        if ((c = _Curl.PerformPost("https://" EPIC_GAMES_HOST "/id/api/exchange/generate")) != 0)
            return EGS_Api::Error{ EGS_Api::ErrorType::CURLError, _Curl.GetError(c) };

        auto err = _HasError(_Curl.GetAnswer());
        if (err.error != EGS_Api::ErrorType::OK)
            return err;

        try
        {
            exchange_code = nlohmann::json::parse(_Curl.GetAnswer().data())["code"].get<std::string>();
            return EGS_Api::Error{ EGS_Api::ErrorType::OK, "" };
        }
        catch (std::exception& e)
        {
        }

        return EGS_Api::Error{ EGS_Api::ErrorType::Unknown, "Failed to parse JSON answer." };
    }

    EGS_Api::Error _StartSession(EGS_Api::TokenType type, std::string const& token)
    {
        int c;

        switch (type)
        {
            case EGS_Api::TokenType::ExchangeCode:
                _Curl.SetData("grant_type", "exchange_code");
                _Curl.SetData("exchange_code", token);
                break;

            case EGS_Api::TokenType::RefreshToken:
                _Curl.SetData("grant_type", "refresh_token");
                _Curl.SetData("refresh_token", token);
                break;

            default:
                return EGS_Api::Error{ EGS_Api::ErrorType::InvalidParam, "Invalid token type." };
        }

        _Curl.SetData("token_type", "eg1");

        _Curl.ResetHeaders();
        _Curl.SetHeader("User-Agent", EGS_OAUTH_UAGENT);
        _Curl.SetHeader("Accept-Encoding", "gzip, deflate");
        _Curl.SetHeader("Accept", "*/*");
        _Curl.SetHeader("Authorization", fmt::format("Basic {}", base64::encode(EGS_USER ":" EGS_PASS)));

        if ((c = _Curl.PerformPost("https://" EGS_OAUTH_HOST "/account/api/oauth/token")) != 0)
            return EGS_Api::Error{ EGS_Api::ErrorType::CURLError, _Curl.GetError(c) };

        auto err = _HasError(_Curl.GetAnswer());
        if (err.error != EGS_Api::ErrorType::OK)
            return err;

        try
        {
            _OAuthInfos = nlohmann::json::parse(_Curl.GetAnswer());
            return EGS_Api::Error{ EGS_Api::ErrorType::OK, "" };
        }
        catch (...)
        {
        }

        return EGS_Api::Error{ EGS_Api::ErrorType::Unknown, "Failed to parse JSON OAuth." };
    }

    EGS_Api::Error _ResumeSession()
    {
        int c;
        EGS_Api::Error err;

        _Curl.ResetHeaders();
        _Curl.SetHeader("User-Agent", EGS_OAUTH_UAGENT);
        _Curl.SetHeader("Accept", "*/*");
        _Curl.SetHeader("Authorization", fmt::format("bearer {}", _OAuthInfos["access_token"].get_ref<std::string const&>()));

        if ((c = _Curl.PerformGet("https://" EGS_OAUTH_HOST "/account/api/oauth/verify")) != 0)
            return EGS_Api::Error{ EGS_Api::ErrorType::CURLError, _Curl.GetError(c) };

        err = _HasError(_Curl.GetAnswer());
        if (err.error != EGS_Api::ErrorType::OK)
            return err;

        try
        {
            nlohmann::json oauth = nlohmann::json::parse(_Curl.GetAnswer().data());
            _UpdateOAuth(oauth);
            return EGS_Api::Error{ EGS_Api::ErrorType::OK, "" };
        }
        catch (...)
        {
        }

        return EGS_Api::Error{ EGS_Api::ErrorType::Unknown, "Failed to parse JSON OAuth." };
    }

public:
    EGS_ApiImpl():
        _IsLoggedIn(false)
    {
        if (!_Curl.Init())
        {
            SPDLOG_ERROR("Failed to initialize curl.");
        }
        else
        {
            _Curl.SetVerifyHost(0);
            _Curl.SetVerifyPeer(0);
            _Curl.SetCookiesFile("");
        }
    }

    EGS_Api::Error LoginSID(std::string const& sid)
    {
        int c;
        EGS_Api::Error err;
        std::string xsrf_token;
        std::string exchange_code;

        _IsLoggedIn = false;

        _Curl.ResetHeaders();
        _Curl.ResetCookies();
        _Curl.SetHeader("User-Agent", EGL_UAGENT);
        _Curl.SetHeader("X-Epic-Event-Action", "login");
        _Curl.SetHeader("X-Epic-Event-Category", "login");
        _Curl.SetHeader("X-Epic-Strategy-Flags", "");
        _Curl.SetHeader("X-Requested-With", "XMLHttpRequest");

        if ((c = _Curl.PerformGet(fmt::format("https://" EPIC_GAMES_HOST "/id/api/set-sid?sid={}", sid))) != 0)
            return EGS_Api::Error{ EGS_Api::ErrorType::CURLError, _Curl.GetError(c) };

        err = _HasError(_Curl.GetAnswer());
        if (err.error != EGS_Api::ErrorType::OK)
            return err;

        err = _GetXSRFToken(xsrf_token);
        if (err.error != EGS_Api::ErrorType::OK)
            return err;

        if (xsrf_token.empty())
            return EGS_Api::Error{ EGS_Api::ErrorType::Unknown, "Empty XSRF Token." };

        err = _GetExchangeCode(xsrf_token, exchange_code);
        if (err.error != EGS_Api::ErrorType::OK)
            return err;

        if (exchange_code.empty())
            return EGS_Api::Error{ EGS_Api::ErrorType::Unknown, "Empty exchange code." };

        err = _StartSession(EGS_Api::TokenType::ExchangeCode, exchange_code);
        if (err.error != EGS_Api::ErrorType::OK)
            return err;

        err = _ResumeSession();
        if (err.error != EGS_Api::ErrorType::OK)
            return err;

        _IsLoggedIn = true;
        return EGS_Api::Error{ EGS_Api::ErrorType::OK, "" };
    }

    EGS_Api::Error Login(nlohmann::json const& oauth)
    {
        EGS_Api::Error err = EGS_Api::Error{ EGS_Api::ErrorType::InvalidParam, "Invalid cached oauth infos." };

        _Curl.ResetCookies();

        std::chrono::seconds now = std::chrono::duration_cast<std::chrono::seconds>(std::chrono::system_clock::now().time_since_epoch());
        std::chrono::seconds expires_at;
        std::string refresh_token;

        auto it = oauth.find("refresh_token");
        if (it == oauth.end())
            return EGS_Api::Error{ EGS_Api::ErrorType::InvalidParam, "Invalid cached oauth infos: missing \"refresh_token\"." };

        refresh_token = it.value().get_ref<std::string const&>();

        it = oauth.find("expires_at");
        if (it != oauth.end())
        {
            _IsLoggedIn = false;

            tm time = {};

            int y, M, d, h, m;
            float s;
#if defined(_WIN32) || defined(_WIN64)
            if (sscanf_s(it.value().get_ref<std::string const&>().c_str(), "%d-%d-%dT%d:%d:%fZ", &y, &M, &d, &h, &m, &s) != 6)
#else
            if (sscanf(it.value().get_ref<std::string const&>().c_str(), "%d-%d-%dT%d:%d:%fZ", &y, &M, &d, &h, &m, &s) != 6)
#endif
                return EGS_Api::Error{ EGS_Api::ErrorType::InvalidParam, "Invalid cached oauth infos: invalid \"expires_at\" date format." };

            time.tm_year = y - 1900;
            time.tm_mon = M - 1;
            time.tm_mday = d;
            time.tm_hour = h;
            time.tm_min = m;
            time.tm_sec = (int)s;

            expires_at = std::chrono::seconds(mktime(&time));

            _OAuthInfos = oauth;

            if (expires_at > now && (expires_at - now) > 10min)
            {
                err = _ResumeSession();
                if (err.error == EGS_Api::ErrorType::OK)
                {
                    _IsLoggedIn = true;
                    return err;
                }
            }

            err = _StartSession(EGS_Api::TokenType::RefreshToken, refresh_token);
            if (err.error != EGS_Api::ErrorType::OK)
                return err;

            err = _ResumeSession();
            _IsLoggedIn = err.error == EGS_Api::ErrorType::OK;
        }

        return err;
    }

    EGS_Api::Error GetGameExchangeCode(std::string& exchange_code)
    {
        int c;
        EGS_Api::Error err;

        if (!_IsLoggedIn)
            return EGS_Api::Error{ EGS_Api::ErrorType::NotLoggedIn, "Not logged-in." };

        _Curl.SetHeader("Authorization", fmt::format("bearer {}", _OAuthInfos["access_token"].get_ref<std::string const&>()));

        if ((c = _Curl.PerformGet("https://" EGS_OAUTH_HOST "/account/api/oauth/exchange")) != 0)
            return EGS_Api::Error{ EGS_Api::ErrorType::CURLError, _Curl.GetError(c) };

        err = _HasError(_Curl.GetAnswer());
        if (err.error != EGS_Api::ErrorType::OK)
            return err;

        try
        {
            exchange_code = nlohmann::json::parse(_Curl.GetAnswer())["code"];
            return EGS_Api::Error{ EGS_Api::ErrorType::OK, "" };
        }
        catch (...)
        {
        }

        return EGS_Api::Error{ EGS_Api::ErrorType::Unknown, "Failed to parse JSON game token." };
    }

    EGS_Api::Error GetGameRefreshToken(std::string const& exchange_code, std::string const& deployement_id, std::string const& client_id, std::string const& client_secret, std::string& refresh_token)
    {
        int c;
        EGS_Api::Error err;

        if (!_IsLoggedIn)
            return EGS_Api::Error{ EGS_Api::ErrorType::NotLoggedIn, "Not logged-in." };

        _Curl.ResetQueryDatas();
        _Curl.SetData("grant_type", "exchange_code");
        _Curl.SetData("exchange_code", exchange_code);
        _Curl.SetData("scope", "basic_profile friends_list presence");
        _Curl.SetData("deployment_id", deployement_id);

        _Curl.SetHeader("Authorization", fmt::format("Basic {}", base64::encode(client_id + ":" + client_secret)));
        if ((c = _Curl.PerformPost("https://" EGS_DEV_HOST "/epic/oauth/v1/token")) != 0)
            return EGS_Api::Error{ EGS_Api::ErrorType::CURLError, _Curl.GetError(c) };

        err = _HasError(_Curl.GetAnswer());
        if (err.error != EGS_Api::ErrorType::OK)
            return err;

        try
        {
            refresh_token = nlohmann::json::parse(_Curl.GetAnswer())["refresh_token"];
            return EGS_Api::Error{ EGS_Api::ErrorType::OK, "" };
        }
        catch (...)
        {
        }

        return EGS_Api::Error{ EGS_Api::ErrorType::Unknown, "Failed to parse JSON game token." };
    }

    EGS_Api::Error GetGameCommandLine(std::string const& appid, std::string& command_line)
    {
        EGS_Api::Error err;
        std::string exchange_code;
        std::string username;
        std::string userid;

        if (!_IsLoggedIn)
            return EGS_Api::Error{ EGS_Api::ErrorType::NotLoggedIn, "Not logged-in." };

        err = GetGameExchangeCode(exchange_code);
        if (err.error != EGS_Api::ErrorType::OK)
            return err;

        try { username = _OAuthInfos["displayName"]; }
        catch(...) { return EGS_Api::Error{ EGS_Api::ErrorType::Unknown, "Failed to read user displayName." }; }

        try { userid = _OAuthInfos["account_id"]; }
        catch (...) { return EGS_Api::Error{ EGS_Api::ErrorType::Unknown, "Failed to read user account_id." }; }

        command_line = fmt::format("-AUTH_LOGIN=unused -AUTH_PASSWORD={} -AUTH_TYPE=exchangecode -epicapp={} -epicenv=Prod -EpicPortal -epicusername={} -epicuserid={} -epiclocal=en", exchange_code, appid, username, userid);

        return EGS_Api::Error{ EGS_Api::ErrorType::OK, "" };
    }

    EGS_Api::Error GetUserOAuth(nlohmann::json& oauth)
    {
        if (!_IsLoggedIn)
            return EGS_Api::Error{ EGS_Api::ErrorType::NotLoggedIn, "Not logged-in." };

        oauth = _OAuthInfos;

        return EGS_Api::Error{ EGS_Api::ErrorType::OK, "" };
    }
};





EGS_Api::EGS_Api(): _impl(new EGS_ApiImpl)
{}

EGS_Api::~EGS_Api()
{
    delete _impl;
}

EGS_Api::Error EGS_Api::LoginSID(std::string const& sid)
{
    return _impl->LoginSID(sid);
}

EGS_Api::Error EGS_Api::Login(nlohmann::json const& oauth)
{
    return _impl->Login(oauth);
}

EGS_Api::Error EGS_Api::GetGameExchangeCode(std::string& exchange_code)
{
    return _impl->GetGameExchangeCode(exchange_code);
}

EGS_Api::Error EGS_Api::GetGameRefreshToken(std::string const& exchange_code, std::string const& deployement_id, std::string const& client_id, std::string const& client_secret, std::string& refresh_token)
{
    return _impl->GetGameRefreshToken(exchange_code, deployement_id, client_id, client_secret, refresh_token);
}

EGS_Api::Error EGS_Api::GetGameCommandLine(std::string const& appid, std::string& command_line)
{
    return _impl->GetGameCommandLine(appid, command_line);
}

EGS_Api::Error EGS_Api::GetUserOAuth(nlohmann::json& oauth)
{
    return _impl->GetUserOAuth(oauth);
}
