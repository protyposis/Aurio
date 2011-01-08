using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NAudio.Wave;

namespace AudioAlign.Audio.NAudio {
    public class VolumeControlStream : WaveStream {

        public VolumeControlStream(WaveStream sourceStream) {
            SourceStream = sourceStream;
            if (sourceStream.WaveFormat.BitsPerSample != 32) {
                throw new ArgumentException("Metering Stream expects 32 bit floating point audio", "sourceStream");
            }

            Volume = 1.0f;
        }

        public WaveStream SourceStream { get; private set; }

        /// <summary>
        /// Gets or sets the audio volume. 0 is mute, 1.0 is the default volume (no adjustment).
        /// </summary>
        public float Volume { get; set; }

        /// <summary>
        /// Gets or sets if the track is muted.
        /// </summary>
        public bool Mute { get; set; }

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
                SourceStream.Position = value;
            }
        }

        public override int Read(byte[] buffer, int offset, int count) {
            int bytesRead = SourceStream.Read(buffer, offset, count);

            // adjust volume
            unsafe {
                fixed (byte* sampleBuffer = &buffer[offset]) {
                    float* samples = (float*)sampleBuffer;
                    for (int x = 0; x < bytesRead / 4; x++) {
                        if (Mute) {
                            // in mute mode, just set the samples to zero (-inf dB).
                            samples[x] = 0.0f;
                        }
                        else {
                            samples[x] *= Volume;
                        }
                    }
                }
            }

            return bytesRead;
        }
    }
}
