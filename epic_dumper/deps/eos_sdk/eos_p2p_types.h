// Copyright Epic Games, Inc. All Rights Reserved.

#pragma once

#include "eos_common.h"

enum { k_iP2PCallbackBase = 9000 };
// next free callback_id: k_iP2PCallbackBase + 4

#define EOS_P2P_SocketId                                  EOS_P2P_SocketId001
#define EOS_P2P_SendPacketOptions                         EOS_P2P_SendPacketOptions002
#define EOS_P2P_GetNextReceivedPacketSizeOptions          EOS_P2P_GetNextReceivedPacketSizeOptions002
#define EOS_P2P_ReceivePacketOptions                      EOS_P2P_ReceivePacketOptions002
#define EOS_P2P_AddNotifyPeerConnectionRequestOptions     EOS_P2P_AddNotifyPeerConnectionRequestOptions001
#define EOS_P2P_AddNotifyPeerConnectionClosedOptions      EOS_P2P_AddNotifyPeerConnectionClosedOptions001
#define EOS_P2P_AcceptConnectionOptions                   EOS_P2P_AcceptConnectionOptions001
#define EOS_P2P_CloseConnectionOptions                    EOS_P2P_CloseConnectionOptions001
#define EOS_P2P_CloseConnectionsOptions                   EOS_P2P_CloseConnectionsOptions001
#define EOS_P2P_QueryNATTypeOptions                       EOS_P2P_QueryNATTypeOptions001
#define EOS_P2P_GetNATTypeOptions                         EOS_P2P_GetNATTypeOptions001
#define EOS_P2P_SetRelayControlOptions                    EOS_P2P_SetRelayControlOptions001
#define EOS_P2P_GetRelayControlOptions                    EOS_P2P_GetRelayControlOptions001
#define EOS_P2P_SetPortRangeOptions                       EOS_P2P_SetPortRangeOptions001
#define EOS_P2P_GetPortRangeOptions                       EOS_P2P_GetPortRangeOptions001
#define EOS_P2P_SetPacketQueueSizeOptions                 EOS_P2P_SetPacketQueueSizeOptions001
#define EOS_P2P_GetPacketQueueInfoOptions                 EOS_P2P_GetPacketQueueInfoOptions001
#define EOS_P2P_AddNotifyIncomingPacketQueueFullOptions   EOS_P2P_AddNotifyIncomingPacketQueueFullOptions001
#define EOS_P2P_AddNotifyPeerConnectionEstablishedOptions EOS_P2P_AddNotifyPeerConnectionEstablishedOptions001
#define EOS_P2P_ClearPacketQueueOptions                   EOS_P2P_ClearPacketQueueOptions001

#include "eos_p2p_types1.14.1.h"
#include "eos_p2p_types1.5.0.h"

#define EOS_P2P_SOCKETID_API_LATEST                           EOS_P2P_SOCKETID_API_001
#define EOS_P2P_SENDPACKET_API_LATEST                         EOS_P2P_SENDPACKET_API_002
#define EOS_P2P_GETNEXTRECEIVEDPACKETSIZE_API_LATEST          EOS_P2P_GETNEXTRECEIVEDPACKETSIZE_API_002
#define EOS_P2P_RECEIVEPACKET_API_LATEST                      EOS_P2P_RECEIVEPACKET_API_002
#define EOS_P2P_ADDNOTIFYPEERCONNECTIONREQUEST_API_LATEST     EOS_P2P_ADDNOTIFYPEERCONNECTIONREQUEST_API_001
#define EOS_P2P_ADDNOTIFYPEERCONNECTIONCLOSED_API_LATEST      EOS_P2P_ADDNOTIFYPEERCONNECTIONCLOSED_API_001
#define EOS_P2P_ACCEPTCONNECTION_API_LATEST                   EOS_P2P_ACCEPTCONNECTION_API_001
#define EOS_P2P_CLOSECONNECTION_API_LATEST                    EOS_P2P_CLOSECONNECTION_API_001
#define EOS_P2P_CLOSECONNECTIONS_API_LATEST                   EOS_P2P_CLOSECONNECTIONS_API_001
#define EOS_P2P_QUERYNATTYPE_API_LATEST                       EOS_P2P_QUERYNATTYPE_API_001
#define EOS_P2P_GETNATTYPE_API_LATEST                         EOS_P2P_GETNATTYPE_API_001
#define EOS_P2P_SETRELAYCONTROL_API_LATEST                    EOS_P2P_SETRELAYCONTROL_API_001
#define EOS_P2P_GETRELAYCONTROL_API_LATEST                    EOS_P2P_GETRELAYCONTROL_API_001
#define EOS_P2P_SETPORTRANGE_API_LATEST                       EOS_P2P_SETPORTRANGE_API_001
#define EOS_P2P_GETPORTRANGE_API_LATEST                       EOS_P2P_GETPORTRANGE_API_001
#define EOS_P2P_SETPACKETQUEUESIZE_API_LATEST                 EOS_P2P_SETPACKETQUEUESIZE_API_001
#define EOS_P2P_GETPACKETQUEUEINFO_API_LATEST                 EOS_P2P_GETPACKETQUEUEINFO_API_001
#define EOS_P2P_ADDNOTIFYINCOMINGPACKETQUEUEFULL_API_LATEST   EOS_P2P_ADDNOTIFYINCOMINGPACKETQUEUEFULL_API_001
#define EOS_P2P_ADDNOTIFYPEERCONNECTIONESTABLISHED_API_LATEST EOS_P2P_ADDNOTIFYPEERCONNECTIONESTABLISHED_API_001
#define EOS_P2P_CLEARPACKETQUEUE_API_LATEST                   EOS_P2P_CLEARPACKETQUEUE_API_001