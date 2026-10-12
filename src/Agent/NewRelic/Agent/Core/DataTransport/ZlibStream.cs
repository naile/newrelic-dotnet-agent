// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using System.IO.Compression;

namespace NewRelic.Agent.Core.DataTransport;

/// <summary>
/// Writes the zlib format (RFC 1950) that HTTP "deflate" means: a 2-byte header, the raw deflate data that DeflateStream
/// produces (RFC 1951), and an Adler-32 trailer. ZLibStream does the same but needs .NET 6.
/// </summary>
public class ZlibStream : Stream
{
    private const uint AdlerModulus = 65521;
    private const int AdlerBlock = 5552;
    private readonly Stream _output;
    private readonly DeflateStream _deflate;
    private uint _a = 1;
    private uint _b;

    public ZlibStream(Stream output)
    {
        _output = output;
        _output.WriteByte(0x78);
        _output.WriteByte(0x9C);
        _deflate = new DeflateStream(output, CompressionLevel.Optimal, true);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        _deflate.Write(buffer, offset, count);
        while (count > 0)
        {
            var end = offset + Math.Min(count, AdlerBlock);
            count -= end - offset;
            for (; offset < end; offset++)
            {
                _a += buffer[offset];
                _b += _a;
            }
            _a %= AdlerModulus;
            _b %= AdlerModulus;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _deflate.Dispose();
            var adler = (_b << 16) | _a;
            _output.WriteByte((byte)(adler >> 24));
            _output.WriteByte((byte)(adler >> 16));
            _output.WriteByte((byte)(adler >> 8));
            _output.WriteByte((byte)adler);
        }
        base.Dispose(disposing);
    }

    public override void Flush() => _deflate.Flush();
    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
}
