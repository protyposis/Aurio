using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NAudio.Wave;

namespace AudioAlign.Audio.NAudio {
    /// <summary>
    /// This stream doesn't throw exceptions when reading from negative positions or positions that exceed 
    /// the length of the stream.
    /// It is helpful when mixing multiple WaveFileReaders of different lengths together. Without this
    /// stream, an exception gets thrown when the end of the shortest stream is reached.
    /// </summary>
    public class TolerantWaveStream : WaveStream {

        public TolerantWaveStream(WaveStream sourceStream) {
            SourceStream = sourceStream;
        }

        public WaveStream SourceStream { get; private set; }

        public override WaveFormat WaveFormat {
            get { return SourceStream.WaveFormat; }
        }

        public override long Length {
            get { return SourceStream.Length; }
        }

        public override long Position {
            get {
                return SourceStream.Position;
            }
            set {
                if (value < 0 || value > SourceStream.Length) {
                    SourceStream.Position = SourceStream.Length;
                }
                else {
                    SourceStream.Position = value;
                }
            }
        }

        public override int Read(byte[] buffer, int offset, int count) {
            return SourceStream.Read(buffer, offset, count);
        }
    }
}
