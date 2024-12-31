using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using SteamKit2;
using SteamKit2.Internal;

namespace SteamRetriever;

public class SteamUserStats : ClientMsgHandler
{
    Dictionary<EMsg, Action<IPacketMsg>> dispatchMap;

    internal SteamUserStats()
    {
        dispatchMap = new Dictionary<EMsg, Action<IPacketMsg>>
            {
                { EMsg.ClientGetUserStatsResponse, HandleUserStatsResponse },
            };
    }

    // define our custom callback class 
    // this will pass data back to the user of the handler 
    public class GetUserStatsCallback : CallbackMsg
    {
        public sealed class Stat
        {
            uint StatId;
            uint StatValue;

            public Stat(CMsgClientGetUserStatsResponse.Stats stat)
            {
                StatId = stat.stat_id;
                StatValue = stat.stat_value;
            }
        }

        public EResult Result;
        public ReadOnlyCollection<Stat> Stats;
        public uint CRCStats;
        public ulong GameId;
        public KeyValue Schema;

        // generally we don't want user code to instantiate callback objects, 
        // but rather only let handlers create them 
        internal GetUserStatsCallback(JobID jobID, CMsgClientGetUserStatsResponse msg)
        {
            this.JobID = jobID;

            this.Result = (EResult)msg.eresult;
            if (this.Result == EResult.OK)
            {
                var stats_list = msg.stats.Select(s => new Stat(s))
                    .ToList();

                this.Stats = new ReadOnlyCollection<Stat>(stats_list);
                this.CRCStats = msg.crc_stats;
                this.GameId = msg.game_id;
                this.Schema = new KeyValue();
                if (!Schema.TryReadAsBinary(new MemoryStream(msg.schema)))
                    this.Schema = null;
            }
        }
    }

    public override void HandleMsg(IPacketMsg packetMsg)
    {
        if (packetMsg == null)
        {
            throw new ArgumentNullException(nameof(packetMsg));
        }

        if (!dispatchMap.TryGetValue(packetMsg.MsgType, out var handlerFunc))
        {
            // ignore messages that we don't have a handler function for 
            return;
        }

        handlerFunc(packetMsg);
    }

    void HandleUserStatsResponse(IPacketMsg packetMsg)
    {
        var userStats = new ClientMsgProtobuf<CMsgClientGetUserStatsResponse>(packetMsg);

        var callback = new GetUserStatsCallback(packetMsg.TargetJobID, userStats.Body);
        this.Client.PostCallback(callback);
    }

    public AsyncJob<GetUserStatsCallback> GetUserStats(uint appid, ulong user_id)
    {
        var request = new ClientMsgProtobuf<CMsgClientGetUserStats>(EMsg.ClientGetUserStats);
        request.SourceJobID = this.Client.GetNextJobID();

        request.Body.steam_id_for_user = user_id;
        request.Body.game_id = appid;
        request.Body.crc_stats = 0;
        request.Body.schema_local_version = -1;

        this.Client.Send(request);

        return new AsyncJob<GetUserStatsCallback>(this.Client, request.SourceJobID);
    }
}
