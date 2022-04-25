// Copyright Epic Games, Inc. All Rights Reserved.

#pragma once

#pragma pack(push, 8)

/** The most recent version of the EOS_Sessions_CreateSessionModification API. */
#define EOS_SESSIONS_CREATESESSIONMODIFICATION_API_002 2

/**
 * Input parameters for the EOS_Sessions_CreateSessionModification Function.
 */
EOS_STRUCT(EOS_Sessions_CreateSessionModificationOptions002, (
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
	/** 
	 * If true than this session will be used as the session associated with presence.
	 * Only one session at a time can have this flag true.
	 */
	EOS_Bool bPresenceEnabled;
));

#pragma pack(pop)
