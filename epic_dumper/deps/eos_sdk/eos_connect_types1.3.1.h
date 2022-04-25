// Copyright Epic Games, Inc. All Rights Reserved.
#pragma once

#pragma pack(push, 8)

/** The most recent version of the EOS_Connect_Login API. */
#define EOS_CONNECT_LOGIN_API_001 1

/**
 * Input parameters for the EOS_Connect_Login Function.
 */
EOS_STRUCT(EOS_Connect_LoginOptions001, (
	/** Version of the API */
	int32_t ApiVersion;
	/** Credentials specified for a given login method */
	const EOS_Connect_Credentials* Credentials;
));

#pragma pack(pop)
