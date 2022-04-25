// Copyright Epic Games, Inc. All Rights Reserved.
#pragma once

#include "eos_common.h"

enum { k_iModsCallbackBase = 17000 };
// next free callback_id: k_iModsCallbackBase + 0

#define EOS_Mod_Identifier            EOS_Mod_Identifier001
#define EOS_Mods_InstallModOptions    EOS_Mods_InstallModOptions001
#define EOS_Mods_UninstallModOptions  EOS_Mods_UninstallModOptions001
#define EOS_Mods_EnumerateModsOptions EOS_Mods_EnumerateModsOptions001
#define EOS_Mods_CopyModInfoOptions   EOS_Mods_CopyModInfoOptions001
#define EOS_Mods_ModInfo              EOS_Mods_ModInfo001
#define EOS_Mods_UpdateModOptions     EOS_Mods_UpdateModOptions001

#include "eos_mods_types1.14.0.h"

#define EOS_MOD_IDENTIFIER_API_LATEST     EOS_MOD_IDENTIFIER_API_001
#define EOS_MODS_INSTALLMOD_API_LATEST    EOS_MODS_INSTALLMOD_API_001
#define EOS_MODS_UNINSTALLMOD_API_LATEST  EOS_MODS_UNINSTALLMOD_API_001
#define EOS_MODS_ENUMERATEMODS_API_LATEST EOS_MODS_ENUMERATEMODS_API_001
#define EOS_MODS_COPYMODINFO_API_LATEST   EOS_MODS_COPYMODINFO_API_001
#define EOS_MODS_MODINFO_API_LATEST       EOS_MODS_MODINFO_API_001
#define EOS_MODS_UPDATEMOD_API_LATEST     EOS_MODS_UPDATEMOD_API_001