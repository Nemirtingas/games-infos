using Newtonsoft.Json;

namespace EpicKit.WebAPI.Models
{
    public class AppAsset
    {
        [JsonProperty(PropertyName = "appName")]
        public string AppName { get; set; }

        [JsonProperty(PropertyName = "labelName")]
        public string LabelName { get; set; }

        [JsonProperty(PropertyName = "buildVersion")]
        public string BuildVersion { get; set; }

        [JsonProperty(PropertyName = "catalogItemId")]
        public string CatalogItemId { get; set; }

        [JsonProperty(PropertyName = "namespace")]
        public string Namespace { get; set; }

        [JsonProperty(PropertyName = "assetId")]
        public string AssetId { get; set; }

        public void Reset()
        {
            AppName = string.Empty;
            LabelName = string.Empty;
            BuildVersion = string.Empty;
            CatalogItemId = string.Empty;
            Namespace = string.Empty;
            AssetId = string.Empty;
        }
    }
}