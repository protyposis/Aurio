//
// Aurio: Audio Processing, Analysis and Retrieval Library
// Copyright (C) 2010-2017  Mario Guggenberger <mg@protyposis.net>
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
using System.Diagnostics;
using System.Threading.Tasks;

namespace Aurio.Streams
{
    public class VisualizingStream : AbstractAudioStreamWrapper
    {
        public event EventHandler WaveformChanged;

        private byte[] buffer;
        private long bufferPosition; // the stream position from which the data in the buffer originates
        private long bufferLength; // the length of the currently buffered data
        private PeakStore peakStore;

        public VisualizingStream(IAudioStream sourceStream)
            : base(sourceStream)
        {
            if (
                !(
                    sourceStream.Properties.Format == AudioFormat.IEEE
                    && sourceStream.Properties.BitDepth == 32
                )
            )
            {
                throw new ArgumentException(
                    "unsupported source format: " + sourceStream.Properties
                );
            }

            buffer = new byte[0];
        }

        public VisualizingStream(IAudioStream sourceStream, PeakStore peakStore)
            : this(sourceStream)
        {
            PeakStore = peakStore;
        }

        public PeakStore PeakStore
        {
            get { return peakStore; }
            set
            {
                if (peakStore != null)
                {
                    peakStore.PeaksChanged -= peakStore_PeaksChanged;
                }
                peakStore = value;
                if (peakStore != null)
                {
                    peakStore.PeaksChanged += peakStore_PeaksChanged;
                }
            }
        }

        public TimeSpan TimeLength
        {
            get { return TimeUtil.BytesToTimeSpan(this.Length, this.Properties); }
        }

        public TimeSpan TimePosition
        {
            get { return TimeUtil.BytesToTimeSpan(this.Position, this.Properties); }
            set { this.Position = TimeUtil.TimeSpanToBytes(value, this.Properties); }
        }

        public int ReadSamples(float[][] samples, int count)
        {
            int requiredBytes = count * Properties.SampleBlockByteSize;
            ResizeBuffer(requiredBytes);
            int bytesRead = StreamUtil.ForceRead(sourceStream, buffer, 0, requiredBytes);

            if (bytesRead % Properties.SampleBlockByteSize != 0)
            {
                throw new Exception();
            }
            if (samples.Length < Properties.Channels)
            {
                throw new Exception();
            }
            if (samples[0].Length < count)
            {
                throw new Exception();
            }

            int samplesRead = bytesRead / Properties.SampleBlockByteSize;
            unsafe
            {
                fixed (byte* bufferB = &buffer[0])
                {
                    float* bufferF = (float*)bufferB;

                    for (int channel = 0; channel < Properties.Channels; channel++)
                    {
                        int index = channel;
                        for (int i = 0; i < count; i++)
                        {
                            samples[channel][i] = bufferF[index];
                            index += Properties.Channels;
                        }
                    }
                }
            }

            return samplesRead;
        }

        public int ReadPeaks(float[][] peaks, int sampleCount, int peakCount)
        {
            long streamPosition = sourceStream.Position;

            int samplesPerPeak = (int)Math.Ceiling((float)sampleCount / peakCount);
            unsafe
            {
                // if the samplesPerPeak count is beyond the PeakStore threshold, load the peaks from there
                if (samplesPerPeak > peakStore.SamplesPerPeak)
                {
                    byte[][] peakData = peakStore.GetData(samplesPerPeak, out samplesPerPeak);
                    int positionOffset =
                        (int)(streamPosition / SampleBlockSize / samplesPerPeak) * sizeof(Peak);
                    int sourcePeakCount = (int)Math.Round((float)sampleCount / samplesPerPeak);
                    float sourceToTargetIndex = 1 / ((float)sourcePeakCount / peakCount);

                    for (int channel = 0; channel < Properties.Channels; channel++)
                    {
                        fixed (byte* peakChannelDataB = &peakData[channel][positionOffset])
                        {
                            Peak* peakChannelDataP = (Peak*)peakChannelDataB;

                            fixed (float* peakChannelF = &peaks[channel][0])
                            {
                                Peak* peakChannelP = (Peak*)peakChannelF;
                                int peak = 0;
                                peakChannelP[0] = new Peak(float.MaxValue, float.MinValue);
                                for (int p = 0; p < sourcePeakCount; p++)
                                {
                                    if ((int)(p * sourceToTargetIndex) > peak)
                                    {
                                        peak++;
                                        peakChannelP[peak] = new Peak(
                                            float.MaxValue,
                                            float.MinValue
                                        );
                                    }
                                    peakChannelP[peak].Merge(peakChannelDataP[p]);
                                }
                            }
                        }
                    }

                    return peakCount;
                }

                // else load samples from the stream and generate peaks
                int requiredBytes = sampleCount * Properties.SampleBlockByteSize;
                int bufferOffset = 0;
                int bytesRead = 0;

                if (
                    bufferPosition <= sourceStream.Position
                    && sourceStream.Position + requiredBytes <= bufferPosition + bufferLength
                )
                {
                    // the requested data can be read directly from the buffer, no need to read from the stream
                    bufferOffset = (int)(sourceStream.Position - bufferPosition);
                    bytesRead = requiredBytes;
                }
                else
                {
                    // resize buffer and read data from stream
                    ResizeBuffer(requiredBytes);
                    bufferPosition = sourceStream.Position;
                    bytesRead = StreamUtil.ForceRead(sourceStream, buffer, 0, requiredBytes);
                    bufferLength = bytesRead;
                }

                if (bytesRead % Properties.SampleBlockByteSize != 0)
                {
                    throw new Exception();
                }
                int samplesRead = bytesRead / Properties.SampleBlockByteSize;

                float sampleIndexToPeakIndex = 1 / ((float)samplesRead / peakCount);
                fixed (byte* bufferB = &buffer[bufferOffset])
                {
                    float* bufferF = (float*)bufferB;

                    for (int channel = 0; channel < Properties.Channels; channel++)
                    {
                        fixed (float* peakChannelF = &peaks[channel][0])
                        {
                            Peak* peakChannelP = (Peak*)peakChannelF;
                            int index = channel;
                            int peakIndex = 0;
                            peakChannelP[peakIndex] = new Peak(float.MaxValue, float.MinValue);

                            for (int i = 0; i < samplesRead; i++)
                            {
                                float sampleValue = bufferF[index];
                                index += Properties.Channels;

                                peakChannelP[peakIndex].Merge(sampleValue, sampleValue);

                                if ((int)(i * sampleIndexToPeakIndex) > peakIndex)
                                {
                                    peakChannelP[++peakIndex] = new Peak(sampleValue, sampleValue);
                                }
                            }
                        }
                    }
                }
            }

            return peakCount;
        }

        private void ResizeBuffer(int requiredSize)
        {
            if (requiredSize > buffer.Length)
            {
                Debug.WriteLine(
                    "VisualizingStream buffer resize: " + buffer.Length + " -> " + requiredSize
                );
                buffer = new byte[requiredSize];
            }
        }

        private void OnWaveformChanged()
        {
            WaveformChanged?.Invoke(this, EventArgs.Empty);
        }

        private void peakStore_PeaksChanged(object sender, EventArgs e)
        {
            OnWaveformChanged();
        }

        public static async Task<VisualizingStream> Create(IAudioStream stream)
        {
            return await Task.Run(() =>
                {
                    var peakStore = new PeakStore(
                        Aurio.PeakStore.DefaultSamplesPerPeak,
                        stream.Properties.Channels,
                        (int)
                            Math.Ceiling(
                                (double)stream.Length
                                    / stream.SampleBlockSize
                                    / PeakStore.DefaultSamplesPerPeak
                            )
                    );
                    peakStore.Fill(stream);
                    return new VisualizingStream(stream, peakStore);
                })
                .ConfigureAwait(false);
        }
    }
}
