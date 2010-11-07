using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.CompilerServices;

namespace AudioAlign.Audio {
    public class PeakStore {

        private const int PEAK_BYTE_SIZE = 2 * 4; // 2 4-byte single numbers

        private byte[][] data;
        private MemoryStream[] streams;

        /// <summary>
        /// Creates a PeakStore that stores a given number of peaks for a given number of channels.
        /// </summary>
        /// <param name="channels">the number of channels to store peaks for</param>
        /// <param name="peaksPerChannel">the number of peaks to store for each channel</param>
        public PeakStore(int channels, int peaksPerChannel) {
            data = AudioUtil.CreateArray<byte>(channels, peaksPerChannel * PEAK_BYTE_SIZE);
            streams = CreateMemoryStreams();
        }

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
    }
}
