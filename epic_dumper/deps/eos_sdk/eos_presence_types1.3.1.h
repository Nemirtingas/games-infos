// Copyright Epic Games, Inc. All Rights Reserved.

#pragma once

#pragma pack(push, 8)

#define EOS_PRESENCE_INFO_API_001 1
/**
 * All the known presence information for a specific user. This object must be released by calling EOS_Presence_Info_Release.
 *
 * @see EOS_Presence_CopyPresence
 * @see EOS_Presence_Info_Release
 */
EOS_STRUCT(EOS_Presence_Info001, (
	/** API Version for the EOS_Presence_Info struct */
	int32_t ApiVersion;
	/** The status of the user */
	EOS_Presence_EStatus Status;
	/** The account id of the user */
	EOS_EpicAccountId UserId;
	/** The product id that the user is logged in from */
	const char* ProductId;
	/** The version of the product the user is logged in from */
	const char* ProductVersion;
	/** The platform of that the user is logged in from */
	const char* Platform;
	/** The rich-text of the user */
	const char* RichText;
	/** The count of records available */
	int32_t RecordsCount;
	/** The first data record, or NULL if RecordsCount is not at least 1 */
	const EOS_Presence_DataRecord* Records;
));

#define EOS_PRESENCE_COPYPRESENCE_API_001 1

#pragma pack(pop)
