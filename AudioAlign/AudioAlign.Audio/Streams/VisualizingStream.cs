using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace AudioAlign.Audio.Streams {
    public class VisualizingStream : AbstractAudioStreamWrapper {

        private byte[] buffer;
        private long bufferPosition; // the stream position from which the data in the buffer originates
        private long bufferLength; // the length of the currently buffered data
        private PeakStore peakStore;

        public VisualizingStream(IAudioStream sourceStream) : base(sourceStream) {
            if (!(sourceStream.Properties.Format == AudioFormat.IEEE && sourceStream.Properties.BitDepth == 32)) {
                throw new ArgumentException("unsupported source format: " + sourceStream.Properties);
            }

            buffer = new byte[0];
        }

        public VisualizingStream(IAudioStream sourceStream, PeakStore peakStore) : this(sourceStream) {
                this.peakStore = peakStore;
        }

        public TimeSpan TimeLength {
            get { return TimeUtil.BytesToTimeSpan(this.Length, this.Properties); }
        }

        public TimeSpan TimePosition {
            get { return TimeUtil.BytesToTimeSpan(this.Position, this.Properties); }
            set { this.Position = TimeUtil.TimeSpanToBytes(value, this.Properties); }
        }

        public int ReadSamples(float[][] samples, int count) {
            int requiredBytes = count * Properties.SampleBlockByteSize;
            ResizeBuffer(requiredBytes);
            int bytesRead = StreamUtil.ForceRead(sourceStream, buffer, 0, requiredBytes);

            if (bytesRead % Properties.SampleBlockByteSize != 0) {
                throw new Exception();
            }
            if(samples.Length < Properties.Channels) {
                throw new Exception();
            }
            if(samples[0].Length < count) {
                throw new Exception();
            }

            int samplesRead = bytesRead / Properties.SampleBlockByteSize;

            unsafe {
                fixed (byte* bufferB = &buffer[0]) {
                    float* bufferF = (float*)bufferB;

                    for (int channel = 0; channel < Properties.Channels; channel++) {
                        int index = channel;
                        for (int i = 0; i < count; i++) {
                            samples[channel][i] = bufferF[index];
                            index += Properties.Channels;
                        }
                    }
                }
            }

            return samplesRead;
        }

        public int ReadPeaks(float[][] peaks, int sampleCount, int peakCount) {
            long streamPosition = sourceStream.Position;

            int samplesPerPeak = (int)Math.Ceiling((float)sampleCount / peakCount);
            unsafe {

                // if the samplesPerPeak count is beyond the PeakStore threshold, load the peaks from there
                if (samplesPerPeak > peakStore.SamplesPerPeak) {
                    byte[][] peakData = peakStore.GetData(samplesPerPeak, out samplesPerPeak);
                    int positionOffset = (int)(streamPosition / SampleBlockSize / samplesPerPeak) * sizeof(Peak);
                    int sourcePeakCount = (int)Math.Ceiling((float)sampleCount / samplesPerPeak);
                    float sourceToTargetIndex = 1 / ((float)sourcePeakCount / peakCount);

                    for (int channel = 0; channel < Properties.Channels; channel++) {
                        fixed (byte* peakChannelDataB = &peakData[channel][positionOffset]) {
                            Peak* peakChannelDataP = (Peak*)peakChannelDataB;

                            fixed (float* peakChannelF = &peaks[channel][0]) {
                                Peak* peakChannelP = (Peak*)peakChannelF;
                                int peak = 0;
                                peakChannelP[0] = new Peak(float.MaxValue, float.MinValue);
                                for (int p = 0; p < sourcePeakCount; p++) {
                                    if ((int)(p * sourceToTargetIndex) > peak) {
                                        peak++;
                                        peakChannelP[peak] = new Peak(float.MaxValue, float.MinValue);
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

                if (bufferPosition <= sourceStream.Position && sourceStream.Position + requiredBytes <= bufferPosition + bufferLength) {
                    // the requested data can be read directly from the buffer, no need to read from the stream
                    bufferOffset = (int)(sourceStream.Position - bufferPosition);
                    bytesRead = requiredBytes;
                }
                else {
                    // resize buffer and read data from stream
                    ResizeBuffer(requiredBytes);
                    bufferPosition = sourceStream.Position;
                    bytesRead = StreamUtil.ForceRead(sourceStream, buffer, 0, requiredBytes);
                    bufferLength = bytesRead;
                }

                if (bytesRead % Properties.SampleBlockByteSize != 0) {
                    throw new Exception();
                }
                int samplesRead = bytesRead / Properties.SampleBlockByteSize;

                float sampleIndexToPeakIndex = 1 / ((float)samplesRead / peakCount);
                fixed (byte* bufferB = &buffer[bufferOffset]) {
                    float* bufferF = (float*)bufferB;

                    for (int channel = 0; channel < Properties.Channels; channel++) {
                        fixed (float* peakChannelF = &peaks[channel][0]) {
                            Peak* peakChannelP = (Peak*)peakChannelF;
                            int index = channel;
                            int peakIndex = 0;
                            peakChannelP[peakIndex] = new Peak(float.MaxValue, float.MinValue);

                            for (int i = 0; i < samplesRead; i++) {
                                float sampleValue = bufferF[index];
                                index += Properties.Channels;

                                peakChannelP[peakIndex].Merge(sampleValue, sampleValue);

                                if ((int)(i * sampleIndexToPeakIndex) > peakIndex) {
                                    peakChannelP[++peakIndex] = new Peak(sampleValue, sampleValue);
                                }
                            }
                        }
                    }
                }
            }

            return peakCount;
        }

        private void ResizeBuffer(int requiredSize) {
            if (requiredSize > buffer.Length) {
                Debug.WriteLine("VisualizingStream buffer resize: " + buffer.Length + " -> " + requiredSize);
                buffer = new byte[requiredSize];
            }
        }
    }
}
