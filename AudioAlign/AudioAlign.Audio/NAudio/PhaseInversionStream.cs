using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NAudio.Wave;

namespace AudioAlign.Audio.NAudio {
    public class PhaseInversionStream : WaveStream {

        public PhaseInversionStream(WaveStream sourceStream) {
            SourceStream = sourceStream;
            if (sourceStream.WaveFormat.BitsPerSample != 32) {
                throw new ArgumentException("Metering Stream expects 32 bit floating point audio", "sourceStream");
            }
            Invert = false;
        }

        public WaveStream SourceStream { get; private set; }

        /// <summary>
        /// Enables or disables audio phase inversion.
        /// </summary>
        public bool Invert { get; set; }

        public override WaveFormat WaveFormat {
            get { return SourceStream.WaveFormat; }
        }

        public override long Length {
            get { return SourceStream.Length; }
        }

        public override long Position {
            get { return SourceStream.Position; }
            set { SourceStream.Position = value; }
        }

        public override int Read(byte[] buffer, int offset, int count) {
            int bytesRead = SourceStream.Read(buffer, offset, count);

            if (Invert && bytesRead > 0) {
                unsafe {
                    fixed (byte* sampleBuffer = &buffer[offset]) {
                        float* samples = (float*)sampleBuffer;
                        for (int x = 0; x < bytesRead / 4; x++) {
                            samples[x] *= -1;
                        }
                    }
                }
            }

            return bytesRead;
        }
    }
}
