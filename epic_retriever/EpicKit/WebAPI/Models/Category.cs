using Newtonsoft.Json;

namespace EpicKit.WebAPI.Models
{
    public class Category
    {
        [JsonProperty(PropertyName = "path")]
        public string Path { get; set; }

        public Category()
        {
            Reset();
        }

        public void Reset()
        {
            Path = string.Empty;
        }
    }
}