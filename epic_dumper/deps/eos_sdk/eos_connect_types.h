// Copyright Epic Games, Inc. All Rights Reserved.
#pragma once

#include "eos_common.h"

enum { k_iConnectCallbackBase = 3000 };
// next free callback_id: enum { k_iCallback = k_iConnectCallbackBase + 12 };

#define EOS_Connect_Credentials                                        EOS_Connect_Credentials001
#define EOS_Connect_UserLoginInfo                                      EOS_Connect_UserLoginInfo001
#define EOS_Connect_LoginOptions                                       EOS_Connect_LoginOptions002
#define EOS_Connect_CreateUserOptions                                  EOS_Connect_CreateUserOptions001
#define EOS_Connect_LinkAccountOptions                                 EOS_Connect_LinkAccountOptions001
#define EOS_Connect_UnlinkAccountOptions                               EOS_Connect_UnlinkAccountOptions001
#define EOS_Connect_CreateDeviceIdOptions                              EOS_Connect_CreateDeviceIdOptions001
#define EOS_Connect_DeleteDeviceIdOptions                              EOS_Connect_DeleteDeviceIdOptions001
#define EOS_Connect_TransferDeviceIdAccountOptions                     EOS_Connect_TransferDeviceIdAccountOptions001
#define EOS_Connect_QueryExternalAccountMappingsOptions                EOS_Connect_QueryExternalAccountMappingsOptions001
#define EOS_Connect_GetExternalAccountMappingsOptions                  EOS_Connect_GetExternalAccountMappingsOptions001
#define EOS_Connect_QueryProductUserIdMappingsOptions                  EOS_Connect_QueryProductUserIdMappingsOptions002
#define EOS_Connect_GetProductUserIdMappingOptions                     EOS_Connect_GetProductUserIdMappingOptions001
#define EOS_Connect_AddNotifyAuthExpirationOptions                     EOS_Connect_AddNotifyAuthExpirationOptions001
// This hasn't got a ApiVersion member
//define EOS_Connect_AuthExpirationCallbackInfo EOS_Connect_AuthExpirationCallbackInfo001
#define EOS_Connect_AddNotifyLoginStatusChangedOptions                 EOS_Connect_AddNotifyLoginStatusChangedOptions001
#define EOS_Connect_GetProductUserExternalAccountCountOptions          EOS_Connect_GetProductUserExternalAccountCountOptions001
#define EOS_Connect_CopyProductUserExternalAccountByIndexOptions       EOS_Connect_CopyProductUserExternalAccountByIndexOptions001
#define EOS_Connect_CopyProductUserExternalAccountByAccountTypeOptions EOS_Connect_CopyProductUserExternalAccountByAccountTypeOptions001
#define EOS_Connect_CopyProductUserExternalAccountByAccountIdOptions   EOS_Connect_CopyProductUserExternalAccountByAccountIdOptions001
#define EOS_Connect_CopyProductUserInfoOptions                         EOS_Connect_CopyProductUserInfoOptions001
#define EOS_Connect_ExternalAccountInfo                                EOS_Connect_ExternalAccountInfo001
#define EOS_Connect_IdToken                                            EOS_Connect_IdToken001
#define EOS_Connect_CopyIdTokenOptions                                 EOS_Connect_CopyIdTokenOptions001
#define EOS_Connect_VerifyIdTokenOptions                               EOS_Connect_VerifyIdTokenOptions001

#include "eos_connect_types1.14.2.h"
#include "eos_connect_types1.7.0.h"
#include "eos_connect_types1.3.1.h"

#define EOS_CONNECT_CREDENTIALS_API_LATEST                                 EOS_CONNECT_CREDENTIALS_API_001
#define EOS_CONNECT_USERLOGININFO_API_LATEST                               EOS_CONNECT_USERLOGININFO_API_001
#define EOS_CONNECT_LOGIN_API_LATEST                                       EOS_CONNECT_LOGIN_API_002
#define EOS_CONNECT_CREATEUSER_API_LATEST                                  EOS_CONNECT_CREATEUSER_API_001
#define EOS_CONNECT_LINKACCOUNT_API_LATEST                                 EOS_CONNECT_LINKACCOUNT_API_001
#define EOS_CONNECT_UNLINKACCOUNT_API_LATEST                               EOS_CONNECT_UNLINKACCOUNT_API_001
#define EOS_CONNECT_CREATEDEVICEID_API_LATEST                              EOS_CONNECT_CREATEDEVICEID_API_001
#define EOS_CONNECT_DELETEDEVICEID_API_LATEST                              EOS_CONNECT_DELETEDEVICEID_API_001
#define EOS_CONNECT_TRANSFERDEVICEIDACCOUNT_API_LATEST                     EOS_CONNECT_TRANSFERDEVICEIDACCOUNT_API_001
#define EOS_CONNECT_QUERYEXTERNALACCOUNTMAPPINGS_API_LATEST                EOS_CONNECT_QUERYEXTERNALACCOUNTMAPPINGS_API_001
#define EOS_CONNECT_GETEXTERNALACCOUNTMAPPING_API_LATEST                   EOS_CONNECT_GETEXTERNALACCOUNTMAPPING_API_001
#define EOS_CONNECT_QUERYPRODUCTUSERIDMAPPINGS_API_LATEST                  EOS_CONNECT_QUERYPRODUCTUSERIDMAPPINGS_API_002
#define EOS_CONNECT_GETPRODUCTUSERIDMAPPING_API_LATEST                     EOS_CONNECT_GETPRODUCTUSERIDMAPPING_API_001
#define EOS_CONNECT_ADDNOTIFYAUTHEXPIRATION_API_LATEST                     EOS_CONNECT_ADDNOTIFYAUTHEXPIRATION_API_001
#define EOS_CONNECT_ONAUTHEXPIRATIONCALLBACK_API_LATEST                    EOS_CONNECT_ONAUTHEXPIRATIONCALLBACK_API_001
#define EOS_CONNECT_ADDNOTIFYLOGINSTATUSCHANGED_API_LATEST                 EOS_CONNECT_ADDNOTIFYLOGINSTATUSCHANGED_API_001
#define EOS_CONNECT_GETPRODUCTUSEREXTERNALACCOUNTCOUNT_API_LATEST          EOS_CONNECT_GETPRODUCTUSEREXTERNALACCOUNTCOUNT_API_001
#define EOS_CONNECT_COPYPRODUCTUSEREXTERNALACCOUNTBYINDEX_API_LATEST       EOS_CONNECT_COPYPRODUCTUSEREXTERNALACCOUNTBYINDEX_API_001
#define EOS_CONNECT_COPYPRODUCTUSEREXTERNALACCOUNTBYACCOUNTTYPE_API_LATEST EOS_CONNECT_COPYPRODUCTUSEREXTERNALACCOUNTBYACCOUNTTYPE_API_001
#define EOS_CONNECT_COPYPRODUCTUSEREXTERNALACCOUNTBYACCOUNTID_API_LATEST   EOS_CONNECT_COPYPRODUCTUSEREXTERNALACCOUNTBYACCOUNTID_API_001
#define EOS_CONNECT_COPYPRODUCTUSERINFO_API_LATEST                         EOS_CONNECT_COPYPRODUCTUSERINFO_API_001
#define EOS_CONNECT_EXTERNALACCOUNTINFO_API_LATEST                         EOS_CONNECT_EXTERNALACCOUNTINFO_API_001
#define EOS_CONNECT_IDTOKEN_API_LATEST                                     EOS_CONNECT_IDTOKEN_API_001
#define EOS_CONNECT_COPYIDTOKEN_API_LATEST                                 EOS_CONNECT_COPYIDTOKEN_API_001
#define EOS_CONNECT_VERIFYIDTOKEN_API_LATEST                               EOS_CONNECT_VERIFYIDTOKEN_API_001