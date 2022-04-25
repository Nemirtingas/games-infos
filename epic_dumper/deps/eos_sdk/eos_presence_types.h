// Copyright Epic Games, Inc. All Rights Reserved.

#pragma once

#include "eos_common.h"
#include "eos_ui_types.h"

enum { k_iPresenceCallbackBase = 11000 };
// next free callback_id: k_iPresenceCallbackBase + 4

#define EOS_Presence_DataRecord                        EOS_Presence_DataRecord001
#define EOS_Presence_Info                              EOS_Presence_Info002
#define EOS_Presence_QueryPresenceOptions              EOS_Presence_QueryPresenceOptions001
#define EOS_Presence_HasPresenceOptions                EOS_Presence_HasPresenceOptions001
#define EOS_Presence_CopyPresenceOptions               EOS_Presence_CopyPresenceOptions001
#define EOS_Presence_CreatePresenceModificationOptions EOS_Presence_CreatePresenceModificationOptions001
#define EOS_Presence_SetPresenceOptions                EOS_Presence_SetPresenceOptions001
#define EOS_Presence_AddNotifyOnPresenceChangedOptions EOS_Presence_AddNotifyOnPresenceChangedOptions001
#define EOS_Presence_AddNotifyJoinGameAcceptedOptions  EOS_Presence_AddNotifyJoinGameAcceptedOptions001
#define EOS_Presence_GetJoinInfoOptions                EOS_Presence_GetJoinInfoOptions001
#define EOS_PresenceModification_SetJoinInfoOptions    EOS_PresenceModification_SetJoinInfoOptions001
#define EOS_PresenceModification_SetStatusOptions      EOS_PresenceModification_SetStatusOptions001
#define EOS_PresenceModification_SetRawRichTextOptions EOS_PresenceModification_SetRawRichTextOptions001
#define EOS_PresenceModification_SetDataOptions        EOS_PresenceModification_SetDataOptions001
#define EOS_PresenceModification_DataRecordId          EOS_PresenceModification_DataRecordId001
#define EOS_PresenceModification_DeleteDataOptions     EOS_PresenceModification_DeleteDataOptions001

#include <eos_presence_types1.14.0.h>
#include <eos_presence_types1.5.0.h>
#include <eos_presence_types1.3.1.h>

#define EOS_PRESENCE_DATARECORD_API_LATEST                 EOS_PRESENCE_DATARECORD_API_001
#define EOS_PRESENCE_INFO_API_LATEST                       EOS_PRESENCE_INFO_API_002
#define EOS_PRESENCE_QUERYPRESENCE_API_LATEST              EOS_PRESENCE_QUERYPRESENCE_API_001
#define EOS_PRESENCE_HASPRESENCE_API_LATEST                EOS_PRESENCE_HASPRESENCE_API_001
#define EOS_PRESENCE_COPYPRESENCE_API_LATEST               EOS_PRESENCE_COPYPRESENCE_API_002
#define EOS_PRESENCE_CREATEPRESENCEMODIFICATION_API_LATEST EOS_PRESENCE_CREATEPRESENCEMODIFICATION_API_001
#define EOS_PRESENCE_SETPRESENCE_API_LATEST                EOS_PRESENCE_SETPRESENCE_API_001
#define EOS_PRESENCE_ADDNOTIFYONPRESENCECHANGED_API_LATEST EOS_PRESENCE_ADDNOTIFYONPRESENCECHANGED_API_001
#define EOS_PRESENCE_ADDNOTIFYJOINGAMEACCEPTED_API_LATEST  EOS_PRESENCE_ADDNOTIFYJOINGAMEACCEPTED_API_001
#define EOS_PRESENCE_GETJOININFO_API_LATEST                EOS_PRESENCE_GETJOININFO_API_001
#define EOS_PRESENCEMODIFICATION_SETJOININFO_API_LATEST    EOS_PRESENCEMODIFICATION_SETJOININFO_API_001
#define EOS_PRESENCEMODIFICATION_SETSTATUS_API_LATEST      EOS_PRESENCEMODIFICATION_SETSTATUS_API_001
#define EOS_PRESENCEMODIFICATION_SETRAWRICHTEXT_API_LATEST EOS_PRESENCEMODIFICATION_SETRAWRICHTEXT_API_001
#define EOS_PRESENCEMODIFICATION_SETDATA_API_LATEST        EOS_PRESENCEMODIFICATION_SETDATA_API_001
#define EOS_PRESENCEMODIFICATION_DATARECORDID_API_LATEST   EOS_PRESENCEMODIFICATION_DATARECORDID_API_001
#define EOS_PRESENCEMODIFICATION_DELETEDATA_API_LATEST     EOS_PRESENCEMODIFICATION_DELETEDATA_API_001