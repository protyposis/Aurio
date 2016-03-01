// 
// Aurio: Audio Processing, Analysis and Retrieval Library
// Copyright (C) 2010-2016  Mario Guggenberger <mg@protyposis.net>
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
using System.Linq;
using System.Text;
using System.Diagnostics;
using Aurio.DataStructures;

namespace Aurio.Streams {
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

        public void Close() {
            foreach (IAudioStream s in sourceStreams) {
                s.Close();
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

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    Close();
                }

                disposedValue = true;
            }
        }

        public void Dispose() {
            Dispose(true);
        }
        #endregion
    }
}
