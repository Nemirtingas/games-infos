// Copyright Epic Games, Inc. All Rights Reserved.

#pragma once

#pragma pack(push, 8)

/** The most recent version of the EOS_Achievements_QueryDefinitions struct. */
#define EOS_ACHIEVEMENTS_QUERYDEFINITIONS_API_001 1

/**
 * Input parameters for the EOS_Achievements_QueryDefinitions Function.
 */
EOS_STRUCT(EOS_Achievements_QueryDefinitionsOptions001, (
	/** API Version. */
	int32_t ApiVersion;
	/** Product User ID for user who is querying definitions, if not valid default text will be returned. */
	EOS_ProductUserId UserId;
	/** Epic account ID for user who is querying definitions, if not valid default text will be returned. */
	EOS_EpicAccountId EpicUserId;
	/** An array of Achievement IDs for hidden achievements to get full details for. */
	const char** HiddenAchievementIds;
	/** The number of hidden achievements to get full details for. */
	uint32_t HiddenAchievementsCount;
));

#pragma pack(pop)
