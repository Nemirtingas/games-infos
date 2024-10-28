
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
        // AccountPortal
        public const int AccountPortalValidation = 1;
        public const int AccountPortalCsrfTokenInvalid = 2;
        public const int AccountPortalSessionIdInvalid = 3;
        // AccountOauth
        public const int AccountOauthExchangeCodeNotFound = 18057;
        // AccountAuthToken
        public const int AccountAuthTokenInvalidRefreshToken = 18036;
        // CommonMethod
        public const int CommonMethodNotAllowed = 1009;
        // CommonOautj
        public const int CommonOauthInvalidToken = 7;
        // CommonAuthentication
        public const int CommonAuthenticationAuthenticationFailed = 1032;

        public int ErrorCode { get; set; }
        public string Message { get; set; }
    }

    class Error<T>
    {
        public int ErrorCode { get; set; }
        public string Message { get; set; }

        public T Result { get; set; }

        public static explicit operator Error(Error<T> res)
        {
            return new Error { ErrorCode = res.ErrorCode, Message = res.Message };
        }
    }

}