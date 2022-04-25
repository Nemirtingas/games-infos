// Copyright Epic Games, Inc. All Rights Reserved.
#pragma once

#pragma pack(push, 8)

/** The most recent version of the EOS_Auth_Credentials struct. */
#define EOS_AUTH_CREDENTIALS_API_002 2

/**
 * A structure that contains login credentials. What is required is dependent on the type of login being initiated.
 * 
 * This is part of the input structure EOS_Auth_LoginOptions and related to device auth
 *
 * EOS_LCT_Password - (id/token) required with email/password
 * EOS_LCT_ExchangeCode - (token) exchange code
 * EOS_LCT_PersistentAuth - (Desktop & Mobile: N/A, Console: token) login using previously stored persistent access credentials for the local user of the device
 * EOS_LCT_DeviceCode - (N/A) initiate a pin grant completed via an external device
 *
 * @see EOS_ELoginCredentialType
 * @see EOS_Auth_Login
 * @see EOS_Auth_DeletePersistentAuthOptions
 */ 
EOS_STRUCT(EOS_Auth_Credentials002, (
	/** Version of the API */
	int32_t ApiVersion;
	/** Id of the user logging in, based on EOS_ELoginCredentialType */
	const char* Id;
	/** Credentials or token related to the user logging in */
	const char* Token;
	/** Type of login. Needed to identify the auth method to use */
	EOS_ELoginCredentialType Type;
));

#pragma pack(pop)
