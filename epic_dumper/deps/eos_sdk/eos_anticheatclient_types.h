#pragma once

#include "eos_common.h"
#include "eos_anticheatcommon_types.h"

enum { k_iAntiCheatClientCallbackBase = 22000 };
// next free callback_id: k_iAntiCheatCommonCallbackBase + 1

#define EOS_AntiCheatClient_AddNotifyMessageToServerOptions       EOS_AntiCheatClient_AddNotifyMessageToServerOptions001
#define EOS_AntiCheatClient_AddNotifyMessageToPeerOptions         EOS_AntiCheatClient_AddNotifyMessageToPeerOptions001
#define EOS_AntiCheatClient_AddNotifyPeerActionRequiredOptions    EOS_AntiCheatClient_AddNotifyPeerActionRequiredOptions001
#define EOS_AntiCheatClient_AddNotifyPeerAuthStatusChangedOptions EOS_AntiCheatClient_AddNotifyPeerAuthStatusChangedOptions001
#define EOS_AntiCheatClient_BeginSessionOptions                   EOS_AntiCheatClient_BeginSessionOptions003
#define EOS_AntiCheatClient_EndSessionOptions                     EOS_AntiCheatClient_EndSessionOptions001
#define EOS_AntiCheatClient_PollStatusOptions                     EOS_AntiCheatClient_PollStatusOptions001
#define EOS_AntiCheatClient_AddExternalIntegrityCatalogOptions    EOS_AntiCheatClient_AddExternalIntegrityCatalogOptions001
#define EOS_AntiCheatClient_ReceiveMessageFromServerOptions       EOS_AntiCheatClient_ReceiveMessageFromServerOptions001
#define EOS_AntiCheatClient_GetProtectMessageOutputLengthOptions  EOS_AntiCheatClient_GetProtectMessageOutputLengthOptions001
#define EOS_AntiCheatClient_ProtectMessageOptions                 EOS_AntiCheatClient_ProtectMessageOptions001
#define EOS_AntiCheatClient_UnprotectMessageOptions               EOS_AntiCheatClient_UnprotectMessageOptions001
#define EOS_AntiCheatClient_RegisterPeerOptions                   EOS_AntiCheatClient_RegisterPeerOptions001
#define EOS_AntiCheatClient_UnregisterPeerOptions                 EOS_AntiCheatClient_UnregisterPeerOptions001
#define EOS_AntiCheatClient_ReceiveMessageFromPeerOptions         EOS_AntiCheatClient_ReceiveMessageFromPeerOptions001

#include "eos_anticheatclient_types1.14.0.h"

#define EOS_ANTICHEATCLIENT_ADDNOTIFYMESSAGETOSERVER_API_LATEST       EOS_ANTICHEATCLIENT_ADDNOTIFYMESSAGETOSERVER_API_001
#define EOS_ANTICHEATCLIENT_ADDNOTIFYMESSAGETOPEER_API_LATEST         EOS_ANTICHEATCLIENT_ADDNOTIFYMESSAGETOPEER_API_001
#define EOS_ANTICHEATCLIENT_ADDNOTIFYPEERACTIONREQUIRED_API_LATEST    EOS_ANTICHEATCLIENT_ADDNOTIFYPEERACTIONREQUIRED_API_001
#define EOS_ANTICHEATCLIENT_ADDNOTIFYPEERAUTHSTATUSCHANGED_API_LATEST EOS_ANTICHEATCLIENT_ADDNOTIFYPEERAUTHSTATUSCHANGED_API_001
#define EOS_ANTICHEATCLIENT_BEGINSESSION_API_LATEST                   EOS_ANTICHEATCLIENT_BEGINSESSION_API_003
#define EOS_ANTICHEATCLIENT_ENDSESSION_API_LATEST                     EOS_ANTICHEATCLIENT_ENDSESSION_API_001
#define EOS_ANTICHEATCLIENT_POLLSTATUS_API_LATEST                     EOS_ANTICHEATCLIENT_POLLSTATUS_API_001
#define EOS_ANTICHEATCLIENT_ADDEXTERNALINTEGRITYCATALOG_API_LATEST    EOS_ANTICHEATCLIENT_ADDEXTERNALINTEGRITYCATALOG_API_001
#define EOS_ANTICHEATCLIENT_RECEIVEMESSAGEFROMSERVER_API_LATEST       EOS_ANTICHEATCLIENT_RECEIVEMESSAGEFROMSERVER_API_001
#define EOS_ANTICHEATCLIENT_GETPROTECTMESSAGEOUTPUTLENGTH_API_LATEST  EOS_ANTICHEATCLIENT_GETPROTECTMESSAGEOUTPUTLENGTH_API_001
#define EOS_ANTICHEATCLIENT_PROTECTMESSAGE_API_LATEST                 EOS_ANTICHEATCLIENT_PROTECTMESSAGE_API_001
#define EOS_ANTICHEATCLIENT_UNPROTECTMESSAGE_API_LATEST               EOS_ANTICHEATCLIENT_UNPROTECTMESSAGE_API_001
#define EOS_ANTICHEATCLIENT_REGISTERPEER_API_LATEST                   EOS_ANTICHEATCLIENT_REGISTERPEER_API_001
#define EOS_ANTICHEATCLIENT_UNREGISTERPEER_API_LATEST                 EOS_ANTICHEATCLIENT_UNREGISTERPEER_API_001
#define EOS_ANTICHEATCLIENT_RECEIVEMESSAGEFROMPEER_API_LATEST         EOS_ANTICHEATCLIENT_RECEIVEMESSAGEFROMPEER_API_001