using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Aurio.Streams
{
    /// <summary>
    /// A stream with a fixed length operating on a circular memory buffer. Arbitrary many data can be written to this stream,
    /// but only the most recent data that fits the length of this stream is retained. Writes at the end of the stream discard
    /// the same amount of bytes at the beginning of the stream. Writes within the stream overwrite existing data.
    /// </summary>
    public class CircularMemoryWriterStream : MemorySourceStream, IAudioWriterStream
    {
        private long _bufferFillLevel;
        private long _bufferHead;
        private long _position;

        public CircularMemoryWriterStream(AudioProperties properties, MemoryStream target)
            : base(target, properties)
        {
            if (target.Capacity == 0)
            {
                throw new Exception("Circular stream must have a fixed capacity");
            }

            _bufferFillLevel = 0;
            _bufferHead = 0;
            _position = 0;
        }

        public CircularMemoryWriterStream(AudioProperties properties, int capacity)
            : this(properties, new MemoryStream(new byte[capacity])) { }

        public override long Length
        {
            get { return _bufferFillLevel; }
        }

        public long Capacity
        {
            get { return source.Length; }
        }

        public override long Position
        {
            get { return _position; }
            set
            {
                if (value < 0 || value > source.Capacity)
                {
                    throw new ArgumentOutOfRangeException(
                        "Cannot set a position outside the circular memory"
                    );
                }
                _position = value;
                source.Position =
                    ((_bufferHead - _bufferFillLevel) + _position + source.Length) % source.Length;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer must not be null");
            }
            if (!source.CanRead)
            {
                throw new NotSupportedException("target stream is not readable");
            }
            if (offset < 0 || count < 0)
            {
                throw new ArgumentOutOfRangeException("offset and count must not be negative");
            }

            // Clamp read count to the number of remaining bytes from the current position until the end
            count = (int)Math.Min(count, _bufferFillLevel - _position);

            var bytesBeforeWrap = (int)(source.Length - source.Position);
            var bytesAfterWrap = count - bytesBeforeWrap;

            if (bytesAfterWrap > 0)
            {
                source.Read(buffer, offset, bytesBeforeWrap);
                source.Position = 0;
                source.Read(buffer, offset + bytesBeforeWrap, bytesAfterWrap);
            }
            else
            {
                source.Read(buffer, offset, count);
            }

            _position = Math.Min(Length, _position + count);

            return count;
        }

        public virtual void Write(byte[] buffer, int offset, int count)
        {
            // Default stream checks according to MSDN
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer must not be null");
            }
            if (!source.CanWrite)
            {
                throw new NotSupportedException("target stream is not writable");
            }
            if (offset < 0 || count < 0)
            {
                throw new ArgumentOutOfRangeException("offset and count must not be negative");
            }

            // Check block alignment
            if (count % SampleBlockSize != 0)
            {
                throw new ArgumentException("count must be a multiple of the sample block size");
            }

            if (count > source.Capacity)
            {
                int overflow = count - source.Capacity;
                Debug.WriteLine(
                    "Writing more data ({0,10}) than the stream can hold ({1,10}), some data will be lost ({1,10})",
                    count,
                    source.Capacity
                );

                // Skip data that's going to be overwritten instantly if we were to write all data circularly
                // E.g. when writing 15 bytes to a 10-byte circular buffer, the first 5 can be skipped because they would be overwritten
                offset += overflow;
                count -= overflow;
            }

            if (_position > _bufferFillLevel)
            {
                var delta = _position - _bufferFillLevel;
                _bufferFillLevel = _position;
                _bufferHead = (_bufferHead + delta) % source.Length;
                source.Position = _bufferHead;
            }

            var replacedBytes = _bufferFillLevel - _position;
            var addedBytes = count - replacedBytes;

            var bytesBeforeWrap = (int)(source.Length - source.Position);
            var bytesAfterWrap = count - bytesBeforeWrap;

            if (bytesAfterWrap > 0)
            {
                source.Write(buffer, offset, bytesBeforeWrap);
                source.Position = 0;
                source.Write(buffer, offset + bytesBeforeWrap, bytesAfterWrap);
                //_bufferPosition = bytesAfterWrap;
            }
            else
            {
                source.Write(buffer, offset, count);
                //_bufferPosition = (int)((_bufferPosition + count) % Length);
            }

            _bufferFillLevel = Math.Min(source.Length, _bufferFillLevel + addedBytes);
            _bufferHead = (_bufferHead + addedBytes) % source.Length;
            _position = Math.Min(source.Length, _position + count);
        }
    }
}
