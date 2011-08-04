using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NAudio.Wave;

namespace AudioAlign.Audio.Streams {
    public class VolumeMeteringStream : AbstractAudioStreamWrapper {

        /// <summary>
        /// Gets or sets the number of processed samples after which the StreamVolume event
        /// will be automatically fired. Set the value to 0 to deactivate automatic firing
        /// of the event.
        /// </summary>
        public int SamplesPerNotification { get; set; }

        private float[] maxSamples;
        private int sampleCount;

        public event EventHandler<StreamVolumeEventArgs> StreamVolume;

        public VolumeMeteringStream(IAudioStream sourceStream) :
            this(sourceStream, 0) {
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

                            if (SamplesPerNotification > 0 && sampleCount >= SamplesPerNotification) {
                                RaiseStreamVolumeNotification();
                                sampleCount = 0;
                            }
                        }
                    }
                }
            }

            return bytesRead;
        }

        /// <summary>
        /// Gets the maximum absolute sample values for all channels. Calling this
        /// function resets the internal maximum values to 0 (but they will be updated
        /// by calling the read function). That means that the function returns the
        /// maximum values that are detected between two calls of this function.
        /// </summary>
        /// <returns></returns>
        public float[] GetMaxSampleValues() {
            float[] maxSampleValues = (float[])maxSamples.Clone();
            Array.Clear(maxSamples, 0, maxSamples.Length);
            return maxSampleValues;
        }

        private void RaiseStreamVolumeNotification() {
            if (StreamVolume != null) {
                StreamVolume(this, new StreamVolumeEventArgs() { MaxSampleValues = GetMaxSampleValues() });
            }
        }
    }

    public class StreamVolumeEventArgs : EventArgs {
        public float[] MaxSampleValues { get; set; }
    }
}
