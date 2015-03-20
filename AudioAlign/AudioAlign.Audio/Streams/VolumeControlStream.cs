using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NAudio.Wave;

namespace AudioAlign.Audio.Streams {
    public class VolumeControlStream : AbstractAudioStreamWrapper {

        public const float DefaultVolume = 1.0f;
        public const float DefaultBalance = 0.0f;

        public VolumeControlStream(IAudioStream sourceStream)
            : base(sourceStream) {
            if (!(sourceStream.Properties.Format == AudioFormat.IEEE && sourceStream.Properties.BitDepth == 32)) {
                throw new ArgumentException("unsupported source format: " + sourceStream.Properties);
            }

            Volume = DefaultVolume;
            Balance = DefaultBalance;
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
                bool mute = Mute;
                float balance = Balance;
                float volume = Volume;

                if (mute) {
                    // in mute mode, just set the samples to zero (-inf dB).
                    Array.Clear(buffer, offset, bytesRead);
                }
                else if (volume != DefaultVolume || balance != DefaultBalance) {
                    unsafe {
                        fixed (byte* sampleBuffer = &buffer[offset]) {
                            float* samples = (float*)sampleBuffer;

                            for (int x = 0; x < bytesRead / 4; x += 2) {
                                // adjust balance
                                if (balance != 0) {
                                    // left channel
                                    if (balance > 0) {
                                        samples[x] *= 1 - balance;
                                    }
                                    // right channel
                                    else {
                                        samples[x + 1] *= 1 + balance;
                                    }
                                }

                                // adjust volume
                                samples[x] *= volume;
                                samples[x + 1] *= volume;
                            }
                        }
                    }
                }
            }

            return bytesRead;
        }
    }
}
