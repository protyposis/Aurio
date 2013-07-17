using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NAudio.Wave;
using System.Diagnostics;
using System.Threading.Tasks;
using AudioAlign.Audio.TaskMonitor;
using AudioAlign.Audio.Streams;

namespace AudioAlign.Audio.Matching {
    public class CrossCorrelation {

        private static unsafe int Calculate(float* x, float* y, int length, IProgressReporter reporter, out double maxVal) {
            int n = length;
            int maxdelay = length / 2;
            if (maxdelay * 2 > n) {
                throw new Exception("maximum delay must be <= half of the interval to be analyzed");
            }

            Debug.WriteLine("CC window: " + n + " samples");

            float[] r = new float[maxdelay * 2];

            // the following code is taken and adapted from: http://local.wasp.uwa.edu.au/~pbourke/miscellaneous/correlate/

            /* Calculate the mean of the two series x[], y[] */
            double mx = 0;
            double my = 0;
            for (int i = 0; i < n; i++) {
                mx += x[i];
                my += y[i];
            }
            mx /= n;
            my /= n;

            /* Calculate the denominator */
            double sx = 0;
            double sy = 0;
            for (int i = 0; i < n; i++) {
                sx += (x[i] - mx) * (x[i] - mx);
                sy += (y[i] - my) * (y[i] - my);
            }
            double denom = Math.Sqrt(sx * sy);

            /* Calculate the correlation series */
            for (int delay = -maxdelay; delay < maxdelay; delay++) {
                double sxy = 0;
                int j;
                for (int i = 0; i < n; i++) {
                    j = i + delay;
                    if (j < 0 || j >= n)
                        continue;
                    else
                        sxy += (x[i] - mx) * (y[j] - my);
                    /* Or should it be (?)
                    if (j < 0 || j >= n)
                        sxy += (x[i] - mx) * (-my);
                    else
                        sxy += (x[i] - mx) * (y[j] - my);
                    */
                }
                r[delay + maxdelay] = (float)(sxy / denom);

                /* r is the correlation coefficient at "delay" */

                if (reporter != null) {
                    reporter.ReportProgress(((double)delay + maxdelay) / (maxdelay * 2) * 100);
                }
            }

            float maxval = float.MinValue;
            int maxindex = 0;
            for (int i = 0; i < r.Length; i++) {
                if (r[i] > maxval) {
                    maxval = r[i];
                    maxindex = i;
                }
            }

            Debug.WriteLine("max val: {0} index: {1} adjusted index: {2}", maxval, maxindex, maxindex - maxdelay);
            maxVal = maxval;
            return maxindex - maxdelay;
        }

        private static unsafe int Calculate(float[] x, float[] y, IProgressReporter reporter, out double maxVal) {
            if (x.Length != y.Length) {
                throw new ArgumentException("interval lengths do not match");
            }
            fixed (float* xF = &x[0], yF = &y[0]) {
                return Calculate(xF, yF, x.Length, reporter, out maxVal);
            }
        }

        /// <summary>
        /// Calculates the Cross-Correlation of two time series and returns the adjustment offset.
        /// </summary>
        /// <param name="x">the first input series</param>
        /// <param name="y">the second input series</param>
        /// <returns>the adjustment offset as the number of samples that the second series if off from the first series</returns>
        public static int Calculate(float[] x, float[] y, out double maxVal) {
            return Calculate(x, y, null, out maxVal);
        }

        /// <summary>
        /// Calculates the Cross-Correlation of two time series and returns the adjustment offset.
        /// </summary>
        /// <param name="x">the first input series</param>
        /// <param name="y">the second input series</param>
        /// <param name="length">the length of each input series</param>
        /// <returns>the adjustment offset as the number of samples that the second series if off from the first series</returns>
        public static unsafe int Calculate(float* x, float* y, int length, out double maxVal) {
            return Calculate(x, y, length, null, out maxVal);
        }

        public static TimeSpan Calculate(IAudioStream s1, Interval i1, IAudioStream s2, Interval i2, ProgressMonitor progressMonitor, out double maxVal) {
            if (i1.Length != i2.Length) {
                throw new ArgumentException("interval lengths do not match");
            }

            s1 = PrepareStream(s1, 11050);
            s2 = PrepareStream(s2, 11050);

            IProgressReporter progress = progressMonitor.BeginTask("calculating cross-correlation", true);

            float seconds = (float)(i1.Length / 10d / 1000 / 1000);
            int sampleRate = s1.Properties.SampleRate;
            int n = (int)(seconds * sampleRate);
            float[] x = new float[n];
            float[] y = new float[n];
            int maxdelay = (int)(sampleRate * seconds / 4);

            if (maxdelay * 2 > n) {
                throw new Exception("maximum delay must be <= half of the interval to be analyzed");
            }

            Debug.WriteLine("CC window: " + seconds + " secs = " + n + " samples");

            float[] r = new float[maxdelay * 2];

            StreamUtil.ForceReadIntervalSamples(s1, i1, x);
            StreamUtil.ForceReadIntervalSamples(s2, i2, y);

            int indexOffset = Calculate(x, y, progress, out maxVal);

            TimeSpan offset = new TimeSpan((long)(indexOffset / (float)sampleRate * TimeUtil.SECS_TO_TICKS));
            Debug.WriteLine("peak offset @ " + offset);
            progress.Finish();
            return offset;
        }

        public static void CalculateAsync(IAudioStream s1, Interval i1, IAudioStream s2, Interval i2, ProgressMonitor progressMonitor) {
            Task.Factory.StartNew(() => {
                double maxValTemp;
                Calculate(s1, i1, s2, i2, progressMonitor, out maxValTemp);
            });
        }

        public static Match Adjust(Match match, ProgressMonitor progressMonitor) {
            try {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                long intervalLength = TimeUtil.SECS_TO_TICKS * 1;

                Interval iT1 = new Interval(match.Track1Time.Ticks, match.Track1Time.Ticks + intervalLength);
                Interval iT2 = new Interval(match.Track2Time.Ticks, match.Track2Time.Ticks + intervalLength);

                long iT1overflow = match.Track1.Length.Ticks - iT1.To;
                long iT2overflow = match.Track2.Length.Ticks - iT2.To;
                long adjust = 0;
                if (iT1overflow < 0) {
                    adjust = iT1overflow;
                }
                if (iT2overflow < 0 && iT2overflow < iT1overflow) {
                    adjust = iT2overflow;
                }
                iT1 += adjust;
                iT2 += adjust;

                double maxValTemp;
                TimeSpan offset = Calculate(
                    match.Track1.CreateAudioStream(), iT1,
                    match.Track2.CreateAudioStream(), iT2,
                    progressMonitor, out maxValTemp);
                Debug.WriteLine("CC: " + match + ": " + offset + " (" + sw.Elapsed + ")");

                return new Match(match) {
                    Track2Time = match.Track2Time + offset,
                    Similarity = (float)maxValTemp,
                    Source = "CC"
                };
            }
            catch (Exception e) {
                Debug.WriteLine("CC adjust failed: " + e.Message);
            }

            return null;
        }

        public static unsafe double Correlate(float* x, float* y, int length) {
            int n = length;

            // the following code is taken and adapted from: http://local.wasp.uwa.edu.au/~pbourke/miscellaneous/correlate/

            /* Calculate the mean of the two series x[], y[] */
            double mx = 0;
            double my = 0;
            for (int i = 0; i < n; i++) {
                mx += x[i];
                my += y[i];
            }
            mx /= n;
            my /= n;

            /* Calculate the denominator */
            double sx = 0;
            double sy = 0;
            for (int i = 0; i < n; i++) {
                sx += (x[i] - mx) * (x[i] - mx);
                sy += (y[i] - my) * (y[i] - my);
            }
            double denom = Math.Sqrt(sx * sy);

            /* Calculate the correlation */
            double sxy = 0;
            for (int i = 0; i < n; i++) {
                sxy += (x[i] - mx) * (y[i] - my);
            }

            return denom != 0 ? sxy / denom : 0;
        }

        public static unsafe double Correlate(float[] x, float[] y) {
            if (x.Length != y.Length) {
                throw new ArgumentException("interval lengths do not match");
            }
            fixed(float* xF = &x[0], yF = &y[0]) {
                return Correlate(xF, yF, x.Length);
            }
        }

        public static unsafe double Correlate(byte[] x, byte[] y) {
            if (x.Length != y.Length) {
                throw new ArgumentException("interval lengths do not match");
            }
            fixed (byte* xB = &x[0], yB = &y[0]) {
                return Correlate((float*)xB, (float*)yB, x.Length / sizeof(float));
            }
        }

        public static double Correlate(IAudioStream s1, Interval i1, IAudioStream s2, Interval i2) {
            if (i1.Length != i2.Length) {
                throw new ArgumentException("interval lengths do not match");
            }

            s1 = PrepareStream(s1, 11050);
            s2 = PrepareStream(s2, 11050);

            float seconds = (float)(i1.Length / 10d / 1000 / 1000);
            int sampleRate = s1.Properties.SampleRate;
            int n = (int)(seconds * sampleRate);
            float[] x = new float[n];
            float[] y = new float[n];

            Debug.WriteLine("C window: " + seconds + " secs = " + n + " samples");

            DateTime timeBeforeDataRead = DateTime.Now;

            StreamUtil.ForceReadIntervalSamples(s1, i1, x);
            StreamUtil.ForceReadIntervalSamples(s2, i2, y);

            TimeSpan timeOfDataRead = DateTime.Now - timeBeforeDataRead;
            Debug.WriteLine("C data read duration: " + timeOfDataRead);

            double r = Correlate(x, y);

            Debug.WriteLine("C result: " + r);
            return r;
        }

        public static IAudioStream PrepareStream(IAudioStream stream, int sampleRate) {
            if (stream.Properties.Format != AudioFormat.IEEE) {
                stream = new IeeeStream(stream);
            }
            if (stream.Properties.Channels > 1) {
                stream = new MonoStream(stream);
            }
            if (stream.Properties.SampleRate != 11050) {
                stream = new ResamplingStream(stream, ResamplingQuality.SincFastest, sampleRate);
            }
            return stream;
        }
    }
}
