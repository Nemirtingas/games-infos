// Copyright Epic Games, Inc. All Rights Reserved.
#pragma once

#include "eos_common.h"

enum { k_iUserInfoCallbackBase = 16000 };
// next free callback_id: k_iUserInfoCallbackBase + 4

#define EOS_UserInfo_QueryUserInfoOptions                     EOS_UserInfo_QueryUserInfoOptions001
#define EOS_UserInfo_QueryUserInfoByDisplayNameOptions        EOS_UserInfo_QueryUserInfoByDisplayNameOptions001
#define EOS_UserInfo_QueryUserInfoByExternalAccountOptions    EOS_UserInfo_QueryUserInfoByExternalAccountOptions001
#define EOS_UserInfo                                          EOS_UserInfo002
#define EOS_UserInfo_CopyUserInfoOptions                      EOS_UserInfo_CopyUserInfoOptions001
#define EOS_UserInfo_ExternalUserInfo                         EOS_UserInfo_ExternalUserInfo001
#define EOS_UserInfo_GetExternalUserInfoCountOptions          EOS_UserInfo_GetExternalUserInfoCountOptions001
#define EOS_UserInfo_CopyExternalUserInfoByIndexOptions       EOS_UserInfo_CopyExternalUserInfoByIndexOptions001
#define EOS_UserInfo_CopyExternalUserInfoByAccountTypeOptions EOS_UserInfo_CopyExternalUserInfoByAccountTypeOptions001
#define EOS_UserInfo_CopyExternalUserInfoByAccountIdOptions   EOS_UserInfo_CopyExternalUserInfoByAccountIdOptions001

#include <eos_userinfo_types1.14.0.h>
#include <eos_userinfo_types1.3.1.h>

#define EOS_USERINFO_QUERYUSERINFO_API_LATEST                     EOS_USERINFO_QUERYUSERINFO_API_001
#define EOS_USERINFO_QUERYUSERINFOBYDISPLAYNAME_API_LATEST        EOS_USERINFO_QUERYUSERINFOBYDISPLAYNAME_API_001
#define EOS_USERINFO_QUERYUSERINFOBYEXTERNALACCOUNT_API_LATEST    EOS_USERINFO_QUERYUSERINFOBYEXTERNALACCOUNT_API_001
#define EOS_USERINFO_COPYUSERINFO_API_LATEST                      EOS_USERINFO_COPYUSERINFO_API_002
#define EOS_USERINFO_EXTERNALUSERINFO_API_LATEST                  EOS_USERINFO_EXTERNALUSERINFO_API_001
#define EOS_USERINFO_GETEXTERNALUSERINFOCOUNT_API_LATEST          EOS_USERINFO_GETEXTERNALUSERINFOCOUNT_API_001
#define EOS_USERINFO_COPYEXTERNALUSERINFOBYINDEX_API_LATEST       EOS_USERINFO_COPYEXTERNALUSERINFOBYINDEX_API_001
#define EOS_USERINFO_COPYEXTERNALUSERINFOBYACCOUNTTYPE_API_LATEST EOS_USERINFO_COPYEXTERNALUSERINFOBYACCOUNTTYPE_API_001
#define EOS_USERINFO_COPYEXTERNALUSERINFOBYACCOUNTID_API_LATEST   EOS_USERINFO_COPYEXTERNALUSERINFOBYACCOUNTID_API_001