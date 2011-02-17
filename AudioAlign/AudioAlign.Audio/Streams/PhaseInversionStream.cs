using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NAudio.Wave;

namespace AudioAlign.Audio.Streams {
    public class PhaseInversionStream : AbstractAudioStreamWrapper {

        public PhaseInversionStream(IAudioStream sourceStream)
            : base(sourceStream) {
            if (!(sourceStream.Properties.Format == AudioFormat.IEEE && sourceStream.Properties.BitDepth == 32)) {
                throw new ArgumentException("unsupported source format: " + sourceStream.Properties);
            }
            Invert = false;
        }

        /// <summary>
        /// Enables or disables audio phase inversion.
        /// </summary>
        public bool Invert { get; set; }

        public override int Read(byte[] buffer, int offset, int count) {
            int bytesRead = sourceStream.Read(buffer, offset, count);

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
