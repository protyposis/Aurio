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

        public static TimeSpan Calculate(IAudioStream s1, Interval i1, IAudioStream s2, Interval i2, ProgressMonitor progressMonitor) {
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

            DateTime timeBeforeDataRead = DateTime.Now;

            StreamUtil.ForceReadIntervalSamples(s1, i1, x);
            StreamUtil.ForceReadIntervalSamples(s2, i2, y);

            TimeSpan timeOfDataRead = DateTime.Now - timeBeforeDataRead;
            Debug.WriteLine("data read duration: " + timeOfDataRead);

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

                progress.ReportProgress(((double)delay + maxdelay) / maxdelay * 100);
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
            TimeSpan offset = new TimeSpan((long)((maxindex - maxdelay) / (float)sampleRate * 1000 * 1000 * 10));
            Debug.WriteLine("peak offset @ " + offset);
            progress.Finish();
            return offset;
        }

        public static void CalculateAsync(IAudioStream s1, Interval i1, IAudioStream s2, Interval i2, ProgressMonitor progressMonitor) {
            Task.Factory.StartNew(() => {
                Calculate(s1, i1, s2, i2, progressMonitor);
            });
        }

        public static void Adjust(Match match, ProgressMonitor progressMonitor) {
            try {
                long secfactor = 1000 * 1000 * 10;
                Stopwatch sw = new Stopwatch();
                sw.Start();
                long intervalLength = secfactor * 1;

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

                TimeSpan offset = Calculate(
                    match.Track1.CreateAudioStream(), iT1,
                    match.Track2.CreateAudioStream(), iT2,
                    progressMonitor);
                Debug.WriteLine("CC: " + match + ": " + offset + " (" + sw.Elapsed + ")");
                match.Track2Time += offset;
            }
            catch (Exception e) {
                Debug.WriteLine("CC adjust failed: " + e.Message);
            }
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
            
            return sxy / denom;
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

        public static double Correlate(IAudioStream s1, Interval i1, IAudioStream s2, Interval i2, ProgressMonitor progressMonitor) {
            if (i1.Length != i2.Length) {
                throw new ArgumentException("interval lengths do not match");
            }

            s1 = PrepareStream(s1, 11050);
            s2 = PrepareStream(s2, 11050);

            IProgressReporter progress = progressMonitor.BeginTask("calculating correlation", true);

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

            progress.ReportProgress(100);

            Debug.WriteLine("C result: " + r);
            progress.Finish();
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
