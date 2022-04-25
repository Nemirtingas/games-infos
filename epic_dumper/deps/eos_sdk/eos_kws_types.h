// Copyright Epic Games, Inc. All Rights Reserved.
#pragma once

#include "eos_common.h"

enum { k_iKwsCallbackBase = 20000 };
// next free callback_id: k_iKwsCallbackBase + 6

#define EOS_KWS_PermissionStatus                          EOS_KWS_PermissionStatus001
#define EOS_KWS_QueryAgeGateOptions                       EOS_KWS_QueryAgeGateOptions001
#define EOS_KWS_CreateUserOptions                         EOS_KWS_CreateUserOptions001
#define EOS_KWS_QueryPermissionsOptions                   EOS_KWS_QueryPermissionsOptions001
#define EOS_KWS_UpdateParentEmailOptions                  EOS_KWS_UpdateParentEmailOptions001
#define EOS_KWS_RequestPermissionsOptions                 EOS_KWS_RequestPermissionsOptions001
#define EOS_KWS_GetPermissionsCountOptions                EOS_KWS_GetPermissionsCountOptions001
#define EOS_KWS_CopyPermissionByIndexOptions              EOS_KWS_CopyPermissionByIndexOptions001
#define EOS_KWS_GetPermissionByKeyOptions                 EOS_KWS_GetPermissionByKeyOptions001
#define EOS_KWS_AddNotifyPermissionsUpdateReceivedOptions EOS_KWS_AddNotifyPermissionsUpdateReceivedOptions001

#include "eos_kws_types1.12.0.h"

#define EOS_KWS_PERMISSIONSTATUS_API_LATEST                   EOS_KWS_PERMISSIONSTATUS_API_001
#define EOS_KWS_QUERYAGEGATE_API_LATEST                       EOS_KWS_QUERYAGEGATE_API_001
#define EOS_KWS_CREATEUSER_API_LATEST                         EOS_KWS_CREATEUSER_API_001
#define EOS_KWS_QUERYPERMISSIONS_API_LATEST                   EOS_KWS_QUERYPERMISSIONS_API_001
#define EOS_KWS_UPDATEPARENTEMAIL_API_LATEST                  EOS_KWS_UPDATEPARENTEMAIL_API_001
#define EOS_KWS_REQUESTPERMISSIONS_API_LATEST                 EOS_KWS_REQUESTPERMISSIONS_API_001
#define EOS_KWS_GETPERMISSIONSCOUNT_API_LATEST                EOS_KWS_GETPERMISSIONSCOUNT_API_001
#define EOS_KWS_COPYPERMISSIONBYINDEX_API_LATEST              EOS_KWS_COPYPERMISSIONBYINDEX_API_001
#define EOS_KWS_GETPERMISSIONBYKEY_API_LATEST                 EOS_KWS_GETPERMISSIONBYKEY_API_001
#define EOS_KWS_ADDNOTIFYPERMISSIONSUPDATERECEIVED_API_LATEST EOS_KWS_ADDNOTIFYPERMISSIONSUPDATERECEIVED_API_LATEST
