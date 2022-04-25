// Copyright Epic Games, Inc. All Rights Reserved.
#pragma once

#include "eos_common.h"

enum { k_iCustomInvitesCallbacks = 27000 };
// next free callback_id: k_iCustomInvitesCallbacks + 3

#define EOS_CustomInvites_SetCustomInviteOptions               EOS_CustomInvites_SetCustomInviteOptions001
#define EOS_CustomInvites_SendCustomInviteOptions              EOS_CustomInvites_SendCustomInviteOptions001
#define EOS_CustomInvites_AddNotifyCustomInviteReceivedOptions EOS_CustomInvites_AddNotifyCustomInviteReceivedOptions001
#define EOS_CustomInvites_AddNotifyCustomInviteAcceptedOptions EOS_CustomInvites_AddNotifyCustomInviteAcceptedOptions001
#define EOS_CustomInvites_FinalizeInviteOptions                EOS_CustomInvites_FinalizeInviteOptions001

#include "eos_custominvites_types1.14.2.h"

#define EOS_CUSTOMINVITES_SETCUSTOMINVITE_API_LATEST               EOS_CUSTOMINVITES_SETCUSTOMINVITE_API_001
#define EOS_CUSTOMINVITES_SENDCUSTOMINVITE_API_LATEST              EOS_CUSTOMINVITES_SENDCUSTOMINVITE_API_001
#define EOS_CUSTOMINVITES_ADDNOTIFYCUSTOMINVITERECEIVED_API_LATEST EOS_CUSTOMINVITES_ADDNOTIFYCUSTOMINVITERECEIVED_API_001
#define EOS_CUSTOMINVITES_ADDNOTIFYCUSTOMINVITEACCEPTED_API_LATEST EOS_CUSTOMINVITES_ADDNOTIFYCUSTOMINVITEACCEPTED_API_001
#define EOS_CUSTOMINVITES_FINALIZEINVITE_API_LATEST                EOS_CUSTOMINVITES_FINALIZEINVITE_API_001