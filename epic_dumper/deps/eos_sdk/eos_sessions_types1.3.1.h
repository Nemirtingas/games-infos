// Copyright Epic Games, Inc. All Rights Reserved.

#pragma once

#pragma pack(push, 8)

/** The most recent version of the EOS_Sessions_CreateSessionModification API. */
#define EOS_SESSIONS_CREATESESSIONMODIFICATION_API_001 1

/**
 * Input parameters for the EOS_Sessions_CreateSessionModification Function.
 */
EOS_STRUCT(EOS_Sessions_CreateSessionModificationOptions001, (
	/** Version of the API */
	int32_t ApiVersion;
	/** Name of the session to create */
	const char* SessionName;
	/** Bucket id associated with the session */
	const char* BucketId;
	/** Maximum number of players allowed in the session */
	uint32_t MaxPlayers;
	/** Local user id associated with the session */
	EOS_ProductUserId LocalUserId;
));

/** The most recent version of the EOS_Sessions_JoinSession API. */
#define EOS_SESSIONS_JOINSESSION_API_001 1

/**
 * Input parameters for the EOS_Sessions_JoinSession function.
 */
EOS_STRUCT(EOS_Sessions_JoinSessionOptions001, (
	/** Version of the API */
	int32_t ApiVersion;
	/** Name of the session to create after joining session */
	const char* SessionName;
	/** Session handle to join */
	EOS_HSessionDetails SessionHandle;
	/** Local user joining the session */
	EOS_ProductUserId LocalUserId;
));

/** The most recent version of the EOS_SessionDetails_Settings struct. */
#define EOS_SESSIONDETAILS_SETTINGS_API_001 1

/** Common settings associated with a single session */
EOS_STRUCT(EOS_SessionDetails_Settings001, (
	/** Version of the API */
	int32_t ApiVersion;
	/** The main indexed parameter for this session, can be any string (ie "Region:GameMode") */
	const char* BucketId;
	/** Number of total players allowed in the session */
	uint32_t NumPublicConnections;
	/** Are players allowed to join the session while it is in the "in progress" state */
	EOS_Bool bAllowJoinInProgress;
	/** Permission level describing allowed access to the session when joining or searching for the session */
	EOS_EOnlineSessionPermissionLevel PermissionLevel;
));

#pragma pack(pop)
