// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0

using System.IO;
using Newtonsoft.Json;

namespace NewRelic.Agent.Core.DataTransport;

public class JsonSerializer : ISerializer
{
    public string Serialize(object[] parameters)
    {
        return JsonConvert.SerializeObject(parameters);
    }

    public void Serialize(object[] parameters, TextWriter writer)
    {
        using (var jsonWriter = new JsonTextWriter(writer) { CloseOutput = false })
            Newtonsoft.Json.JsonSerializer.CreateDefault().Serialize(jsonWriter, parameters);
    }

    public T Deserialize<T>(string responseBody)
    {
        return JsonConvert.DeserializeObject<T>(responseBody);
    }
}