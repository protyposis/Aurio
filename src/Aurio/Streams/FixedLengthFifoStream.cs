using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace Aurio.Streams
{
    /// <summary>
    /// A stream with a fixed maximum capacity that can be written to and read from, basically working
    /// as an audio data FIFO queue.
    /// </summary>
    public class FixedLengthFifoStream : CircularMemoryWriterStream
    {
        private long readPosition;

        public FixedLengthFifoStream(AudioProperties properties, int capacity)
            : base(properties, capacity)
        {
            readPosition = 0;
        }

        public override long Length
        {
            get { return base.Length; }
        }

        /// <summary>
        /// By convention of the stream interface, there is normally only one position which is both
        /// the read and write position. This stream exposes read and write positions separately because
        /// writes are appended at the end of the stream, while reads are done at the beginning of
        /// the stream (FIFO approach).
        /// <see cref="WritePosition"/>
        public override long Position
        {
            get => readPosition;
            set => throw new InvalidOperationException("Cannot set the read position");
        }

        public virtual long WritePosition
        {
            get => base.Position;
            set => base.Position = value;
        }

        /// <summary>
        /// Returns the amount of data that has been written but not yet read.
        /// Does not include dropped data when more data is written than the stream can hold.
        /// </summary>
        public virtual long ReadDelay
        {
            get => WritePosition - Position;
        }

        /// <summary>
        /// Reads data from the stream, i.e. from the beginning of the FIFO buffer.
        /// </summary>
        public override int Read(byte[] buffer, int offset, int count)
        {
            // Store write position so we can revert to it after reading
            var writePosition = base.Position;
            // Seek to read position
            base.Position = readPosition;
            // Read data and update the read position
            var bytesRead = base.Read(buffer, offset, count);
            // Debug.WriteLine("{0} bytes read, {1} available", bytesRead, bytesAvailable);
            readPosition += bytesRead;

            // Revert to write position
            base.Position = writePosition;

            return bytesRead;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            // Write data
            // The read method makes sure that the stream is always at the write position when
            // writing so we don't need to take care of that here
            // Debug.WriteLine("Writing {0} bytes @ position {1}", count, base.Position);
            var writePosition = base.Position;
            var writeOverflow = count - (base.Capacity - writePosition);
            base.Write(buffer, offset, count);
            // Debug.WriteLine("Written until position {0}", base.Position);

            if (writeOverflow > 0)
            {
                readPosition -= writeOverflow;

                if (readPosition < 0)
                {
                    Debug.WriteLine("Lost {0} unread bytes", -readPosition);
                    readPosition = 0;
                }
            }
        }
    }
}
