using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NAudio.Wave;

namespace AudioAlign.Audio.Streams {
    public class VolumeMeteringStream : AbstractAudioStreamWrapper {

        public int SamplesPerNotification { get; set; }

        private float[] maxSamples;
        private int sampleCount;

        public event EventHandler<StreamVolumeEventArgs> StreamVolume;

        public VolumeMeteringStream(IAudioStream sourceStream) :
            this(sourceStream, sourceStream.Properties.SampleRate / 10) {
        }

        public VolumeMeteringStream(IAudioStream sourceStream, int samplesPerNotification)
            : base(sourceStream) {
            if (!(sourceStream.Properties.Format == AudioFormat.IEEE && sourceStream.Properties.BitDepth == 32)) {
                throw new ArgumentException("unsupported source format: " + sourceStream.Properties);
            }
            maxSamples = new float[sourceStream.Properties.Channels];
            this.SamplesPerNotification = samplesPerNotification;
        }

        public override int Read(byte[] buffer, int offset, int count) {
            int bytesRead = sourceStream.Read(buffer, offset, count);

            if (bytesRead > 0) {
                unsafe {
                    fixed (byte* sampleBuffer = &buffer[offset]) {
                        float* samples = (float*)sampleBuffer;
                        int channel = 0;
                        float sampleValue;
                        for (int x = 0; x < bytesRead / 4; x++) {
                            sampleValue = samples[x];
                            if (sampleValue < 0) sampleValue = -sampleValue;

                            if (sampleValue > maxSamples[channel]) {
                                maxSamples[channel] = sampleValue;
                            }

                            channel = ++channel % sourceStream.Properties.Channels;
                            sampleCount++;

                            if (sampleCount >= SamplesPerNotification) {
                                RaiseStreamVolumeNotification();
                                sampleCount = 0;
                                Array.Clear(maxSamples, 0, maxSamples.Length);
                            }
                        }
                    }
                }
            }

            return bytesRead;
        }

        private void RaiseStreamVolumeNotification() {
            if (StreamVolume != null) {
                StreamVolume(this, new StreamVolumeEventArgs() { MaxSampleValues = (float[])maxSamples.Clone() });
            }
        }
    }

    public class StreamVolumeEventArgs : EventArgs {
        public float[] MaxSampleValues { get; set; }
    }
}
