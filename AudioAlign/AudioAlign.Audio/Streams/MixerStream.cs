using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace AudioAlign.Audio.Streams {
    public class MixerStream : IAudioStream {

        private AudioProperties properties;
        private List<IAudioStream> sourceStreams;
        private long length;
        private long position;
        private byte[] sourceBuffer;

        public MixerStream(int channels, int sampleRate) {
            properties = new AudioProperties(channels, sampleRate, 32, AudioFormat.IEEE);
            sourceStreams = new List<IAudioStream>();
            length = 0;
            position = 0;
            sourceBuffer = new byte[0];
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

        public int Read(byte[] buffer, int offset, int count) {
            lock (this) {
                // check for end of stream
                if (position > length) {
                    return 0;
                }

                // dynamically increase buffer size
                if (sourceBuffer.Length < count) {
                    int oldSize = sourceBuffer.Length;
                    sourceBuffer = new byte[count];
                    Debug.WriteLine("MixerStream: buffer size increased: " + oldSize + " -> " + count);
                }

                // clear output buffer
                Array.Clear(buffer, offset, count);
                int maxTotalBytesRead = 0;
                unsafe {
                    fixed (byte* outputByteBuffer = &buffer[offset], inputByteBuffer = &sourceBuffer[0]) {
                        float* outputFloatBuffer = (float*)outputByteBuffer;
                        float* inputFloatBuffer = (float*)inputByteBuffer;
                        foreach (IAudioStream sourceStream in sourceStreams) {
                            if (sourceStream.Length <= position) {
                                continue;
                            }

                            int bytesRead = 0;
                            int totalBytesRead = 0;

                            // try to read requested amount of bytes
                            while (count - totalBytesRead > 0 && (bytesRead = sourceStream.Read(sourceBuffer, totalBytesRead, count - totalBytesRead)) > 0) {
                                totalBytesRead += bytesRead;
                            }

                            // if end of underlying stream has been reached, fill up with zeros
                            if (totalBytesRead < count) {
                                Array.Clear(sourceBuffer, totalBytesRead, count - totalBytesRead);
                            }

                            // add stream data to output buffer
                            for (int x = 0; x < totalBytesRead / sizeof(float); x++) {
                                outputFloatBuffer[x] += inputFloatBuffer[x];
                            }

                            if (totalBytesRead > maxTotalBytesRead) {
                                maxTotalBytesRead = totalBytesRead;
                            }
                        }
                    }
                }
                position += maxTotalBytesRead;
                return maxTotalBytesRead;
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
