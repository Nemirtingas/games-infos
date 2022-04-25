// Copyright Epic Games, Inc. All Rights Reserved.
#pragma once

#pragma pack(push, 8)

/** The most recent version of the EOS_Initialize API. */
#define EOS_INITIALIZE_API_003 3

/**
 * Options for initializing the Epic Online Services SDK.
 */
EOS_STRUCT(EOS_InitializeOptions003, (
	/** API Version: Set this to EOS_INITIALIZE_API_LATEST. */
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
	/** 
	 * This field is for system specific initialization if any.
	 *
	 * If provided then the structure will be located in <System>/eos_<system>.h.
	 * The structure will be named EOS_<System>_InitializeOptions.
	 */
	void* SystemInitializeOptions;
));

#pragma pack(pop)
