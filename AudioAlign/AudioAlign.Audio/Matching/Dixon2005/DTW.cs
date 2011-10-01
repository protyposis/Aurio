using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AudioAlign.Audio.Streams;
using AudioAlign.Audio.TaskMonitor;
using System.Diagnostics;

namespace AudioAlign.Audio.Matching.Dixon2005 {
    public class DTW {

        [DebuggerDisplay("Pair {i1} <-> {i2}")]
        private struct Pair {
            public int i1, i2;

            public Pair(int i1, int i2) {
                this.i1 = i1;
                this.i2 = i2;
            }
        }

        private ProgressMonitor progressMonitor;

        public DTW() {
            progressMonitor = ProgressMonitor.GlobalInstance;
        }

        public List<Tuple<TimeSpan, TimeSpan>> Execute(IAudioStream s1, IAudioStream s2) {
            s1 = PrepareStream(s1);
            s2 = PrepareStream(s2);

            float[][] frames1 = ReadFrames(s1);
            float[][] frames2 = ReadFrames(s2);
            double[,] dtw = AccumulatedCostMatrix(frames1, frames2);
            List<Pair> path = OptimalWarpingPath(dtw);
            
            List<Tuple<TimeSpan, TimeSpan>> pathTimes = new List<Tuple<TimeSpan, TimeSpan>>();
            foreach (Pair pair in path) {
                pathTimes.Add(new Tuple<TimeSpan, TimeSpan>(IndexToTimeSpan(pair.i1), IndexToTimeSpan(pair.i2)));
            }

            return pathTimes;
        }

        private IAudioStream PrepareStream(IAudioStream stream) {
            if (stream.Properties.Channels > 1) {
                stream = new MonoStream(stream);
            }
            if (stream.Properties.SampleRate != FrameReader.SAMPLERATE) {
                stream = new ResamplingStream(stream, ResamplingQuality.SincFastest, FrameReader.SAMPLERATE);
            }
            return stream;
        }

        private float[][] ReadFrames(IAudioStream stream) {
            IProgressReporter progressReporter = progressMonitor.BeginTask("Reading DTW frames...", true);
            float[][] frames = new float[(stream.Length / stream.SampleBlockSize - FrameReader.WINDOW_SIZE) / 
                FrameReader.WINDOW_HOP_SIZE + 1][];
            FrameReader frameReader = new FrameReader(stream);
            int index = 0;
            while (frameReader.HasNext()) {
                float[] frame = new float[FrameReader.FRAME_SIZE];
                frameReader.ReadFrame(frame);
                frames[index++] = frame;
                progressReporter.ReportProgress((double)index / frames.Length * 100);
            }
            progressReporter.Finish();
            if (index < frames.Length) {
                Debug.WriteLine("frames array too large!!!");
            }
            return frames;
        }

        private double[,] AccumulatedCostMatrix(float[][] X, float[][] Y) {
            IProgressReporter progressReporter = progressMonitor.BeginTask("Calculating cost matrix...", true);
            int n = X.Length;
            int m = Y.Length;
            double totalProgressSteps = n + m + n * m;
            double progressStep = 0;
            double[,] dtw = new double[n + 1, m + 1];
            dtw[0, 0] = 0;
            for (int i = 1; i <= n; i++) {
                dtw[i, 1] = dtw[i - 1, 1] + CalculateCost(X[i - 1], Y[1 - 1]);
                progressReporter.ReportProgress(progressStep++ / totalProgressSteps * 100);
            }
            for (int j = 1; j <= m; j++) {
                dtw[1, j] = dtw[1, j - 1] + CalculateCost(X[1 - 1], Y[j - 1]);
                progressReporter.ReportProgress(progressStep++ / totalProgressSteps * 100);
            }
            for (int i = 1; i <= n; i++) {
                for (int j = 1; j <= m; j++) {
                    dtw[i, j] = CalculateCost(X[i - 1], Y[j - 1]) + Min(dtw[i - 1, j], dtw[i, j - 1], dtw[i - 1, j - 1]);
                    progressReporter.ReportProgress(progressStep++ / totalProgressSteps * 100);
                }
            }
            progressReporter.Finish();
            return dtw;
        }

        private List<Pair> OptimalWarpingPath(double[,] dtw) {
            List<Pair> path = new List<Pair>();
            int i = dtw.GetUpperBound(0);
            int j = dtw.GetUpperBound(1);
            while (i > 1 && j > 1) {
                if (i == 1) {
                    j--;
                }
                else if (j == 1) {
                    i--;
                }
                else {
                    if (dtw[i - 1, j] == Min(dtw[i - 1, j], dtw[i, j - 1], dtw[i - 1, j - 1])) {
                        i--;
                    }
                    else if (dtw[i, j - 1] == Min(dtw[i - 1, j], dtw[i, j - 1], dtw[i - 1, j - 1])) {
                        j--;
                    }
                    else {
                        i--;
                        j--;
                    }
                    path.Add(new Pair(i, j));
                }
            }
            path.Reverse();
            return path;
        }

        private double Min(double val1, double val2, double val3) {
            return Math.Min(val1, Math.Min(val2, val3));
        }

        /// <summary>
        /// Computes the distance between two audio frames.
        /// Dixon / Live Tracking of Musical Performances... / formula 4
        /// </summary>
        protected double CalculateCost(float[] frame1, float[] frame2) {
            double result = 0;
            for (int i = 0; i < frame1.Length; i++) {
                result += Math.Pow(frame1[i] - frame2[i], 2);
            }
            result = Math.Sqrt(result);
            if (double.IsNaN(result)) {
                Debug.WriteLine("NaN");
            }
            return result;
        }

        public static TimeSpan IndexToTimeSpan(int index) {
            return new TimeSpan((long)Math.Round((double)index 
                * FrameReader.WINDOW_HOP_SIZE / FrameReader.SAMPLERATE * 1000 * 1000 * 10));
        }
    }
}
