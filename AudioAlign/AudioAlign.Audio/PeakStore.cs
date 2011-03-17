using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.CompilerServices;

namespace AudioAlign.Audio {
    public class PeakStore {

        private static readonly int PEAK_BYTE_SIZE;

        static PeakStore() {
            unsafe { PEAK_BYTE_SIZE = sizeof(Peak); }
        }

        private int samplesPerPeak;
        private byte[][] data;
        private Dictionary<int, byte[][]> scaledData;

        /// <summary>
        /// Creates a PeakStore that stores a given number of peaks for a given number of channels.
        /// </summary>
        /// <param name="channels">the number of channels to store peaks for</param>
        /// <param name="peaksPerChannel">the number of peaks to store for each channel</param>
        /// <param name="samplesPerPeak">the number of samples that are merged into a peak</param>
        public PeakStore(int samplesPerPeak, int channels, int peaksPerChannel) {
            this.samplesPerPeak = samplesPerPeak;
            this.data = AudioUtil.CreateArray<byte>(channels, peaksPerChannel * PEAK_BYTE_SIZE);

            scaledData = new Dictionary<int, byte[][]>();
        }

        /// <summary>
        /// Gets the number of samples that are contained in one peak. 
        /// This is the threshold from which the PeakStore can be used to render a waveform.
        /// </summary>
        public int SamplesPerPeak { get { return samplesPerPeak; } }

        /// <summary>
        /// Gets the number of channels that this PeakStore is storing peaks for.
        /// </summary>
        public int Channels { get { return data.Length; } }

        /// <summary>
        /// Gets the number of peaks that are store for a channel.
        /// </summary>
        public int Length { get { return data[0].Length / PEAK_BYTE_SIZE; } }

        /// <summary>
        /// Returns an array of MemoryStreams where each stream contains the peaks for a channel.
        /// This method is needed for asynchronous read/write operations - each thread needs to call this
        /// method to obtain a separate instances of the streams. All streams operate on the same base data.
        /// </summary>
        /// <returns>an array of streams where each stream belongs to a channel</returns>
        public MemoryStream[] CreateMemoryStreams() {
            MemoryStream[] streams = new MemoryStream[data.Length];
            for (int channel = 0; channel < data.Length; channel++) {
                streams[channel] = new MemoryStream(data[channel]);
            }
            return streams;
        }

        public byte[][] GetData(int requestedSamplesPerPeak, out int samplesPerPeak) {
            foreach (int downscaledSamplesPerPeak in scaledData.Keys.Reverse()) {
                if (requestedSamplesPerPeak > downscaledSamplesPerPeak) {
                    samplesPerPeak = downscaledSamplesPerPeak;
                    return scaledData[downscaledSamplesPerPeak];
                }
            }
            samplesPerPeak = SamplesPerPeak;
            return data;
        }

        /// <summary>
        /// Reads serialized peaks from a data stream into the store.
        /// </summary>
        /// <param name="inputStream">the stream containing the serialized peaks</param>
        public void ReadFrom(Stream inputStream) {
            long inputPeakCount = inputStream.Length / PEAK_BYTE_SIZE / Channels;
            BinaryReader br = new BinaryReader(inputStream);
            BinaryWriter[] peakWriters = CreateMemoryStreams().WrapWithBinaryWriters();

            inputStream.Position = 0;
            for (long x = 0; x < inputPeakCount; x++) {
                for (int channel = 0; channel < Channels; channel++) {
                    peakWriters[channel].Write(br.ReadPeak());
                }
            }
        }

        /// <summary>
        /// Writes the peaks in this store for all channels to a stream in a serialized form.
        /// </summary>
        /// <param name="outputStream">the stream where the peaks will be written to</param>
        public void StoreTo(Stream outputStream) {
            BinaryWriter bw = new BinaryWriter(outputStream);
            BinaryReader[] peakReaders = CreateMemoryStreams().WrapWithBinaryReaders();

            for (int x = 0; x < Length; x++) {
                for (int channel = 0; channel < Channels; channel++) {
                    bw.Write(peakReaders[channel].ReadPeak());
                }
            }
        }

        public void CalculateScaledData(int scaleFactor, int steps) {
            int peakSize = 0;
            unsafe {
                peakSize = sizeof(Peak);
            }

            int previousStepSamplesPerPeak = SamplesPerPeak;
            for(int step = 1; step <= steps; step++) {
                int samplesPerPeak = previousStepSamplesPerPeak * scaleFactor;
                int peaksPerPeak = steps * scaleFactor;
                byte[][] peakData = step == 1 ? data : scaledData[previousStepSamplesPerPeak];
                int downscaledPeakDataLength = (int)Math.Ceiling((float)peakData[0].Length / scaleFactor);
                if(downscaledPeakDataLength % peakSize != 0) {
                    downscaledPeakDataLength += peakSize - (downscaledPeakDataLength % peakSize);
                }
                byte[][] downscaledPeakData = AudioUtil.CreateArray<byte>(peakData.Length, downscaledPeakDataLength);

                unsafe {
                    // calculate scaled peaks
                    for (int channel = 0; channel < Channels; channel++) {
                        int peakCount = peakData[0].Length / sizeof(Peak);
                        fixed (byte* peaksB = &peakData[channel][0], downscaledPeaksB = &downscaledPeakData[channel][0]) {
                            Peak* peaks = (Peak*)peaksB;
                            Peak* downscaledPeaks = (Peak*)downscaledPeaksB;
                            Peak downscaledPeak = new Peak(float.MaxValue, float.MinValue);
                            int downscaledPeakIndex = 0;
                            for (int p = 0; p < peakCount; p++) {
                                downscaledPeak.Merge(peaks[p]);
                                if ((p > 0 && p % scaleFactor == 0) || p == peakCount - 1) {
                                    downscaledPeaks[downscaledPeakIndex++] = downscaledPeak;
                                    downscaledPeak = new Peak(float.MaxValue, float.MinValue);
                                }
                            }
                        }
                    }
                }

                this.scaledData.Add(samplesPerPeak, downscaledPeakData);
                previousStepSamplesPerPeak = samplesPerPeak;
            }
        }
    }
}
