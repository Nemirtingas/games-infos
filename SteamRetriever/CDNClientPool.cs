using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SteamKit2.CDN;

namespace SteamRetriever;

/// <summary>
/// CDNClientPool provides a pool of connections to CDN endpoints, requesting CDN tokens as needed
/// </summary>
internal class CDNClientPool
{
    private readonly Steam3Session SteamSession;
    internal Client CDNClient { get; }
    internal Server ProxyServer { get; private set; }

    private ConcurrentDictionary<string, int> ContentServerPenalty;
    private readonly List<Server> servers = [];
    private int nextServer;

    internal CDNClientPool(Steam3Session steamSession, ConcurrentDictionary<string, int> contentServerPenalty)
    {
        SteamSession = steamSession;
        ContentServerPenalty = contentServerPenalty;
        CDNClient = new Client(steamSession.steamClient);
    }

    internal async Task UpdateServerList()
    {
        var servers = await this.SteamSession.steamContent.GetServersForSteamPipe();

        ProxyServer = servers.Where(x => x.UseAsProxy).FirstOrDefault();

        var weightedCdnServers = servers
            .Where(server =>
            {
                return (server.Type == "SteamCache" || server.Type == "CDN");
            })
            .Select(server =>
            {
                ContentServerPenalty.TryGetValue(server.Host, out var penalty);

                return (server, penalty);
            })
            .OrderBy(pair => pair.penalty).ThenBy(pair => pair.server.WeightedLoad);

        foreach (var (server, weight) in weightedCdnServers)
        {
            for (var i = 0; i < server.NumEntries; i++)
            {
                this.servers.Add(server);
            }
        }

        if (this.servers.Count == 0)
        {
            throw new Exception("Failed to retrieve any download servers.");
        }
    }

    internal Server GetConnection(uint appId)
    {
        var servers = this.servers.Where(e => e.AllowedAppIds.Length == 0 || e.AllowedAppIds.Contains(appId)).ToList();
        return servers[nextServer % servers.Count];
    }

    internal void ReturnConnection(Server server)
    {
        if (server == null) return;

        // nothing to do, maybe remove from ContentServerPenalty?
    }

    internal void ReturnBrokenConnection(Server server)
    {
        if (server == null) return;

        lock (servers)
        {
            if (servers[nextServer % servers.Count] == server)
            {
                nextServer++;

                // TODO: Add server to ContentServerPenalty
            }
        }
    }
}