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

        public static TimeSpan Calculate(IAudioStream s1, Interval i1, IAudioStream s2, Interval i2) {
            if (i1.Length != i2.Length) {
                throw new ArgumentException("interval lengths do not match");
            }
            if (s1.Properties.SampleRate != s2.Properties.SampleRate) {
                throw new ArgumentException("sample rates do not match");
            }
            if (s1.Properties.BitDepth != s2.Properties.BitDepth) {
                throw new ArgumentException("sample bitdepths do not match");
            }
            if (s1.Properties.Channels > 1) {
                if (s1.Properties.Format != AudioFormat.IEEE) {
                    s1 = new IeeeStream(s1);
                }
                s1 = new MonoStream(s1);
            }
            if (s2.Properties.Channels > 1) {
                if (s2.Properties.Format != AudioFormat.IEEE) {
                    s2 = new IeeeStream(s2);
                }
                s2 = new MonoStream(s2);
            }

            s1 = new ResamplingStream(s1, ResamplingQuality.SincFastest, 11050);
            s2 = new ResamplingStream(s2, ResamplingQuality.SincFastest, 11050);

            ProgressReporter progress = ProgressMonitor.Instance.BeginTask("calculating cross-correlation", true);

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
            ProgressMonitor.Instance.EndTask(progress);
            return offset;
        }

        public static void CalculateAsync(IAudioStream s1, Interval i1, IAudioStream s2, Interval i2) {
            Task.Factory.StartNew(() => {
                Calculate(s1, i1, s2, i2);
            });
        }

        public static void Adjust(Match match) {
            long secfactor = 1000 * 1000 * 10;
            Stopwatch sw = new Stopwatch();
            sw.Start();
            long intervalLength = secfactor * 1;
            TimeSpan offset = Calculate(
                match.Track1.CreateAudioStream(), new Interval(match.Track1Time.Ticks - intervalLength / 2, match.Track1Time.Ticks + intervalLength / 2),
                match.Track2.CreateAudioStream(), new Interval(match.Track2Time.Ticks - intervalLength / 2, match.Track2Time.Ticks + intervalLength / 2));
            Debug.WriteLine("CC: " + match + ": " + offset + " (" + sw.Elapsed + ")");
            match.Track2.Offset -= offset;
        }
    }
}
