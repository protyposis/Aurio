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

        private enum GlobalPathConstraint {
            None,
            SakoeChibaBand
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
            PatchMatrix dtw = AccumulatedCostMatrix(frames1, frames2, GlobalPathConstraint.SakoeChibaBand);
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

        private PatchMatrix AccumulatedCostMatrix(float[][] X, float[][] Y, GlobalPathConstraint constraint) {
            IProgressReporter progressReporter = progressMonitor.BeginTask("Calculating cost matrix...", true);
            int n = X.Length;
            int m = Y.Length;
            //double[,] dtw = new double[n + 1, m + 1];
            PatchMatrix dtw = new PatchMatrix(double.PositiveInfinity);

            // init matrix
            // NOTE do not explicitely init the PatchMatrix, otherwise the sparse matrix characteristic would 
            //      be gone and the matrix would take up all the space like a standard matrix does
            dtw[0, 0] = 0;

            if (constraint == GlobalPathConstraint.None) {
                double totalProgressSteps = n * m;
                double progressStep = 0;
                for (int i = 1; i <= n; i++) {
                    for (int j = 1; j <= m; j++) {
                        dtw[i, j] = CalculateCost(X[i - 1], Y[j - 1]) + Min(dtw[i - 1, j], dtw[i, j - 1], dtw[i - 1, j - 1]);
                        progressReporter.ReportProgress(progressStep++ / totalProgressSteps * 100);
                    }
                }
            }
            else if (constraint == GlobalPathConstraint.SakoeChibaBand) {
                int diagonalWidth = 100;
                double deltaN;
                double deltaM;
                if (m > n) {
                    deltaN = 1d;
                    deltaM = (double)(m - 1) / (n - 1);
                }
                else if (m < n) {
                    deltaN = (double)(n - 1) / (m - 1);
                    deltaM = 1d;
                }
                else {
                    deltaN = 1d;
                    deltaM = 1d;
                }
                double progressN = 0;
                double progressM = 0;
                int x = 0;
                int y = 0;
                while (x < n || y < m) {
                    x = (int)progressN + 1;
                    y = (int)progressM + 1;

                    int i = x;
                    int j = y;
                    for (i = x; i <= Math.Min(x + diagonalWidth, n); i++) {
                        dtw[i, j] = CalculateCost(X[i - 1], Y[j - 1]) + Min(dtw[i - 1, j], dtw[i, j - 1], dtw[i - 1, j - 1]);
                    }

                    i = x;
                    j = y;
                    for (j = x; j <= Math.Min(y + diagonalWidth, m); j++) {
                        dtw[i, j] = CalculateCost(X[i - 1], Y[j - 1]) + Min(dtw[i - 1, j], dtw[i, j - 1], dtw[i - 1, j - 1]);
                    }

                    progressReporter.ReportProgress((double)x / n * 100);

                    progressN += deltaN;
                    progressM += deltaM;
                }
            }
            else {
                throw new NotImplementedException("invalid global path constraint");
            }

            progressReporter.Finish();
            return dtw;
        }

        private List<Pair> OptimalWarpingPath(PatchMatrix dtw) {
            List<Pair> path = new List<Pair>();
            int i = dtw.LengthX - 1;
            int j = dtw.LengthY - 1;
            while (i > 1 && j > 1) {
                if (i == 1) {
                    j--;
                }
                else if (j == 1) {
                    i--;
                }
                else {
                    double min = Min(dtw[i - 1, j], dtw[i, j - 1], dtw[i - 1, j - 1]);
                    if (dtw[i - 1, j] == min) {
                        i--;
                    }
                    else if (dtw[i, j - 1] == min) {
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
            return Math.Sqrt(result);
        }

        public static TimeSpan IndexToTimeSpan(int index) {
            return new TimeSpan((long)Math.Round((double)index 
                * FrameReader.WINDOW_HOP_SIZE / FrameReader.SAMPLERATE * 1000 * 1000 * 10));
        }
    }
}
