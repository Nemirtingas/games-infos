// Copyright Epic Games, Inc. All Rights Reserved.
#pragma once

#include "eos_common.h"
#include "eos_ui_types.h"

enum { k_iLobbyCallbackBase = 7000 };
// next free callback_id: k_iLobbyCallbackBase + 18

#define EOS_LobbyDetails_Info                               EOS_LobbyDetails_Info001
#define EOS_Lobby_LocalRTCOptions                           EOS_Lobby_LocalRTCOptions001
#define EOS_Lobby_CreateLobbyOptions                        EOS_Lobby_CreateLobbyOptions007
#define EOS_Lobby_DestroyLobbyOptions                       EOS_Lobby_DestroyLobbyOptions001
#define EOS_Lobby_JoinLobbyOptions                          EOS_Lobby_JoinLobbyOptions002
#define EOS_Lobby_LeaveLobbyOptions                         EOS_Lobby_LeaveLobbyOptions001
#define EOS_Lobby_UpdateLobbyModificationOptions            EOS_Lobby_UpdateLobbyModificationOptions001
#define EOS_Lobby_UpdateLobbyOptions                        EOS_Lobby_UpdateLobbyOptions001
#define EOS_Lobby_PromoteMemberOptions                      EOS_Lobby_PromoteMemberOptions001
#define EOS_Lobby_KickMemberOptions                         EOS_Lobby_KickMemberOptions001
#define EOS_Lobby_AddNotifyLobbyUpdateReceivedOptions       EOS_Lobby_AddNotifyLobbyUpdateReceivedOptions001
#define EOS_Lobby_AddNotifyLobbyMemberUpdateReceivedOptions EOS_Lobby_AddNotifyLobbyMemberUpdateReceivedOptions001
#define EOS_Lobby_AddNotifyLobbyMemberStatusReceivedOptions EOS_Lobby_AddNotifyLobbyMemberStatusReceivedOptions001
#define EOS_Lobby_AddNotifyLobbyInviteReceivedOptions       EOS_Lobby_AddNotifyLobbyInviteReceivedOptions001
#define EOS_Lobby_CopyLobbyDetailsHandleByInviteIdOptions   EOS_Lobby_CopyLobbyDetailsHandleByInviteIdOptions001
#define EOS_Lobby_CreateLobbySearchOptions                  EOS_Lobby_CreateLobbySearchOptions001
#define EOS_Lobby_SendInviteOptions                         EOS_Lobby_SendInviteOptions001
#define EOS_Lobby_RejectInviteOptions                       EOS_Lobby_RejectInviteOptions001
#define EOS_Lobby_QueryInvitesOptions                       EOS_Lobby_QueryInvitesOptions001
#define EOS_Lobby_GetInviteCountOptions                     EOS_Lobby_GetInviteCountOptions001
#define EOS_Lobby_GetInviteIdByIndexOptions                 EOS_Lobby_GetInviteIdByIndexOptions001
#define EOS_Lobby_CopyLobbyDetailsHandleOptions             EOS_Lobby_CopyLobbyDetailsHandleOptions001
#define EOS_Lobby_GetRTCRoomNameOptions                     EOS_Lobby_GetRTCRoomNameOptions001
#define EOS_Lobby_IsRTCRoomConnectedOptions                 EOS_Lobby_IsRTCRoomConnectedOptions001
#define EOS_Lobby_AddNotifyRTCRoomConnectionChangedOptions  EOS_Lobby_AddNotifyRTCRoomConnectionChangedOptions001
#define EOS_Lobby_AttributeData                             EOS_Lobby_AttributeData001
#define EOS_Lobby_Attribute                                 EOS_Lobby_Attribute001
#define EOS_LobbyModification_SetBucketIdOptions            EOS_LobbyModification_SetBucketIdOptions001
#define EOS_LobbyModification_SetPermissionLevelOptions     EOS_LobbyModification_SetPermissionLevelOptions001
#define EOS_LobbyModification_SetMaxMembersOptions          EOS_LobbyModification_SetMaxMembersOptions001
#define EOS_LobbyModification_SetInvitesAllowedOptions      EOS_LobbyModification_SetInvitesAllowedOptions001
#define EOS_LobbyModification_AddAttributeOptions           EOS_LobbyModification_AddAttributeOptions001
#define EOS_LobbyModification_RemoveAttributeOptions        EOS_LobbyModification_RemoveAttributeOptions001
#define EOS_LobbyModification_AddMemberAttributeOptions     EOS_LobbyModification_AddMemberAttributeOptions001
#define EOS_LobbyModification_RemoveMemberAttributeOptions  EOS_LobbyModification_RemoveMemberAttributeOptions001
#define EOS_LobbyDetails_GetLobbyOwnerOptions               EOS_LobbyDetails_GetLobbyOwnerOptions001
#define EOS_LobbyDetails_CopyInfoOptions                    EOS_LobbyDetails_CopyInfoOptions001
#define EOS_LobbyDetails_GetAttributeCountOptions           EOS_LobbyDetails_GetAttributeCountOptions001
#define EOS_LobbyDetails_CopyAttributeByIndexOptions        EOS_LobbyDetails_CopyAttributeByIndexOptions001
#define EOS_LobbyDetails_CopyAttributeByKeyOptions          EOS_LobbyDetails_CopyAttributeByKeyOptions001
#define EOS_LobbyDetails_GetMemberAttributeCountOptions     EOS_LobbyDetails_GetMemberAttributeCountOptions001
#define EOS_LobbyDetails_CopyMemberAttributeByIndexOptions  EOS_LobbyDetails_CopyMemberAttributeByIndexOptions001
#define EOS_LobbyDetails_CopyMemberAttributeByKeyOptions    EOS_LobbyDetails_CopyMemberAttributeByKeyOptions001
#define EOS_LobbyDetails_GetMemberCountOptions              EOS_LobbyDetails_GetMemberCountOptions001
#define EOS_LobbyDetails_GetMemberByIndexOptions            EOS_LobbyDetails_GetMemberByIndexOptions001
#define EOS_LobbySearch_FindOptions                         EOS_LobbySearch_FindOptions001
#define EOS_LobbySearch_SetLobbyIdOptions                   EOS_LobbySearch_SetLobbyIdOptions001
#define EOS_LobbySearch_SetTargetUserIdOptions              EOS_LobbySearch_SetTargetUserIdOptions001
#define EOS_LobbySearch_SetParameterOptions                 EOS_LobbySearch_SetParameterOptions001
#define EOS_LobbySearch_RemoveParameterOptions              EOS_LobbySearch_RemoveParameterOptions001
#define EOS_LobbySearch_SetMaxResultsOptions                EOS_LobbySearch_SetMaxResultsOptions001
#define EOS_LobbySearch_GetSearchResultCountOptions         EOS_LobbySearch_GetSearchResultCountOptions001
#define EOS_LobbySearch_CopySearchResultByIndexOptions      EOS_LobbySearch_CopySearchResultByIndexOptions001
#define EOS_Lobby_AddNotifyLobbyInviteAcceptedOptions       EOS_Lobby_AddNotifyLobbyInviteAcceptedOptions001
#define EOS_Lobby_AddNotifyJoinLobbyAcceptedOptions         EOS_Lobby_AddNotifyJoinLobbyAcceptedOptions001
#define EOS_Lobby_CopyLobbyDetailsHandleByUiEventIdOptions  EOS_Lobby_CopyLobbyDetailsHandleByUiEventIdOptions001

#include "eos_lobby_types1.14.2.h"
#include "eos_lobby_types1.12.0.h"
#include "eos_lobby_types1.11.0.h"
#include "eos_lobby_types1.10.2.h"
#include "eos_lobby_types1.6.0.h"

#define EOS_LOBBYDETAILS_INFO_API_LATEST                        EOS_LOBBYDETAILS_INFO_API_001
#define EOS_LOBBY_LOCALRTCOPTIONS_API_LATEST                    EOS_LOBBY_LOCALRTCOPTIONS_API_001
#define EOS_LOBBY_CREATELOBBY_API_LATEST                        EOS_LOBBY_CREATELOBBY_API_007
#define EOS_LOBBY_DESTROYLOBBY_API_LATEST                       EOS_LOBBY_DESTROYLOBBY_API_001
#define EOS_LOBBY_JOINLOBBY_API_LATEST                          EOS_LOBBY_JOINLOBBY_API_002
#define EOS_LOBBY_LEAVELOBBY_API_LATEST                         EOS_LOBBY_LEAVELOBBY_API_001
#define EOS_LOBBY_UPDATELOBBYMODIFICATION_API_LATEST            EOS_LOBBY_UPDATELOBBYMODIFICATION_API_001
#define EOS_LOBBY_UPDATELOBBY_API_LATEST                        EOS_LOBBY_UPDATELOBBY_API_001
#define EOS_LOBBY_PROMOTEMEMBER_API_LATEST                      EOS_LOBBY_PROMOTEMEMBER_API_001
#define EOS_LOBBY_KICKMEMBER_API_LATEST                         EOS_LOBBY_KICKMEMBER_API_001
#define EOS_LOBBY_ADDNOTIFYLOBBYUPDATERECEIVED_API_LATEST       EOS_LOBBY_ADDNOTIFYLOBBYUPDATERECEIVED_API_001
#define EOS_LOBBY_ADDNOTIFYLOBBYMEMBERUPDATERECEIVED_API_LATEST EOS_LOBBY_ADDNOTIFYLOBBYMEMBERUPDATERECEIVED_API_001
#define EOS_LOBBY_ADDNOTIFYLOBBYMEMBERSTATUSRECEIVED_API_LATEST EOS_LOBBY_ADDNOTIFYLOBBYMEMBERSTATUSRECEIVED_API_001
#define EOS_LOBBY_ADDNOTIFYLOBBYINVITERECEIVED_API_LATEST       EOS_LOBBY_ADDNOTIFYLOBBYINVITERECEIVED_API_001
#define EOS_LOBBY_COPYLOBBYDETAILSHANDLEBYINVITEID_API_LATEST   EOS_LOBBY_COPYLOBBYDETAILSHANDLEBYINVITEID_API_001
#define EOS_LOBBY_CREATELOBBYSEARCH_API_LATEST                  EOS_LOBBY_CREATELOBBYSEARCH_API_001
#define EOS_LOBBY_SENDINVITE_API_LATEST                         EOS_LOBBY_SENDINVITE_API_001
#define EOS_LOBBY_REJECTINVITE_API_LATEST                       EOS_LOBBY_REJECTINVITE_API_001
#define EOS_LOBBY_QUERYINVITES_API_LATEST                       EOS_LOBBY_QUERYINVITES_API_001
#define EOS_LOBBY_GETINVITECOUNT_API_LATEST                     EOS_LOBBY_GETINVITECOUNT_API_001
#define EOS_LOBBY_GETINVITEIDBYINDEX_API_LATEST                 EOS_LOBBY_GETINVITEIDBYINDEX_API_001
#define EOS_LOBBY_COPYLOBBYDETAILSHANDLE_API_LATEST             EOS_LOBBY_COPYLOBBYDETAILSHANDLE_API_001
#define EOS_LOBBY_GETRTCROOMNAME_API_LATEST                     EOS_LOBBY_GETRTCROOMNAME_API_001
#define EOS_LOBBY_ISRTCROOMCONNECTED_API_LATEST                 EOS_LOBBY_ISRTCROOMCONNECTED_API_001
#define EOS_LOBBY_ADDNOTIFYRTCROOMCONNECTIONCHANGED_API_LATEST  EOS_LOBBY_ADDNOTIFYRTCROOMCONNECTIONCHANGED_API_001
#define EOS_LOBBY_ATTRIBUTEDATA_API_LATEST                      EOS_LOBBY_ATTRIBUTEDATA_API_001
#define EOS_LOBBY_ATTRIBUTE_API_LATEST                          EOS_LOBBY_ATTRIBUTE_API_001
#define EOS_LOBBYMODIFICATION_SETBUCKETID_API_LATEST            EOS_LOBBYMODIFICATION_SETBUCKETID_API_001
#define EOS_LOBBYMODIFICATION_SETPERMISSIONLEVEL_API_LATEST     EOS_LOBBYMODIFICATION_SETPERMISSIONLEVEL_API_001
#define EOS_LOBBYMODIFICATION_SETMAXMEMBERS_API_LATEST          EOS_LOBBYMODIFICATION_SETMAXMEMBERS_API_001
#define EOS_LOBBYMODIFICATION_SETINVITESALLOWED_API_LATEST      EOS_LOBBYMODIFICATION_SETINVITESALLOWED_API_001
#define EOS_LOBBYMODIFICATION_ADDATTRIBUTE_API_LATEST           EOS_LOBBYMODIFICATION_ADDATTRIBUTE_API_001
#define EOS_LOBBYMODIFICATION_REMOVEATTRIBUTE_API_LATEST        EOS_LOBBYMODIFICATION_REMOVEATTRIBUTE_API_001
#define EOS_LOBBYMODIFICATION_ADDMEMBERATTRIBUTE_API_LATEST     EOS_LOBBYMODIFICATION_ADDMEMBERATTRIBUTE_API_001
#define EOS_LOBBYMODIFICATION_REMOVEMEMBERATTRIBUTE_API_LATEST  EOS_LOBBYMODIFICATION_REMOVEMEMBERATTRIBUTE_API_001
#define EOS_LOBBYDETAILS_GETLOBBYOWNER_API_LATEST               EOS_LOBBYDETAILS_GETLOBBYOWNER_API_001
#define EOS_LOBBYDETAILS_COPYINFO_API_LATEST                    EOS_LOBBYDETAILS_COPYINFO_API_001
#define EOS_LOBBYDETAILS_GETATTRIBUTECOUNT_API_LATEST           EOS_LOBBYDETAILS_GETATTRIBUTECOUNT_API_001
#define EOS_LOBBYDETAILS_COPYATTRIBUTEBYINDEX_API_LATEST        EOS_LOBBYDETAILS_COPYATTRIBUTEBYINDEX_API_001
#define EOS_LOBBYDETAILS_COPYATTRIBUTEBYKEY_API_LATEST          EOS_LOBBYDETAILS_COPYATTRIBUTEBYKEY_API_001
#define EOS_LOBBYDETAILS_GETMEMBERATTRIBUTECOUNT_API_LATEST     EOS_LOBBYDETAILS_GETMEMBERATTRIBUTECOUNT_API_001
#define EOS_LOBBYDETAILS_COPYMEMBERATTRIBUTEBYINDEX_API_LATEST  EOS_LOBBYDETAILS_COPYMEMBERATTRIBUTEBYINDEX_API_001
#define EOS_LOBBYDETAILS_COPYMEMBERATTRIBUTEBYKEY_API_LATEST    EOS_LOBBYDETAILS_COPYMEMBERATTRIBUTEBYKEY_API_001
#define EOS_LOBBYDETAILS_GETMEMBERCOUNT_API_LATEST              EOS_LOBBYDETAILS_GETMEMBERCOUNT_API_001
#define EOS_LOBBYDETAILS_GETMEMBERBYINDEX_API_LATEST            EOS_LOBBYDETAILS_GETMEMBERBYINDEX_API_001
#define EOS_LOBBYSEARCH_FIND_API_LATEST                         EOS_LOBBYSEARCH_FIND_API_001
#define EOS_LOBBYSEARCH_SETLOBBYID_API_LATEST                   EOS_LOBBYSEARCH_SETLOBBYID_API_001
#define EOS_LOBBYSEARCH_SETTARGETUSERID_API_LATEST              EOS_LOBBYSEARCH_SETTARGETUSERID_API_001
#define EOS_LOBBYSEARCH_SETPARAMETER_API_LATEST                 EOS_LOBBYSEARCH_SETPARAMETER_API_001
#define EOS_LOBBYSEARCH_REMOVEPARAMETER_API_LATEST              EOS_LOBBYSEARCH_REMOVEPARAMETER_API_001
#define EOS_LOBBYSEARCH_SETMAXRESULTS_API_LATEST                EOS_LOBBYSEARCH_SETMAXRESULTS_API_001
#define EOS_LOBBYSEARCH_GETSEARCHRESULTCOUNT_API_LATEST         EOS_LOBBYSEARCH_GETSEARCHRESULTCOUNT_API_001
#define EOS_LOBBYSEARCH_COPYSEARCHRESULTBYINDEX_API_LATEST      EOS_LOBBYSEARCH_COPYSEARCHRESULTBYINDEX_API_001
#define EOS_LOBBY_ADDNOTIFYLOBBYINVITEACCEPTED_API_LATEST       EOS_LOBBY_ADDNOTIFYLOBBYINVITEACCEPTED_API_001
#define EOS_LOBBY_ADDNOTIFYJOINLOBBYACCEPTED_API_LATEST         EOS_LOBBY_ADDNOTIFYJOINLOBBYACCEPTED_API_001
#define EOS_LOBBY_COPYLOBBYDETAILSHANDLEBYUIEVENTID_API_LATEST  EOS_LOBBY_COPYLOBBYDETAILSHANDLEBYUIEVENTID_API_001