using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NAudio.Wave;

namespace AudioAlign.Audio.NAudio {
    public class VolumeClipStream : WaveStream {

        public VolumeClipStream(WaveStream sourceStream) {
            SourceStream = sourceStream;
            if (sourceStream.WaveFormat.BitsPerSample != 32) {
                throw new ArgumentException("Metering Stream expects 32 bit floating point audio", "sourceStream");
            }
            Clip = true;
        }

        public WaveStream SourceStream { get; private set; }

        /// <summary>
        /// Enables or disables audio sample clipping.
        /// </summary>
        public bool Clip { get; set; }

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

            if (Clip && bytesRead > 0) {
                unsafe {
                    fixed (byte* sampleBuffer = &buffer[offset]) {
                        float* samples = (float*)sampleBuffer;
                        for (int x = 0; x < bytesRead / 4; x++) {
                            if (samples[x] > 0) {
                                samples[x] = samples[x] > 1 ? 1.0f : samples[x];
                            }
                            else {
                                samples[x] = samples[x] < -1 ? -1.0f : samples[x];
                            }
                        }
                    }
                }
            }

            return bytesRead;
        }
    }
}
