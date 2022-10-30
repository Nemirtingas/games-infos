
using Newtonsoft.Json.Linq;
using System.IO;
using System.Net;
using System;
using System.Linq;

namespace EGS
{
    class Error
    {
        public const int NotLoggedIn = -5;
        public const int InvalidParam = -4;
        public const int NotFound = -3;
        public const int WebError = -2;
        public const int Unknown = -1;
        public const int OK = 0;
        // Global error
        public const int Unauthorized = 8;
        // AccountPortal
        public const int AccountPortalValidation = 1;
        public const int AccountPortalCsrfTokenInvalid = 2;
        public const int AccountPortalSessionIdInvalid = 3;
        // AccountOauth
        public const int AccountOauthExchangeCodeNotFound = 18057;
        public const int AccountOauthAuthorizationCodeNotFound = 18059;

        // AccountAuthToken
        public const int AccountAuthTokenInvalidRefreshToken = 18036;
        // CommonMethod
        public const int CommonMethodNotAllowed = 1009;
        // CommonOauth
        public const int CommonOauthInvalidToken = 7;
        // CommonAuthentication
        public const int CommonAuthenticationAuthenticationFailed = 1032;

        public const int OAuthScopeConsentRequired = 58005;

        public int ErrorCode { get; set; }
        public string Message { get; set; }

        static public int ErrorCodeFromString(string error)
        {
            if (string.IsNullOrEmpty(error))
                return Error.Unknown;

            switch(error)
            {
                case "errors.com.epicgames.unauthorized"                               : return Error.Unauthorized;
                case "errors.com.epicgames.common.authentication.authentication_failed": return Error.CommonAuthenticationAuthenticationFailed;
                case "errors.com.epicgames.common.method_not_allowed"                  : return Error.CommonMethodNotAllowed;
                case "errors.com.epicgames.common.oauth.invalid_token"                 : return Error.CommonOauthInvalidToken;

                case "errors.com.epicgames.accountportal.session_id_invalid"           : return Error.AccountPortalSessionIdInvalid;
                case "errors.com.epicgames.accountportal.validation"                   : return Error.AccountPortalValidation;
                case "errors.com.epicgames.accountportal.csrf_token_invalid"           : return Error.AccountPortalCsrfTokenInvalid;

                case "errors.com.epicgames.account.oauth.exchange_code_not_found"      : return Error.AccountOauthExchangeCodeNotFound;
                case "errors.com.epicgames.account.oauth.authorization_code_not_found" : return Error.AccountOauthAuthorizationCodeNotFound;
                case "errors.com.epicgames.account.auth_token.invalid_refresh_token"   : return Error.AccountAuthTokenInvalidRefreshToken;

                case "errors.com.epicgames.oauth.scope_consent_required"               : return Error.OAuthScopeConsentRequired;

                default: return Error.Unknown;
            }
        }

        static public Error GetErrorFromJson(JObject json)
        {
            Error err = new Error();

            string error_name = string.Empty;
            string message = string.Empty;
            int error_code = 0;

            if (json != null)
            {
                if (json.ContainsKey("message"))
                    message = (string)json["message"];
                else if (json.ContainsKey("errorMessage"))
                    message = (string)json["errorMessage"];

                if (error_code == 0 && json.ContainsKey("numericErrorCode"))
                {
                    error_code = (int)json["numericErrorCode"];
                }
                if (string.IsNullOrWhiteSpace(error_name) && json.ContainsKey("errorCode"))
                {
                    error_name = (string)json["errorCode"];
                }
            }

            err.Message = string.IsNullOrWhiteSpace(message) ? error_name : message;

            if (error_code != 0)
            {
                err.ErrorCode = Error.ErrorCodeFromString(error_name);
            }
            else
            {
                err.ErrorCode = Error.Unknown;
            }

            return err;
        }

        static public Error GetWebErrorFromException(Exception e)
        {
            Error err = new Error();
            if (e is WebException && ((WebException)e).Response != null)
            {
                WebException we = (WebException)e;
                try
                {
                    int error_code = 0;
                    string error_name = string.Empty;
                    string message = string.Empty;

                    if (we.Response.Headers.AllKeys.Contains("X-Epic-Error-Code"))
                    {
                        error_code = int.Parse(we.Response.Headers["X-Epic-Error-Code"]);
                    }
                    if (we.Response.Headers.AllKeys.Contains("X-Epic-Error-Name"))
                    {
                        error_name = we.Response.Headers["X-Epic-Error-Name"];
                    }

                    JObject json = null;
                    try
                    {
                        using (StreamReader reader = new StreamReader(we.Response.GetResponseStream()))
                        {
                            json = JObject.Parse(reader.ReadToEnd());
                        }
                    }
                    catch (Exception)
                    { }

                    err = GetErrorFromJson(json);
                }
                catch (Exception)
                {
                    err.Message = e.Message;
                    err.ErrorCode = Error.WebError;
                }
            }
            else
            {
                err.Message = e.Message;
                err.ErrorCode = Error.WebError;
            }

            return err;
        }
    }

    class Error<T>
    {
        public int ErrorCode { get; set; }
        public string Message { get; set; }

        public T Result { get; set; }

        public void FromError(Error err)
        {
            ErrorCode = err.ErrorCode;
            Message = err.Message;
        }

        public static explicit operator Error(Error<T> res)
        {
            return new Error { ErrorCode = res.ErrorCode, Message = res.Message };
        }
    }

}