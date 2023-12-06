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
using System.Collections.Generic;
using Aurio.Streams;

namespace Aurio
{
    public static class AudioUtil
    {
        /// <summary>
        /// Calculates the length of a sample in units of ticks. Results is a floating point number as it
        /// most probably won't be a whole number.
        /// </summary>
        /// <param name="audioProperties">the audio properties of an audio stream containing the sample rate</param>
        /// <returns>the length of a sample in floating point ticks</returns>
        public static double CalculateSampleTicks(AudioProperties audioProperties)
        {
            return (double)TimeUtil.SECS_TO_TICKS / audioProperties.SampleRate;
        }

        /// <summary>
        /// Calculates the number of samples that a time span in an audio stream consists of.
        /// </summary>
        /// <param name="audioProperties">the audio properties of an audio stream containing the sample rate</param>
        /// <param name="timeSpan">the time span for which the number of samples should be calculated</param>
        /// <returns>the number of samples that the given time span contains in an audio stream with the given properties</returns>
        public static int CalculateSamples(AudioProperties audioProperties, TimeSpan timeSpan)
        {
            return (int)Math.Round(timeSpan.Ticks / CalculateSampleTicks(audioProperties));
        }

        /// <summary>
        /// Adjusts the beginning and the end of a time-interval to the sample interval length so that the
        /// adjusted input interval includes the preceding and following sample.
        /// Since audio samples can be between two integer ticks, the outputInterval.From is less or equal its
        /// matching sample's time, and outputInterval.To is greater or equal its matching sample's time. Recursive
        /// usage may therefore enlarge the output interval with every execution.
        ///
        /// Example:
        /// audio stream samples:    X-----X-----X-----X-----X-----X-----X-----X-----X-----X-----
        /// input interval:                   [---------------]
        /// output interval:               [-----------------------)
        /// </summary>
        /// <param name="intervalToAlign">the interval that should be sample-aligned</param>
        /// <param name="audioProperties">the audio properties containing the sample rate</param>
        /// <returns>the sample aligned interval</returns>
        public static Interval AlignToSamples(
            Interval intervalToAlign,
            AudioProperties audioProperties
        )
        {
            double sampleLength = CalculateSampleTicks(audioProperties);
            return new Interval(
                (long)(intervalToAlign.From - ((double)intervalToAlign.From % sampleLength)),
                (long)(
                    intervalToAlign.To
                    + (sampleLength - ((double)intervalToAlign.To % sampleLength))
                )
            );
        }

        /// <summary>
        /// Creates an array of buffers with a given size for a given number of channels.
        /// </summary>
        /// <param name="channels">the number of channels the buffer should be capable</param>
        /// <param name="size">the size of each channel's buffer</param>
        /// <returns>the multichannel buffer</returns>
        public static T[][] CreateArray<T>(int channels, int size)
        {
            T[][] buffer = new T[channels][];
            for (int channel = 0; channel < channels; channel++)
            {
                buffer[channel] = new T[size];
            }
            return buffer;
        }

        /// <summary>
        /// Creates an array of lists with a given size for a given number of channels.
        /// </summary>
        /// <param name="channels">the number of channels the list should be created for</param>
        /// <param name="size">the size of each channel's list</param>
        /// <returns>the multichannel list</returns>
        public static List<T>[] CreateList<T>(int channels, int size)
        {
            List<T>[] list = new List<T>[channels];
            for (int channel = 0; channel < channels; channel++)
            {
                list[channel] = new List<T>(size);
            }
            return list;
        }

        public static float[][] Uninterleave(
            AudioProperties audioProperties,
            byte[] buffer,
            int offset,
            int count,
            bool downmix
        )
        {
            int channels = audioProperties.Channels;
            int downmixChannel = downmix ? 1 : 0;
            float[][] uninterleavedSamples = CreateArray<float>(
                channels + downmixChannel,
                count / (audioProperties.BitDepth / 8) / audioProperties.Channels
            );
            unsafe
            {
                fixed (byte* sampleBuffer = &buffer[offset])
                {
                    float* samples = (float*)sampleBuffer;
                    int sampleCount = 0;

                    for (int x = 0; x < count / 4; x += channels)
                    {
                        float sum = 0;
                        for (int channel = 0; channel < channels; channel++)
                        {
                            sum += samples[x + channel];
                            uninterleavedSamples[channel + downmixChannel][sampleCount] = samples[
                                x + channel
                            ];
                        }
                        if (downmix)
                        {
                            uninterleavedSamples[0][sampleCount] = sum / channels;
                        }
                        sampleCount++;
                    }
                }
            }

            return uninterleavedSamples;
        }

        public static double CalculateRMS(float[] samples)
        {
            float sampleValue = 0;
            double frameRMS = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                sampleValue = samples[i];
                frameRMS += sampleValue * sampleValue;
            }
            return Math.Sqrt(frameRMS / samples.Length);
        }
    }
}
