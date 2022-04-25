// Copyright Epic Games, Inc. All Rights Reserved.

#pragma once

#include "eos_common.h"
#include "eos_ui_types.h"

enum { k_iSessionsCallbackBase = 12000 };
// next free callback_id: k_iSessionsCallbackBase + 14

#define EOS_Sessions_CreateSessionModificationOptions           EOS_Sessions_CreateSessionModificationOptions004
#define EOS_Sessions_UpdateSessionModificationOptions           EOS_Sessions_UpdateSessionModificationOptions001
#define EOS_Sessions_SendInviteOptions                          EOS_Sessions_SendInviteOptions001
#define EOS_Sessions_RejectInviteOptions                        EOS_Sessions_RejectInviteOptions001
#define EOS_Sessions_QueryInvitesOptions                        EOS_Sessions_QueryInvitesOptions001
#define EOS_Sessions_GetInviteCountOptions                      EOS_Sessions_GetInviteCountOptions001
#define EOS_Sessions_GetInviteIdByIndexOptions                  EOS_Sessions_GetInviteIdByIndexOptions001
#define EOS_Sessions_CreateSessionSearchOptions                 EOS_Sessions_CreateSessionSearchOptions001
#define EOS_Sessions_UpdateSessionOptions                       EOS_Sessions_UpdateSessionOptions001
#define EOS_Sessions_DestroySessionOptions                      EOS_Sessions_DestroySessionOptions001
#define EOS_Sessions_JoinSessionOptions                         EOS_Sessions_JoinSessionOptions002
#define EOS_Sessions_StartSessionOptions                        EOS_Sessions_StartSessionOptions001
#define EOS_Sessions_EndSessionOptions                          EOS_Sessions_EndSessionOptions001
#define EOS_Sessions_RegisterPlayersOptions                     EOS_Sessions_RegisterPlayersOptions002
#define EOS_Sessions_UnregisterPlayersOptions                   EOS_Sessions_UnregisterPlayersOptions002
#define EOS_SessionModification_SetBucketIdOptions              EOS_SessionModification_SetBucketIdOptions001
#define EOS_SessionModification_SetHostAddressOptions           EOS_SessionModification_SetHostAddressOptions001
#define EOS_SessionModification_SetPermissionLevelOptions       EOS_SessionModification_SetPermissionLevelOptions001
#define EOS_SessionModification_SetJoinInProgressAllowedOptions EOS_SessionModification_SetJoinInProgressAllowedOptions001
#define EOS_SessionModification_SetMaxPlayersOptions            EOS_SessionModification_SetMaxPlayersOptions001
#define EOS_SessionModification_SetInvitesAllowedOptions        EOS_SessionModification_SetInvitesAllowedOptions001
#define EOS_Sessions_AttributeData                              EOS_Sessions_AttributeData001
#define EOS_ActiveSession_CopyInfoOptions                       EOS_ActiveSession_CopyInfoOptions001
#define EOS_ActiveSession_GetRegisteredPlayerCountOptions       EOS_ActiveSession_GetRegisteredPlayerCountOptions001
#define EOS_ActiveSession_GetRegisteredPlayerByIndexOptions     EOS_ActiveSession_GetRegisteredPlayerByIndexOptions001
#define EOS_SessionDetails_Attribute                            EOS_SessionDetails_Attribute001
#define EOS_SessionModification_AddAttributeOptions             EOS_SessionModification_AddAttributeOptions001
#define EOS_SessionModification_RemoveAttributeOptions          EOS_SessionModification_RemoveAttributeOptions001
#define EOS_SessionSearch_SetMaxResultsOptions                  EOS_SessionSearch_SetMaxResultsOptions001
#define EOS_SessionSearch_FindOptions                           EOS_SessionSearch_FindOptions002
#define EOS_SessionSearch_GetSearchResultCountOptions           EOS_SessionSearch_GetSearchResultCountOptions001
#define EOS_SessionSearch_CopySearchResultByIndexOptions        EOS_SessionSearch_CopySearchResultByIndexOptions001
#define EOS_SessionSearch_SetSessionIdOptions                   EOS_SessionSearch_SetSessionIdOptions001
#define EOS_SessionSearch_SetTargetUserIdOptions                EOS_SessionSearch_SetTargetUserIdOptions001
#define EOS_SessionSearch_SetParameterOptions                   EOS_SessionSearch_SetParameterOptions001
#define EOS_SessionSearch_RemoveParameterOptions                EOS_SessionSearch_RemoveParameterOptions001
#define EOS_SessionDetails_Settings                             EOS_SessionDetails_Settings003
#define EOS_SessionDetails_Info                                 EOS_SessionDetails_Info001
#define EOS_SessionDetails_CopyInfoOptions                      EOS_SessionDetails_CopyInfoOptions001
#define EOS_SessionDetails_GetSessionAttributeCountOptions      EOS_SessionDetails_GetSessionAttributeCountOptions001
#define EOS_SessionDetails_CopySessionAttributeByIndexOptions   EOS_SessionDetails_CopySessionAttributeByIndexOptions001
#define EOS_SessionDetails_CopySessionAttributeByKeyOptions     EOS_SessionDetails_CopySessionAttributeByKeyOptions001
#define EOS_ActiveSession_Info                                  EOS_ActiveSession_Info001
#define EOS_Sessions_CopyActiveSessionHandleOptions             EOS_Sessions_CopyActiveSessionHandleOptions001
#define EOS_Sessions_AddNotifySessionInviteReceivedOptions      EOS_Sessions_AddNotifySessionInviteReceivedOptions001
#define EOS_Sessions_AddNotifySessionInviteAcceptedOptions      EOS_Sessions_AddNotifySessionInviteAcceptedOptions001
#define EOS_Sessions_CopySessionHandleByInviteIdOptions         EOS_Sessions_CopySessionHandleByInviteIdOptions001
#define EOS_Sessions_CopySessionHandleForPresenceOptions        EOS_Sessions_CopySessionHandleForPresenceOptions001
#define EOS_Sessions_IsUserInSessionOptions                     EOS_Sessions_IsUserInSessionOptions001
#define EOS_Sessions_DumpSessionStateOptions                    EOS_Sessions_DumpSessionStateOptions001
#define EOS_Sessions_AddNotifyJoinSessionAcceptedOptions        EOS_Sessions_AddNotifyJoinSessionAcceptedOptions001
#define EOS_Sessions_CopySessionHandleByUiEventIdOptions        EOS_Sessions_CopySessionHandleByUiEventIdOptions001

/**
 * Input parameters for the EOS_Sessions_CopySessionHandleByUiEventId Function.
 */

#include <eos_sessions_types1.14.2.h>
#include <eos_sessions_types1.13.0.h>
#include <eos_sessions_types1.7.1.h>
#include <eos_sessions_types1.5.0.h>
#include <eos_sessions_types1.3.1.h>

#define EOS_SESSIONS_CREATESESSIONMODIFICATION_API_LATEST           EOS_SESSIONS_CREATESESSIONMODIFICATION_API_004
#define EOS_SESSIONS_UPDATESESSIONMODIFICATION_API_LATEST           EOS_SESSIONS_UPDATESESSIONMODIFICATION_API_001
#define EOS_SESSIONS_SENDINVITE_API_LATEST                          EOS_SESSIONS_SENDINVITE_API_001
#define EOS_SESSIONS_REJECTINVITE_API_LATEST                        EOS_SESSIONS_REJECTINVITE_API_001
#define EOS_SESSIONS_QUERYINVITES_API_LATEST                        EOS_SESSIONS_QUERYINVITES_API_001
#define EOS_SESSIONS_GETINVITECOUNT_API_LATEST                      EOS_SESSIONS_GETINVITECOUNT_API_001
#define EOS_SESSIONS_GETINVITEIDBYINDEX_API_LATEST                  EOS_SESSIONS_GETINVITEIDBYINDEX_API_001
#define EOS_SESSIONS_CREATESESSIONSEARCH_API_LATEST                 EOS_SESSIONS_CREATESESSIONSEARCH_API_001
#define EOS_SESSIONS_UPDATESESSION_API_LATEST                       EOS_SESSIONS_UPDATESESSION_API_001
#define EOS_SESSIONS_DESTROYSESSION_API_LATEST                      EOS_SESSIONS_DESTROYSESSION_API_001
#define EOS_SESSIONS_JOINSESSION_API_LATEST                         EOS_SESSIONS_JOINSESSION_API_002
#define EOS_SESSIONS_STARTSESSION_API_LATEST                        EOS_SESSIONS_STARTSESSION_API_001
#define EOS_SESSIONS_ENDSESSION_API_LATEST                          EOS_SESSIONS_ENDSESSION_API_001
#define EOS_SESSIONS_REGISTERPLAYERS_API_LATEST                     EOS_SESSIONS_REGISTERPLAYERS_API_002
#define EOS_SESSIONS_UNREGISTERPLAYERS_API_LATEST                   EOS_SESSIONS_UNREGISTERPLAYERS_API_002
#define EOS_SESSIONMODIFICATION_SETBUCKETID_API_LATEST              EOS_SESSIONMODIFICATION_SETBUCKETID_API_001
#define EOS_SESSIONMODIFICATION_SETHOSTADDRESS_API_LATEST           EOS_SESSIONMODIFICATION_SETHOSTADDRESS_API_001
#define EOS_SESSIONMODIFICATION_SETPERMISSIONLEVEL_API_LATEST       EOS_SESSIONMODIFICATION_SETPERMISSIONLEVEL_API_001
#define EOS_SESSIONMODIFICATION_SETJOININPROGRESSALLOWED_API_LATEST EOS_SESSIONMODIFICATION_SETJOININPROGRESSALLOWED_API_001
#define EOS_SESSIONMODIFICATION_SETMAXPLAYERS_API_LATEST            EOS_SESSIONMODIFICATION_SETMAXPLAYERS_API_001
#define EOS_SESSIONMODIFICATION_SETINVITESALLOWED_API_LATEST        EOS_SESSIONMODIFICATION_SETINVITESALLOWED_API_001
#define EOS_SESSIONS_ATTRIBUTEDATA_API_LATEST                       EOS_SESSIONS_ATTRIBUTEDATA_API_001
#define EOS_ACTIVESESSION_COPYINFO_API_LATEST                       EOS_ACTIVESESSION_COPYINFO_API_001
#define EOS_ACTIVESESSION_GETREGISTEREDPLAYERCOUNT_API_LATEST       EOS_ACTIVESESSION_GETREGISTEREDPLAYERCOUNT_API_001
#define EOS_ACTIVESESSION_GETREGISTEREDPLAYERBYINDEX_API_LATEST     EOS_ACTIVESESSION_GETREGISTEREDPLAYERBYINDEX_API_001
#define EOS_SESSIONDETAILS_ATTRIBUTE_API_LATEST                     EOS_SESSIONDETAILS_ATTRIBUTE_API_001
#define EOS_SESSIONMODIFICATION_ADDATTRIBUTE_API_LATEST             EOS_SESSIONMODIFICATION_ADDATTRIBUTE_API_001
#define EOS_SESSIONMODIFICATION_REMOVEATTRIBUTE_API_LATEST          EOS_SESSIONMODIFICATION_REMOVEATTRIBUTE_API_001
#define EOS_SESSIONSEARCH_SETMAXSEARCHRESULTS_API_LATEST            EOS_SESSIONSEARCH_SETMAXSEARCHRESULTS_API_001
#define EOS_SESSIONSEARCH_FIND_API_LATEST                           EOS_SESSIONSEARCH_FIND_API_001
#define EOS_SESSIONSEARCH_GETSEARCHRESULTCOUNT_API_LATEST           EOS_SESSIONSEARCH_GETSEARCHRESULTCOUNT_API_001
#define EOS_SESSIONSEARCH_COPYSEARCHRESULTBYINDEX_API_LATEST        EOS_SESSIONSEARCH_COPYSEARCHRESULTBYINDEX_API_001
#define EOS_SESSIONSEARCH_SETSESSIONID_API_LATEST                   EOS_SESSIONSEARCH_SETSESSIONID_API_001
#define EOS_SESSIONSEARCH_SETTARGETUSERID_API_LATEST                EOS_SESSIONSEARCH_SETTARGETUSERID_API_001
#define EOS_SESSIONSEARCH_SETPARAMETER_API_LATEST                   EOS_SESSIONSEARCH_SETPARAMETER_API_001
#define EOS_SESSIONSEARCH_REMOVEPARAMETER_API_LATEST                EOS_SESSIONSEARCH_REMOVEPARAMETER_API_001
#define EOS_SESSIONDETAILS_SETTINGS_API_LATEST                      EOS_SESSIONDETAILS_SETTINGS_API_003
#define EOS_SESSIONDETAILS_INFO_API_LATEST                          EOS_SESSIONDETAILS_INFO_API_001
#define EOS_SESSIONDETAILS_COPYINFO_API_LATEST                      EOS_SESSIONDETAILS_COPYINFO_API_001
#define EOS_SESSIONDETAILS_GETSESSIONATTRIBUTECOUNT_API_LATEST      EOS_SESSIONDETAILS_GETSESSIONATTRIBUTECOUNT_API_001
#define EOS_SESSIONDETAILS_COPYSESSIONATTRIBUTEBYINDEX_API_LATEST   EOS_SESSIONDETAILS_COPYSESSIONATTRIBUTEBYINDEX_API_001
#define EOS_SESSIONDETAILS_COPYSESSIONATTRIBUTEBYKEY_API_LATEST     EOS_SESSIONDETAILS_COPYSESSIONATTRIBUTEBYKEY_API_001
#define EOS_ACTIVESESSION_INFO_API_LATEST                           EOS_ACTIVESESSION_INFO_API_001
#define EOS_SESSIONS_COPYACTIVESESSIONHANDLE_API_LATEST             EOS_SESSIONS_COPYACTIVESESSIONHANDLE_API_001
#define EOS_SESSIONS_ADDNOTIFYSESSIONINVITERECEIVED_API_LATEST      EOS_SESSIONS_ADDNOTIFYSESSIONINVITERECEIVED_API_001
#define EOS_SESSIONS_ADDNOTIFYSESSIONINVITEACCEPTED_API_LATEST      EOS_SESSIONS_ADDNOTIFYSESSIONINVITEACCEPTED_API_001
#define EOS_SESSIONS_COPYSESSIONHANDLEBYINVITEID_API_LATEST         EOS_SESSIONS_COPYSESSIONHANDLEBYINVITEID_API_001
#define EOS_SESSIONS_COPYSESSIONHANDLEFORPRESENCE_API_LATEST        EOS_SESSIONS_COPYSESSIONHANDLEFORPRESENCE_API_001
#define EOS_SESSIONS_ISUSERINSESSION_API_LATEST                     EOS_SESSIONS_ISUSERINSESSION_API_001
#define EOS_SESSIONS_DUMPSESSIONSTATE_API_LATEST                    EOS_SESSIONS_DUMPSESSIONSTATE_API_001
#define EOS_SESSIONS_ADDNOTIFYJOINSESSIONACCEPTED_API_LATEST        EOS_SESSIONS_ADDNOTIFYJOINSESSIONACCEPTED_API_001
#define EOS_SESSIONS_COPYSESSIONHANDLEBYUIEVENTID_API_LATEST        EOS_SESSIONS_COPYSESSIONHANDLEBYUIEVENTID_API_001