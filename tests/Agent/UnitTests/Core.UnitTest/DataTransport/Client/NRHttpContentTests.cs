// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using NewRelic.Agent.Core.WireModels;
using Telerik.JustMock.Helpers;

namespace NewRelic.Agent.Core.DataTransport.Client;

using global::NewRelic.Agent.Configuration;
using NUnit.Framework;
using Telerik.JustMock;

[TestFixture]
public class NRHttpContentTests
{
    private IConfiguration _mockConfiguration;
    private NRHttpContent _nrHttpContent;

    [SetUp]
    public void Setup()
    {
        _mockConfiguration = Mock.Create<IConfiguration>();
        _mockConfiguration.Arrange(c => c.CollectorMaxPayloadSizeInBytes).Returns(Int32.MaxValue);
        _mockConfiguration.Arrange(c => c.CompressedContentEncoding).Returns("gzip");

        _nrHttpContent = new NRHttpContent(_mockConfiguration);

    }

    [Test]
    public void Encoding_InitializesAndReturnsCorrectEncoding_WhenNotInitialized()
    {
        _nrHttpContent.SerializedData = "Test";
        Assert.That(_nrHttpContent.Encoding, Is.EqualTo("identity")); // assuming uncompressed by default
    }

    [Test]
    public void PayloadBytes_InitializesAndReturnsBytes_WhenNotInitialized()
    {
        _nrHttpContent.SerializedData = "Test";
        var expectedBytes = new UTF8Encoding().GetBytes("Test");
        Assert.That(_nrHttpContent.PayloadBytes, Is.EqualTo(expectedBytes).AsCollection);
    }

    [Test]
    public void UncompressedByteCount_InitializesAndReturnsByteCount_WhenNotInitialized()
    {
        _nrHttpContent.SerializedData = "Test";
        var expectedCount = new UTF8Encoding().GetBytes("Test").Length;
        Assert.That(_nrHttpContent.UncompressedByteCount, Is.EqualTo(expectedCount));
    }

    [Test]
    public void IsCompressed_InitializesAndReturnsCompressedStatus_WhenNotInitialized()
    {
        // Assuming a condition where the data would be compressed based on the length
        _nrHttpContent.SerializedData = new string('a', Constants.CompressMinimumByteLength);
        Assert.That(_nrHttpContent.IsCompressed, Is.True);
    }

    [Test]
    public void CompressionType_InitializesAndReturnsCompressionType_WhenNotInitialized()
    {
        // Assuming a condition where the data would be compressed
        _mockConfiguration.Arrange(config => config.CompressedContentEncoding).Returns("gzip");
        _nrHttpContent.SerializedData = new string('a', Constants.CompressMinimumByteLength);
        Assert.That(_nrHttpContent.CompressionType, Is.EqualTo("gzip"));
    }

    [Test]
    public void InitializePayload_ThrowsPayloadSizeExceededException_WhenPayloadSizeExceedsMax()
    {
        _mockConfiguration.Arrange(config => config.CollectorMaxPayloadSizeInBytes).Returns(10);
        _nrHttpContent.SerializedData = new string('a', 11);

        Assert.Throws<PayloadSizeExceededException>(() => { var _ = _nrHttpContent.PayloadBytes; });
    }

    [TestCase("deflate")]
    [TestCase("gzip")]
    public void StreamedPayload_SendsTheSameUncompressedBytesAsTheSerializedString(string compression)
    {
        _mockConfiguration.Arrange(c => c.CompressedContentEncoding).Returns(compression);
        var metric = MetricWireModel.BuildMetric("DotNet/name", "WebTransaction/DotNet/name", MetricDataWireModel.BuildTimingData(TimeSpan.FromSeconds(3.0), TimeSpan.FromSeconds(2.0)));
        var data = new object[] { "run-id", new MetricWireModelCollection("440491846668652", 1450462672.0, 1450462710.0, new List<MetricWireModel> { metric }), new { small = 8.2E-05, text = "\u00e9\ud83d\ude00\ud800", n = Enumerable.Range(0, 5000) } };
        var serializer = new JsonSerializer();
        var fromString = new NRHttpContent(_mockConfiguration) { SerializedData = serializer.Serialize(data) };
        var streamed = new NRHttpContent(_mockConfiguration) { Payload = new SerializedPayload(serializer, data) };

        Assert.That(streamed.IsCompressed, Is.True);
        Assert.That(streamed.Encoding, Is.EqualTo(compression));
        Assert.That(streamed.UncompressedByteCount, Is.EqualTo(fromString.UncompressedByteCount));
        Assert.That(Inflate(streamed.PayloadBytes, compression), Is.EqualTo(Inflate(fromString.PayloadBytes, compression)));
    }

    [Test]
    public void StreamedPayload_ShorterThanTheCompressionMinimum_IsSentUncompressed()
    {
        var data = new object[] { 1 };
        var streamed = new NRHttpContent(_mockConfiguration) { Payload = new SerializedPayload(new JsonSerializer(), data) };

        Assert.That(streamed.IsCompressed, Is.False);
        Assert.That(streamed.PayloadBytes, Is.EqualTo(Encoding.UTF8.GetBytes("[1]")));
        Assert.That(streamed.UncompressedByteCount, Is.EqualTo(3));
    }

    private static byte[] Inflate(byte[] payload, string compression)
    {
        using var input = compression == "gzip" ? new MemoryStream(payload) : new MemoryStream(payload, 2, payload.Length - 2);
        using Stream inflater = compression == "gzip" ? new GZipStream(input, CompressionMode.Decompress) : new DeflateStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        inflater.CopyTo(output);
        return output.ToArray();
    }
}
