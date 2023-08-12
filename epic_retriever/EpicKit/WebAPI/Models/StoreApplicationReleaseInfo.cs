using Newtonsoft.Json;

namespace EpicKit.WebAPI.Models
{
    public class StoreApplicationReleaseInfo
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "appId")]
        public string AppId { get; set; }

        [JsonProperty(PropertyName = "platform")]
        public List<string> Platform { get; set; }

        [JsonProperty(PropertyName = "dateAdded")]
        public DateTime DateAdded { get; set; }

        public StoreApplicationReleaseInfo()
        {
            Reset();
        }

        public void Reset()
        {
            Id = string.Empty;
            AppId = string.Empty;
            Platform = new List<string>();
            DateAdded = new DateTime();
        }
    }
}