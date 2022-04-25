// Copyright Epic Games, Inc. All Rights Reserved.

#pragma pack(push, 8)

#define EOS_ANTICHEATCOMMON_LOGPLAYERTAKEDAMAGE_API_002 2
EOS_STRUCT(EOS_AntiCheatCommon_LogPlayerTakeDamageOptions002, (
	/** API Version: Set this to EOS_ANTICHEATCOMMON_LOGPLAYERTAKEDAMAGE_API_LATEST. */
	int32_t ApiVersion;
	/** Locally unique value used in RegisterClient/RegisterPeer */
	EOS_AntiCheatCommon_ClientHandle VictimPlayerHandle;
	/** Victim player's current world position as a 3D vector */
	EOS_AntiCheatCommon_Vec3f* VictimPlayerPosition;
	/** Victim player's view rotation as a quaternion */
	EOS_AntiCheatCommon_Quat* VictimPlayerViewRotation;
	/** Locally unique value used in RegisterClient/RegisterPeer */
	EOS_AntiCheatCommon_ClientHandle AttackerPlayerHandle;
	/** Attacker player's current world position as a 3D vector */
	EOS_AntiCheatCommon_Vec3f* AttackerPlayerPosition;
	/** Attacker player's view rotation as a quaternion */
	EOS_AntiCheatCommon_Quat* AttackerPlayerViewRotation;
	/**
	 * True if the damage was applied instantly at the time of attack from the game
	 * simulation's perspective, otherwise false (simulated ballistics, arrow, etc).
	 */
	EOS_Bool bIsHitscanAttack;
	/**
	 * True if there is a visible line of sight between the attacker and the victim at the time
	 * that damage is being applied, false if there is an obstacle like a wall or terrain in
	 * the way. For some situations like melee or hitscan weapons this is trivially
	 * true, for others like projectiles with simulated physics it may not be e.g. a player
	 * could fire a slow moving projectile and then move behind cover before it strikes.
	 */
	EOS_Bool bHasLineOfSight;
	/** True if this was a critical hit that causes extra damage (e.g. headshot) */
	EOS_Bool bIsCriticalHit;
	/** Identifier of the victim bone hit in this damage event */
	uint32_t HitBoneId;
	/** Number of health points that the victim lost due to this damage event */
	float DamageTaken;
	/** Number of health points that the victim has remaining after this damage event */
	float HealthRemaining;
	/** Source of the damage event */
	EOS_EAntiCheatCommonPlayerTakeDamageSource DamageSource;
	/** Type of the damage being applied */
	EOS_EAntiCheatCommonPlayerTakeDamageType DamageType;
	/** Result of the damage for the victim, if any */
	EOS_EAntiCheatCommonPlayerTakeDamageResult DamageResult;
	/** PlayerUseWeaponData associated with this damage event if available, otherwise NULL */
	EOS_AntiCheatCommon_LogPlayerUseWeaponData* PlayerUseWeaponData;
	/** Time in milliseconds since the PlayerUseWeaponData event occurred if available, otherwise 0 */
	uint32_t TimeSincePlayerUseWeaponMs;
));

#pragma pack(pop)
