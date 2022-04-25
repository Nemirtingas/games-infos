// Copyright Epic Games, Inc. All Rights Reserved.
#pragma once

#pragma pack(push, 8)

/** The most recent version of the EOS_Auth_DeletePersistentAuth API. */
#define EOS_AUTH_DELETEPERSISTENTAUTH_API_001 1

/**
 * Input parameters for the EOS_Auth_DeletePersistentAuth Function.
 */
EOS_STRUCT(EOS_Auth_DeletePersistentAuthOptions001, (
	/** Version of the API */
	int32_t ApiVersion;
));

#pragma pack(pop)
