// Copyright 1998-2019 Epic Games, Inc. All Rights Reserved.
#pragma once

#pragma pack(push, 8)

/** The most recent version of the EOS_Auth_Token struct. */
#define EOS_AUTH_TOKEN_API_001 1

/** A structure that contains an auth token. These structures are created by EOS_Auth_CopyUserAuthToken and must be passed to EOS_Auth_Token_Release. */
EOS_STRUCT(EOS_Auth_Token001, (
	/** Version of the API */
	int32_t ApiVersion;
	/** Name of the app related to the client id involved with this token */
	const char* App;
	/** Client id that requested this token */
	const char* ClientId;
	/** User account associated with this auth token */
	EOS_AccountId AccountId;
	/** Signature of the access token */
	const char* AccessToken;
	/** Time before the access token expires, in seconds, relative to the call to EOS_Auth_CopyUserAuthToken */
	double ExpiresIn;
	/** Absolute time in UTC before the access token expires, in ISO 8601 format */
	const char* ExpiresAt;
	/** Type of auth token */
	EOS_EAuthTokenType AuthType;
));

/** The most recent version of the EOS_Auth_Credentials struct. */
#define EOS_AUTH_CREDENTIALS_API_001 1

using EOS_Auth_Credentials001 = EOS_Auth_Credentials002;

/** The most recent version of the EOS_Auth_PinGrantInfo struct. */
#define EOS_AUTH_PINGRANTINFO_API_001 1

/**
 * These structures are returned by EOS_Auth_LoginCallbackInfo.  
 * The data inside should be exposed to the user for entry on a secondary device
 * All data must be copied out before the completion of this callback
 */
EOS_STRUCT(EOS_Auth_PinGrantInfo001, (
	/** Version of the API */
	int32_t ApiVersion;
	/** Code the user must input on an external device to activate the login */
	const char* UserCode;
	/** URI where the user must login */
	const char* VerificationURI;
	/** Time the user has, in seconds, to complete the process or else timeout */
	int32_t ExpiresIn;
));

/** The most recent version of the EOS_Auth_Login API. */
#define EOS_AUTH_LOGIN_API_001 1

/**
 * Input parameters for the EOS_Auth_Login Function.
 */
EOS_STRUCT(EOS_Auth_LoginOptions001, (
	/** Version of the API */
	int32_t ApiVersion;
	/** Credentials specified for a given login method */
	EOS_Auth_Credentials* Credentials;
));

/** The most recent version of the EOS_Auth_CreateDeviceAuth API. */
#define EOS_AUTH_CREATEDEVICEAUTH_API_001 1

/**
 * Input parameters for the EOS_Auth_CreateDeviceAuth Function.
 */
EOS_STRUCT(EOS_Auth_CreateDeviceAuthOptions001, (
	/** Version of the API */
	int32_t ApiVersion;
	/** Account ID of the local player to create device auth for */
	EOS_AccountId LocalUserId;
	/** Device details related to the create device request, details stored on the backend */
	EOS_DeviceInfo* DeviceInfo;
));

/** The most recent version of the EOS_Auth_DeleteDeviceAuth API. */
#define EOS_AUTH_DELETEDEVICEAUTH_API_001 1

/**
 * Input parameters for the EOS_Auth_DeleteDeviceAuth Function.
 */
EOS_STRUCT(EOS_Auth_DeleteDeviceAuthOptions001, (
	/** Version of the API */
	int32_t ApiVersion;
	/** Account ID of the local player whose device auth details will be deleted */
	EOS_AccountId LocalUserId;
	/** Device auth credentials to be deleted */
	EOS_Auth_Credentials* Credentials;
));

#pragma pack(pop)
