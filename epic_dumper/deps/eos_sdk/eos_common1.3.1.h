// Copyright Epic Games, Inc. All Rights Reserved.

#pragma once

#include "eos_base.h"

#pragma pack(push, 8)

/** The most recent version of the EOS_DeviceInfo struct. */
#define EOS_AUTH_DEVICEINFO_API_001 1

EOS_STRUCT(EOS_DeviceInfo001, (
	/** Version of the API */
	int32_t ApiVersion;
	/** Type of the device */
	const char* Type;
	/** Model of the device */
	const char* Model;
	/** OS of the device */
	const char* OS;
));

#pragma pack(pop)
