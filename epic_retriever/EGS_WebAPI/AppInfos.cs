
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace EGS
{
    class AppInfos
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "title")]
        public string Title { get; set; }

        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }

        [JsonProperty(PropertyName = "longDescription")]
        public string LongDescription { get; set; }

        [JsonProperty(PropertyName = "keyImages")]
        public List<KeyImage> KeyImages { get; set; }

        [JsonProperty(PropertyName = "categories")]
        public List<Category> Categories { get; set; }

        [JsonProperty(PropertyName = "namespace")]
        public string Namespace { get; set; }

        [JsonProperty(PropertyName = "status")]
        public string Status { get; set; }

        [JsonProperty(PropertyName = "creationDate")]
        public DateTime CreationDate { get; set; }

        [JsonProperty(PropertyName = "lastModifiedDate")]
        public DateTime LastModifiedDate { get; set; }

        [JsonProperty(PropertyName = "customAttributes")]
        public Dictionary<string, CustomAttribute> CustomAttributes { get; set; }

        [JsonProperty(PropertyName = "entitlementName")]
        public string EntitlementName { get; set; }

        [JsonProperty(PropertyName = "entitlementType")]
        public string EntitlementType { get; set; }

        [JsonProperty(PropertyName = "itemType")]
        public string ItemType { get; set; }

        [JsonProperty(PropertyName = "releaseInfo")]
        public List<ReleaseInfo> ReleaseInfo { get; set; }

        [JsonProperty(PropertyName = "developer")]
        public string Developer { get; set; }

        [JsonProperty(PropertyName = "developerId")]
        public string DeveloperId { get; set; }

        [JsonProperty(PropertyName = "eulaIds")]
        public List<string> EulaIds { get; set; }

        [JsonProperty(PropertyName = "endOfSupport")]
        public bool EndOfSupport { get; set; }

        [JsonProperty(PropertyName = "mainGameItem")]
        public MainGame MainGameItem { get; set; }

        [JsonProperty(PropertyName = "dlcItemList")]
        public List<AppInfos> DlcItemList { get; set; }

        [JsonProperty(PropertyName = "ageGatings")]
        public JObject AgeGatings { get; set; }

        [JsonProperty(PropertyName = "applicationId")]
        public string ApplicationId { get; set; }

        [JsonProperty(PropertyName = "unsearchable")]
        public bool Unsearchable { get; set; }

        public AppInfos()
        {
            Reset();
        }

        public bool IsDlc { get => MainGameItem != null; }

        public void Reset()
        {
            Id = string.Empty;
            Title = string.Empty;
            Description = string.Empty;
            LongDescription = string.Empty;
            KeyImages = new List<KeyImage>();
            Categories = new List<Category>();
            Namespace = string.Empty;
            Status = string.Empty;
            CreationDate = new DateTime();
            LastModifiedDate = new DateTime();
            CustomAttributes = new Dictionary<string, CustomAttribute>();
            EntitlementName = string.Empty;
            EntitlementType = string.Empty;
            ItemType = string.Empty;
            ReleaseInfo = new List<ReleaseInfo>();
            Developer = string.Empty;
            DeveloperId = string.Empty;
            EulaIds = new List<string>();
            EndOfSupport = false;
            MainGameItem = null;
            DlcItemList = new List<AppInfos>();
            AgeGatings = new JObject();
            ApplicationId = string.Empty;
            Unsearchable = false;
        }
    }
}