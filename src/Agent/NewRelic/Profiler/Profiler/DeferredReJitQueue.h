// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0

#pragma once
#include <chrono>
#include <condition_variable>
#include <cor.h>
#include <corprof.h>
#include <functional>
#include <map>
#include <memory>
#include <mutex>
#include <set>
#include <thread>

namespace NewRelic { namespace Profiler
{
    // The runtime only re-JITs precompiled inliners in assemblies that have finished loading, which excludes the requesting
    // module's own assembly while ModuleLoadFinished is being delivered, so precompiled modules are requested again after a delay.
    class DeferredReJitQueue
    {
    public:
        typedef std::shared_ptr<std::set<mdMethodDef>> MethodDefs;
        typedef std::function<void(ModuleID, MethodDefs)> RequestFunction;

        DeferredReJitQueue(RequestFunction request, std::chrono::milliseconds delay) : _request(request), _delay(delay) {}

        ~DeferredReJitQueue()
        {
            Shutdown();
            if (_worker.joinable()) _worker.join();
        }

        void Add(ModuleID moduleId, MethodDefs methodDefs)
        {
            std::lock_guard<std::mutex> lock(_mutex);
            _pending[moduleId] = methodDefs;
            if (!_worker.joinable()) _worker = std::thread([this] { Run(); });
            _signal.notify_one();
        }

        void Remove(ModuleID moduleId)
        {
            std::lock_guard<std::mutex> lock(_mutex);
            _pending.erase(moduleId);
        }

        void Shutdown()
        {
            std::lock_guard<std::mutex> lock(_mutex);
            _shuttingDown = true;
            _signal.notify_all();
        }

    private:
        void Run()
        {
            for (;;)
            {
                std::map<ModuleID, MethodDefs> batch;
                {
                    std::unique_lock<std::mutex> lock(_mutex);
                    _signal.wait(lock, [this] { return !_pending.empty() || _shuttingDown; });
                    if (_shuttingDown) return;
                }
                std::this_thread::sleep_for(_delay);
                {
                    std::lock_guard<std::mutex> lock(_mutex);
                    if (_shuttingDown) return;
                    batch.swap(_pending);
                }
                for (auto& pending : batch) _request(pending.first, pending.second);
            }
        }

        RequestFunction _request;
        std::chrono::milliseconds _delay;
        std::mutex _mutex;
        std::condition_variable _signal;
        std::map<ModuleID, MethodDefs> _pending;
        std::thread _worker;
        bool _shuttingDown = false;
    };
}}
