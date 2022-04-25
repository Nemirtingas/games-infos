#pragma pack(push, 8)

/** The most recent version of the EOS_Sessions_CreateSessionModification API. */
#define EOS_SESSIONS_CREATESESSIONMODIFICATION_API_003 3

/**
 * Input parameters for the EOS_Sessions_CreateSessionModification function.
 */
EOS_STRUCT(EOS_Sessions_CreateSessionModificationOptions003, (
	/** API Version: Set this to EOS_SESSIONS_CREATESESSIONMODIFICATION_API_LATEST. */
	int32_t ApiVersion;
	/** Name of the session to create */
	const char* SessionName;
	/** Bucket ID associated with the session */
	const char* BucketId;
	/** Maximum number of players allowed in the session */
	uint32_t MaxPlayers;
	/** The Product User ID of the local user associated with the session */
	EOS_ProductUserId LocalUserId;
	/** 
	 * If true, this session will be associated with presence. Only one session at a time can have this flag true.
	 * This affects the ability of the Social Overlay to show game related actions to take in the user's social graph.
	 * 
	 * @note The Social Overlay can handle only one of the following three options at a time:
	 * * using the bPresenceEnabled flags within the Sessions interface
	 * * using the bPresenceEnabled flags within the Lobby interface
	 * * using EOS_PresenceModification_SetJoinInfo
	 *
	 * @see EOS_PresenceModification_SetJoinInfoOptions
	 * @see EOS_Lobby_CreateLobbyOptions
	 * @see EOS_Lobby_JoinLobbyOptions
	 * @see EOS_Sessions_JoinSessionOptions
	 */
	EOS_Bool bPresenceEnabled;
	/**
	 * Optional session id - set to a globally unique value to override the backend assignment
	 * If not specified the backend service will assign one to the session.  Do not mix and match.
	 * This value can be of size [EOS_SESSIONMODIFICATION_MIN_SESSIONIDOVERRIDE_LENGTH, EOS_SESSIONMODIFICATION_MAX_SESSIONIDOVERRIDE_LENGTH]
	 */
	const char* SessionId;
));

/** The most recent version of the EOS_Sessions_RegisterPlayers API. */
#define EOS_SESSIONS_REGISTERPLAYERS_API_001 1

/**
 * Input parameters for the EOS_Sessions_RegisterPlayers function.
 */
EOS_STRUCT(EOS_Sessions_RegisterPlayersOptions001, (
	/** API Version: Set this to EOS_SESSIONS_REGISTERPLAYERS_API_LATEST. */
	int32_t ApiVersion;
	/** Name of the session for which to register players */
	const char* SessionName;
	/** Array of players to register with the session */
	EOS_ProductUserId* PlayersToRegister;
	/** Number of players in the array */
	uint32_t PlayersToRegisterCount;
));

/** The most recent version of the EOS_Sessions_UnregisterPlayers API. */
#define EOS_SESSIONS_UNREGISTERPLAYERS_API_001 1

/**
 * Input parameters for the EOS_Sessions_UnregisterPlayers function.
 */
EOS_STRUCT(EOS_Sessions_UnregisterPlayersOptions001, (
	/** API Version: Set this to EOS_SESSIONS_UNREGISTERPLAYERS_API_LATEST. */
	int32_t ApiVersion;
	/** Name of the session for which to unregister players */
	const char* SessionName;
	/** Array of players to unregister from the session */
	EOS_ProductUserId* PlayersToUnregister;
	/** Number of players in the array */
	uint32_t PlayersToUnregisterCount;
));

/** The most recent version of the EOS_SessionDetails_Settings struct. */
#define EOS_SESSIONDETAILS_SETTINGS_API_002 2

/** Common settings associated with a single session */
EOS_STRUCT(EOS_SessionDetails_Settings002, (
	/** API Version: Set this to EOS_SESSIONDETAILS_SETTINGS_API_LATEST. */
	int32_t ApiVersion;
	/** The main indexed parameter for this session, can be any string (ie "Region:GameMode") */
	const char* BucketId;
	/** Number of total players allowed in the session */
	uint32_t NumPublicConnections;
	/** Are players allowed to join the session while it is in the "in progress" state */
	EOS_Bool bAllowJoinInProgress;
	/** Permission level describing allowed access to the session when joining or searching for the session */
	EOS_EOnlineSessionPermissionLevel PermissionLevel;
	/** Are players allowed to send invites for the session */
	EOS_Bool bInvitesAllowed;
));

#pragma pack(pop)
