#pragma once

#include <string>
#include <nlohmann/json.hpp>

class EGS_Api
{
public:
    enum class ErrorType
    {
        NotLoggedIn = -4,
        InvalidParam = -3,
        CURLError = -2,
        Unknown = -1,
        OK = 0,
        ExchangeCodeNotFound,
        MethodNotAllowed,
        CSRFTokenInvalid,
        Validation,
        InvalidCredentials,
        InvalidRefreshToken,
        InvalidToken,
    };

    enum class TokenType
    {
        AuthorizationCode,
        ExchangeCode,
        RefreshToken,
    };

    struct Error
    {
        ErrorType error;
        std::string message;
    };

private:
    class EGS_ApiImpl* _impl;

public:
    EGS_Api();
    ~EGS_Api();

    EGS_Api::Error LoginSID(std::string const& sid);

    EGS_Api::Error LoginAuthorizationCode(std::string const& auth_code);

    EGS_Api::Error Login(nlohmann::json const& oauth);

    EGS_Api::Error GetGameExchangeCode(std::string& exchange_code);

    EGS_Api::Error GetGameRefreshToken(std::string const& exchange_code, std::string const& deployement_id, std::string const& client_id, std::string const& client_secret, std::string& refresh_token);

    EGS_Api::Error GetGameCommandLine(std::string const& appid, std::string& command_line);

    EGS_Api::Error GetUserOAuth(nlohmann::json& oauth);
};