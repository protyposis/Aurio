using AudioAlign.FFmpeg;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AudioAlign.Audio.Streams {
    public class FFmpegSourceStream : IAudioStream {

        private FFmpegReader reader;
        private AudioProperties properties;
        private long readerPosition; // samples

        private byte[] sourceBuffer;
        private int sourceBufferLength; // samples
        private int sourceBufferPosition; // samples


        public FFmpegSourceStream(FileInfo fileInfo) {
            reader = new FFmpegReader(fileInfo);
            properties = new AudioProperties(
                reader.OutputConfig.format.channels,
                reader.OutputConfig.format.sample_rate,
                reader.OutputConfig.format.sample_size * 8,
                reader.OutputConfig.format.sample_size == 4 ? AudioFormat.IEEE : AudioFormat.LPCM);

            readerPosition = 0;
            sourceBuffer = new byte[reader.OutputConfig.frame_size * 
                reader.OutputConfig.format.channels * 
                reader.OutputConfig.format.sample_size];
            sourceBufferPosition = 0;
            sourceBufferLength = -1; // -1 means buffer empty, >= 0 means valid buffer data
        }

        public AudioProperties Properties {
            get { return properties; }
        }

        public long Length {
            get { return reader.OutputConfig.length * properties.SampleBlockByteSize; }
        }

        private long SamplePosition {
            get { return readerPosition + sourceBufferPosition; }
        }

        public long Position {
            get {
                return SamplePosition * SampleBlockSize;
            }
            set {
                long seekTarget = value / SampleBlockSize;

                // seek to target position
                reader.Seek(seekTarget);

                // get target position
                sourceBufferLength = reader.ReadFrame(out readerPosition, sourceBuffer, sourceBuffer.Length);

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
                    Console.WriteLine("should not happen!!!");
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
                sourceBufferLength = reader.ReadFrame(out readerPosition, sourceBuffer, sourceBuffer.Length);
                sourceBufferPosition = 0;

                if (sourceBufferLength == -1) {
                    return 0; // end of stream
                }
            }

            int bytesToCopy = Math.Min(count, (sourceBufferLength - sourceBufferPosition) * SampleBlockSize);
            Array.Copy(sourceBuffer, sourceBufferPosition * SampleBlockSize, buffer, offset, bytesToCopy);
            sourceBufferPosition += (bytesToCopy / SampleBlockSize);
            if (sourceBufferPosition > sourceBufferLength) {
                throw new Exception("overflow");
            }
            else if (sourceBufferPosition == sourceBufferLength) {
                // buffer read completely, need to read next frame
                sourceBufferLength = -1;
            }

            return bytesToCopy;
        }
    }
}
