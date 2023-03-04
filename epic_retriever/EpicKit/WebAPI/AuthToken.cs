
namespace EpicKit
{
    class AuthToken
    {
        public enum TokenType
        {
            ExchangeCode,
            RefreshToken,
            AuthorizationCode,
            ClientCredentials,
        }

        public string Token { get; set; }
        public TokenType Type { get; set; }
    }
}