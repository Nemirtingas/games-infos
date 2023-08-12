using Newtonsoft.Json;

namespace EpicKit.WebAPI
{
    public class ApplicationInfos
    {
        [JsonProperty("clientId")]
        public string ClientId { get; set; }
        [JsonProperty("clientName")]
        public string ClientName { get; set; }
        [JsonProperty("redirectUrl")]
        public string RedirectUrl { get; set; }
        [JsonProperty("internal")]
        public bool Internal { get; set; }
        [JsonProperty("type")]
        public string Type { get; set; }
        [JsonProperty("logo")]
        public string Logo { get; set; }
        [JsonProperty("verified")]
        public bool Verified { get; set; }
        [JsonProperty("privacyPolicy")]
        public string PrivacyPolicy { get; set; }
        [JsonProperty("mailingList")]
        public string MailingList { get; set; }
        [JsonProperty("native")]
        public bool Native { get; set; }
        [JsonProperty("product")]
        public string Product { get; set; }
        [JsonProperty("allowedScopes")]
        public List<AuthorizationScopes> AllowedScopes { get; set; }
        [JsonProperty("epicLoginOnly")]
        public bool EpicLoginOnly { get; set; }
        [JsonProperty("thirdParty")]
        public bool ThirdParty { get; set; }
    }
}
