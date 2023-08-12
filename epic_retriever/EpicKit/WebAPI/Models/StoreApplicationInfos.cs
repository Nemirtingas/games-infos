using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EpicKit.WebAPI.Models
{
    public class StoreApplicationInfos
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
        public List<StoreApplicationKeyImage> KeyImages { get; set; }

        [JsonProperty(PropertyName = "categories")]
        public List<StoreApplicationCategory> Categories { get; set; }

        [JsonProperty(PropertyName = "namespace")]
        public string Namespace { get; set; }

        [JsonProperty(PropertyName = "status")]
        public string Status { get; set; }

        [JsonProperty(PropertyName = "creationDate")]
        public DateTime CreationDate { get; set; }

        [JsonProperty(PropertyName = "lastModifiedDate")]
        public DateTime LastModifiedDate { get; set; }

        [JsonProperty(PropertyName = "customAttributes")]
        public Dictionary<string, StoreApplicationCustomAttribute> CustomAttributes { get; set; }

        [JsonProperty(PropertyName = "entitlementName")]
        public string EntitlementName { get; set; }

        [JsonProperty(PropertyName = "entitlementType")]
        public string EntitlementType { get; set; }

        [JsonProperty(PropertyName = "itemType")]
        public string ItemType { get; set; }

        [JsonProperty(PropertyName = "releaseInfo")]
        public List<StoreApplicationReleaseInfo> ReleaseInfo { get; set; }

        [JsonProperty(PropertyName = "developer")]
        public string Developer { get; set; }

        [JsonProperty(PropertyName = "developerId")]
        public string DeveloperId { get; set; }

        [JsonProperty(PropertyName = "eulaIds")]
        public List<string> EulaIds { get; set; }

        [JsonProperty(PropertyName = "endOfSupport")]
        public bool EndOfSupport { get; set; }

        [JsonProperty(PropertyName = "mainGameItem")]
        public StoreApplicationMainGameModel MainGameItem { get; set; }

        [JsonProperty(PropertyName = "dlcItemList")]
        public List<StoreApplicationInfos> DlcItemList { get; set; }

        [JsonProperty(PropertyName = "ageGatings")]
        public JObject AgeGatings { get; set; }

        [JsonProperty(PropertyName = "applicationId")]
        public string ApplicationId { get; set; }

        [JsonProperty(PropertyName = "unsearchable")]
        public bool Unsearchable { get; set; }

        public StoreApplicationInfos()
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
            KeyImages = new List<StoreApplicationKeyImage>();
            Categories = new List<StoreApplicationCategory>();
            Namespace = string.Empty;
            Status = string.Empty;
            CreationDate = new DateTime();
            LastModifiedDate = new DateTime();
            CustomAttributes = new Dictionary<string, StoreApplicationCustomAttribute>();
            EntitlementName = string.Empty;
            EntitlementType = string.Empty;
            ItemType = string.Empty;
            ReleaseInfo = new List<StoreApplicationReleaseInfo>();
            Developer = string.Empty;
            DeveloperId = string.Empty;
            EulaIds = new List<string>();
            EndOfSupport = false;
            MainGameItem = null;
            DlcItemList = new List<StoreApplicationInfos>();
            AgeGatings = new JObject();
            ApplicationId = string.Empty;
            Unsearchable = false;
        }
    }
}