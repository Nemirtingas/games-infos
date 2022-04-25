// Copyright Epic Games, Inc. All Rights Reserved.
#pragma once

#pragma pack(push, 8)

 /** The most recent version of the EOS_UserInfo_CopyUserInfo API. */
#define EOS_USERINFO_COPYUSERINFO_API_001 1

/** A structure that contains the user information. These structures are created by EOS_UserInfo_CopyUserInfo and must be passed to EOS_UserInfo_Release. */
EOS_STRUCT(EOS_UserInfo001, (
	/** Version of the structure. This value is matched to the API version of EOS_UserInfo_CopyUserInfo. */
	int32_t ApiVersion;
	/** The account id of the user */
	EOS_EpicAccountId UserId;
	/** The name of the owner's country. Only available when querying the local user. */
	const char* Country;
	/** The display name. */
	const char* DisplayName;
	/** The ISO 639 language code for the user's preferred language. Defaults to en. Only available when querying the local user. */
	const char* PreferredLanguage;
));

#pragma pack(pop)
