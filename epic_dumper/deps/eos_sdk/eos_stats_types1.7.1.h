// Copyright Epic Games, Inc. All Rights Reserved.

#pragma once

#pragma pack(push, 8)

/** The most recent version of the EOS_Stats_QueryStats struct. */
#define EOS_STATS_QUERYSTATS_API_001 1

/**
 * Input parameters for the EOS_Stats_QueryStats Function.
 */
EOS_STRUCT(EOS_Stats_QueryStatsOptions001, (
	/** API Version. */
	int32_t ApiVersion;
	/** The account ID for the user whose stats are to be retrieved. */
	EOS_ProductUserId UserId;
	/** If not EOS_STATS_TIME_UNDEFINED then this is the POSIX timestamp for start time (Optional). */
	int64_t StartTime;
	/** If not EOS_STATS_TIME_UNDEFINED then this is the POSIX timestamp for end time (Optional). */
	int64_t EndTime;
	/** An array of stat names to query for (Optional). */
	const char** StatNames;
	/** The number of stat names included in query (Optional), may not exceed EOS_STATS_MAX_QUERY_STATS. */
	uint32_t StatNamesCount;
));

/** The most recent version of the EOS_Stats_IngestStat struct. */
#define EOS_STATS_INGESTSTAT_API_001 1

/**
 * Input parameters for the EOS_Stats_IngestStat Function.
 */
EOS_STRUCT(EOS_Stats_IngestStatOptions001, (
	/** API Version. */
	int32_t ApiVersion;
	/** The account ID for the user whose stat is being ingested. */
	EOS_ProductUserId UserId;
	/** Stats to ingest. */
	const EOS_Stats_IngestData* Stats;
	/** The number of stats to ingest, may not exceed EOS_STATS_MAX_INGEST_STATS. */
	uint32_t StatsCount;
));

#pragma pack(pop)
