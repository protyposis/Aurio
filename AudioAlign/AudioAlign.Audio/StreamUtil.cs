using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AudioAlign.Audio.Streams;

namespace AudioAlign.Audio {
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
    }
}
