using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioAlign.Audio {
    public static class AudioUtil {
        /// <summary>
        /// Calculates the length of a sample in units of ticks. Results is a floating point number as it
        /// most probably won't be a whole number.
        /// </summary>
        /// <param name="audioProperties">the audio properties of an audio stream containing the sample rate</param>
        /// <returns>the length of a sample in floating point ticks</returns>
        public static float CalculateSampleTicks(AudioProperties audioProperties) {
            // 1 sec * 1000 = millisecs | * 1000 = microsecs | * 10 = ticks (100-nanosecond units)
            // http://msdn.microsoft.com/en-us/library/zz841zbz.aspx
            return (float) 1 * 1000 * 1000 * 10 / audioProperties.SampleRate;
        }

        /// <summary>
        /// Calculates the number of samples that a time span in an audio stream consists of.
        /// </summary>
        /// <param name="audioProperties">the audio properties of an audio stream containing the sample rate</param>
        /// <param name="timeSpan">the time span for which the number of samples should be calculated</param>
        /// <returns>the number of samples that the given time span contains in an audio stream with the given properties</returns>
        public static int CalculateSamples(AudioProperties audioProperties, TimeSpan timeSpan) {
            return (int)Math.Ceiling(timeSpan.Ticks / CalculateSampleTicks(audioProperties));
        }

        /// <summary>
        /// Creates an array of buffers with a given size for a given number of channels.
        /// </summary>
        /// <param name="channels">the number of channels the buffer should be capable</param>
        /// <param name="size">the size of each channel's buffer</param>
        /// <returns>the multichannel buffer</returns>
        public static T[][] CreateArray<T>(int channels, int size) {
            T[][] buffer = new T[channels][];
            for (int channel = 0; channel < channels; channel++) {
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
        public static List<T>[] CreateList<T>(int channels, int size) {
            List<T>[] list = new List<T>[channels];
            for (int channel = 0; channel < channels; channel++) {
                list[channel] = new List<T>(size);
            }
            return list;
        }
    }
}
