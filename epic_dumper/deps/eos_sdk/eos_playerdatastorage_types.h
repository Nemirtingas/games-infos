// Copyright Epic Games, Inc. All Rights Reserved.

#pragma once

#include "eos_common.h"

enum { k_iPlayerDataStorageCallbackBase = 10000 };
// next free callback_id: k_iPlayerDataStorageCallbackBase + 10

#define EOS_PlayerDataStorage_FileMetadata                      EOS_PlayerDataStorage_FileMetadata003
#define EOS_PlayerDataStorage_QueryFileOptions                  EOS_PlayerDataStorage_QueryFileOptions001
#define EOS_PlayerDataStorage_QueryFileListOptions              EOS_PlayerDataStorage_QueryFileListOptions001
#define EOS_PlayerDataStorage_GetFileMetadataCountOptions       EOS_PlayerDataStorage_GetFileMetadataCountOptions001
#define EOS_PlayerDataStorage_CopyFileMetadataAtIndexOptions    EOS_PlayerDataStorage_CopyFileMetadataAtIndexOptions001
#define EOS_PlayerDataStorage_CopyFileMetadataByFilenameOptions EOS_PlayerDataStorage_CopyFileMetadataByFilenameOptions001
#define EOS_PlayerDataStorage_DuplicateFileOptions              EOS_PlayerDataStorage_DuplicateFileOptions001
#define EOS_PlayerDataStorage_DeleteFileOptions                 EOS_PlayerDataStorage_DeleteFileOptions001
#define EOS_PlayerDataStorage_ReadFileOptions                   EOS_PlayerDataStorage_ReadFileOptions001
#define EOS_PlayerDataStorage_WriteFileOptions                  EOS_PlayerDataStorage_WriteFileOptions001
#define EOS_PlayerDataStorage_DeleteCacheOptions                EOS_PlayerDataStorage_DeleteCacheOptions001

#include <eos_playerdatastorage_types1.11.0.h>
#include <eos_playerdatastorage_types1.10.0.h>

#define EOS_PLAYERDATASTORAGE_FILEMETADATA_API_LATEST                      EOS_PLAYERDATASTORAGE_FILEMETADATA_API_003
#define EOS_PLAYERDATASTORAGE_QUERYFILEOPTIONS_API_LATEST                  EOS_PLAYERDATASTORAGE_QUERYFILEOPTIONS_API_001
#define EOS_PLAYERDATASTORAGE_QUERYFILELISTOPTIONS_API_LATEST              EOS_PLAYERDATASTORAGE_QUERYFILELISTOPTIONS_API_001
#define EOS_PLAYERDATASTORAGE_GETFILEMETADATACOUNTOPTIONS_API_LATEST       EOS_PLAYERDATASTORAGE_GETFILEMETADATACOUNTOPTIONS_API_001
#define EOS_PLAYERDATASTORAGE_COPYFILEMETADATAATINDEXOPTIONS_API_LATEST    EOS_PLAYERDATASTORAGE_COPYFILEMETADATAATINDEXOPTIONS_API_001
#define EOS_PLAYERDATASTORAGE_COPYFILEMETADATABYFILENAMEOPTIONS_API_LATEST EOS_PLAYERDATASTORAGE_COPYFILEMETADATABYFILENAMEOPTIONS_API_001
#define EOS_PLAYERDATASTORAGE_DUPLICATEFILEOPTIONS_API_LATEST              EOS_PLAYERDATASTORAGE_DUPLICATEFILEOPTIONS_API_001
#define EOS_PLAYERDATASTORAGE_DELETEFILEOPTIONS_API_LATEST                 EOS_PLAYERDATASTORAGE_DELETEFILEOPTIONS_API_001
#define EOS_PLAYERDATASTORAGE_READFILEOPTIONS_API_LATEST                   EOS_PLAYERDATASTORAGE_READFILEOPTIONS_API_001
#define EOS_PLAYERDATASTORAGE_WRITEFILEOPTIONS_API_LATEST                  EOS_PLAYERDATASTORAGE_WRITEFILEOPTIONS_API_001
#define EOS_PLAYERDATASTORAGE_DELETECACHEOPTIONS_API_LATEST                EOS_PLAYERDATASTORAGE_DELETECACHEOPTIONS_API_001
