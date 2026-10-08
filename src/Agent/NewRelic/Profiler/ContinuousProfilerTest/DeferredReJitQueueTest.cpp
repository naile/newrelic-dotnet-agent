/*
* Copyright 2020 New Relic Corporation. All rights reserved.
* SPDX-License-Identifier: Apache-2.0
*/
#include "stdafx.h"
#include "CppUnitTest.h"

#include <atomic>
#include <chrono>
#include <memory>
#include <set>
#include <thread>

#include "../Profiler/DeferredReJitQueue.h"

using namespace Microsoft::VisualStudio::CppUnitTestFramework;

namespace NewRelic { namespace Profiler
{
    TEST_CLASS(DeferredReJitQueueTest)
    {
    public:
        TEST_METHOD(requests_each_module_once_after_the_delay)
        {
            std::atomic<int> requests(0);
            std::atomic<int> methods(0);
            DeferredReJitQueue queue([&](ModuleID, DeferredReJitQueue::MethodDefs methodDefs) { requests++; methods += (int)methodDefs->size(); }, std::chrono::milliseconds(50));
            auto methodDefs = std::make_shared<std::set<mdMethodDef>>();
            methodDefs->insert(0x06000001);
            methodDefs->insert(0x06000002);
            queue.Add(1, methodDefs);
            queue.Add(1, methodDefs);
            std::this_thread::sleep_for(std::chrono::milliseconds(400));
            Assert::AreEqual(1, requests.load());
            Assert::AreEqual(2, methods.load());
        }

        TEST_METHOD(removed_modules_are_not_requested)
        {
            std::atomic<int> requests(0);
            DeferredReJitQueue queue([&](ModuleID, DeferredReJitQueue::MethodDefs) { requests++; }, std::chrono::milliseconds(50));
            queue.Add(1, std::make_shared<std::set<mdMethodDef>>());
            queue.Remove(1);
            std::this_thread::sleep_for(std::chrono::milliseconds(400));
            Assert::AreEqual(0, requests.load());
        }

        TEST_METHOD(shutdown_drops_pending_requests)
        {
            std::atomic<int> requests(0);
            DeferredReJitQueue queue([&](ModuleID, DeferredReJitQueue::MethodDefs) { requests++; }, std::chrono::milliseconds(200));
            queue.Add(1, std::make_shared<std::set<mdMethodDef>>());
            queue.Shutdown();
            std::this_thread::sleep_for(std::chrono::milliseconds(500));
            Assert::AreEqual(0, requests.load());
        }
    };
}}
