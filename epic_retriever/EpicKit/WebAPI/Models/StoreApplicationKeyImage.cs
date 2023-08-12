using Newtonsoft.Json;
using System;

namespace EpicKit.WebAPI.Models
{
    public class StoreApplicationKeyImage
    {
        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }

        [JsonProperty(PropertyName = "url")]
        public string Url { get; set; }

        [JsonProperty(PropertyName = "md5")]
        public string Md5 { get; set; }

        [JsonProperty(PropertyName = "width")]
        public uint Width { get; set; }

        [JsonProperty(PropertyName = "height")]
        public uint Height { get; set; }

        [JsonProperty(PropertyName = "size")]
        public uint Size { get; set; }

        [JsonProperty(PropertyName = "uploadedDate")]
        public DateTime UploadedDate { get; set; }

        public StoreApplicationKeyImage()
        {
            Reset();
        }

        public void Reset()
        {
            Type = string.Empty;
            Url = string.Empty;
            Md5 = string.Empty;
            Width = 0;
            Height = 0;
            Size = 0;
            UploadedDate = new DateTime();
        }
    }
}