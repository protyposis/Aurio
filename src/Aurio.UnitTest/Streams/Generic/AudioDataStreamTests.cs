using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Aurio.Streams;
using Aurio.Streams.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aurio.UnitTest.Streams.Generic;

[TestClass]
public class AudioDataStreamTests
{
    [TestMethod]
    public void CanReadSeekWriteFlags()
    {
        var samples = new float[] { 0.1f, -0.2f, 0.3f, -0.4f };
        using var src = new MemorySourceStream(samples, 44100, 1);
        using var stream = new AudioDataStream(src);

        Assert.IsTrue(stream.CanRead);
        Assert.IsTrue(stream.CanSeek);
        Assert.IsFalse(stream.CanWrite);
    }

    [TestMethod]
    public void LengthAndPositionExposeUnderlyingStream()
    {
        var samples = new float[] { 0.1f, 0.2f, 0.3f, 0.4f };
        using var src = new MemorySourceStream(samples, 48000, 1);
        using var stream = new AudioDataStream(src);

        Assert.AreEqual(src.Length, stream.Length);
        Assert.AreEqual(0L, stream.Position);

        stream.Position = 4; // advance by one float sample (4 bytes)
        Assert.AreEqual(4L, stream.Position);
        Assert.AreEqual(4L, src.Position);
    }

    [TestMethod]
    public void ReadPassesThroughData()
    {
        // Prepare 4 float samples => 16 bytes
        var input = new float[] { 0.5f, -0.25f, 0.75f, -1.0f };
        var inputBytes = MemoryMarshal.AsBytes(input.AsSpan()).ToArray();
        using var src = new MemorySourceStream(input, 44100, 1);
        using var stream = new AudioDataStream(src);

        var buffer = new byte[16];
        var read = stream.Read(buffer, 0, buffer.Length);

        CollectionAssert.AreEqual(inputBytes, buffer);
    }

    [TestMethod]
    public void SeekChangesPositionCorrectly()
    {
        using var sine = new SineGeneratorStream(44100, 440, TimeSpan.FromSeconds(1));
        using var stream = new AudioDataStream(sine);

        // Seek from begin
        var pos = stream.Seek(100, SeekOrigin.Begin);
        Assert.AreEqual(100L, pos);
        Assert.AreEqual(100L, stream.Position);

        // Seek relative to current
        pos = stream.Seek(50, SeekOrigin.Current);
        Assert.AreEqual(150L, pos);

        // Seek relative to end (negative offset)
        pos = stream.Seek(-200, SeekOrigin.End);
        Assert.AreEqual(sine.Length - 200, pos);
    }

    [TestMethod]
    [ExpectedException(typeof(NotImplementedException))]
    public void SetLengthNotSupported()
    {
        using var src = new MemorySourceStream(new float[] { 0.1f, 0.2f }, 44100, 1);
        using var stream = new AudioDataStream(src);

        stream.SetLength(0);
    }

    [TestMethod]
    [ExpectedException(typeof(NotImplementedException))]
    public void WriteNotSupported()
    {
        using var src = new MemorySourceStream(new float[] { 0.1f, 0.2f }, 44100, 1);
        using var stream = new AudioDataStream(src);

        stream.Write(new byte[4], 0, 4);
    }
}
