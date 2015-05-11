using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioAlign.Audio.Streams {
    /// <summary>
    /// A stream to concatenate multiple source streams sequentially into a single longer stream.
    /// </summary>
    public class ConcatenationStream : IAudioStream {

        private IAudioStream[] sourceStreams;

        private long length;
        private long positionOffset;
        private int currentStreamIndex;
        private IAudioStream currentStream;

        /// <summary>
        /// Creates a concatenated stream from the supplied source streams in the order of specification.
        /// </summary>
        /// <param name="sourceStreams">the source streams to concatenate in their given order</param>
        public ConcatenationStream(params IAudioStream[] sourceStreams) {
            this.sourceStreams = sourceStreams;

            currentStreamIndex = 0;
            currentStream = sourceStreams[currentStreamIndex];

            // Check for same format
            foreach (var stream in sourceStreams) {
                if (stream.Properties.SampleRate != currentStream.Properties.SampleRate ||
                    stream.Properties.BitDepth != currentStream.Properties.BitDepth ||
                    stream.Properties.Channels != currentStream.Properties.Channels ||
                    stream.Properties.Format != currentStream.Properties.Format)
                {
                    throw new ArgumentException("the formats of the supplied streams do not match");
                }
            }

            length = sourceStreams.Sum(s => s.Length);
            positionOffset = 0;
        }

        public AudioProperties Properties {
            get { return currentStream.Properties; }
        }

        public long Length {
            get { return length; }
        }

        public long Position {
            get {
                return positionOffset + currentStream.Position;
            }
            set {
                if (value < positionOffset || value >= positionOffset + currentStream.Length) {
                    // We need to switch to a different source stream
                    positionOffset = 0;
                    for (int i = 0; i < sourceStreams.Length; i++) {
                        if (value < positionOffset + sourceStreams[i].Length) {
                            currentStreamIndex = i;
                            currentStream = sourceStreams[i];
                            break;
                        }
                        positionOffset += sourceStreams[i].Length;
                    }
                    if (positionOffset == length) {
                        // A seek to the end of the stream or beyond... 
                        // ...we set the position to the end of the last stream instead of after the last stream
                        currentStreamIndex = sourceStreams.Length - 1;
                        currentStream = sourceStreams[currentStreamIndex];
                        positionOffset -= currentStream.Length;
                    }
                }

                // Seeking the current source stream
                currentStream.Position = value - positionOffset;
            }
        }

        public int SampleBlockSize {
            get { return currentStream.SampleBlockSize; }
        }

        public int Read(byte[] buffer, int offset, int count) {
            if (currentStream.Position == currentStream.Length) {
                if (currentStreamIndex == sourceStreams.Length - 1) {
                    // End of last stream
                    return 0;
                }

                // Switch to next source stream
                positionOffset += currentStream.Length;
                currentStream = sourceStreams[++currentStreamIndex];
                currentStream.Position = 0;
            }

            return currentStream.Read(buffer, offset, count);
        }
    }
}
