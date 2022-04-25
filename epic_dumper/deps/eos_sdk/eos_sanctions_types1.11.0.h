#pragma pack(push, 8)

/** The most recent version of the EOS_Sanctions_PlayerSanction struct. */
#define EOS_SANCTIONS_PLAYERSANCTION_API_001 1

/**
 * Contains information about a single player sanction.
 */
EOS_STRUCT(EOS_Sanctions_PlayerSanction001, (
	/** API Version: Set this to EOS_SANCTIONS_PLAYERSANCTION_API_LATEST. */
	int32_t ApiVersion;
	/** The POSIX timestamp when the sanction was placed */
	int64_t TimePlaced;
	/** The action associated with this sanction */
	const char* Action;
));

#pragma pack(pop)
