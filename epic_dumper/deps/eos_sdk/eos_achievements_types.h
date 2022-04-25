// Copyright Epic Games, Inc. All Rights Reserved.

#pragma once

#include "eos_common.h"

enum { k_iAchievementsCallbacks = 2000 };
// next free callback_id: k_iAchievementsCallbacks + 5

#define EOS_Achievements_QueryDefinitionsOptions                           EOS_Achievements_QueryDefinitionsOptions003
#define EOS_Achievements_StatThresholds                                    EOS_Achievements_StatThresholds001
#define EOS_Achievements_PlayerStatInfo                                    EOS_Achievements_PlayerStatInfo001
#define EOS_Achievements_Definition                                        EOS_Achievements_Definition001
#define EOS_Achievements_GetAchievementDefinitionCountOptions              EOS_Achievements_GetAchievementDefinitionCountOptions001
#define EOS_Achievements_CopyAchievementDefinitionByIndexOptions           EOS_Achievements_CopyAchievementDefinitionByIndexOptions001
#define EOS_Achievements_CopyAchievementDefinitionByAchievementIdOptions   EOS_Achievements_CopyAchievementDefinitionByAchievementIdOptions001
#define EOS_Achievements_QueryPlayerAchievementsOptions                    EOS_Achievements_QueryPlayerAchievementsOptions002
#define EOS_Achievements_PlayerAchievement                                 EOS_Achievements_PlayerAchievement002
#define EOS_Achievements_GetPlayerAchievementCountOptions                  EOS_Achievements_GetPlayerAchievementCountOptions001
#define EOS_Achievements_CopyPlayerAchievementByIndexOptions               EOS_Achievements_CopyPlayerAchievementByIndexOptions002
#define EOS_Achievements_CopyPlayerAchievementByAchievementIdOptions       EOS_Achievements_CopyPlayerAchievementByAchievementIdOptions002
#define EOS_Achievements_UnlockAchievementsOptions                         EOS_Achievements_UnlockAchievementsOptions001
#define EOS_Achievements_UnlockedAchievement                               EOS_Achievements_UnlockedAchievement001
#define EOS_Achievements_GetUnlockedAchievementCountOptions                EOS_Achievements_GetUnlockedAchievementCountOptions001
#define EOS_Achievements_CopyUnlockedAchievementByIndexOptions             EOS_Achievements_CopyUnlockedAchievementByIndexOptions001
#define EOS_Achievements_CopyUnlockedAchievementByAchievementIdOptions     EOS_Achievements_CopyUnlockedAchievementByAchievementIdOptions001
#define EOS_Achievements_AddNotifyAchievementsUnlockedOptions              EOS_Achievements_AddNotifyAchievementsUnlockedOptions001

#define EOS_Achievements_DefinitionV2                                      EOS_Achievements_DefinitionV2002
#define EOS_Achievements_CopyAchievementDefinitionV2ByIndexOptions         EOS_Achievements_CopyAchievementDefinitionV2ByIndexOptions002
#define EOS_Achievements_CopyAchievementDefinitionV2ByAchievementIdOptions EOS_Achievements_CopyAchievementDefinitionV2ByAchievementIdOptions002
#define EOS_Achievements_AddNotifyAchievementsUnlockedV2Options            EOS_Achievements_AddNotifyAchievementsUnlockedV2Options002

#include "eos_achievements_types1.14.0.h"
#include "eos_achievements_types1.10.2.h"
#include "eos_achievements_types1.8.0.h"
#include "eos_achievements_types1.6.1.h"
#include "eos_achievements_types1.5.0.h"

#define EOS_ACHIEVEMENTS_QUERYDEFINITIONS_API_LATEST                           EOS_ACHIEVEMENTS_QUERYDEFINITIONS_API_003
#define EOS_ACHIEVEMENTS_STATTHRESHOLDS_API_LATEST                             EOS_ACHIEVEMENTS_STATTHRESHOLDS_API_001
#define EOS_ACHIEVEMENTS_PLAYERSTATINFO_API_LATEST                             EOS_ACHIEVEMENTS_PLAYERSTATINFO_API_001
#define EOS_ACHIEVEMENTS_DEFINITION_API_LATEST                                 EOS_ACHIEVEMENTS_DEFINITION_API_001
#define EOS_ACHIEVEMENTS_GETACHIEVEMENTDEFINITIONCOUNT_API_LATEST              EOS_ACHIEVEMENTS_GETACHIEVEMENTDEFINITIONCOUNT_API_001
#define EOS_ACHIEVEMENTS_COPYDEFINITIONBYINDEX_API_LATEST                      EOS_ACHIEVEMENTS_COPYDEFINITIONBYINDEX_API_001
#define EOS_ACHIEVEMENTS_COPYDEFINITIONBYACHIEVEMENTID_API_LATEST              EOS_ACHIEVEMENTS_COPYDEFINITIONBYACHIEVEMENTID_API_001
#define EOS_ACHIEVEMENTS_QUERYPLAYERACHIEVEMENTS_API_LATEST                    EOS_ACHIEVEMENTS_QUERYPLAYERACHIEVEMENTS_API_002
#define EOS_ACHIEVEMENTS_PLAYERACHIEVEMENT_API_LATEST                          EOS_ACHIEVEMENTS_PLAYERACHIEVEMENT_API_002
#define EOS_ACHIEVEMENTS_GETPLAYERACHIEVEMENTCOUNT_API_LATEST                  EOS_ACHIEVEMENTS_GETPLAYERACHIEVEMENTCOUNT_API_001
#define EOS_ACHIEVEMENTS_COPYPLAYERACHIEVEMENTBYINDEX_API_LATEST               EOS_ACHIEVEMENTS_COPYPLAYERACHIEVEMENTBYINDEX_API_002
#define EOS_ACHIEVEMENTS_COPYPLAYERACHIEVEMENTBYACHIEVEMENTID_API_LATEST       EOS_ACHIEVEMENTS_COPYPLAYERACHIEVEMENTBYACHIEVEMENTID_API_002
#define EOS_ACHIEVEMENTS_UNLOCKACHIEVEMENTS_API_LATEST                         EOS_ACHIEVEMENTS_UNLOCKACHIEVEMENTS_API_001
#define EOS_ACHIEVEMENTS_UNLOCKEDACHIEVEMENT_API_LATEST                        EOS_ACHIEVEMENTS_UNLOCKEDACHIEVEMENT_API_001
#define EOS_ACHIEVEMENTS_GETUNLOCKEDACHIEVEMENTCOUNT_API_LATEST                EOS_ACHIEVEMENTS_GETUNLOCKEDACHIEVEMENTCOUNT_API_001
#define EOS_ACHIEVEMENTS_COPYUNLOCKEDACHIEVEMENTBYINDEX_API_LATEST             EOS_ACHIEVEMENTS_COPYUNLOCKEDACHIEVEMENTBYINDEX_API_001
#define EOS_ACHIEVEMENTS_COPYUNLOCKEDACHIEVEMENTBYACHIEVEMENTID_API_LATEST     EOS_ACHIEVEMENTS_COPYUNLOCKEDACHIEVEMENTBYACHIEVEMENTID_API_001
#define EOS_ACHIEVEMENTS_ADDNOTIFYACHIEVEMENTSUNLOCKED_API_LATEST              EOS_ACHIEVEMENTS_ADDNOTIFYACHIEVEMENTSUNLOCKED_API_001

#define EOS_ACHIEVEMENTS_DEFINITIONV2_API_LATEST                               EOS_ACHIEVEMENTS_DEFINITIONV2_API_002
#define EOS_ACHIEVEMENTS_COPYACHIEVEMENTDEFINITIONV2BYINDEX_API_LATEST         EOS_ACHIEVEMENTS_COPYACHIEVEMENTDEFINITIONV2BYINDEX_API_002
#define EOS_ACHIEVEMENTS_COPYACHIEVEMENTDEFINITIONV2BYACHIEVEMENTID_API_LATEST EOS_ACHIEVEMENTS_COPYACHIEVEMENTDEFINITIONV2BYACHIEVEMENTID_API_002
#define EOS_ACHIEVEMENTS_ADDNOTIFYACHIEVEMENTSUNLOCKEDV2_API_LATEST            EOS_ACHIEVEMENTS_ADDNOTIFYACHIEVEMENTSUNLOCKEDV2_API_002