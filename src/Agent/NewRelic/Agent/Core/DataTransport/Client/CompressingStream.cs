// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;

namespace NewRelic.Agent.Core.DataTransport.Client;

/// <summary>
/// Compresses what is written into <c>output</c> and counts the uncompressed bytes. The first <c>minimumLength</c> bytes are
/// held back, so a payload shorter than that is not compressed (UncompressedBytes then holds it).
/// </summary>
public class CompressingStream : Stream
{
    private readonly Stream _output;
    private readonly string _compressionType;
    private readonly int _minimumLength;
    private MemoryStream _head = new MemoryStream();
    private Stream _compressor;

    public CompressingStream(Stream output, string compressionType, int minimumLength)
    {
        _output = output;
        _compressionType = compressionType;
        _minimumLength = minimumLength;
    }

    public long UncompressedByteCount { get; private set; }
    public bool IsCompressed => _compressor != null;
    public byte[] UncompressedBytes => _head?.ToArray();

    public override void Write(byte[] buffer, int offset, int count)
    {
        UncompressedByteCount += count;
        if (_compressor != null)
        {
            _compressor.Write(buffer, offset, count);
            return;
        }

        _head.Write(buffer, offset, count);
        if (_head.Length < _minimumLength)
            return;

        _compressor = DataCompressor.CreateCompressionStream(_output, _compressionType);
        _head.WriteTo(_compressor);
        _head = null;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _compressor?.Dispose();
        base.Dispose(disposing);
    }

    public override void Flush() { }
    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
}
