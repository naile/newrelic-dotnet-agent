// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using NUnit.Framework;

namespace NewRelic.Agent.Core.DataTransport;

[TestFixture]
public class DataCompressorTests
{
    [Test]
    [TestCase("")]
    [TestCase("foo")]
    [TestCase("Զԣש", Description = "Some UTF-8 characters")]
    public void DeflateCompressedDataShouldDecompressToSameValue(string input)
    {
        var compressed = DataCompressor.Compress(input);
        var decompressed = DataCompressor.Decompress(compressed);

        Assert.That(decompressed, Is.EqualTo(input));
    }

    [Test]
    [TestCase("")]
    [TestCase("foo")]
    [TestCase("Զԣש", Description = "Some UTF-8 characters")]
    public void GZipCompressedDataShouldDecompressToSameValue(string input)
    {
        var compressed = DataCompressor.Compress(new UTF8Encoding().GetBytes(input), DataCompressor.GzipCompression);
        var decompressed = DecompressGzip(compressed);
        Assert.That(decompressed, Is.EqualTo(input));
    }

    [Test]
    public void DefaultCompressionShouldBeDeflate()
    {
        const string input = "input";
        var defaultCompression = DataCompressor.Compress(input);
        var explicitCompression = DataCompressor.Compress(new UTF8Encoding().GetBytes(input), DataCompressor.DeflateCompression);
        Assert.That(explicitCompression, Is.EqualTo(defaultCompression));
    }

    [Test]
    public void ShouldThrowWhenInvalidCompressionType()
    {
        const string input = "input";
        const string invalidCompression = "invalidType";

        Assert.Throws<ArgumentException>(() => DataCompressor.Compress(new UTF8Encoding().GetBytes(input), invalidCompression));
    }

    [Test]
    [TestCase("DEFLATE")]
    [TestCase("GZIP")]
    public void ShouldHandleInconsistentCompressionCasing(string compressionType)
    {
        const string input = "input";
        Assert.DoesNotThrow(() => DataCompressor.Compress(new UTF8Encoding().GetBytes(input), compressionType));
    }

    [Test]
    public void DeflateCompressionShouldMatchANewDeflaterWhenCalledRepeatedly()
    {
        var previous = new UTF8Encoding().GetBytes(new string('a', 100000));
        var input = new UTF8Encoding().GetBytes("{\"some\":\"payload\"}");
        DataCompressor.Compress(previous);

        Assert.That(DataCompressor.Compress(input), Is.EqualTo(CompressWithNewDeflater(input)));
    }

    [Test]
    public void ConcurrentDeflateCompressionShouldDecompressToSameValues()
    {
        var inputs = Enumerable.Range(0, 64).Select(i => new string((char)('a' + i % 26), 1000 + i)).ToArray();
        var results = new string[inputs.Length];

        Parallel.For(0, inputs.Length, i => results[i] = DataCompressor.Decompress(DataCompressor.Compress(inputs[i])));

        Assert.That(results, Is.EqualTo(inputs));
    }

#if !NETFRAMEWORK
    [Test]
    public void RepeatedDeflateCompressionShouldNotAllocateANewDeflater()
    {
        var input = new UTF8Encoding().GetBytes("input");
        DataCompressor.Compress(input);

        var before = GC.GetAllocatedBytesForCurrentThread();
        DataCompressor.Compress(input);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.That(allocated, Is.LessThan(64 * 1024));
    }
#endif

    private static byte[] CompressWithNewDeflater(byte[] bytes)
    {
        using (var stream = new MemoryStream())
        using (var outputStream = new DeflaterOutputStream(stream, new Deflater(Deflater.DEFAULT_COMPRESSION)))
        {
            outputStream.Write(bytes, 0, bytes.Length);
            outputStream.Flush();
            outputStream.Finish();
            return stream.ToArray();
        }
    }

    private static string DecompressGzip(byte[] compressedBytes)
    {
        using (var memoryStream = new MemoryStream())
        using (var inflaterStream = new GZipInputStream(memoryStream))
        using (var streamReader = new StreamReader(inflaterStream, Encoding.UTF8))
        {
            memoryStream.Write(compressedBytes, 0, compressedBytes.Length);
            memoryStream.Flush();
            memoryStream.Position = 0;
            return streamReader.ReadToEnd();
        }
    }
}