// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace NewRelic.Agent.Core.DataTransport;

public static class DataCompressor
{
    public const string DeflateCompression = "deflate";
    public const string GzipCompression = "gzip";

    public static byte[] Compress(string data)
    {
        var bytes = new UTF8Encoding().GetBytes(data);
        return Compress(bytes);
    }

    public static byte[] Compress(byte[] bytes)
    {
        return Compress(bytes, DeflateCompression);
    }

    public static byte[] Compress(byte[] bytes, string compressionType)
    {
        using (var stream = new MemoryStream())
        {
            using (var outputStream = CreateCompressionStream(stream, compressionType))
                outputStream.Write(bytes, 0, bytes.Length);
            return stream.ToArray();
        }
    }

    public static Stream CreateCompressionStream(Stream output, string requestedCompression)
    {
        var compressionType = requestedCompression.ToLower();
        switch (compressionType)
        {
            case DeflateCompression:
                return new ZlibStream(output);
            case GzipCompression:
                return new GZipStream(output, CompressionLevel.Optimal, true);
            default:
                throw new ArgumentException($"compressionType is not one of the valid options: {compressionType}");
        }
    }

    public static string Decompress(byte[] compressedBytes)
    {
        using (var memoryStream = new MemoryStream(compressedBytes, 2, compressedBytes.Length - 2))
        using (var deflateStream = new DeflateStream(memoryStream, CompressionMode.Decompress))
        using (var streamReader = new StreamReader(deflateStream, Encoding.UTF8))
        {
            return streamReader.ReadToEnd();
        }
    }
}
