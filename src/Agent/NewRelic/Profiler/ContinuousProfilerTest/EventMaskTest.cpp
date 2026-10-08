/*
* Copyright 2020 New Relic Corporation. All rights reserved.
* SPDX-License-Identifier: Apache-2.0
*/
#include "stdafx.h"
#include "CppUnitTest.h"

#include "../Profiler/EventMask.h"

using namespace Microsoft::VisualStudio::CppUnitTestFramework;

namespace NewRelic { namespace Profiler
{
    TEST_CLASS(EventMaskTest)
    {
    public:
        TEST_METHOD(framework_keeps_the_ngen_flags)
        {
            const DWORD mask = BuildEventMask(false, false);
            Assert::IsTrue((mask & COR_PRF_DISABLE_ALL_NGEN_IMAGES) != 0);
            Assert::IsTrue((mask & COR_PRF_USE_PROFILE_IMAGES) != 0);
            Assert::IsTrue((mask & COR_PRF_ENABLE_REJIT) != 0);
        }

        TEST_METHOD(coreclr_leaves_readytorun_enabled)
        {
            const DWORD mask = BuildEventMask(true, false);
            Assert::IsTrue((mask & (COR_PRF_DISABLE_ALL_NGEN_IMAGES | COR_PRF_USE_PROFILE_IMAGES)) == 0);
            Assert::IsTrue((mask & (COR_PRF_MONITOR_JIT_COMPILATION | COR_PRF_MONITOR_MODULE_LOADS | COR_PRF_ENABLE_REJIT)) == (COR_PRF_MONITOR_JIT_COMPILATION | COR_PRF_MONITOR_MODULE_LOADS | COR_PRF_ENABLE_REJIT));
        }

        TEST_METHOD(coreclr_kill_switch_restores_the_framework_mask)
        {
            Assert::IsTrue(BuildEventMask(true, true) == BuildEventMask(false, false));
        }
    };
}}
