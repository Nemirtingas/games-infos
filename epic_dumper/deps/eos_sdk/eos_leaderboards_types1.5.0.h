// Copyright Epic Games, Inc. All Rights Reserved.

#pragma once

#include "eos_common.h"

#pragma pack(push, 8)

EXTERN_C typedef struct EOS_LeaderboardsHandle* EOS_HLeaderboards;

/** The most recent version of the EOS_Leaderboards_LeaderboardRecord struct. */
#define EOS_LEADERBOARDS_LEADERBOARDRECORD_API_001 1

/**
 * Contains information about a single leaderboard record
 */
EOS_STRUCT(EOS_Leaderboards_LeaderboardRecord001, (
	/** Version of the API. */
	int32_t ApiVersion;
	/** User Id */
	EOS_ProductUserId UserId;
	/** Sorted position on leaderboard */
	uint32_t Rank;
	/** Leaderboard score. */
	int32_t Score;
));

/** The most recent version of the EOS_Leaderboards_CopyLeaderboardRecordByIndexOptions struct. */
#define EOS_LEADERBOARDS_COPYLEADERBOARDRECORDBYINDEX_API_001 1

/** The most recent version of the EOS_Leaderboards_CopyLeaderboardRecordByUserIdOptions struct. */
#define EOS_LEADERBOARDS_COPYLEADERBOARDRECORDBYUSERID_API_001 1

#pragma pack(pop)
