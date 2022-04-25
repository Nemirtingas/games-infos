#pragma once

#include "eos_common.h"

enum { k_iRTCAdminCallbackBase = 23000 };
// next free callback_id: k_iRTCAdminCallbackBase + 3

#define EOS_RTCAdmin_QueryJoinRoomTokenOptions     EOS_RTCAdmin_QueryJoinRoomTokenOptions002
#define EOS_RTCAdmin_UserToken                     EOS_RTCAdmin_UserToken001
#define EOS_RTCAdmin_CopyUserTokenByIndexOptions   EOS_RTCAdmin_CopyUserTokenByIndexOptions002
#define EOS_RTCAdmin_CopyUserTokenByUserIdOptions  EOS_RTCAdmin_CopyUserTokenByUserIdOptions002
#define EOS_RTCAdmin_KickOptions                   EOS_RTCAdmin_KickOptions001
#define EOS_RTCAdmin_SetParticipantHardMuteOptions EOS_RTCAdmin_SetParticipantHardMuteOptions001

#include "eos_rtc_admin_types1.13.0.h"
                                                      
#define EOS_RTCADMIN_QUERYJOINROOMTOKEN_API_LATEST     EOS_RTCADMIN_QUERYJOINROOMTOKEN_API_002
#define EOS_RTCADMIN_USERTOKEN_API_LATEST              EOS_RTCADMIN_USERTOKEN_API_001
#define EOS_RTCADMIN_COPYUSERTOKENBYINDEX_API_LATEST   EOS_RTCADMIN_COPYUSERTOKENBYINDEX_API_002
#define EOS_RTCADMIN_COPYUSERTOKENBYUSERID_API_LATEST  EOS_RTCADMIN_COPYUSERTOKENBYUSERID_API_002
#define EOS_RTCADMIN_KICK_API_LATEST                   EOS_RTCADMIN_KICK_API_001
#define EOS_RTCADMIN_SETPARTICIPANTHARDMUTE_API_LATEST EOS_RTCADMIN_SETPARTICIPANTHARDMUTE_API_001