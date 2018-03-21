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
    /// as an audio data FIFO queue. Reads are blocking until data becomes available by writes to the stream.
    /// 
    /// This stream can be used to convert the pull-based stream processing into a push-based approach, 
    /// helpful when input data becomes available more slowly than the output data processing speed, e.g.
    /// in realtime / live stream processing. It also allows for constant memory usage with infinitely long 
    /// streams.
    /// </summary>
    public class BlockingFixedLengthFifoStream : CircularMemoryWriterStream
    {
        private long _readPosition;

        public BlockingFixedLengthFifoStream(AudioProperties properties, int capacity) : base(properties, capacity)
        {
            _readPosition = 0;
        }

        public override long Length
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return base.Length;
            }
        }

        public override long Position
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return base.Position;
            }
            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                base.Position = value;
            }
        }

        public long ReadPosition
        {
            get { return _readPosition; }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public override int Read(byte[] buffer, int offset, int count)
        {
            // Block until data is available
            // By convention, when a read returns 0, it means the end of the stream has been 
            // reached. Because we're potentially processing realtime audio data and audio processing 
            // usually consumes data much faster than it comes in, we would instantly run into an EOS 
            // so we need to block reading until new data has been written.
            long bytesAvailable;
            while ((bytesAvailable = base.Position - _readPosition) <= 0)
            {
                // Debug.WriteLine("Waiting for data... ({0} required, {1} available)", count, bytesAvailable);
                Monitor.Wait(this);
            }

            // Store write position so we can revert to it after reading
            var writePosition = base.Position;
            // Seek to read position
            base.Position = _readPosition;
            // Read data and update the read position
            var bytesRead = base.Read(buffer, offset, count);
            // Debug.WriteLine("{0} bytes read, {1} available", bytesRead, bytesAvailable);
            _readPosition += bytesRead;

            // Revert to write position
            base.Position = writePosition;

            return bytesRead;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
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
                _readPosition -= writeOverflow;

                if (_readPosition < 0)
                {
                    Debug.WriteLine("Lost {0} unread bytes", -_readPosition);
                    _readPosition = 0;
                }
            }

            // Signal a blocked read that new data has become available
            Monitor.PulseAll(this);
        }
    }
}
