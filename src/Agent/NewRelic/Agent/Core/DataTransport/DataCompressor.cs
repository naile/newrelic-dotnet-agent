// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using System.Text;
using System.Threading;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;

namespace NewRelic.Agent.Core.DataTransport;

public static class DataCompressor
{
    public const string DeflateCompression = "deflate";
    public const string GzipCompression = "gzip";

    private static Deflater _cachedDeflater;

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
        var compression = compressionType.ToLower();
        var deflater = compression == DeflateCompression ? Interlocked.Exchange(ref _cachedDeflater, null) ?? new Deflater(Deflater.DEFAULT_COMPRESSION) : null;
        try
        {
            using (var stream = new MemoryStream())
            using (var outputStream = GetCompressionOutputStream(stream, compression, deflater))
            {
                outputStream.Write(bytes, 0, bytes.Length);
                outputStream.Flush();
                outputStream.Finish();
                return stream.ToArray();
            }
        }
        finally
        {
            if (deflater != null)
            {
                deflater.Reset();
                Volatile.Write(ref _cachedDeflater, deflater);
            }
        }
    }

    private static DeflaterOutputStream GetCompressionOutputStream(Stream stream, string compressionType, Deflater deflater)
    {
        switch (compressionType)
        {
            case DeflateCompression:
                return new DeflaterOutputStream(stream, deflater);
            case GzipCompression:
                return new GZipOutputStream(stream);
            default:
                throw new ArgumentException($"compressionType is not one of the valid options: {compressionType}");
        }
    }

    public static string Decompress(byte[] compressedBytes)
    {
        using (var memoryStream = new MemoryStream())
        using (var inflaterStream = new InflaterInputStream(memoryStream, new Inflater()))
        using (var streamReader = new StreamReader(inflaterStream, Encoding.UTF8))
        {
            memoryStream.Write(compressedBytes, 0, compressedBytes.Length);
            memoryStream.Flush();
            memoryStream.Position = 0;
            return streamReader.ReadToEnd();
        }
    }
}