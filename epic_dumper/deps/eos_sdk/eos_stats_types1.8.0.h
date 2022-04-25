// Copyright Epic Games, Inc. All Rights Reserved.

#pragma once

#pragma pack(push, 8)

EXTERN_C typedef struct EOS_StatsHandle* EOS_HStats;

/** The most recent version of the EOS_Stats_IngestStat struct. */
#define EOS_STATS_INGESTSTAT_API_002 2

using EOS_Stats_IngestStatOptions002 = EOS_Stats_IngestStatOptions003;

/** The most recent version of the EOS_Stats_QueryStats struct. */
#define EOS_STATS_QUERYSTATS_API_002 2

using EOS_Stats_QueryStatsOptions002 = EOS_Stats_QueryStatsOptions003;

#pragma pack(pop)
