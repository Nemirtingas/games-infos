// Copyright Epic Games, Inc. All Rights Reserved.

#pragma once

#include "eos_common.h"

#pragma pack(push, 8)

/** The most recent version of the EOS_Leaderboards_QueryLeaderboardDefinitions struct. */
#define EOS_LEADERBOARDS_QUERYLEADERBOARDDEFINITIONS_API_001 1

/**
 * Input parameters for the EOS_Leaderboards_QueryLeaderboardDefinitions function.
 * StartTime and EndTime are optional parameters, they can be used to limit the list of definitions
 * to overlap the time window specified.
 */
EOS_STRUCT(EOS_Leaderboards_QueryLeaderboardDefinitionsOptions001, (
	/** API Version: Set this to EOS_LEADERBOARDS_QUERYLEADERBOARDDEFINITIONS_API_LATEST. */
	int32_t ApiVersion;
	/** An optional POSIX timestamp for the leaderboard's start time, or EOS_LEADERBOARDS_TIME_UNDEFINED */
	int64_t StartTime;
	/** An optional POSIX timestamp for the leaderboard's end time, or EOS_LEADERBOARDS_TIME_UNDEFINED */
	int64_t EndTime;
));

/** The most recent version of the EOS_Leaderboards_QueryLeaderboardUserScores struct. */
#define EOS_LEADERBOARDS_QUERYLEADERBOARDUSERSCORES_API_001 1

/**
 * Input parameters for the EOS_Leaderboards_QueryLeaderboardUserScores function.
 */
EOS_STRUCT(EOS_Leaderboards_QueryLeaderboardUserScoresOptions001, (
	/** API Version: Set this to EOS_LEADERBOARDS_QUERYLEADERBOARDUSERSCORES_API_LATEST. */
	int32_t ApiVersion;
	/** An array of Product User IDs indicating the users whose scores you want to retrieve */
	const EOS_ProductUserId* UserIds;
	/** The number of users included in query */
	uint32_t UserIdsCount;
	/** The stats to be collected, along with the sorting method to use when determining rank order for each stat */
	const EOS_Leaderboards_UserScoresQueryStatInfo* StatInfo;
	/** The number of stats to query */
	uint32_t StatInfoCount;
	/** An optional POSIX timestamp, or EOS_LEADERBOARDS_TIME_UNDEFINED; results will only include scores made after this time */
	int64_t StartTime;
	/** An optional POSIX timestamp, or EOS_LEADERBOARDS_TIME_UNDEFINED; results will only include scores made before this time */
	int64_t EndTime;
));

#pragma pack(pop)

#include "eos_leaderboards_types_deprecated.inl"