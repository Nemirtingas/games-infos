using System;
using Newtonsoft.Json;

namespace SteamRetriever.Models;
public class ApplicationMetadata
{
    public string Name { get; set; }
    public ulong ChangeNumber { get; set; } = 0;
    [JsonConverter(typeof(DateTimeToUnixMillisecondsConverter))]
    public DateTime LastUpdateTimestamp { get; set; }
}
