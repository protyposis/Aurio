using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NAudio.Wave;

namespace AudioAlign.Audio.Streams {
    public class VolumeControlStream : AbstractAudioStreamWrapper {

        public VolumeControlStream(IAudioStream sourceStream)
            : base(sourceStream) {
            if (!(sourceStream.Properties.Format == AudioFormat.IEEE && sourceStream.Properties.BitDepth == 32)) {
                throw new ArgumentException("unsupported source format: " + sourceStream.Properties);
            }

            Volume = 1.0f;
            Balance = 0.0f;
            Mute = false;
        }

        /// <summary>
        /// Gets or sets the audio volume. 0 is mute, 1.0 is the default volume (no adjustment).
        /// </summary>
        public float Volume { get; set; }

        /// <summary>
        /// Gets or sets the audio channel balance. 0 is default, -1.0 is left channel only, 1.0 is right channel only.
        /// </summary>
        public float Balance { get; set; }

        /// <summary>
        /// Gets or sets if the track is muted.
        /// </summary>
        public bool Mute { get; set; }

        public override int Read(byte[] buffer, int offset, int count) {
            int bytesRead = sourceStream.Read(buffer, offset, count);

            if (bytesRead > 0) {
                unsafe {
                    fixed (byte* sampleBuffer = &buffer[offset]) {
                        float* samples = (float*)sampleBuffer;
                        for (int x = 0; x < bytesRead / 4; x++) {
                            if (Mute) {
                                // in mute mode, just set the samples to zero (-inf dB).
                                samples[x] = 0.0f;
                            }
                            else {
                                // adjust balance
                                if (Balance != 0) {
                                    if (x % 2 == 0) {
                                        // left channel
                                        if (Balance > 0) {
                                            samples[x] *= 1 - Balance;
                                        }
                                    }
                                    else {
                                        // right channel
                                        if (Balance < 0) {
                                            samples[x] *= 1 + Balance;
                                        }
                                    }
                                }

                                // adjust volume
                                samples[x] *= Volume;
                            }
                        }
                    }
                }
            }

            return bytesRead;
        }
    }
}
