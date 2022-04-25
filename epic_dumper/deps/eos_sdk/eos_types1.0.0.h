// Copyright Epic Games, Inc. All Rights Reserved.
#pragma once

#pragma pack(push, 8)

#define EOS_PLATFORM_OPTIONS_API_001 1

/** Platform options for EOS_Platform_Create. */
EOS_STRUCT(EOS_Platform_Options001, (
	/** API version of EOS_Platform_Create. */
	int32_t ApiVersion;
	/** A reserved field that should always be nulled. */
	void* Reserved;
	/** The product id for the running application, found on the dev portal */
	const char* ProductId;
	/** The sandbox id for the running application, found on the dev portal */
	const char* SandboxId;
	/** Set of service permissions associated with the running application */
	EOS_Platform_ClientCredentials ClientCredentials;
	/** Is this running as a server */
	EOS_Bool bIsServer;
));

#pragma pack(pop)