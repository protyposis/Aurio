using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NAudio.Wave;

namespace AudioAlign.Audio.Streams {
    public class VolumeClipStream : AbstractAudioStreamWrapper {

        public VolumeClipStream(IAudioStream sourceStream)
            : base(sourceStream) {
            if (!(sourceStream.Properties.Format == AudioFormat.IEEE && sourceStream.Properties.BitDepth == 32)) {
                throw new ArgumentException("unsupported source format: " + sourceStream.Properties);
            }
            Clip = true;
        }

        /// <summary>
        /// Enables or disables audio sample clipping.
        /// </summary>
        public bool Clip { get; set; }

        public override int Read(byte[] buffer, int offset, int count) {
            int bytesRead = sourceStream.Read(buffer, offset, count);

            if (Clip && bytesRead > 0) {
                unsafe {
                    fixed (byte* sampleBuffer = &buffer[offset]) {
                        float* samples = (float*)sampleBuffer;
                        for (int x = 0; x < bytesRead / 4; x++) {
                            if (samples[x] > 1.0f) {
                                samples[x] = 1.0f;
                            }
                            else if(samples[x] < -1.0f) {
                                samples[x] = -1.0f;
                            }
                        }
                    }
                }
            }

            return bytesRead;
        }
    }
}
