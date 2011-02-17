using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NAudio.Wave;
using System.Diagnostics;
using System.Threading.Tasks;
using AudioAlign.Audio.TaskMonitor;

namespace AudioAlign.Audio.Matching {
    public class CrossCorrelation {
        public void CalculateAsync(WaveStream s1, Interval i1, WaveStream s2, Interval i2) {
            if (i1.Length != i2.Length) {
                throw new ArgumentException("interval lengths do not match");
            }
            if (s1.WaveFormat.SampleRate != s2.WaveFormat.SampleRate) {
                throw new ArgumentException("sample rates do not match");
            }
            if (s1.WaveFormat.BitsPerSample != s2.WaveFormat.BitsPerSample) {
                throw new ArgumentException("sample bitdepths do not match");
            }

            Task.Factory.StartNew(() => {
                ProgressReporter progress = ProgressMonitor.Instance.BeginTask("calculating cross-correlation", true);

                float seconds = (float)(i1.Length / 10d / 1000 / 1000);
                int sampleRate = s1.WaveFormat.SampleRate;
                int n = (int)(seconds * sampleRate);
                float[] x = new float[n];
                float[] y = new float[n];
                int maxdelay = (int)(sampleRate * seconds / 2);

                if (maxdelay * 2 > n) {
                    throw new Exception("maximum delay must be <= half of the interval to be analyzed");
                }

                Debug.WriteLine("CC window: " + seconds + " secs = " + n + " samples");

                float[] r = new float[maxdelay * 2];

                DateTime timeBeforeDataRead = DateTime.Now;

                ReadStreamIntoArray(s1, i1, x);
                ReadStreamIntoArray(s2, i2, y);

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

                    progress.ReportProgress(((double)delay + maxdelay) / n * 100);
                }

                float maxval = float.MinValue;
                float maxindex = 0;
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
            });
        }

        private long ReadStreamIntoArray(WaveStream s, Interval i, float[] array) {
            s.CurrentTime = new TimeSpan(i.From);
            long bytesRead = 0;
            long samplesToRead = (long)(i.Length / 10d / 1000 / 1000 * s.WaveFormat.SampleRate);
            long totalSamplesRead = 0;
            int channels = s.WaveFormat.Channels;
            byte[] temp = new byte[1024 * 32 * channels];

            while ((bytesRead = s.Read(temp, 0, temp.Length)) > 0) {
                unsafe {
                    fixed (byte* sampleBuffer = &temp[0]) {
                        float* samples = (float*)sampleBuffer;
                        for (int x = 0; x < bytesRead / 4; x += channels) {
                            for (int channel = 0; channel < channels; channel++) {
                                if (channel == 0) {
                                    array[totalSamplesRead++] = samples[x + channel];
                                    if (samplesToRead == totalSamplesRead) {
                                        return totalSamplesRead;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return totalSamplesRead;
        }
    }
}
