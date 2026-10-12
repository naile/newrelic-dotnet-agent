// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;

namespace NewRelic.Agent.Core.DataTransport;

/// <summary>
/// A collector request body that is serialized straight into the compressed request bytes. The JSON string is built only
/// when something needs it (debug and audit logging, error logging); ToString() returns it.
/// </summary>
public class SerializedPayload
{
    private readonly ISerializer _serializer;
    private readonly object[] _data;
    private string _json;

    public SerializedPayload(ISerializer serializer, object[] data)
    {
        _serializer = serializer;
        _data = data;
    }

    public SerializedPayload(string json)
    {
        _json = json;
    }

    public bool IsMaterialized => _json != null;

    public void WriteTo(TextWriter writer)
    {
        if (_json != null)
            writer.Write(_json);
        else
            _serializer.Serialize(_data, writer);
    }

    public override string ToString()
    {
        try
        {
            return _json ??= _serializer.Serialize(_data);
        }
        catch (Exception ex)
        {
            return $"<payload could not be serialized: {ex.Message}>";
        }
    }
}
