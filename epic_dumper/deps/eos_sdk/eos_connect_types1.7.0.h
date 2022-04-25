// Copyright Epic Games, Inc. All Rights Reserved.
#pragma once

#pragma pack(push, 8)

/** The most recent version of the EOS_Connect_QueryProductUserIdMappings API. */
#define EOS_CONNECT_QUERYPRODUCTUSERIDMAPPINGS_API_001 1
        
/**
 * Input parameters for the EOS_Connect_QueryProductUserIdMappings Function.
 */
EOS_STRUCT(EOS_Connect_QueryProductUserIdMappingsOptions001, (
	/** Version of the API */
	int32_t ApiVersion;
	/** Existing logged in user that is querying account mappings */
	EOS_ProductUserId LocalUserId;
	/** Deprecated - all external mappings are included in this call, it is no longer necessary to specify this value */
	EOS_EExternalAccountType AccountIdType_DEPRECATED;
	/** An array of product user ids to query for the given external account representation */
	EOS_ProductUserId* ProductUserIds;
	/** Number of account ids to query */
	uint32_t ProductUserIdCount;
));

#pragma pack(pop)
