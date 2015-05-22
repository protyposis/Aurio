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
using Aurio.Streams;

namespace Aurio {
    public static class StreamUtil {

        public static int ForceRead(IAudioStream audioStream, byte[] buffer, int offset, int count) {
            int totalBytesRead = 0;
            int bytesRead = 0;

            while (count - totalBytesRead > 0 && (bytesRead = audioStream.Read(buffer, offset + totalBytesRead, count - totalBytesRead)) > 0) {
                totalBytesRead += bytesRead;
            }

            return totalBytesRead;
        }

        public static int ForceReadIntervalSamples(IAudioStream s, Interval i, float[] array) {
            s.Position = TimeUtil.TimeSpanToBytes(i.TimeFrom, s.Properties);
            long bytesRead = 0;
            long samplesToRead = TimeUtil.TimeSpanToBytes(i.TimeLength, s.Properties) / s.Properties.SampleByteSize;
            int totalSamplesRead = 0;
            int channels = s.Properties.Channels;
            byte[] temp = new byte[1024 * 32 * channels];

            if (samplesToRead > array.Length) {
                throw new ArgumentException("cannot read the requested interval (" + samplesToRead 
                    + ") - the target array is too small (" + array.Length + ")");
            }

            while ((bytesRead = s.Read(temp, 0, temp.Length)) > 0) {
                unsafe {
                    fixed (byte* sampleBuffer = &temp[0]) {
                        float* samples = (float*)sampleBuffer;
                        for (int x = 0; x < bytesRead / 4; x++) {
                            array[totalSamplesRead++] = samples[x];
                            if (samplesToRead == totalSamplesRead) {
                                return totalSamplesRead;
                            }
                        }
                    }
                }
            }

            return totalSamplesRead;
        }

        public static long AlignToBlockSize(long value, int blockSize) {
            if (value % blockSize != blockSize) {
                return value - (value % blockSize);
            }
            return value;
        }

        /// <summary>
        /// Reads all bytes until the end of the stream and returns the number of bytes read.
        /// </summary>
        /// <remarks>
        /// This method is intended for testing and debugging.
        /// </remarks>
        public static long ReadAllAndCount(IAudioStream s) {
            var temp = new byte[1024*1024];
            long totalBytesRead = 0;
            int bytesRead;

            while ((bytesRead = s.Read(temp, 0, temp.Length)) > 0) {
                totalBytesRead += bytesRead;
            }

            return totalBytesRead;
        }
    }
}
