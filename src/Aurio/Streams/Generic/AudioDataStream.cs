using System;
using System.IO;

namespace Aurio.Streams.Generic;

/// <summary>
/// Provides a read-only stream for accessing audio data from an underlying audio stream source.
/// </summary>
/// <remarks>This stream supports reading and seeking operations but does not support writing. It is intended for
/// scenarios where raw audio data needs to be consumed in a stream-based manner.</remarks>
public class AudioDataStream : Stream
{
    private readonly IAudioStream _audioStream;

    public AudioDataStream(IAudioStream audioStream)
    {
        _audioStream = audioStream;
    }

    public override bool CanRead => true;

    public override bool CanSeek => true;

    public override bool CanWrite => false;

    public override long Length => _audioStream.Length;

    public override long Position
    {
        get => _audioStream.Position;
        set => _audioStream.Position = value;
    }

    public override void Flush()
    {
        // noop
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        return _audioStream.Read(buffer, offset, count);
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        var position = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => _audioStream.Position + offset,
            SeekOrigin.End => _audioStream.Length + offset,
            _ => throw new ArgumentOutOfRangeException(nameof(origin), origin, null),
        };
        return _audioStream.Position = position;
    }

    public override void SetLength(long value)
    {
        throw new NotImplementedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotImplementedException();
    }
}
