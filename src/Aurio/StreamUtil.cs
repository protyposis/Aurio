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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using Aurio.Streams;

namespace Aurio
{
    public static class StreamUtil
    {
        public const float FLOAT_EPSILON = 0.0000001f;

        public static int ForceRead(IAudioStream audioStream, byte[] buffer, int offset, int count)
        {
            int totalBytesRead = 0;
            int bytesRead = 0;

            while (
                count - totalBytesRead > 0
                && (
                    bytesRead = audioStream.Read(
                        buffer,
                        offset + totalBytesRead,
                        count - totalBytesRead
                    )
                ) > 0
            )
            {
                totalBytesRead += bytesRead;
            }

            return totalBytesRead;
        }

        public static int ForceReadIntervalSamples(IAudioStream s, Interval i, float[] array)
        {
            s.Position = TimeUtil.TimeSpanToBytes(i.TimeFrom, s.Properties);
            long bytesRead = 0;
            long samplesToRead =
                TimeUtil.TimeSpanToBytes(i.TimeLength, s.Properties) / s.Properties.SampleByteSize;
            int totalSamplesRead = 0;
            int channels = s.Properties.Channels;
            byte[] temp = new byte[1024 * 32 * channels];

            if (samplesToRead > array.Length)
            {
                throw new ArgumentException(
                    "cannot read the requested interval ("
                        + samplesToRead
                        + ") - the target array is too small ("
                        + array.Length
                        + ")"
                );
            }

            while ((bytesRead = s.Read(temp, 0, temp.Length)) > 0)
            {
                unsafe
                {
                    fixed (byte* sampleBuffer = &temp[0])
                    {
                        float* samples = (float*)sampleBuffer;
                        for (int x = 0; x < bytesRead / 4; x++)
                        {
                            array[totalSamplesRead++] = samples[x];
                            if (samplesToRead == totalSamplesRead)
                            {
                                return totalSamplesRead;
                            }
                        }
                    }
                }
            }

            return totalSamplesRead;
        }

        public static long AlignToBlockSize(long value, int blockSize)
        {
            if (value % blockSize != blockSize)
            {
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
        public static long ReadAllAndCount(IAudioStream s)
        {
            var temp = new byte[1024 * 1024];
            long totalBytesRead = 0;
            int bytesRead;

            while ((bytesRead = s.Read(temp, 0, temp.Length)) > 0)
            {
                totalBytesRead += bytesRead;
            }

            return totalBytesRead;
        }

        /// <summary>
        /// Compares two streams byte by byte and returns the number of similar bytes.
        /// </summary>
        /// <param name="stream1">the first stream to compare</param>
        /// <param name="stream2">the second stream to compare</param>
        /// <returns>the number of similar bytes</returns>
        public static long CompareBytes(IAudioStream stream1, IAudioStream stream2)
        {
            byte[] buffer1 = new byte[10000 * stream1.SampleBlockSize];
            byte[] buffer2 = new byte[10000 * stream2.SampleBlockSize];

            int s1BytesRead = 0;
            int s2BytesRead = 0;

            int bytesToRead = (int)
                Math.Min(
                    Math.Min(buffer1.Length, stream1.Length - stream1.Position),
                    Math.Min(buffer2.Length, stream2.Length - stream2.Position)
                );

            long similarBytes = 0;

            while (
                (s1BytesRead = ForceRead(stream1, buffer1, 0, bytesToRead)) > 0
                && (s2BytesRead = ForceRead(stream2, buffer2, 0, bytesToRead)) > 0
            )
            {
                if (s1BytesRead != s2BytesRead)
                {
                    // Because of the calculation of bytesToRead, which is the minimum that can be read from both streams, this should never happen
                    throw new Exception("invalid state, shall not happen");
                }

                bool abortComparison = false;
                for (int i = 0; i < s1BytesRead; i++)
                {
                    if (buffer1[i] == buffer2[i])
                    {
                        similarBytes++;
                    }
                    else
                    {
                        // When one byte is different, do not compare any following bytes
                        abortComparison = true;
                        break;
                    }
                }

                if (abortComparison)
                {
                    break;
                }
            }

            return similarBytes;
        }

        /// <summary>
        /// Compares two streams float by float and returns the number of similar floats.
        /// </summary>
        /// <param name="stream1">the first stream to compare</param>
        /// <param name="stream2">the second stream to compare</param>
        /// <param name="epsilon">the allowed variance with which two floats are still considered equal (accounts for floating point inccuracy)</param>
        /// <returns>the number of similar floats</returns>
        public static long CompareFloats(IAudioStream stream1, IAudioStream stream2, float epsilon)
        {
            if (
                stream1.Properties.Format != AudioFormat.IEEE
                || stream1.Properties.Format != AudioFormat.IEEE
            )
            {
                throw new ArgumentException("streams must be in 32bit float format");
            }

            byte[] buffer1 = new byte[1000 * stream1.SampleBlockSize];
            byte[] buffer2 = new byte[1000 * stream2.SampleBlockSize];

            int s1BytesRead = 0;
            int s2BytesRead = 0;

            int bytesToRead = (int)
                Math.Min(
                    Math.Min(buffer1.Length, stream1.Length - stream1.Position),
                    Math.Min(buffer2.Length, stream2.Length - stream2.Position)
                );

            long similarFloats = 0;

            while (
                (s1BytesRead = ForceRead(stream1, buffer1, 0, bytesToRead)) > 0
                && (s2BytesRead = ForceRead(stream2, buffer2, 0, bytesToRead)) > 0
            )
            {
                if (s1BytesRead != s2BytesRead)
                {
                    // Because of the calculation of bytesToRead, which is the minimum that can be read from both streams, this should never happen
                    throw new Exception("invalid state, shall not happen");
                }

                bool abortComparison = false;
                unsafe
                {
                    fixed (
                        byte* pBuffer1 = &buffer1[0],
                            pBuffer2 = &buffer2[0]
                    )
                    {
                        float* fBuffer1 = (float*)pBuffer1;
                        float* fBuffer2 = (float*)pBuffer2;

                        for (int i = 0; i < s1BytesRead / stream1.SampleBlockSize; i++)
                        {
                            if (Math.Abs(fBuffer1[i] - fBuffer2[i]) < epsilon)
                            {
                                similarFloats++;
                            }
                            else
                            {
                                // When one float is different, do not compare any following floats
                                abortComparison = true;
                                break;
                            }
                        }
                    }
                }

                if (abortComparison)
                {
                    break;
                }
            }

            return similarFloats;
        }

        /// <summary>
        /// Compares two streams float by float and returns the number of similar floats.
        /// </summary>
        /// <param name="stream1">the first stream to compare</param>
        /// <param name="stream2">the second stream to compare</param>
        /// <returns>the number of similar floats</returns>
        public static long CompareFloats(IAudioStream stream1, IAudioStream stream2)
        {
            return CompareFloats(stream1, stream2, FLOAT_EPSILON);
        }

        /// <summary>
        /// Reads the specified number of samples from the given stream and returns them as a byte array.
        /// May return less samples if the end of stream is reached.
        /// </summary>
        /// <param name="stream">the stream to read the samples from</param>
        /// <param name="samples">then number of samples to read</param>
        /// <returns>a byte array containing the samples</returns>
        public static byte[] ReadBytes(IAudioStream stream, int samples)
        {
            var bufferSize = samples * stream.SampleBlockSize;
            var buffer = new byte[bufferSize];
            var bytesRead = stream.Read(buffer, 0, bufferSize);

            if (bytesRead < bufferSize)
            {
                Array.Resize(ref buffer, bytesRead);
            }

            return buffer;
        }

        /// <summary>
        /// Reads the specified number of samples from the given stream and returns them as a float array.
        /// May return less samples if the end of stream is reached.
        /// </summary>
        /// <param name="stream">the stream to read the samples from</param>
        /// <param name="samples">then number of samples to read</param>
        /// <returns>a float array containing the samples</returns>
        public static float[] ReadFloats(IAudioStream stream, int samples)
        {
            var byteBuffer = ReadBytes(stream, samples);
            var floatBuffer = new float[byteBuffer.Length / stream.Properties.SampleByteSize];
            Buffer.BlockCopy(byteBuffer, 0, floatBuffer, 0, byteBuffer.Length);
            return floatBuffer;
        }
    }
}
