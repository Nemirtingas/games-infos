// Copyright Epic Games, Inc. All Rights Reserved.

#pragma once

#include "eos_common.h"

enum { k_iMetricsCallbackBase = 8000 };
// next free callback_id: k_iMetricsCallbackBase + 0

#define EOS_Metrics_BeginPlayerSessionOptions EOS_Metrics_BeginPlayerSessionOptions001
#define EOS_Metrics_EndPlayerSessionOptions   EOS_Metrics_EndPlayerSessionOptions001

#include "eos_metrics_types1.14.0.h"

#define EOS_METRICS_BEGINPLAYERSESSION_API_LATEST EOS_METRICS_BEGINPLAYERSESSION_API_001
#define EOS_METRICS_ENDPLAYERSESSION_API_LATEST   EOS_METRICS_ENDPLAYERSESSION_API_001