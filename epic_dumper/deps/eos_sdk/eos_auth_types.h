// Copyright Epic Games, Inc. All Rights Reserved.
#pragma once

#include "eos_common.h"

enum { k_iAuthCallbackBase = 1000 };
// next free callback_id: k_iAuthCallbackBase + 8

#define EOS_Auth_Token                              EOS_Auth_Token002
#define EOS_Auth_Credentials                        EOS_Auth_Credentials003
#define EOS_Auth_PinGrantInfo                       EOS_Auth_PinGrantInfo002
#define EOS_Auth_AccountFeatureRestrictedInfo       EOS_Auth_AccountFeatureRestrictedInfo001
#define EOS_Auth_LoginOptions                       EOS_Auth_LoginOptions002
#define EOS_Auth_LogoutOptions                      EOS_Auth_LogoutOptions001
#define EOS_Auth_VerifyUserAuthOptions              EOS_Auth_VerifyUserAuthOptions001
#define EOS_Auth_LinkAccountOptions                 EOS_Auth_LinkAccountOptions001
#define EOS_Auth_CopyUserAuthTokenOptions           EOS_Auth_CopyUserAuthTokenOptions001
#define EOS_Auth_AddNotifyLoginStatusChangedOptions EOS_Auth_AddNotifyLoginStatusChangedOptions001
#define EOS_Auth_DeletePersistentAuthOptions        EOS_Auth_DeletePersistentAuthOptions002
#define EOS_Auth_CopyIdTokenOptions                 EOS_Auth_CopyIdTokenOptions001
#define EOS_Auth_IdToken                            EOS_Auth_IdToken001
#define EOS_Auth_QueryIdTokenOptions                EOS_Auth_QueryIdTokenOptions001
#define EOS_Auth_VerifyIdTokenOptions               EOS_Auth_VerifyIdTokenOptions001

#define EOS_Auth_CreateDeviceAuthOptions            EOS_Auth_CreateDeviceAuthOptions001
#define EOS_Auth_DeleteDeviceAuthOptions            EOS_Auth_DeleteDeviceAuthOptions001

#include <eos_auth_types1.14.2.h>
#include <eos_auth_types1.6.0.h>
#include <eos_auth_types1.5.0.h>
#include <eos_auth_types1.3.1.h>

#define EOS_AUTH_TOKEN_API_LATEST                        EOS_AUTH_TOKEN_API_002
#define EOS_AUTH_CREDENTIALS_API_LATEST                  EOS_AUTH_CREDENTIALS_API_003
#define EOS_AUTH_PINGRANTINFO_API_LATEST                 EOS_AUTH_PINGRANTINFO_API_002
#define EOS_AUTH_ACCOUNTFEATURERESTRICTEDINFO_API_LATEST EOS_AUTH_ACCOUNTFEATURERESTRICTEDINFO_API_001
#define EOS_AUTH_LOGIN_API_LATEST                        EOS_AUTH_LOGIN_API_002
#define EOS_AUTH_LOGOUT_API_LATEST                       EOS_AUTH_LOGOUT_API_001
#define EOS_AUTH_VERIFYUSERAUTH_API_LATEST               EOS_AUTH_VERIFYUSERAUTH_API_001
#define EOS_AUTH_LINKACCOUNT_API_LATEST                  EOS_AUTH_LINKACCOUNT_API_001
#define EOS_AUTH_COPYUSERAUTHTOKEN_API_LATEST            EOS_AUTH_COPYUSERAUTHTOKEN_API_001
#define EOS_AUTH_ADDNOTIFYLOGINSTATUSCHANGED_API_LATEST  EOS_AUTH_ADDNOTIFYLOGINSTATUSCHANGED_API_001
#define EOS_AUTH_DELETEPERSISTENTAUTH_API_LATEST         EOS_AUTH_DELETEPERSISTENTAUTH_API_002
#define EOS_AUTH_COPYIDTOKEN_API_LATEST                  EOS_AUTH_COPYIDTOKEN_API_001
#define EOS_AUTH_IDTOKEN_API_LATEST                      EOS_AUTH_IDTOKEN_API_001
#define EOS_AUTH_QUERYIDTOKEN_API_LATEST                 EOS_AUTH_QUERYIDTOKEN_API_001
#define EOS_AUTH_VERIFYIDTOKEN_API_LATEST                EOS_AUTH_VERIFYIDTOKEN_API_001

#define EOS_AUTH_CREATEDEVICEAUTH_API_LATEST EOS_AUTH_CREATEDEVICEAUTH_API_001 // Deprecated
#define EOS_AUTH_DELETEDEVICEAUTH_API_LATEST EOS_AUTH_DELETEDEVICEAUTH_API_001 // Deprecated