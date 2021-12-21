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
        private bool _endOfInput;

        public BlockingFixedLengthFifoStream(AudioProperties properties, int capacity) : base(properties, capacity)
        {
            _readPosition = 0;
            _endOfInput = false;
        }

        /// <summary>
        /// The length of the stream. Since the length of this stream is unsually not known and only
        /// the most recent part is buffered, this returns infinity (respectively the max long value)
        /// until the end of input has been signalled, after which the actual length will be returned.
        /// </summary>
        /// <see cref="EndOfInputSignalled"/>
        public override long Length
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                if (_endOfInput)
                {
                    // When EOI has been signalled, we return the actual length of this stream
                    // to allow consumers real all contents by comparing the the length with the 
                    // position (the usual pattern for stream reading).
                    return base.Length;
                }
                else
                {
                    // Without the EOI signal, we return the maximum possible length (meaning
                    // infinity) to tell a consumer that the stream is potentially infinite and
                    // a comparison of length and position never allows the conclusion that the
                    // end of the stream has been reached.
                    return long.MaxValue;
                }
            }
        }

        /// <summary>
        /// By convention of the stream interface, there is normally only one position which is both
        /// the read and write position. This stream exposes read and write positions separately because
        /// writes are appended at the end fo the stream while reads are done from the beginning of 
        /// the stream (FIFO approach).
        /// 
        /// This property exposes the read position, because this is the interesting position that must 
        /// be used to calculate the amount of remaining data by comparing the position with the length.
        /// 
        /// Comparing the read with the write position allows to determine the buffer level, i.e. the amount
        /// of data that has not been consumed yet, independently from how much data the buffer holds in total.
        /// </summary>
        /// <see cref="WritePosition"/>
        public override long Position
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get => _readPosition;
            [MethodImpl(MethodImplOptions.Synchronized)]
            set => throw new InvalidOperationException("Cannot set the read position");
        }

        public long WritePosition
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get => base.Position;
            [MethodImpl(MethodImplOptions.Synchronized)]
            set => base.Position = value;
        }

        /// <summary>
        /// Signals that no more data will be written to the stream and it will come to and end.
        /// After the signal, no more data will be allowed to be written but all buffered data
        /// can still be consumed.
        /// </summary>
        /// <see cref="EndOfInputSignalled"/>
        public void SignalEndOfInput()
        {
            _endOfInput = true;
        }

        /// <summary>
        /// Tells if the end of input has been signalled.
        /// </summary>
        /// <see cref="SignalEndOfInput"/>
        public bool EndOfInputSignalled
        {
            get => _endOfInput;
        }

        /// <summary>
        /// Reads data from the stream, i.e. from the beginning of the FIFO buffer. This method blocks
        /// until additional data is written to the stream and becomes available, or the end of input
        /// has been signalled.
        /// </summary>
        /// <see cref="EndOfInputSignalled"/> 
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
                if (_endOfInput)
                {
                    // At the EOI we don't block because no more data will become available.
                    return 0;
                }

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
            if (_endOfInput)
            {
                throw new Exception("End of input has been signalled, no more data can be written");
            }

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
