
namespace EGS
{
    class AuthToken
    {
        public enum TokenType
        {
            ExchangeCode,
            RefreshToken,
            AuthorizationCode,
        }

        public string Token { get; set; }
        public TokenType Type { get; set; }
    }
}