using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using AudioAlign.Audio.DataStructures;

namespace AudioAlign.Audio.Streams {
    public class MixerStream : IAudioStream {

        private AudioProperties properties;
        private List<IAudioStream> sourceStreams;
        private long length;
        private long position;
        private ByteBuffer sourceBuffer;

        public MixerStream(int channels, int sampleRate) {
            properties = new AudioProperties(channels, sampleRate, 32, AudioFormat.IEEE);
            sourceStreams = new List<IAudioStream>();
            length = 0;
            position = 0;
            sourceBuffer = new ByteBuffer();
        }

        public void Add(IAudioStream sourceStream) {
            lock (this) {
                sourceStreams.Add(sourceStream);
                UpdateLength();
                sourceStream.Position = position;
            }
        }

        public void Remove(IAudioStream sourceStream) {
            lock (this) {
                sourceStreams.Remove(sourceStream);
                UpdateLength();
            }
        }

        public void Clear() {
            lock (this) {
                sourceStreams.Clear();
                UpdateLength();
            }
        }

        public AudioProperties Properties {
            get { return properties; }
        }

        public long Length {
            get { return length; }
        }

        public long Position {
            get {
                return position;
            }
            set {
                lock (this) {
                    position = value;
                    foreach (IAudioStream s in sourceStreams) {
                        s.Position = position;
                    }
                }
            }
        }

        public int SampleBlockSize {
            get { return properties.SampleByteSize * properties.Channels; }
        }

        public int SampleRate {
            get { return properties.SampleRate; }
            set { properties.SampleRate = value; }
        }

        public int Read(byte[] buffer, int offset, int count) {
            lock (this) {
                // check for end of stream
                if (position > length) {
                    return 0;
                }

                // dynamically increase buffer size
                sourceBuffer.ResizeIfTooSmall(count);

                // clear output buffer (because data won't be overwritten, but summed up)
                Array.Clear(buffer, offset, count);
                int maxBytesRead = 0;
                unsafe {
                    fixed (byte* outputByteBuffer = &buffer[offset], inputByteBuffer = &sourceBuffer.Data[0]) {
                        float* outputFloatBuffer = (float*)outputByteBuffer;
                        float* inputFloatBuffer = (float*)inputByteBuffer;
                        foreach (IAudioStream sourceStream in sourceStreams) {
                            if (sourceStream.Length <= position) {
                                continue;
                            }

                            // try to read requested amount of bytes
                            sourceBuffer.Clear();
                            int bytesRead = sourceBuffer.ForceFill(sourceStream, count);

                            // add stream data to output buffer
                            for (int x = 0; x < bytesRead / sizeof(float); x++) {
                                outputFloatBuffer[x] += inputFloatBuffer[x];
                            }

                            if (bytesRead > maxBytesRead) {
                                maxBytesRead = bytesRead;
                            }
                        }
                    }
                }
                position += maxBytesRead;
                return maxBytesRead;
            }
        }

        public void UpdateLength() {
            lock (this) {
                long length = 0;
                foreach (IAudioStream s in sourceStreams) {
                    length = Math.Max(length, s.Length);
                }
                this.length = length;
            }
        }
    }
}
