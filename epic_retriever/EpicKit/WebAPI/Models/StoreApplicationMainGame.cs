using Newtonsoft.Json;

namespace EpicKit.WebAPI.Models
{
    public class StoreApplicationMainGameModel
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "namespace")]
        public string Namespace { get; set; }

        [JsonProperty(PropertyName = "unsearchable")]
        public bool Unsearchable { get; set; }

        public StoreApplicationMainGameModel()
        {
            Reset();
        }

        public void Reset()
        {
            Id = string.Empty;
            Namespace = string.Empty;
            Unsearchable = false;
        }
    }
}