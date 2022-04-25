// Copyright Epic Games, Inc. All Rights Reserved.
#pragma once

#include "eos_common.h"

enum { k_iSanctionsCallbackBase = 19000 };
// next free callback_id: k_iSanctionsCallbackBase + 1

#define EOS_Sanctions_PlayerSanction                    EOS_Sanctions_PlayerSanction002
#define EOS_Sanctions_QueryActivePlayerSanctionsOptions EOS_Sanctions_QueryActivePlayerSanctionsOptions002
#define EOS_Sanctions_GetPlayerSanctionCountOptions     EOS_Sanctions_GetPlayerSanctionCountOptions001
#define EOS_Sanctions_CopyPlayerSanctionByIndexOptions  EOS_Sanctions_CopyPlayerSanctionByIndexOptions001

#include "eos_sanctions_types1.14.0.h"
#include "eos_sanctions_types1.11.0.h"

#define EOS_SANCTIONS_PLAYERSANCTION_API_LATEST             EOS_SANCTIONS_PLAYERSANCTION_API_002
#define EOS_SANCTIONS_QUERYACTIVEPLAYERSANCTIONS_API_LATEST EOS_SANCTIONS_QUERYACTIVEPLAYERSANCTIONS_API_002
#define EOS_SANCTIONS_GETPLAYERSANCTIONCOUNT_API_LATEST     EOS_SANCTIONS_GETPLAYERSANCTIONCOUNT_API_001
#define EOS_SANCTIONS_COPYPLAYERSANCTIONBYINDEX_API_LATEST  EOS_SANCTIONS_COPYPLAYERSANCTIONBYINDEX_API_001