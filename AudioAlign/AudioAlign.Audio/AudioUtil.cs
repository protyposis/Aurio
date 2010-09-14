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
    }
}
