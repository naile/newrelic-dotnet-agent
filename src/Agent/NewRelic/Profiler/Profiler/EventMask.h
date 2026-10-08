// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0

#pragma once
#include <corprof.h>

namespace NewRelic { namespace Profiler
{
    // The NGEN flags are only meaningful on .NET Framework; CoreCLR treats either of them as "ignore all ReadyToRun images".
    inline DWORD BuildEventMask(bool isCoreClr, bool readyToRunDisabled)
    {
        DWORD eventMask = COR_PRF_MONITOR_JIT_COMPILATION | COR_PRF_MONITOR_MODULE_LOADS | COR_PRF_MONITOR_THREADS | COR_PRF_ENABLE_STACK_SNAPSHOT | COR_PRF_ENABLE_REJIT;
        if (!isCoreClr || readyToRunDisabled) eventMask |= COR_PRF_USE_PROFILE_IMAGES | COR_PRF_DISABLE_ALL_NGEN_IMAGES;
        return eventMask;
    }
}}
