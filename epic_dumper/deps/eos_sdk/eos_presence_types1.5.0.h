// Copyright Epic Games, Inc. All Rights Reserved.

#pragma once

#pragma pack(push, 8)

#define EOS_PRESENCE_ADDNOTIFYJOINGAMEACCEPTED_API_001 1

/**
 * Output parameters for the EOS_Presence_OnJoinGameAcceptedCallback Function.
 */
EOS_STRUCT(EOS_Presence_JoinGameAcceptedCallbackInfo001, (
	/** Context that was passed into EOS_Presence_AddNotifyJoinGameAccepted */
	void* ClientData;
	/** 
	 * The Join Info custom game-data string to use to join the target user.
	 * Set to a null pointer to delete the value.
	 */
	const char* JoinInfo;
	/** User that accepted the invite */
	EOS_EpicAccountId LocalUserId;
	/** Target user that sent the invite */
	EOS_EpicAccountId TargetUserId;
));

#pragma pack(pop)
