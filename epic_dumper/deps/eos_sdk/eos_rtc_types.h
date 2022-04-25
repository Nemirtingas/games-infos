// Copyright Epic Games, Inc. All Rights Reserved.

#pragma once

#include "eos_common.h"

#define EOS_RTC_JoinRoomOptions                          EOS_RTC_JoinRoomOptions001
#define EOS_RTC_LeaveRoomOptions                         EOS_RTC_LeaveRoomOptions001
#define EOS_RTC_BlockParticipantOptions                  EOS_RTC_BlockParticipantOptions001
#define EOS_RTC_AddNotifyDisconnectedOptions             EOS_RTC_AddNotifyDisconnectedOptions001
#define EOS_RTC_ParticipantMetadata                      EOS_RTC_ParticipantMetadata001
#define EOS_RTC_AddNotifyParticipantStatusChangedOptions EOS_RTC_AddNotifyParticipantStatusChangedOptions001
#define EOS_RTC_SetSettingOptions                        EOS_RTC_SetSettingOptions001
#define EOS_RTC_SetRoomSettingOptions                    EOS_RTC_SetRoomSettingOptions001

enum { k_iRTCCallbackBase = 24000 };
// next free callback_id: k_iRTCCallbackBase + 5

#include "eos_rtc_types1.14.0.h"

#define EOS_RTC_JOINROOM_API_LATEST                          EOS_RTC_JOINROOM_API_001
#define EOS_RTC_LEAVEROOM_API_LATEST                         EOS_RTC_LEAVEROOM_API_001
#define EOS_RTC_BLOCKPARTICIPANT_API_LATEST                  EOS_RTC_BLOCKPARTICIPANT_API_001
#define EOS_RTC_ADDNOTIFYDISCONNECTED_API_LATEST             EOS_RTC_ADDNOTIFYDISCONNECTED_API_001
#define EOS_RTC_PARTICIPANTMETADATA_API_LATEST               EOS_RTC_PARTICIPANTMETADATA_API_001
#define EOS_RTC_ADDNOTIFYPARTICIPANTSTATUSCHANGED_API_LATEST EOS_RTC_ADDNOTIFYPARTICIPANTSTATUSCHANGED_API_001
#define EOS_RTC_SETSETTING_API_LATEST                        EOS_RTC_SETSETTING_API_001
#define EOS_RTC_SETROOMSETTING_API_LATEST                    EOS_RTC_SETROOMSETTING_API_001