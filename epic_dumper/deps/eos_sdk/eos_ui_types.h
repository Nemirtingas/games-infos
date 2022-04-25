// Copyright Epic Games, Inc. All Rights Reserved.

#pragma once

#include "eos_common.h"

enum { k_iUICallbackBase = 15000 };
// next free callback_id: k_iUICallbackBase + 3

#define EOS_UI_ShowFriendsOptions                     EOS_UI_ShowFriendsOptions001
#define EOS_UI_HideFriendsOptions                     EOS_UI_HideFriendsOptions001
#define EOS_UI_GetFriendsVisibleOptions               EOS_UI_GetFriendsVisibleOptions001
#define EOS_UI_SetToggleFriendsKeyOptions             EOS_UI_SetToggleFriendsKeyOptions001
#define EOS_UI_GetToggleFriendsKeyOptions             EOS_UI_GetToggleFriendsKeyOptions001
#define EOS_UI_SetDisplayPreferenceOptions            EOS_UI_SetDisplayPreferenceOptions001
#define EOS_UI_AcknowledgeEventIdOptions              EOS_UI_AcknowledgeEventIdOptions001
#define EOS_UI_AddNotifyDisplaySettingsUpdatedOptions EOS_UI_AddNotifyDisplaySettingsUpdatedOptions001

#include <eos_ui_types1.14.0.h>

#define EOS_UI_SHOWFRIENDS_API_LATEST                     EOS_UI_SHOWFRIENDS_API_001
#define EOS_UI_HIDEFRIENDS_API_LATEST                     EOS_UI_HIDEFRIENDS_API_001
#define EOS_UI_GETFRIENDSVISIBLE_API_LATEST               EOS_UI_GETFRIENDSVISIBLE_API_001
#define EOS_UI_SETTOGGLEFRIENDSKEY_API_LATEST             EOS_UI_SETTOGGLEFRIENDSKEY_API_001
#define EOS_UI_GETTOGGLEFRIENDSKEY_API_LATEST             EOS_UI_GETTOGGLEFRIENDSKEY_API_001
#define EOS_UI_SETDISPLAYPREFERENCE_API_LATEST            EOS_UI_SETDISPLAYPREFERENCE_API_001
#define EOS_UI_ACKNOWLEDGEEVENTID_API_LATEST              EOS_UI_ACKNOWLEDGEEVENTID_API_001
#define EOS_UI_ADDNOTIFYDISPLAYSETTINGSUPDATED_API_LATEST EOS_UI_ADDNOTIFYDISPLAYSETTINGSUPDATED_API_001