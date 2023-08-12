using Newtonsoft.Json;

namespace EpicKit.WebAPI.Models
{
    public class StoreApplicationCustomAttribute
    {
        [JsonProperty(PropertyName = "type")]
        public string Type;

        [JsonProperty(PropertyName = "value")]
        public string Value;

        public StoreApplicationCustomAttribute()
        {
            Reset();
        }

        public void Reset()
        {
            Type = string.Empty;
            Value = string.Empty;
        }
    }
}