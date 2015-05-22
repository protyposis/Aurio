// 
// Aurio: Audio Processing, Analysis and Retrieval Library
// Copyright (C) 2010-2015  Mario Guggenberger <mg@protyposis.net>
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as
// published by the Free Software Foundation, either version 3 of the
// License, or (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NAudio.Wave;

namespace Aurio.Streams {
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

        public bool Disabled {
            get;
            set;
        }

        public override int Read(byte[] buffer, int offset, int count) {
            if (Disabled) {
                return sourceStream.Read(buffer, offset, count);
            }

            int bytesRead = sourceStream.Read(buffer, offset, count);

            if (bytesRead > 0) {
                // local value copies for speed optimization
                int sourceStreamChannels = sourceStream.Properties.Channels;
                int samplesPerNotification = SamplesPerNotification;

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

                            channel = ++channel % sourceStreamChannels;
                            sampleCount++;

                            if (samplesPerNotification > 0 && sampleCount >= samplesPerNotification) {
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
