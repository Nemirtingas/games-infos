// Copyright Epic Games, Inc. All Rights Reserved.

#pragma once

#pragma pack(push, 8)

/** The most recent version of the EOS_TitleStorage_FileMetadata API. */
#define EOS_TITLESTORAGE_FILEMETADATA_API_001 1

/**
 * Metadata information for a specific file
 */
EOS_STRUCT(EOS_TitleStorage_FileMetadata001, (
	/** API Version: Set this to EOS_TITLESTORAGE_FILEMETADATA_API_LATEST. */
	int32_t ApiVersion;
	/** The total size of the file in bytes (Includes file header in addition to file contents). */
	uint32_t FileSizeBytes;
	/** The MD5 Hash of the entire file (including additional file header), in hex digits */
	const char* MD5Hash;
	/** The file's name */
	const char* Filename;
));

#pragma pack(pop)
