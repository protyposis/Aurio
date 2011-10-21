using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AudioAlign.Audio.Streams;
using AudioAlign.Audio.TaskMonitor;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace AudioAlign.Audio.Matching.Dixon2005 {
    public class DTW {

        [DebuggerDisplay("Pair {i1} <-> {i2}")]
        protected struct Pair {
            public int i1, i2;

            public Pair(int i1, int i2) {
                this.i1 = i1;
                this.i2 = i2;
            }
        }

        protected ProgressMonitor progressMonitor;
        protected TimeSpan searchWidth;

        private BlockingCollection<float[]> stream1FrameQueue;
        private BlockingCollection<float[]> stream2FrameQueue;
        float[][] rb1;
        private int rb1FrameCount;
        float[][] rb2;
        private int rb2FrameCount;

        public DTW(TimeSpan searchWidth, ProgressMonitor progressMonitor) {
            this.searchWidth = searchWidth;
            this.progressMonitor = progressMonitor;
        }

        public List<Tuple<TimeSpan, TimeSpan>> Execute(IAudioStream s1, IAudioStream s2) {
            s1 = PrepareStream(s1);
            s2 = PrepareStream(s2);

            int diagonalWidth = (int)(searchWidth.TotalSeconds * (1d * FrameReader.SAMPLERATE / FrameReader.WINDOW_HOP_SIZE));

            // init ring buffers
            rb1 = new float[diagonalWidth][];
            rb1FrameCount = 0;
            rb2 = new float[diagonalWidth][];
            rb2FrameCount = 0;

            stream1FrameQueue = new BlockingCollection<float[]>(20);
            FrameReader stream1FrameReader = new FrameReader(s1);
            Task.Factory.StartNew(() => {
                while (stream1FrameReader.HasNext()) {
                    float[] frame = new float[FrameReader.FRAME_SIZE];
                    stream1FrameReader.ReadFrame(frame);
                    stream1FrameQueue.Add(frame);
                }
                stream1FrameQueue.CompleteAdding();
            });

            stream2FrameQueue = new BlockingCollection<float[]>(20);
            FrameReader stream2FrameReader = new FrameReader(s2);
            Task.Factory.StartNew(() => {
                while (stream2FrameReader.HasNext()) {
                    float[] frame = new float[FrameReader.FRAME_SIZE];
                    stream2FrameReader.ReadFrame(frame);
                    stream2FrameQueue.Add(frame);
                }
                stream2FrameQueue.CompleteAdding();
            });

            IProgressReporter progressReporter = progressMonitor.BeginTask("DTW...", true);
            int n = stream1FrameReader.WindowCount;
            int m = stream2FrameReader.WindowCount;
            PatchMatrix dtw = new PatchMatrix(double.PositiveInfinity);

            // init matrix
            // NOTE do not explicitely init the PatchMatrix, otherwise the sparse matrix characteristic would 
            //      be gone and the matrix would take up all the space like a standard matrix does
            dtw[0, 0] = 0;

            double deltaN;
            double deltaM;
            if (m > n) {
                deltaN = (double)(n - 1) / (m - 1);
                deltaM = 1d;
            }
            else if (m < n) {
                deltaN = 1d;
                deltaM = (double)(m - 1) / (n - 1);;
            }
            else {
                deltaN = 1d;
                deltaM = 1d;
            }
            double progressN = 0;
            double progressM = 0;
            int i, x = 0;
            int j, y = 0;
            while (x < n || y < m) {
                x = (int)progressN + 1;
                y = (int)progressM + 1;
                ReadFrames(x, y);

                i = Math.Max(x - diagonalWidth, 1);
                j = y;
                for (; i <= x; i++) {
                    dtw[i, j] = CalculateCost(rb1[(i - 1) % diagonalWidth], rb2[(j - 1) % diagonalWidth]) + 
                        Min(dtw[i - 1, j], dtw[i, j - 1], dtw[i - 1, j - 1]);
                }

                i = x;
                j = Math.Max(y - diagonalWidth, 1);
                for (; j <= y; j++) {
                    dtw[i, j] = CalculateCost(rb1[(i - 1) % diagonalWidth], rb2[(j - 1) % diagonalWidth]) + 
                        Min(dtw[i - 1, j], dtw[i, j - 1], dtw[i - 1, j - 1]);
                }

                progressReporter.ReportProgress((double)x / n * 100);

                progressN += deltaN;
                progressM += deltaM;
            }
            progressReporter.Finish();

            List<Pair> path = OptimalWarpingPath(dtw);

            List<Tuple<TimeSpan, TimeSpan>> pathTimes = new List<Tuple<TimeSpan, TimeSpan>>();
            foreach (Pair pair in path) {
                Tuple<TimeSpan, TimeSpan> timePair = new Tuple<TimeSpan, TimeSpan>(
                    PositionToTimeSpan(pair.i1 * FrameReader.WINDOW_HOP_SIZE),
                    PositionToTimeSpan(pair.i2 * FrameReader.WINDOW_HOP_SIZE));
                if (timePair.Item1 >= TimeSpan.Zero && timePair.Item2 >= TimeSpan.Zero) {
                    pathTimes.Add(timePair);
                }
            }

            return pathTimes;
        }

        private void ReadFrames(int x, int y) {
            if (rb1FrameCount < x) {
                for (; rb1FrameCount < x; rb1FrameCount++) {
                    if (stream1FrameQueue.IsCompleted) {
                        rb1FrameCount--;
                        break;
                    }
                    rb1[rb1FrameCount % rb1.Length] = (stream1FrameQueue.Take());
                }
            }

            if (rb2FrameCount < y) {
                for (; rb2FrameCount < y; rb2FrameCount++) {
                    if (stream2FrameQueue.IsCompleted) {
                        rb2FrameCount--;
                        break;
                    }
                    rb2[rb2FrameCount % rb2.Length] = (stream2FrameQueue.Take());
                }
            }
        }

        protected IAudioStream PrepareStream(IAudioStream stream) {
            if (stream.Properties.Channels > 1) {
                stream = new MonoStream(stream);
            }
            if (stream.Properties.SampleRate != FrameReader.SAMPLERATE) {
                stream = new ResamplingStream(stream, ResamplingQuality.SincFastest, FrameReader.SAMPLERATE);
            }
            return stream;
        }

        protected List<Pair> OptimalWarpingPath(PatchMatrix dtw, int i, int j) {
            List<Pair> path = new List<Pair>();
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

        protected List<Pair> OptimalWarpingPath(PatchMatrix dtw) {
            int i = dtw.LengthX - 1;
            int j = dtw.LengthY - 1;
            return OptimalWarpingPath(dtw, i, j);
        }

        protected double Min(double val1, double val2, double val3) {
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

        public static TimeSpan PositionToTimeSpan(long position) {
            return new TimeSpan((long)Math.Round((double)position / FrameReader.SAMPLERATE * TimeUtil.SECS_TO_TICKS));
        }

        public static TimeSpan IndexToTimeSpan(int index) {
            return PositionToTimeSpan(index * FrameReader.WINDOW_HOP_SIZE);
        }
    }
}
