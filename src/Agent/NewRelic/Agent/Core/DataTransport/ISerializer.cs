// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0

using System.IO;

namespace NewRelic.Agent.Core.DataTransport;

public interface ISerializer
{
    string Serialize(object[] parameters);
    void Serialize(object[] parameters, TextWriter writer);
    T Deserialize<T>(string responseBody);
}