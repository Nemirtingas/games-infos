using Newtonsoft.Json;

namespace EpicKit.WebAPI.Models
{
    public class CustomAttribute
    {
        [JsonProperty(PropertyName = "type")]
        public string Type;

        [JsonProperty(PropertyName = "value")]
        public string Value;

        public CustomAttribute()
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