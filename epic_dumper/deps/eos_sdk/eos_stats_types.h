// Copyright Epic Games, Inc. All Rights Reserved.

#pragma once

#include "eos_common.h"

enum { k_iStatsCallbackBase = 13000 };
// next free callback_id: k_iStatsCallbackBase + 2

#define EOS_Stats_IngestData             EOS_Stats_IngestData001
#define EOS_Stats_IngestStatOptions      EOS_Stats_IngestStatOptions003
#define EOS_Stats_QueryStatsOptions      EOS_Stats_QueryStatsOptions003
#define EOS_Stats_Stat                   EOS_Stats_Stat001
#define EOS_Stats_GetStatCountOptions    EOS_Stats_GetStatCountOptions001
#define EOS_Stats_CopyStatByIndexOptions EOS_Stats_CopyStatByIndexOptions001
#define EOS_Stats_CopyStatByNameOptions  EOS_Stats_CopyStatByNameOptions001

#include <eos_stats_types1.10.2.h>
#include <eos_stats_types1.8.0.h>
#include <eos_stats_types1.7.1.h>

#define EOS_STATS_INGESTDATA_API_LATEST      EOS_STATS_INGESTDATA_API_001
#define EOS_STATS_INGESTSTAT_API_LATEST      EOS_STATS_INGESTSTAT_API_003
#define EOS_STATS_QUERYSTATS_API_LATEST      EOS_STATS_QUERYSTATS_API_003
#define EOS_STATS_STAT_API_LATEST            EOS_STATS_STAT_API_001
#define EOS_STATS_GETSTATSCOUNT_API_LATEST   EOS_STATS_GETSTATSCOUNT_API_001
#define EOS_STATS_COPYSTATBYINDEX_API_LATEST EOS_STATS_COPYSTATBYINDEX_API_001
#define EOS_STATS_COPYSTATBYNAME_API_LATEST  EOS_STATS_COPYSTATBYNAME_API_001