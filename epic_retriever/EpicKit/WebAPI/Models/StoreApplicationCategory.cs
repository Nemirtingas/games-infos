using Newtonsoft.Json;

namespace EpicKit.WebAPI.Models
{
    public class StoreApplicationCategory
    {
        [JsonProperty(PropertyName = "path")]
        public string Path { get; set; }

        public StoreApplicationCategory()
        {
            Reset();
        }

        public void Reset()
        {
            Path = string.Empty;
        }
    }
}