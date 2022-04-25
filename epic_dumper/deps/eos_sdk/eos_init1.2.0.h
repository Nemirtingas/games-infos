// Copyright 1998-2019 Epic Games, Inc. All Rights Reserved.
#pragma once

#pragma pack(push, 8)

/** The most recent version of the EOS_Initialize API. */
#define EOS_INITIALIZE_API_002 2

/**
 * Options for initializing the Epic Online Services SDK.
 */
EOS_STRUCT(EOS_InitializeOptions002, (
	/** API version of EOS_Initialize. */
	int32_t ApiVersion;
	/** A custom memory allocator, if desired. */
	EOS_AllocateMemoryFunc AllocateMemoryFunction;
	/** A corresponding memory reallocator. If the AllocateMemoryFunction is nulled, then this field must also be nulled. */
	EOS_ReallocateMemoryFunc ReallocateMemoryFunction;
	/** A corresponding memory releaser. If the AllocateMemoryFunction is nulled, then this field must also be nulled. */
	EOS_ReleaseMemoryFunc ReleaseMemoryFunction;
	/**
	 * The name of the product using the Epic Online Services SDK.
	 *
	 * The name string is required to be non-empty and at maximum of 64 characters long.
	 * The string buffer can consist of the following characters:
	 * A-Z, a-z, 0-9, dot, underscore, space, exclamation mark, question mark, and sign, hyphen, parenthesis, plus, minus, colon.
	 */
	const char* ProductName;
	/**
	 * Product version of the running application.
	 *
	 * The name string has same requirements as the ProductName string.
	 */
	const char* ProductVersion;
	/** A reserved field that should always be nulled. */
	void* Reserved;
));

#pragma pack(pop)
