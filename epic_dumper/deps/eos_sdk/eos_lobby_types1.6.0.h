// Copyright Epic Games, Inc. All Rights Reserved.
#pragma once

#pragma pack(push, 8)

/** The most recent version of the EOS_Lobby_CreateLobby API. */
#define EOS_LOBBY_CREATELOBBY_API_001 1

/**
 * Input parameters for the EOS_Lobby_CreateLobby Function.
 */
EOS_STRUCT(EOS_Lobby_CreateLobbyOptions001, (
	/** Version of the API */
	int32_t ApiVersion;
	/** Local user creating the lobby */
	EOS_ProductUserId LocalUserId;
	/** Max members allowed in the lobby */
	uint32_t MaxLobbyMembers;
	/** The initial permission level of the lobby */
	EOS_ELobbyPermissionLevel PermissionLevel;
));

/** The most recent version of the EOS_Lobby_JoinLobby API. */
#define EOS_LOBBY_JOINLOBBY_API_001 1

/**
 * Input parameters for the EOS_Lobby_JoinLobby Function.
 */
EOS_STRUCT(EOS_Lobby_JoinLobbyOptions001, (
	/** Version of the API */
	int32_t ApiVersion;
	/** Lobby handle to join */
	EOS_HLobbyDetails LobbyDetailsHandle;
	/** Local user joining the lobby */
	EOS_ProductUserId LocalUserId;
));

#pragma pack(pop)
