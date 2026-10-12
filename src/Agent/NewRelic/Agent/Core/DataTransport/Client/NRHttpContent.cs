// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.IO;
using System.Text;
using NewRelic.Agent.Configuration;
using NewRelic.Agent.Core.DataTransport.Client.Interfaces;

namespace NewRelic.Agent.Core.DataTransport.Client;

/// <summary>
/// Abstraction of content sent in client requests
/// </summary>
public class NRHttpContent : IHttpContent
{
    private readonly IConfiguration _configuration;
    private CollectorRequestPayload _collectorRequestPayload;

    private bool _payloadInitialized;
    private long _uncompressedByteCount;

    public NRHttpContent(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string ContentType { get; set; }

    public string Encoding
    {
        get
        {
            if (!_payloadInitialized)
            {
                InitializePayload();
            }

            return _collectorRequestPayload.IsCompressed
                ? _collectorRequestPayload.CompressionType.ToLower()
                : "identity";
        }
    }

    public string SerializedData
    {
        get => Payload?.ToString();
        set => Payload = new SerializedPayload(value);
    }

    public SerializedPayload Payload { get; set; }

    public byte[] PayloadBytes
    {
        get
        {
            if (!_payloadInitialized)
            {
                InitializePayload();
            }

            return _collectorRequestPayload.Data;
        }
    }

    public long UncompressedByteCount
    {
        get
        {
            if (!_payloadInitialized)
            {
                InitializePayload();
            }

            return _uncompressedByteCount;
        }
    }

    public List<KeyValuePair<string, string>> Headers { get; } = new List<KeyValuePair<string, string>>();

    public bool IsCompressed
    {
        get
        {
            if (!_payloadInitialized)
            {
                InitializePayload();
            }

            return _collectorRequestPayload.IsCompressed;
        }
    }

    public string CompressionType
    {
        get
        {
            if (!_payloadInitialized)
            {
                InitializePayload();
            }

            return _collectorRequestPayload.CompressionType;
        }
    }

    private void InitializePayload()
    {
        if (_payloadInitialized)
        {
            return;
        }

        if (Payload.IsMaterialized)
        {
            var bytes = new UTF8Encoding().GetBytes(Payload.ToString());
            _uncompressedByteCount = bytes.Length;
            _collectorRequestPayload = GetRequestPayload(bytes);
        }
        else
        {
            _collectorRequestPayload = StreamRequestPayload();
        }

        _payloadInitialized = true;

        if (_collectorRequestPayload.Data.Length > _configuration.CollectorMaxPayloadSizeInBytes)
        {
            throw new PayloadSizeExceededException();
        }
    }

    private CollectorRequestPayload StreamRequestPayload()
    {
        var compressionType = _configuration.CompressedContentEncoding;
        using (var output = new MemoryStream())
        {
            var stream = new CompressingStream(output, compressionType, Constants.CompressMinimumByteLength);
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                Payload.WriteTo(writer);
            _uncompressedByteCount = stream.UncompressedByteCount;
            return stream.IsCompressed
                ? new CollectorRequestPayload(true, compressionType, output.ToArray())
                : new CollectorRequestPayload(false, null, stream.UncompressedBytes);
        }
    }

    private CollectorRequestPayload GetRequestPayload(byte[] bytes)
    {
        var shouldCompress = bytes.Length >= Constants.CompressMinimumByteLength;

        string compressionType = null;
        if (shouldCompress)
        {
            compressionType = _configuration.CompressedContentEncoding;
            bytes = DataCompressor.Compress(bytes, compressionType);
        }

        var payload = new CollectorRequestPayload(shouldCompress, compressionType, bytes);

        return payload;
    }
}