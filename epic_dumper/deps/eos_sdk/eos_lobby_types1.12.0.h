// Copyright Epic Games, Inc. All Rights Reserved.
#pragma once

#pragma pack(push, 8)

/** The most recent version of the EOS_Lobby_CreateLobby API. */
#define EOS_LOBBY_CREATELOBBY_API_005 5

/**
 * Input parameters for the EOS_Lobby_CreateLobby function.
 */
EOS_STRUCT(EOS_Lobby_CreateLobbyOptions005, (
	/** API Version: Set this to EOS_LOBBY_CREATELOBBY_API_LATEST. */
	int32_t ApiVersion;
	/** The Product User ID of the local user creating the lobby; this user will automatically join the lobby as its owner */
	EOS_ProductUserId LocalUserId;
	/** The maximum number of users who can be in the lobby at a time */
	uint32_t MaxLobbyMembers;
	/** The initial permission level of the lobby */
	EOS_ELobbyPermissionLevel PermissionLevel;
	/** If true, this lobby will be associated with presence information. A user's presence can only be associated with one lobby at a time.
	 * This affects the ability of the Social Overlay to show game related actions to take in the user's social graph.
	 *
	 * @note The Social Overlay can handle only one of the following three options at a time:
	 * * using the bPresenceEnabled flags within the Sessions interface
	 * * using the bPresenceEnabled flags within the Lobby interface
	 * * using EOS_PresenceModification_SetJoinInfo
	 *
	 * @see EOS_PresenceModification_SetJoinInfoOptions
	 * @see EOS_Lobby_JoinLobbyOptions
	 * @see EOS_Sessions_CreateSessionModificationOptions
	 * @see EOS_Sessions_JoinSessionOptions
	 */
	EOS_Bool bPresenceEnabled;
	/** Are members of the lobby allowed to invite others */
	EOS_Bool bAllowInvites;
	/** Bucket ID associated with the lobby */
	const char* BucketId;
	/**
	 * Is host migration allowed (will the lobby stay open if the original host leaves?)
	 * NOTE: EOS_Lobby_PromoteMember is still allowed regardless of this setting
	 */
	EOS_Bool bDisableHostMigration;
));

/** The most recent version of the EOS_Lobby_JoinLobby API. */
#define EOS_LOBBY_JOINLOBBY_API_002 2

/**
 * Input parameters for the EOS_Lobby_JoinLobby function.
 */
EOS_STRUCT(EOS_Lobby_JoinLobbyOptions002, (
	/** API Version: Set this to EOS_LOBBY_JOINLOBBY_API_LATEST. */
	int32_t ApiVersion;
	/** The handle of the lobby to join */
	EOS_HLobbyDetails LobbyDetailsHandle;
	/** The Product User ID of the local user joining the lobby */
	EOS_ProductUserId LocalUserId;
	/** If true, this lobby will be associated with the user's presence information. A user can only associate one lobby at a time with their presence information.
	 * This affects the ability of the Social Overlay to show game related actions to take in the user's social graph.
	 *
	 * @note The Social Overlay can handle only one of the following three options at a time:
	 * * using the bPresenceEnabled flags within the Sessions interface
	 * * using the bPresenceEnabled flags within the Lobby interface
	 * * using EOS_PresenceModification_SetJoinInfo
	 *
	 * @see EOS_PresenceModification_SetJoinInfoOptions
	 * @see EOS_Lobby_CreateLobbyOptions
	 * @see EOS_Lobby_JoinLobbyOptions
	 * @see EOS_Sessions_CreateSessionModificationOptions
	 * @see EOS_Sessions_JoinSessionOptions
	 */
	EOS_Bool bPresenceEnabled;
));

#pragma pack(pop)
