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
using Aurio.FFmpeg;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Aurio.Streams {
    public class FFmpegSourceStream : IAudioStream {

        private FFmpegReader reader;
        private AudioProperties properties;
        private long readerPosition; // samples
        private long readerFirstPTS; // samples

        private byte[] sourceBuffer;
        private int sourceBufferLength; // samples
        private int sourceBufferPosition; // samples

        public FFmpegSourceStream(FileInfo fileInfo) : this(fileInfo.OpenRead()) {
            //reader = new FFmpegReader(fileInfo); // use filesystem IO
            //reader = new FFmpegReader(fileInfo.OpenRead()); // use buffered IO with stream
        }

        public FFmpegSourceStream(Stream stream) {
            reader = new FFmpegReader(stream, FFmpeg.Type.Audio);

            if (reader.AudioOutputConfig.length == long.MinValue) {
                /* 
                 * length == FFmpeg AV_NOPTS_VALUE
                 * 
                 * This means that for the opened file/format, there is no length/PTS data 
                 * available, which also makes seeking more or less impossible.
                 * 
                 * As a workaround, an index could be created to map the frames to the file
                 * position, and then seek by file position. The index could be created by 
                 * linearly reading through the file (decoding not necessary), and creating
                 * a mapping of AVPacket.pos to the frame time.
                 */
                throw new FileNotSeekableException();
            }

            properties = new AudioProperties(
                reader.AudioOutputConfig.format.channels,
                reader.AudioOutputConfig.format.sample_rate,
                reader.AudioOutputConfig.format.sample_size * 8,
                reader.AudioOutputConfig.format.sample_size == 4 ? AudioFormat.IEEE : AudioFormat.LPCM);

            readerPosition = 0;
            sourceBuffer = new byte[reader.AudioOutputConfig.frame_size * 
                reader.AudioOutputConfig.format.channels * 
                reader.AudioOutputConfig.format.sample_size];
            sourceBufferPosition = 0;
            sourceBufferLength = -1; // -1 means buffer empty, >= 0 means valid buffer data

            // determine first PTS to handle cases where it is > 0
            try {
                Position = 0;
            }
            catch(InvalidOperationException) {
                readerFirstPTS = readerPosition;
                readerPosition = 0;
                Console.WriteLine("first PTS = " + readerFirstPTS);
            }
        }

        public AudioProperties Properties {
            get { return properties; }
        }

        public long Length {
            get { return reader.AudioOutputConfig.length * properties.SampleBlockByteSize; }
        }

        private long SamplePosition {
            get { return readerPosition + sourceBufferPosition; }
        }

        public long Position {
            get {
                return SamplePosition * SampleBlockSize;
            }
            set {
                long seekTarget = (value / SampleBlockSize) + readerFirstPTS;

                // seek to target position
                reader.Seek(seekTarget, FFmpeg.Type.Audio);

                // get target position
                FFmpeg.Type type;
                sourceBufferLength = reader.ReadFrame(out readerPosition, sourceBuffer, sourceBuffer.Length, out type);

                // check if seek ended up at seek target (or earlier because of frame size, depends on file format and stream codec)
                // TODO handle seek offset with bufferPosition
                if (seekTarget == readerPosition) {
                    // perfect case
                    sourceBufferPosition = 0;
                }
                else if(seekTarget > readerPosition && seekTarget <= (readerPosition + sourceBufferLength)) {
                    sourceBufferPosition = (int)(seekTarget - readerPosition);
                }
                else if (seekTarget < readerPosition) {
                    throw new InvalidOperationException("illegal state");
                }

                // seek back to seek point for successive reads to return expected data (not one frame in advance) PROBABLY NOT NEEDED
                // TODO handle this case, e.g. when it is necessery and when it isn't (e.g. when block is chached because of bufferPosition > 0)
                //reader.Seek(readerPosition);
            }
        }

        public int SampleBlockSize {
            get { return properties.SampleBlockByteSize; }
        }

        public int Read(byte[] buffer, int offset, int count) {
            if (sourceBufferLength == -1) {
                long newPosition;
                FFmpeg.Type type;
                sourceBufferLength = reader.ReadFrame(out newPosition, sourceBuffer, sourceBuffer.Length, out type);

                if (newPosition == -1 || sourceBufferLength == -1) {
                    return 0; // end of stream
                }

                readerPosition = newPosition;
                sourceBufferPosition = 0;
            }

            int bytesToCopy = Math.Min(count, (sourceBufferLength - sourceBufferPosition) * SampleBlockSize);
            Array.Copy(sourceBuffer, sourceBufferPosition * SampleBlockSize, buffer, offset, bytesToCopy);
            sourceBufferPosition += (bytesToCopy / SampleBlockSize);
            if (sourceBufferPosition > sourceBufferLength) {
                throw new InvalidOperationException("overflow");
            }
            else if (sourceBufferPosition == sourceBufferLength) {
                // buffer read completely, need to read next frame
                sourceBufferLength = -1;
            }

            return bytesToCopy;
        }

        public static FileInfo CreateWaveProxy(FileInfo fileInfo) {
            var outputFileInfo = new FileInfo(fileInfo.FullName + ".ffproxy.wav");

            if (outputFileInfo.Exists) {
                Console.WriteLine("Proxy already existing, using " + outputFileInfo.Name);
                return outputFileInfo;
            }

            var reader = new FFmpegReader(fileInfo, FFmpeg.Type.Audio);

            // workaround to get NAudio WaveFormat (instead of creating it manually here)
            var mss = new MemorySourceStream(null, new AudioProperties(
                reader.AudioOutputConfig.format.channels, 
                reader.AudioOutputConfig.format.sample_rate, 
                reader.AudioOutputConfig.format.sample_size * 8, 
                reader.AudioOutputConfig.format.sample_size == 4 ? AudioFormat.IEEE : AudioFormat.LPCM));
            var nass = new NAudioSinkStream(mss);
            var waveFormat = nass.WaveFormat;

            var writer = new WaveFileWriter(outputFileInfo.FullName, waveFormat);

            int output_buffer_size = reader.AudioOutputConfig.frame_size * mss.SampleBlockSize;
            byte[] output_buffer = new byte[output_buffer_size];

            int samplesRead;
            long timestamp;
            FFmpeg.Type type;

            // sequentially read samples from decoder and write it to wav file
            while ((samplesRead = reader.ReadFrame(out timestamp, output_buffer, output_buffer_size, out type)) > 0) {
                int bytesRead = samplesRead * mss.SampleBlockSize;
                writer.Write(output_buffer, 0, bytesRead);
            }

            reader.Dispose();
            writer.Close();

            return outputFileInfo;
        }

        public class FileNotSeekableException : Exception {
            public FileNotSeekableException() : base() { }
            public FileNotSeekableException(string message) : base(message) { }
            public FileNotSeekableException(string message, Exception innerException) : base(message, innerException) { }
        }
    }
}
