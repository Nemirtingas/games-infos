
using Newtonsoft.Json;
using System;

namespace EGS
{
    class Entitlement
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "entitlementName")]
        public string EntitlementName { get; set; }

        [JsonProperty(PropertyName = "namespace")]
        public string Namespace { get; set; }

        [JsonProperty(PropertyName = "catalogItemId")]
        public string CatalogItemId { get; set; }

        [JsonProperty(PropertyName = "accountId")]
        public string AccountId { get; set; }

        [JsonProperty(PropertyName = "identityId")]
        public string IdentityId { get; set; }

        [JsonProperty(PropertyName = "entitlementType")]
        public string EntitlementType { get; set; }

        [JsonProperty(PropertyName = "grantDate")]
        public DateTime GrantDate { get; set; }

        [JsonProperty(PropertyName = "consumable")]
        public string Consumable { get; set; }

        [JsonProperty(PropertyName = "status")]
        public string Status { get; set; }

        [JsonProperty(PropertyName = "active")]
        public string Active { get; set; }

        [JsonProperty(PropertyName = "useCount")]
        public string UseCount { get; set; }

        [JsonProperty(PropertyName = "originalUseCount")]
        public string OriginalUseCount { get; set; }

        [JsonProperty(PropertyName = "platformType")]
        public string PlatformType { get; set; }

        [JsonProperty(PropertyName = "created")]
        public DateTime Created { get; set; }

        [JsonProperty(PropertyName = "updated")]
        public DateTime Updated { get; set; }

        [JsonProperty(PropertyName = "groupEntitlement")]
        public string GroupEntitlement { get; set; }

        [JsonProperty(PropertyName = "country")]
        public string Country { get; set; }

        public Entitlement()
        {
            Reset();
        }

        public void Reset()
        {
            Id = string.Empty;
            EntitlementName = string.Empty;
            Namespace = string.Empty;
            CatalogItemId = string.Empty;
            AccountId = string.Empty;
            IdentityId = string.Empty;
            EntitlementType = string.Empty;
            GrantDate = new DateTime();
            Consumable = string.Empty;
            Status = string.Empty;
            Active = string.Empty;
            UseCount = string.Empty;
            OriginalUseCount = string.Empty;
            PlatformType = string.Empty;
            Created = new DateTime();
            Updated = new DateTime();
            GroupEntitlement = string.Empty;
            Country = string.Empty;
        }
    }
}