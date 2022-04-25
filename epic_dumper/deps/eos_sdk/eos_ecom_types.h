// Copyright Epic Games, Inc. All Rights Reserved.

#pragma once

#include "eos_common.h"

enum { k_iEcomCallbackBase = 4000 };
// next free callback_id: k_iEcomCallbackBase + 6

#define EOS_Ecom_Entitlement                               EOS_Ecom_Entitlement002
#define EOS_Ecom_ItemOwnership                             EOS_Ecom_ItemOwnership001
#define EOS_Ecom_CatalogItem                               EOS_Ecom_CatalogItem001
#define EOS_Ecom_CatalogOffer                              EOS_Ecom_CatalogOffer004
#define EOS_Ecom_KeyImageInfo                              EOS_Ecom_KeyImageInfo001
#define EOS_Ecom_CatalogRelease                            EOS_Ecom_CatalogRelease001
#define EOS_Ecom_CheckoutEntry                             EOS_Ecom_CheckoutEntry001
#define EOS_Ecom_QueryOwnershipOptions                     EOS_Ecom_QueryOwnershipOptions002
#define EOS_Ecom_QueryOwnershipTokenOptions                EOS_Ecom_QueryOwnershipTokenOptions002
#define EOS_Ecom_QueryEntitlementsOptions                  EOS_Ecom_QueryEntitlementsOptions002
#define EOS_Ecom_QueryOffersOptions                        EOS_Ecom_QueryOffersOptions001
#define EOS_Ecom_CheckoutOptions                           EOS_Ecom_CheckoutOptions001
#define EOS_Ecom_RedeemEntitlementsOptions                 EOS_Ecom_RedeemEntitlementsOptions001
#define EOS_Ecom_GetEntitlementsCountOptions               EOS_Ecom_GetEntitlementsCountOptions001
#define EOS_Ecom_GetEntitlementsByNameCountOptions         EOS_Ecom_GetEntitlementsByNameCountOptions001
#define EOS_Ecom_CopyEntitlementByIndexOptions             EOS_Ecom_CopyEntitlementByIndexOptions001
#define EOS_Ecom_CopyEntitlementByNameAndIndexOptions      EOS_Ecom_CopyEntitlementByNameAndIndexOptions001
#define EOS_Ecom_CopyEntitlementByIdOptions                EOS_Ecom_CopyEntitlementByIdOptions002
#define EOS_Ecom_GetOfferCountOptions                      EOS_Ecom_GetOfferCountOptions001
#define EOS_Ecom_CopyOfferByIndexOptions                   EOS_Ecom_CopyOfferByIndexOptions001
#define EOS_Ecom_CopyOfferByIdOptions                      EOS_Ecom_CopyOfferByIdOptions001
#define EOS_Ecom_GetOfferItemCountOptions                  EOS_Ecom_GetOfferItemCountOptions001
#define EOS_Ecom_CopyOfferItemByIndexOptions               EOS_Ecom_CopyOfferItemByIndexOptions001
#define EOS_Ecom_CopyItemByIdOptions                       EOS_Ecom_CopyItemByIdOptions001
#define EOS_Ecom_GetOfferImageInfoCountOptions             EOS_Ecom_GetOfferImageInfoCountOptions001
#define EOS_Ecom_CopyOfferImageInfoByIndexOptions          EOS_Ecom_CopyOfferImageInfoByIndexOptions001
#define EOS_Ecom_GetItemImageInfoCountOptions              EOS_Ecom_GetItemImageInfoCountOptions001
#define EOS_Ecom_CopyItemImageInfoByIndexOptions           EOS_Ecom_CopyItemImageInfoByIndexOptions001
#define EOS_Ecom_GetItemReleaseCountOptions                EOS_Ecom_GetItemReleaseCountOptions001
#define EOS_Ecom_CopyItemReleaseByIndexOptions             EOS_Ecom_CopyItemReleaseByIndexOptions001
#define EOS_Ecom_GetTransactionCountOptions                EOS_Ecom_GetTransactionCountOptions001
#define EOS_Ecom_CopyTransactionByIndexOptions             EOS_Ecom_CopyTransactionByIndexOptions001
#define EOS_Ecom_CopyTransactionByIdOptions                EOS_Ecom_CopyTransactionByIdOptions001
#define EOS_Ecom_Transaction_GetEntitlementsCountOptions   EOS_Ecom_Transaction_GetEntitlementsCountOptions001
#define EOS_Ecom_Transaction_CopyEntitlementByIndexOptions EOS_Ecom_Transaction_CopyEntitlementByIndexOptions001

#include "eos_ecom_types1.14.0.h"
#include "eos_ecom_types1.12.0.h"
#include "eos_ecom_types1.10.0.h"
#include "eos_ecom_types1.3.1.h"
#include "eos_ecom_types1.1.0.h"

#define EOS_ECOM_ENTITLEMENT_API_LATEST                        EOS_ECOM_ENTITLEMENT_API_002
#define EOS_ECOM_ITEMOWNERSHIP_API_LATEST                      EOS_ECOM_ITEMOWNERSHIP_API_001
#define EOS_ECOM_CATALOGITEM_API_LATEST                        EOS_ECOM_CATALOGITEM_API_001
#define EOS_ECOM_CATALOGOFFER_API_LATEST                       EOS_ECOM_CATALOGOFFER_API_004
#define EOS_ECOM_KEYIMAGEINFO_API_LATEST                       EOS_ECOM_KEYIMAGEINFO_API_001
#define EOS_ECOM_CATALOGRELEASE_API_LATEST                     EOS_ECOM_CATALOGRELEASE_API_001
#define EOS_ECOM_CHECKOUTENTRY_API_LATEST                      EOS_ECOM_CHECKOUTENTRY_API_001
#define EOS_ECOM_QUERYOWNERSHIP_API_LATEST                     EOS_ECOM_QUERYOWNERSHIP_API_002
#define EOS_ECOM_QUERYOWNERSHIPTOKEN_API_LATEST                EOS_ECOM_QUERYOWNERSHIPTOKEN_API_002
#define EOS_ECOM_QUERYENTITLEMENTS_API_LATEST                  EOS_ECOM_QUERYENTITLEMENTS_API_002
#define EOS_ECOM_QUERYOFFERS_API_LATEST                        EOS_ECOM_QUERYOFFERS_API_001
#define EOS_ECOM_CHECKOUT_API_LATEST                           EOS_ECOM_CHECKOUT_API_001
#define EOS_ECOM_REDEEMENTITLEMENTS_API_LATEST                 EOS_ECOM_REDEEMENTITLEMENTS_API_001
#define EOS_ECOM_GETENTITLEMENTSCOUNT_API_LATEST               EOS_ECOM_GETENTITLEMENTSCOUNT_API_001
#define EOS_ECOM_GETENTITLEMENTSBYNAMECOUNT_API_LATEST         EOS_ECOM_GETENTITLEMENTSBYNAMECOUNT_API_001
#define EOS_ECOM_COPYENTITLEMENTBYINDEX_API_LATEST             EOS_ECOM_COPYENTITLEMENTBYINDEX_API_001
#define EOS_ECOM_COPYENTITLEMENTBYNAMEANDINDEX_API_LATEST      EOS_ECOM_COPYENTITLEMENTBYNAMEANDINDEX_API_001
#define EOS_ECOM_COPYENTITLEMENTBYID_API_LATEST                EOS_ECOM_COPYENTITLEMENTBYID_API_002
#define EOS_ECOM_GETOFFERCOUNT_API_LATEST                      EOS_ECOM_GETOFFERCOUNT_API_001
#define EOS_ECOM_COPYOFFERBYINDEX_API_LATEST                   EOS_ECOM_COPYOFFERBYINDEX_API_002
#define EOS_ECOM_COPYOFFERBYID_API_LATEST                      EOS_ECOM_COPYOFFERBYID_API_002
#define EOS_ECOM_GETOFFERITEMCOUNT_API_LATEST                  EOS_ECOM_GETOFFERITEMCOUNT_API_001
#define EOS_ECOM_COPYOFFERITEMBYINDEX_API_LATEST               EOS_ECOM_COPYOFFERITEMBYINDEX_API_001
#define EOS_ECOM_COPYITEMBYID_API_LATEST                       EOS_ECOM_COPYITEMBYID_API_001
#define EOS_ECOM_GETOFFERIMAGEINFOCOUNT_API_LATEST             EOS_ECOM_GETOFFERIMAGEINFOCOUNT_API_001
#define EOS_ECOM_COPYOFFERIMAGEINFOBYINDEX_API_LATEST          EOS_ECOM_COPYOFFERIMAGEINFOBYINDEX_API_001
#define EOS_ECOM_GETITEMIMAGEINFOCOUNT_API_LATEST              EOS_ECOM_GETITEMIMAGEINFOCOUNT_API_001
#define EOS_ECOM_COPYITEMIMAGEINFOBYINDEX_API_LATEST           EOS_ECOM_COPYITEMIMAGEINFOBYINDEX_API_001
#define EOS_ECOM_GETITEMRELEASECOUNT_API_LATEST                EOS_ECOM_GETITEMRELEASECOUNT_API_001
#define EOS_ECOM_COPYITEMRELEASEBYINDEX_API_LATEST             EOS_ECOM_COPYITEMRELEASEBYINDEX_API_001
#define EOS_ECOM_GETTRANSACTIONCOUNT_API_LATEST                EOS_ECOM_GETTRANSACTIONCOUNT_API_001
#define EOS_ECOM_COPYTRANSACTIONBYINDEX_API_LATEST             EOS_ECOM_COPYTRANSACTIONBYINDEX_API_001
#define EOS_ECOM_COPYTRANSACTIONBYID_API_LATEST                EOS_ECOM_COPYTRANSACTIONBYID_API_001
#define EOS_ECOM_TRANSACTION_GETENTITLEMENTSCOUNT_API_LATEST   EOS_ECOM_TRANSACTION_GETENTITLEMENTSCOUNT_API_001
#define EOS_ECOM_TRANSACTION_COPYENTITLEMENTBYINDEX_API_LATEST EOS_ECOM_TRANSACTION_COPYENTITLEMENTBYINDEX_API_001
