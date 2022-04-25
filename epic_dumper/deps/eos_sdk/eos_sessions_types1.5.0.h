// Copyright Epic Games, Inc. All Rights Reserved.

#pragma once

#pragma pack(push, 8)

/** The most recent version of the EOS_SessionSearch_Find API. */
#define EOS_SESSIONSEARCH_FIND_API_001 1

/**
 * Input parameters for the EOS_SessionSearch_Find Function.
 */
EOS_STRUCT(EOS_SessionSearch_FindOptions001, (
	/** Version of the API */
	int32_t ApiVersion;
));

EOS_STRUCT(EOS_SessionSearch_FindCallbackInfo001, (
	/** Result code for the operation. EOS_Success is returned for a successful operation, otherwise one of the error codes is returned. See eos_common.h */
	EOS_EResult ResultCode;
	/** Context that was passed into EOS_SessionSearch_Find */
	void* ClientData;
));

#pragma pack(pop)
