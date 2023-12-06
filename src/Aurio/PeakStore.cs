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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Aurio.Streams;
using Aurio.TaskMonitor;

namespace Aurio
{
    public class PeakStore
    {
        private static readonly int PEAK_BYTE_SIZE;
        private static readonly char[] MAGICNUMBER = { 'A', 'A', 'P', 'K' }; // AurioAudioPeaKfile

        /// <summary>
        /// The first file format didn't have a header and just contained the raw peaks.
        ///
        /// Format 2 adds a header with:
        /// - magic number
        /// - version number (int32)
        /// - audio file last modified date
        /// The LMD serves to check if a peak file is still valid for an audio file; if the audio file
        /// has changed, the peakfile is invalid (and a new one gets generated).
        /// </summary>
        private const int FILEFORMAT = 2;
        private const int HEADERSIZE =
            4 /* magic number */
            + sizeof(int)
            + sizeof(long);

        static PeakStore()
        {
            unsafe
            {
                PEAK_BYTE_SIZE = sizeof(Peak);
            }
        }

        public event EventHandler PeaksChanged;

        private int samplesPerPeak;
        private byte[][] data;
        private Dictionary<int, byte[][]> scaledData;

        /// <summary>
        /// Creates a PeakStore that stores a given number of peaks for a given number of channels.
        /// </summary>
        /// <param name="channels">the number of channels to store peaks for</param>
        /// <param name="peaksPerChannel">the number of peaks to store for each channel</param>
        /// <param name="samplesPerPeak">the number of samples that are merged into a peak</param>
        public PeakStore(int samplesPerPeak, int channels, int peaksPerChannel)
        {
            this.samplesPerPeak = samplesPerPeak;
            this.data = AudioUtil.CreateArray<byte>(channels, peaksPerChannel * PEAK_BYTE_SIZE);

            scaledData = new Dictionary<int, byte[][]>();
        }

        /// <summary>
        /// Gets the number of samples that are contained in one peak.
        /// This is the threshold from which the PeakStore can be used to render a waveform.
        /// </summary>
        public int SamplesPerPeak
        {
            get { return samplesPerPeak; }
        }

        /// <summary>
        /// Gets the number of channels that this PeakStore is storing peaks for.
        /// </summary>
        public int Channels
        {
            get { return data.Length; }
        }

        /// <summary>
        /// Gets the number of peaks that are store for a channel.
        /// </summary>
        public int Length
        {
            get { return data[0].Length / PEAK_BYTE_SIZE; }
        }

        /// <summary>
        /// Returns an array of MemoryStreams where each stream contains the peaks for a channel.
        /// This method is needed for asynchronous read/write operations - each thread needs to call this
        /// method to obtain a separate instances of the streams. All streams operate on the same base data.
        /// </summary>
        /// <returns>an array of streams where each stream belongs to a channel</returns>
        public MemoryStream[] CreateMemoryStreams()
        {
            MemoryStream[] streams = new MemoryStream[data.Length];
            for (int channel = 0; channel < data.Length; channel++)
            {
                streams[channel] = new MemoryStream(data[channel]);
            }
            return streams;
        }

        public byte[][] GetData(int requestedSamplesPerPeak, out int samplesPerPeak)
        {
            foreach (int downscaledSamplesPerPeak in scaledData.Keys.Reverse())
            {
                if (requestedSamplesPerPeak > downscaledSamplesPerPeak)
                {
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
        public void ReadFrom(Stream inputStream, DateTime modifiedDate)
        {
            long inputPeakCount = (inputStream.Length - HEADERSIZE) / PEAK_BYTE_SIZE / Channels;
            BinaryReader br = new BinaryReader(inputStream);
            BinaryWriter[] peakWriters = CreateMemoryStreams().WrapWithBinaryWriters();

            inputStream.Position = 0;

            // read header
            var magicNumber = br.ReadChars(4);
            var fileFormat = br.ReadInt32();
            var date = br.ReadInt64();

            if (!magicNumber.SequenceEqual(MAGICNUMBER) || fileFormat != FILEFORMAT)
            {
                throw new Exception("invalid peak file");
            }
            if (date != modifiedDate.Ticks)
            {
                throw new Exception("peakfile date does not match audio file modification date");
            }

            // read payload
            for (long x = 0; x < inputPeakCount; x++)
            {
                for (int channel = 0; channel < Channels; channel++)
                {
                    peakWriters[channel].Write(br.ReadPeak());
                }
            }
        }

        /// <summary>
        /// Writes the peaks in this store for all channels to a stream in a serialized form.
        /// </summary>
        /// <param name="outputStream">the stream where the peaks will be written to</param>
        public void StoreTo(Stream outputStream, DateTime modifiedDate)
        {
            BinaryWriter bw = new BinaryWriter(outputStream);
            BinaryReader[] peakReaders = CreateMemoryStreams().WrapWithBinaryReaders();

            // write header
            bw.Write(MAGICNUMBER);
            bw.Write(FILEFORMAT);
            bw.Write(modifiedDate.Ticks);

            // write payload
            for (int x = 0; x < Length; x++)
            {
                for (int channel = 0; channel < Channels; channel++)
                {
                    bw.Write(peakReaders[channel].ReadPeak());
                }
            }
        }

        public void CalculateScaledData(int scaleFactor, int steps)
        {
            int peakSize = 0;
            unsafe
            {
                peakSize = sizeof(Peak);
            }

            int previousStepSamplesPerPeak = SamplesPerPeak;
            for (int step = 1; step <= steps; step++)
            {
                int samplesPerPeak = previousStepSamplesPerPeak * scaleFactor;
                int peaksPerPeak = steps * scaleFactor;
                byte[][] peakData = step == 1 ? data : scaledData[previousStepSamplesPerPeak];
                int downscaledPeakDataLength = (int)
                    Math.Ceiling((float)peakData[0].Length / scaleFactor);
                if (downscaledPeakDataLength % peakSize != 0)
                {
                    downscaledPeakDataLength += peakSize - (downscaledPeakDataLength % peakSize);
                }
                byte[][] downscaledPeakData = AudioUtil.CreateArray<byte>(
                    peakData.Length,
                    downscaledPeakDataLength
                );
                unsafe
                {
                    // calculate scaled peaks
                    for (int channel = 0; channel < Channels; channel++)
                    {
                        int peakCount = peakData[0].Length / sizeof(Peak);
                        fixed (
                            byte* peaksB = &peakData[channel][0],
                                downscaledPeaksB = &downscaledPeakData[channel][0]
                        )
                        {
                            Peak* peaks = (Peak*)peaksB;
                            Peak* downscaledPeaks = (Peak*)downscaledPeaksB;
                            Peak downscaledPeak = new Peak(float.MaxValue, float.MinValue);
                            int downscaledPeakIndex = 0;
                            for (int p = 0; p < peakCount; p++)
                            {
                                downscaledPeak.Merge(peaks[p]);
                                if ((p > 0 && p % scaleFactor == 0) || p == peakCount - 1)
                                {
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

        public void OnPeaksChanged()
        {
            if (PeaksChanged != null)
            {
                PeaksChanged(this, EventArgs.Empty);
            }
        }

        public void Fill(IAudioStream audioInputStream, IProgressReporter progressReporter = null)
        {
            var channels = Channels;
            var buffer = new byte[65536 * audioInputStream.SampleBlockSize];
            var min = new float[channels];
            var max = new float[channels];
            var peakWriters = CreateMemoryStreams().WrapWithBinaryWriters();

            var sampleBlockCount = 0;
            var totalSampleBlocks = audioInputStream.Length / audioInputStream.SampleBlockSize;

            for (int i = 0; i < channels; i++)
            {
                min[i] = float.MaxValue;
                max[i] = float.MinValue;
            }

            unsafe
            {
                fixed (byte* bufferB = &buffer[0])
                {
                    var bufferF = (float*)bufferB;
                    var peakStoreFull = false;
                    int bytesRead;

                    while (
                        (
                            bytesRead = StreamUtil.ForceRead(
                                audioInputStream,
                                buffer,
                                0,
                                buffer.Length
                            )
                        ) > 0
                    )
                    {
                        var samplesRead = bytesRead / audioInputStream.Properties.SampleByteSize;
                        var samplesProcessed = 0;

                        do
                        {
                            for (int channel = 0; channel < channels; channel++)
                            {
                                if (min[channel] > bufferF[samplesProcessed])
                                {
                                    min[channel] = bufferF[samplesProcessed];
                                }
                                if (max[channel] < bufferF[samplesProcessed])
                                {
                                    max[channel] = bufferF[samplesProcessed];
                                }
                                samplesProcessed++;
                            }

                            if (
                                ++sampleBlockCount % samplesPerPeak == 0
                                || sampleBlockCount == totalSampleBlocks
                            )
                            {
                                // write peak
                                for (int channel = 0; channel < channels; channel++)
                                {
                                    peakWriters[channel].Write(
                                        new Peak(min[channel], max[channel])
                                    );
                                    // add last sample of previous peak as first sample of current peak to make consecutive peaks overlap
                                    // this gives the impression of a continuous waveform
                                    min[channel] = max[channel] = bufferF[
                                        samplesProcessed - channels
                                    ];
                                }
                                //sampleBlockCount = 0;
                            }

                            if (
                                sampleBlockCount == totalSampleBlocks
                                && samplesProcessed < samplesRead
                            )
                            {
                                // There's no more space for more peaks
                                // TODO how to handle this case? why is there still audio data left?
                                Console.WriteLine(
                                    "peakstore full, but there are samples left ({0} < {1})",
                                    samplesProcessed,
                                    samplesRead
                                );
                                peakStoreFull = true;
                                break;
                            }
                        } while (samplesProcessed < samplesRead);

                        progressReporter?.ReportProgress(
                            100.0f / audioInputStream.Length * audioInputStream.Position
                        );

                        if (peakStoreFull)
                        {
                            break;
                        }
                    }
                }
            }

            CalculateScaledData(8, 6);
        }
    }
}
