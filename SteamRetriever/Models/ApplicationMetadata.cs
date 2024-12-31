using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SteamRetriever.Models;
public class ApplicationMetadata
{
    public ulong ChangeNumber { get; set; } = 0;
    [JsonConverter(typeof(DateTimeToUnixMillisecondsConverter))]
    public DateTime LastUpdateTimestamp { get; set; }
}
