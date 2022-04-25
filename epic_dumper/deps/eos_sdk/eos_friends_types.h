// Copyright Epic Games, Inc. All Rights Reserved.
#pragma once

#include "eos_common.h"

enum { k_iFriendsCallbackBase = 5000 };
// next free callback_id: k_iFriendsCallbackBase + 6

#define EOS_Friends_QueryFriendsOptions           EOS_Friends_QueryFriendsOptions001
#define EOS_Friends_SendInviteOptions             EOS_Friends_SendInviteOptions001
#define EOS_Friends_AcceptInviteOptions           EOS_Friends_AcceptInviteOptions001
#define EOS_Friends_RejectInviteOptions           EOS_Friends_RejectInviteOptions001
#define EOS_Friends_DeleteFriendOptions           EOS_Friends_DeleteFriendOptions001
#define EOS_Friends_GetFriendsCountOptions        EOS_Friends_GetFriendsCountOptions001
#define EOS_Friends_GetFriendAtIndexOptions       EOS_Friends_GetFriendAtIndexOptions001
#define EOS_Friends_GetStatusOptions              EOS_Friends_GetStatusOptions001
#define EOS_Friends_AddNotifyFriendsUpdateOptions EOS_Friends_AddNotifyFriendsUpdateOptions001

#include "eos_friends_types1.14.0.h"

#define EOS_FRIENDS_QUERYFRIENDS_API_LATEST           EOS_FRIENDS_QUERYFRIENDS_API_001
#define EOS_FRIENDS_SENDINVITE_API_LATEST             EOS_FRIENDS_SENDINVITE_API_001
#define EOS_FRIENDS_ACCEPTINVITE_API_LATEST           EOS_FRIENDS_ACCEPTINVITE_API_001
#define EOS_FRIENDS_REJECTINVITE_API_LATEST           EOS_FRIENDS_REJECTINVITE_API_001
#define EOS_FRIENDS_DELETEFRIEND_API_LATEST           EOS_FRIENDS_DELETEFRIEND_API_001
#define EOS_FRIENDS_GETFRIENDSCOUNT_API_LATEST        EOS_FRIENDS_GETFRIENDSCOUNT_API_001
#define EOS_FRIENDS_GETFRIENDATINDEX_API_LATEST       EOS_FRIENDS_GETFRIENDATINDEX_API_001
#define EOS_FRIENDS_GETSTATUS_API_LATEST              EOS_FRIENDS_GETFRIENDATINDEX_API_001
#define EOS_FRIENDS_ADDNOTIFYFRIENDSUPDATE_API_LATEST EOS_FRIENDS_ADDNOTIFYFRIENDSUPDATE_API_001
