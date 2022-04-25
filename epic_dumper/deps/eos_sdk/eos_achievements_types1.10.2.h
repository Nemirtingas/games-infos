// Copyright Epic Games, Inc. All Rights Reserved.

#pragma once

#pragma pack(push, 8)

/** The most recent version of the EOS_Achievements_QueryPlayerAchievements struct. */
#define EOS_ACHIEVEMENTS_QUERYPLAYERACHIEVEMENTS_API_001 1

/**
 * Input parameters for the EOS_Achievements_QueryPlayerAchievements function.
 */
EOS_STRUCT(EOS_Achievements_QueryPlayerAchievementsOptions001, (
	/** API Version: Set this to EOS_ACHIEVEMENTS_QUERYPLAYERACHIEVEMENTS_API_LATEST. */
	int32_t ApiVersion;
	/** The Product User ID for the user whose achievements are to be retrieved. */
	EOS_ProductUserId UserId;
));

/** The most recent version of the EOS_Achievements_CopyPlayerAchievementByIndexOptions struct. */
#define EOS_ACHIEVEMENTS_COPYPLAYERACHIEVEMENTBYINDEX_API_001 1

/**
 * Input parameters for the EOS_Achievements_CopyPlayerAchievementByIndex function.
 */
EOS_STRUCT(EOS_Achievements_CopyPlayerAchievementByIndexOptions001, (
	/** API Version: Set this to EOS_ACHIEVEMENTS_COPYPLAYERACHIEVEMENTBYINDEX_API_LATEST. */
	int32_t ApiVersion;
	/** The Product User ID for the user who is copying the achievement. */
	EOS_ProductUserId UserId;
	/** The index of the player achievement data to retrieve from the cache. */
	uint32_t AchievementIndex;
));

/** The most recent version of the EOS_Achievements_CopyPlayerAchievementByAchievementIdOptions struct. */
#define EOS_ACHIEVEMENTS_COPYPLAYERACHIEVEMENTBYACHIEVEMENTID_API_001 1

/**
 * Input parameters for the EOS_Achievements_CopyPlayerAchievementByAchievementId function.
 */
EOS_STRUCT(EOS_Achievements_CopyPlayerAchievementByAchievementIdOptions001, (
	/** API Version: Set this to EOS_ACHIEVEMENTS_COPYPLAYERACHIEVEMENTBYACHIEVEMENTID_API_LATEST. */
	int32_t ApiVersion;
	/** The Product User ID for the user who is copying the achievement. */
	EOS_ProductUserId UserId;
	/** Achievement ID to search for when retrieving player achievement data from the cache. */
	const char* AchievementId;
));

#pragma pack(pop)
