
using Newtonsoft.Json.Linq;
using System.IO;
using System.Net;
using System;
using System.Linq;
using System.Runtime.Serialization;

namespace EpicKit
{
    public class WebApiException : Exception
    {
        public const int InvalidData = -6;
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

        public WebApiException() : base()
        { }

        public WebApiException(string message, int errorCode):base(message)
        {
            ErrorCode = errorCode;
        }

        public WebApiException(string message, Exception innerException) : base(message, innerException)
        { }
        
        protected WebApiException(SerializationInfo info, StreamingContext context): base(info, context)
        { }

        static public int ErrorCodeFromString(string error)
        {
            if (string.IsNullOrEmpty(error))
                return Unknown;

            switch (error)
            {
                case "errors.com.epicgames.unauthorized": return Unauthorized;
                case "errors.com.epicgames.common.authentication.authentication_failed": return CommonAuthenticationAuthenticationFailed;
                case "errors.com.epicgames.common.method_not_allowed": return CommonMethodNotAllowed;
                case "errors.com.epicgames.common.oauth.invalid_token": return CommonOauthInvalidToken;

                case "errors.com.epicgames.accountportal.session_id_invalid": return AccountPortalSessionIdInvalid;
                case "errors.com.epicgames.accountportal.validation": return AccountPortalValidation;
                case "errors.com.epicgames.accountportal.csrf_token_invalid": return AccountPortalCsrfTokenInvalid;

                case "errors.com.epicgames.account.oauth.exchange_code_not_found": return AccountOauthExchangeCodeNotFound;
                case "errors.com.epicgames.account.oauth.authorization_code_not_found": return AccountOauthAuthorizationCodeNotFound;
                case "errors.com.epicgames.account.auth_token.invalid_refresh_token": return AccountAuthTokenInvalidRefreshToken;

                case "errors.com.epicgames.oauth.scope_consent_required": return OAuthScopeConsentRequired;

                default: return Unknown;
            }
        }

        static public void BuildErrorFromJson(JObject json)
        {
            var err = default(WebApiException);

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

            err = new WebApiException(string.IsNullOrWhiteSpace(message) ? error_name : message, error_code != 0 ? ErrorCodeFromString(error_name) : err.ErrorCode = Unknown);

            throw err;
        }

        static public void BuildExceptionFromWebException(Exception e)
        {
            var err = default(WebApiException);
            if (e is WebException we && we.Response != null)
            {
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

                    BuildErrorFromJson(json);
                }
                catch (Exception)
                {
                    err = new WebApiException(e.Message, WebError);
                }
            }
            else
            {
                err = new WebApiException(e.Message, WebError);
            }

            if (err != null)
                throw err;
        }
    }

    public class WebApiOAuthScopeConsentRequiredException : WebApiException
    {
        public string ContinuationToken { get; set; }

        public WebApiOAuthScopeConsentRequiredException() : base()
        { }

        public WebApiOAuthScopeConsentRequiredException(string message) : base(message, WebApiException.OAuthScopeConsentRequired)
        {
        }

        public WebApiOAuthScopeConsentRequiredException(string message, Exception innerException) : base(message, innerException)
        { }

        protected WebApiOAuthScopeConsentRequiredException(SerializationInfo info, StreamingContext context) : base(info, context)
        { }
    }
}