
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace EGS
{
    class ReleaseInfo
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "appId")]
        public string AppId { get; set; }

        [JsonProperty(PropertyName = "platform")]
        public List<string> Platform { get; set; }

        [JsonProperty(PropertyName = "dateAdded")]
        public DateTime DateAdded { get; set; }

        public ReleaseInfo()
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