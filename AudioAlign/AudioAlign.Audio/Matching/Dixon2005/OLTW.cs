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
    public class OLTW : DTW {

        private const int MAX_RUN_COUNT = 3; // MaxRunCount
        private const int SEARCH_WIDTH = 500; // c = 500 frames = ~10 seconds

        private enum GetIncResult {
            None = 0,
            Row,
            Column,
            Both
        }

        private BlockingCollection<float[]> stream1FrameQueue;
        private BlockingCollection<float[]> stream2FrameQueue;

        private PatchMatrix matrix;
        private RingBuffer<float[]> rb1;
        private int rb1FrameCount;
        private RingBuffer<float[]> rb2;
        private int rb2FrameCount;
        private int t; // pointer to the current position in series U
        private int j; // pointer to the current position in series V
        private GetIncResult previous;
        private int runCount;
        private int c;

        public OLTW(ProgressMonitor progressMonitor)
            : base(new TimeSpan(0), progressMonitor) {
        }

        public new List<Tuple<TimeSpan, TimeSpan>> Execute(IAudioStream s1, IAudioStream s2) {
            s1 = PrepareStream(s1);
            s2 = PrepareStream(s2);

            int rbCapacity = SEARCH_WIDTH; // TODO make min size SEARCH_WIDTH
            matrix = new PatchMatrix(double.PositiveInfinity);
            rb1 = new RingBuffer<float[]>(rbCapacity);
            rb1FrameCount = 0;
            rb2 = new RingBuffer<float[]>(rbCapacity);
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

            // init matrix
            // NOTE do not explicitely init the PatchMatrix, otherwise the sparse matrix characteristic would 
            //      be gone and the matrix would take up all the space like a standard matrix does
            matrix[0, 0] = 0;

            IProgressReporter progressReporter = progressMonitor.BeginTask("OLTW", true);
            int totalFrames = stream1FrameReader.WindowCount + stream2FrameReader.WindowCount;

            // --------- OLTW -----------

            t = 1;
            j = 1;
            previous = GetIncResult.None;
            runCount = 0;
            c = SEARCH_WIDTH;
            EvaluatePathCost(t, j);
            while (rb1FrameCount + rb2FrameCount < totalFrames) {
                GetIncResult getInc = GetInc(t, j);
                if (t < stream1FrameReader.WindowCount && getInc != GetIncResult.Column) {
                    t++;
                    for (int k = j - c + 1; k <= j; k++) {
                        if (k > 0) {
                            EvaluatePathCost(t, k);
                        }
                    }
                }
                if (j < stream2FrameReader.WindowCount && getInc != GetIncResult.Row) {
                    j++;
                    for (int k = t - c + 1; k <= t; k++) {
                        if (k > 0) {
                            EvaluatePathCost(k, j);
                        }
                    }
                }
                if (getInc == previous) {
                    runCount++;
                }
                else {
                    runCount = 1;
                }
                if (getInc != GetIncResult.Both) {
                    previous = getInc;
                }

                //Debug.WriteLine(t + " " + j + " " + getInc);

                progressReporter.ReportProgress((rb1FrameCount + rb2FrameCount) / (double)totalFrames * 100);
            }

            Debug.WriteLine("OLTW finished @ t={0}/{1}, j={2}/{3}",
                t, stream1FrameReader.WindowCount,
                j, stream2FrameReader.WindowCount);

            progressReporter.Finish();


            // --------- generate results -----------

            List<Pair> path = OptimalWarpingPath(matrix);

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

        private void EvaluatePathCost(int t1, int t2) {
            if (rb1FrameCount < t1) {
                // read frames until position t
                for (; rb1FrameCount < t1; rb1FrameCount++) {
                    if (stream1FrameQueue.IsCompleted) {
                        rb1FrameCount--;
                        break;
                    }
                    rb1.Add(stream1FrameQueue.Take());
                }
            }

            if (rb2FrameCount < t2) {
                // read frames until position k
                for (; rb2FrameCount < t2; rb2FrameCount++) {
                    if (stream2FrameQueue.IsCompleted) {
                        rb2FrameCount--;
                        break;
                    }
                    rb2.Add(stream2FrameQueue.Take());
                }
            }

            if (t1 < rb1FrameCount - rb1.Count) {
                throw new Exception("required frame is not in the buffer any more");
            }
            if (t2 < rb2FrameCount - rb2.Count) {
                throw new Exception("required frame is not in the buffer any more");
            }

            float[] frame1 = rb1[rb1.Count - (rb1FrameCount - t1) - 1];
            float[] frame2 = rb2[rb2.Count - (rb2FrameCount - t2) - 1];
            double cost = CalculateCost(frame1, frame2);
            //Debug.WriteLine("cost " + t1 + "/" + t2 + ": " + cost);
            matrix[t1, t2] = Min(
                matrix[t1, t2 - 1] + cost,
                matrix[t1 - 1, t2] + cost,
                matrix[t1 - 1, t2 - 1] + 2 * cost);
            //Debug.WriteLine("cost " + t1 + "/" + t2 + ": " + matrix[t1, t2]);
        }

        private GetIncResult GetInc(int t, int j) {
            if (t < c) {
                return GetIncResult.Both;
            }
            if (runCount >= MAX_RUN_COUNT) { // ERROR in paper: ">" results in 4 similar runs in a row instead of 3
                if (previous == GetIncResult.Row) {
                    return GetIncResult.Column;
                }
                else {
                    return GetIncResult.Row;
                }
            }

            // x = argmin(pathCost(t,l|*))
            // y = argmin(pathCost(k|*,j))
            int x = ArgminRow(t, j);
            int y = ArgminCol(t, j);

            // NOTE the following block is taken from: Dixon / Live Tracking of Musical...
            //      it doesn't work correctly (ERROR in paper?)
            //if (x < t) {
            //    return GetIncResult.Row;
            //}
            //else if (y < j) {
            //    return GetIncResult.Column;
            //}
            //else {
            //    return GetIncResult.Both;
            //}

            // NOTE the following block is taken from: Arzt / Score Following with Dynamic...
            //      it's different to Dixon's code but works
            //      (in the work it is said that it's derivated of Dixon's original MATCH source code)
            if (x < y) {
                return GetIncResult.Column;
            }
            else if (x == t) {
                return GetIncResult.Both;
            }
            else {
                return GetIncResult.Row;
            }
        }

        private int ArgminRow(int row, int col) {
            double minVal = double.MaxValue;
            int minIndex = 0;
            for (int i = Math.Max(col - c, 1); i <= col; i++) {
                double val = matrix[row, i];
                if (val < minVal) {
                    minVal = val;
                    minIndex = i;
                }
            }
            return minIndex;
        }

        private int ArgminCol(int row, int col) {
            double minVal = double.MaxValue;
            int minIndex = 0;
            for (int i = Math.Max(row - c, 1); i <= row; i++) {
                double val = matrix[i, col];
                if (val < minVal) {
                    minVal = val;
                    minIndex = i;
                }
            }
            return minIndex;
        }

        private void DebugPrintMatrix(int size) {
            for (int i = 0; i < size; i++) {
                for (int j = 0; j < size; j++) {
                    Debug.Write(String.Format("{0:00000.00} ", matrix[i, j]));
                }
                Debug.WriteLine("");
            }
        }
    }
}
