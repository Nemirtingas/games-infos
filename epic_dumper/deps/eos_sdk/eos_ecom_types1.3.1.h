// Copyright Epic Games, Inc. All Rights Reserved.

#pragma once

#pragma pack(push, 8)

/** The most recent version of the EOS_Ecom_CopyEntitlementById API. */
#define EOS_ECOM_COPYENTITLEMENTBYID_API_001 1

/**
 * Input parameters for the EOS_Ecom_CopyEntitlementById Function.
 */
EOS_STRUCT(EOS_Ecom_CopyEntitlementByIdOptions001, (
	/** Version of the API */
	int32_t ApiVersion;
	/** Local user whose entitlement is being copied */
	EOS_EpicAccountId LocalUserId;
	/** Id of the entitlement to retrieve from the cache */
	EOS_Ecom_EntitlementId EntitlementId;
));

#pragma pack(pop)
