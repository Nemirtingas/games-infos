// Copyright 1998-2019 Epic Games, Inc. All Rights Reserved.

#pragma once

#pragma pack(push, 8)

/** The most recent version of the EOS_Ecom_Entitlement struct. */
#define EOS_ECOM_ENTITLEMENT_API_001 1

/**
 * Contains information about a single entitlement associated with an account. These structures are created by EOS_Ecom_CopyEntitlementByIndex and EOS_Ecom_CopyEntitlementById and must be passed to EOS_Ecom_Entitlement_Release
 */
EOS_STRUCT(EOS_Ecom_Entitlement001, (
	/** Version of the API */
	int32_t ApiVersion;
	/** Id of the entitlement */
	EOS_Ecom_EntitlementId Id;
));

/** The most recent version of the EOS_Ecom_QueryOwnership API. */
#define EOS_ECOM_QUERYOWNERSHIP_API_001 1

/**
 * Input parameters for the EOS_Ecom_QueryOwnership Function.
 */
EOS_STRUCT(EOS_Ecom_QueryOwnershipOptions001, (
	/** Version of the API */
	int32_t ApiVersion;
	/** Local user to query whose ownership is to be queried */
	EOS_AccountId LocalUserId;
	/** List of catalog item ids to check for ownership, matching in number to the CatalogItemIdCount */
	EOS_Ecom_CatalogItemId* CatalogItemIds;
	/** Number of catalog item ids to query */
	uint32_t CatalogItemIdCount;
));

/** The most recent version of the EOS_Ecom_QueryOwnershipToken API. */
#define EOS_ECOM_QUERYOWNERSHIPTOKEN_API_001 1

/**
 * Input parameters for the EOS_Ecom_QueryOwnershipToken Function.
 */
EOS_STRUCT(EOS_Ecom_QueryOwnershipTokenOptions001, (
	/** Version of the API */
	int32_t ApiVersion;
	/** Local user to query whose ownership token is to be queried */
	EOS_AccountId LocalUserId;
	/** List of catalog item ids to check for ownership, matching in number to the CatalogItemIdCount */
	EOS_Ecom_CatalogItemId* CatalogItemIds;
	/** Number of catalog item ids to query */
	uint32_t CatalogItemIdCount;
));

/** The most recent version of the EOS_Ecom_QueryEntitlement API. */
#define EOS_ECOM_QUERYENTITLEMENT_API_001 1

/**
 * The maximum number of entitlements that may be queried in a single pass
 */
#define EOS_ECOM_QUERYENTITLEMENT_MAX_ENTITLEMENT_IDS 32

/**
 * Input parameters for the EOS_Ecom_QueryEntitlement Function.
 */
EOS_STRUCT(EOS_Ecom_QueryEntitlementOptions001, (
	/** Version of the API */
	int32_t ApiVersion;
	/** Local user to query whose entitlement is to be queried */
	EOS_AccountId LocalUserId;
	/** List of entitlement ids, matching in number to the EntitlementIdCount */
	EOS_Ecom_EntitlementId* EntitlementIds;
	/** Number of entitlement ids to query */
	uint32_t EntitlementIdCount;
));

#pragma pack(pop)
