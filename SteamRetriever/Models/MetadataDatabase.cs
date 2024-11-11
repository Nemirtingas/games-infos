using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamRetriever.Models;

public class MetadataDatabase
{
    private const uint LatestVersion = 1;
    public uint Version { get; set; } = LatestVersion;
    public Dictionary<long, ApplicationMetadata> ApplicationDetails { get; set; } = new();

    public Task UpdateMetadataDatabaseAsync()
    {
        //if (Version < LatestVersion && Version == 1)
        //    await UpgradeMetadataDatabaseVersion2Async();
        //
        //if (Version < LatestVersion && Version == 2)
        //    await UpgradeMetadataDatabaseVersion3Async();

        return Task.CompletedTask;
    }
}
