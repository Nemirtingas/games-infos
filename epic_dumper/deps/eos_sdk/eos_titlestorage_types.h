// Copyright Epic Games, Inc. All Rights Reserved.

#pragma once

#include "eos_common.h"

enum { k_iTitleStorageCallbackBase = 14000 };
// next free callback_id: k_iTitleStorageCallbackBase + 6

#define EOS_TitleStorage_FileMetadata                      EOS_TitleStorage_FileMetadata002
#define EOS_TitleStorage_QueryFileOptions                  EOS_TitleStorage_QueryFileOptions001
#define EOS_TitleStorage_QueryFileListOptions              EOS_TitleStorage_QueryFileListOptions001
#define EOS_TitleStorage_GetFileMetadataCountOptions       EOS_TitleStorage_GetFileMetadataCountOptions001
#define EOS_TitleStorage_CopyFileMetadataAtIndexOptions    EOS_TitleStorage_CopyFileMetadataAtIndexOptions001
#define EOS_TitleStorage_CopyFileMetadataByFilenameOptions EOS_TitleStorage_CopyFileMetadataByFilenameOptions001
#define EOS_TitleStorage_ReadFileOptions                   EOS_TitleStorage_ReadFileOptions001
#define EOS_TitleStorage_DeleteCacheOptions                EOS_TitleStorage_DeleteCacheOptions001

#include <eos_titlestorage_types1.11.0.h>
#include <eos_titlestorage_types1.10.0.h>

#define EOS_TITLESTORAGE_FILEMETADATA_API_LATEST                      EOS_TITLESTORAGE_FILEMETADATA_API_002
#define EOS_TITLESTORAGE_QUERYFILEOPTIONS_API_LATEST                  EOS_TITLESTORAGE_QUERYFILEOPTIONS_API_001
#define EOS_TITLESTORAGE_QUERYFILELISTOPTIONS_API_LATEST              EOS_TITLESTORAGE_QUERYFILELISTOPTIONS_API_001
#define EOS_TITLESTORAGE_GETFILEMETADATACOUNTOPTIONS_API_LATEST       EOS_TITLESTORAGE_GETFILEMETADATACOUNTOPTIONS_API_001
#define EOS_TITLESTORAGE_COPYFILEMETADATAATINDEXOPTIONS_API_LATEST    EOS_TITLESTORAGE_COPYFILEMETADATAATINDEXOPTIONS_API_001
#define EOS_TITLESTORAGE_COPYFILEMETADATABYFILENAMEOPTIONS_API_LATEST EOS_TITLESTORAGE_COPYFILEMETADATABYFILENAMEOPTIONS_API_001
#define EOS_TITLESTORAGE_READFILEOPTIONS_API_LATEST                   EOS_TITLESTORAGE_READFILEOPTIONS_API_001
#define EOS_TITLESTORAGE_DELETECACHEOPTIONS_API_LATEST                EOS_TITLESTORAGE_DELETECACHEOPTIONS_API_001
