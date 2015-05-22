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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Aurio.DataStructures;

namespace Aurio.Streams {
    public class IeeeStream : AbstractAudioStreamWrapper {

        private AudioProperties properties;
        private bool passthrough;

        private delegate int DReadAndConvert(byte[] buffer, int offset, int count);
        private DReadAndConvert ReadAndConvert;
        private ByteBuffer sourceBuffer;


        public IeeeStream(IAudioStream sourceStream)
            : base(sourceStream) {
            if (sourceStream.Properties.Format == AudioFormat.IEEE && sourceStream.Properties.BitDepth == 32) {
                passthrough = true;
                properties = sourceStream.Properties;
            }
            else if (sourceStream.Properties.Format == AudioFormat.LPCM && sourceStream.Properties.BitDepth == 16) {
                ReadAndConvert = ReadPCM16;
                properties = new AudioProperties(sourceStream.Properties.Channels,
                    sourceStream.Properties.SampleRate, 32, AudioFormat.IEEE);
            }
            else if (sourceStream.Properties.Format == AudioFormat.LPCM && sourceStream.Properties.BitDepth == 24) {
                ReadAndConvert = ReadPCM24;
                properties = new AudioProperties(sourceStream.Properties.Channels, 
                    sourceStream.Properties.SampleRate, 32, AudioFormat.IEEE);
            }
            else {
                throw new ArgumentException("unsupported source format: " + sourceStream.Properties);
            }

            sourceBuffer = new ByteBuffer();
        }

        public override AudioProperties Properties {
            get { return properties; }
        }

        public override long Length {
            get { return sourceStream.Length / sourceStream.SampleBlockSize * SampleBlockSize; }
        }

        public override long Position {
            get { return sourceStream.Position / sourceStream.SampleBlockSize * SampleBlockSize; }
            set { sourceStream.Position = value / SampleBlockSize * sourceStream.SampleBlockSize; }
        }

        public override int SampleBlockSize {
            get { return properties.SampleBlockByteSize; }
        }

        public override int Read(byte[] buffer, int offset, int count) {
            if (passthrough) {
                return sourceStream.Read(buffer, offset, count);
            }

            return ReadAndConvert(buffer, offset, count);
        }

        private int ReadPCM16(byte[] buffer, int offset, int count) {
            int sourceBytesToRead = count / SampleBlockSize * sourceStream.SampleBlockSize;
            sourceBuffer.FillIfEmpty(sourceStream, sourceBytesToRead);
            int samples = sourceBuffer.Count / 2; // #bytes / 2 = #shorts

            unsafe {
                fixed (byte* sourceByteBuffer = &sourceBuffer.Data[0], targetByteBuffer = &buffer[offset]) {
                    short* sourceShortBuffer = (short*)sourceByteBuffer;
                    float* targetFloatBuffer = (float*)targetByteBuffer;

                    for (int x = 0; x < samples; x++) {
                        targetFloatBuffer[x] = (float)sourceShortBuffer[x] / short.MaxValue;
                    }
                }
            }

            sourceBuffer.Clear();

            return samples * 4;
        }

        private int ReadPCM24(byte[] buffer, int offset, int count) {
            int sourceBytesToRead = count / SampleBlockSize * sourceStream.SampleBlockSize;
            sourceBuffer.FillIfEmpty(sourceStream, sourceBytesToRead);
            int samples = sourceBuffer.Count / 3; // #bytes / 3 = #24bitsamples

            unsafe {
                fixed (byte* targetByteBuffer = &buffer[offset]) {
                    float* targetFloatBuffer = (float*)targetByteBuffer;
                    for (int x = 0; x < sourceBuffer.Count; x += 3) {
                        targetFloatBuffer[x / 3] = (sourceBuffer.Data[x] << 8 | sourceBuffer.Data[x + 1] << 16 | sourceBuffer.Data[x + 2] << 24) / 2147483648f;
                    }
                }
            }

            sourceBuffer.Clear();

            return samples * 4;
        }
    }
}
