// Copyright Epic Games, Inc. All Rights Reserved.

#pragma once

#include "eos_common.h"

enum { k_iLeaderboardsCallbackBase = 6000 };
// next free callback_id: k_iLeaderboardsCallbackBase + 3

#define EOS_Leaderboards_QueryLeaderboardDefinitionsOptions              EOS_Leaderboards_QueryLeaderboardDefinitionsOptions002
#define EOS_Leaderboards_Definition                                      EOS_Leaderboards_Definition001
#define EOS_Leaderboards_GetLeaderboardDefinitionCountOptions            EOS_Leaderboards_GetLeaderboardDefinitionCountOptions001
#define EOS_Leaderboards_CopyLeaderboardDefinitionByIndexOptions         EOS_Leaderboards_CopyLeaderboardDefinitionByIndexOptions001
#define EOS_Leaderboards_CopyLeaderboardDefinitionByLeaderboardIdOptions EOS_Leaderboards_CopyLeaderboardDefinitionByLeaderboardIdOptions001
#define EOS_Leaderboards_UserScoresQueryStatInfo                         EOS_Leaderboards_UserScoresQueryStatInfo001
#define EOS_Leaderboards_QueryLeaderboardUserScoresOptions               EOS_Leaderboards_QueryLeaderboardUserScoresOptions002
#define EOS_Leaderboards_LeaderboardUserScore                            EOS_Leaderboards_LeaderboardUserScore001
#define EOS_Leaderboards_GetLeaderboardUserScoreCountOptions             EOS_Leaderboards_GetLeaderboardUserScoreCountOptions001
#define EOS_Leaderboards_CopyLeaderboardUserScoreByIndexOptions          EOS_Leaderboards_CopyLeaderboardUserScoreByIndexOptions001
#define EOS_Leaderboards_CopyLeaderboardUserScoreByUserIdOptions         EOS_Leaderboards_CopyLeaderboardUserScoreByUserIdOptions001
#define EOS_Leaderboards_QueryLeaderboardRanksOptions                    EOS_Leaderboards_QueryLeaderboardRanksOptions001
#define EOS_Leaderboards_LeaderboardRecord                               EOS_Leaderboards_LeaderboardRecord002
#define EOS_Leaderboards_GetLeaderboardRecordCountOptions                EOS_Leaderboards_GetLeaderboardRecordCountOptions001
#define EOS_Leaderboards_CopyLeaderboardRecordByIndexOptions             EOS_Leaderboards_CopyLeaderboardRecordByIndexOptions001
#define EOS_Leaderboards_CopyLeaderboardRecordByUserIdOptions            EOS_Leaderboards_CopyLeaderboardRecordByUserIdOptions001

#include "eos_leaderboards_types1.10.2.h"
#include "eos_leaderboards_types1.8.0.h"
#include "eos_leaderboards_types1.5.0.h"

#define EOS_LEADERBOARDS_QUERYLEADERBOARDDEFINITIONS_API_LATEST              EOS_LEADERBOARDS_QUERYLEADERBOARDDEFINITIONS_API_002
#define EOS_LEADERBOARDS_DEFINITION_API_LATEST                               EOS_LEADERBOARDS_DEFINITION_API_001
#define EOS_LEADERBOARDS_GETLEADERBOARDDEFINITIONCOUNT_API_LATEST            EOS_LEADERBOARDS_GETLEADERBOARDDEFINITIONCOUNT_API_001
#define EOS_LEADERBOARDS_COPYLEADERBOARDDEFINITIONBYINDEX_API_LATEST         EOS_LEADERBOARDS_COPYLEADERBOARDDEFINITIONBYINDEX_API_001
#define EOS_LEADERBOARDS_COPYLEADERBOARDDEFINITIONBYLEADERBOARDID_API_LATEST EOS_LEADERBOARDS_COPYLEADERBOARDDEFINITIONBYLEADERBOARDID_API_001
#define EOS_LEADERBOARDS_USERSCORESQUERYSTATINFO_API_LATEST                  EOS_LEADERBOARDS_USERSCORESQUERYSTATINFO_API_001
#define EOS_LEADERBOARDS_QUERYLEADERBOARDUSERSCORES_API_LATEST               EOS_LEADERBOARDS_QUERYLEADERBOARDUSERSCORES_API_002
#define EOS_LEADERBOARDS_LEADERBOARDUSERSCORE_API_LATEST                     EOS_LEADERBOARDS_LEADERBOARDUSERSCORE_API_001
#define EOS_LEADERBOARDS_GETLEADERBOARDUSERSCORECOUNT_API_LATEST             EOS_LEADERBOARDS_GETLEADERBOARDUSERSCORECOUNT_API_001
#define EOS_LEADERBOARDS_COPYLEADERBOARDUSERSCOREBYINDEX_API_LATEST          EOS_LEADERBOARDS_COPYLEADERBOARDUSERSCOREBYINDEX_API_001
#define EOS_LEADERBOARDS_COPYLEADERBOARDUSERSCOREBYUSERID_API_LATEST         EOS_LEADERBOARDS_COPYLEADERBOARDUSERSCOREBYUSERID_API_001
#define EOS_LEADERBOARDS_QUERYLEADERBOARDRANKS_API_LATEST                    EOS_LEADERBOARDS_QUERYLEADERBOARDRANKS_API_001
#define EOS_LEADERBOARDS_LEADERBOARDRECORD_API_LATEST                        EOS_LEADERBOARDS_LEADERBOARDRECORD_API_002
#define EOS_LEADERBOARDS_GETLEADERBOARDRECORDCOUNT_API_LATEST                EOS_LEADERBOARDS_GETLEADERBOARDRECORDCOUNT_API_001
#define EOS_LEADERBOARDS_COPYLEADERBOARDRECORDBYINDEX_API_LATEST             EOS_LEADERBOARDS_COPYLEADERBOARDRECORDBYINDEX_API_002
#define EOS_LEADERBOARDS_COPYLEADERBOARDRECORDBYUSERID_API_LATEST            EOS_LEADERBOARDS_COPYLEADERBOARDRECORDBYUSERID_API_002
