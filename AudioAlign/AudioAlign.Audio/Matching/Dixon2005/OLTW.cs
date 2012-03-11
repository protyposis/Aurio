using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AudioAlign.Audio.Streams;
using AudioAlign.Audio.TaskMonitor;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Threading;

namespace AudioAlign.Audio.Matching.Dixon2005 {
    public class OLTW : DTW {

        private const int MAX_RUN_COUNT = 3; // MaxRunCount

        private enum GetIncResult {
            None = 0,
            Row,
            Column,
            Both
        }

        private BlockingCollection<float[]> stream1FrameQueue;
        private BlockingCollection<float[]> stream2FrameQueue;

        private IMatrix totalCostMatrix, cellCostMatrix;
        private RingBuffer<float[]> rb1;
        private int rb1FrameCount;
        private RingBuffer<float[]> rb2;
        private int rb2FrameCount;
        private int t; // pointer to the current position in series U
        private int j; // pointer to the current position in series V
        private GetIncResult previous;
        private int runCount;
        private int c;

        public OLTW(TimeSpan searchWidth, ProgressMonitor progressMonitor)
            : base(searchWidth, progressMonitor) {
        }

        public OLTW(ProgressMonitor progressMonitor)
            : this(new TimeSpan(0, 0, 10), progressMonitor) {
            // default contructor with default search width (500) like specified in the paper
        }

        public override List<Tuple<TimeSpan, TimeSpan>> Execute(IAudioStream s1, IAudioStream s2) {
            s1 = PrepareStream(s1);
            s2 = PrepareStream(s2);

            int searchWidth = (int)(this.searchWidth.TotalSeconds * (1d * FrameReader.SAMPLERATE / FrameReader.WINDOW_HOP_SIZE));
            totalCostMatrix = new PatchMatrix(double.PositiveInfinity);
            cellCostMatrix = new PatchMatrix(double.PositiveInfinity);
            rb1 = new RingBuffer<float[]>(searchWidth);
            rb1FrameCount = 0;
            rb2 = new RingBuffer<float[]>(searchWidth);
            rb2FrameCount = 0;

            stream1FrameQueue = new BlockingCollection<float[]>(20);
            FrameReader stream1FrameReader = new FrameReader(s1);
            Task.Factory.StartNew(() => {
                while (stream1FrameReader.HasNext()) {
                    float[] frame = new float[FrameReader.FRAME_SIZE];
                    stream1FrameReader.ReadFrame(frame);
                    Thread.Sleep(20);
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
                    Thread.Sleep(20);
                    stream2FrameQueue.Add(frame);
                }
                stream2FrameQueue.CompleteAdding();
            });

            // init matrix
            // NOTE do not explicitely init the PatchMatrix, otherwise the sparse matrix characteristic would 
            //      be gone and the matrix would take up all the space like a standard matrix does
            totalCostMatrix[0, 0] = 0;
            cellCostMatrix[0, 0] = 0;

            IProgressReporter progressReporter = progressMonitor.BeginTask("OLTW", true);
            int totalFrames = stream1FrameReader.WindowCount + stream2FrameReader.WindowCount;

            // --------- OLTW -----------

            t = 1;
            j = 1;
            previous = GetIncResult.None;
            runCount = 0;
            c = searchWidth;
            EvaluatePathCost(t, j);

            FireOltwInit(c, cellCostMatrix, totalCostMatrix);

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

                FireOltwProgress(t, j, t, j, false);

                //Debug.WriteLine(t + " " + j + " " + getInc);

                progressReporter.ReportProgress((rb1FrameCount + rb2FrameCount) / (double)totalFrames * 100);
            }

            FireOltwProgress(t, j, t, j, true);

            Debug.WriteLine("OLTW finished @ t={0}/{1}, j={2}/{3}",
                t, stream1FrameReader.WindowCount,
                j, stream2FrameReader.WindowCount);

            progressReporter.Finish();


            // --------- generate results -----------

            List<Pair> path = OptimalWarpingPath(totalCostMatrix);

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
            totalCostMatrix[t1, t2] = Min(
                totalCostMatrix[t1, t2 - 1] + cost,
                totalCostMatrix[t1 - 1, t2] + cost,
                totalCostMatrix[t1 - 1, t2 - 1] + 2 * cost);
            cellCostMatrix[t1, t2] = cost;
            //Debug.WriteLine("cost " + t1 + "/" + t2 + ": " + matrix[t1, t2]);
        }

        private GetIncResult GetInc(int t, int j) {
            // for the first c steps just proceed diagonally and build a square matrix
            if (t < c) {
                return GetIncResult.Both;
            }

            if (runCount >= MAX_RUN_COUNT) { // ERROR in paper: ">" results in 4 similar runs in a row instead of 3
                if (previous == GetIncResult.Row) {
                    return GetIncResult.Column;
                }
                else if(previous == GetIncResult.Column) {
                    return GetIncResult.Row;
                }
            }

            // x = argmin(pathCost(t,l|*))
            // y = argmin(pathCost(k|*,j))
            double minValX, minValY;
            int x = ArgminRow(t, j, out minValX);
            int y = ArgminCol(t, j, out minValY);

            // NOTE the following block is taken from: Dixon / Live Tracking of Musical...
            //      and has been enhanced with the minVal* comparisons since since it would otherwise wrongly prefer columns
            if (minValX < minValY && x < t) {
                return GetIncResult.Column;
            }
            else if (minValY < minValX && y < j) {
                return GetIncResult.Row;
            }
            else {
                return GetIncResult.Both;
            }
        }

        private int ArgminRow(int row, int col, out double minVal) {
            minVal = double.MaxValue;
            int minIndex = 0;
            for (int i = Math.Max(row - c, 1); i <= row; i++) {
                double val = totalCostMatrix[i, col];
                if (val < minVal) {
                    minVal = val;
                    minIndex = i;
                }
            }
            return minIndex;
        }

        private int ArgminCol(int row, int col, out double minVal) {
            minVal = double.MaxValue;
            int minIndex = 0;
            for (int i = Math.Max(col - c, 1); i <= col; i++) {
                double val = totalCostMatrix[row, i];
                if (val < minVal) {
                    minVal = val;
                    minIndex = i;
                }
            }
            return minIndex;
        }

        private void DebugPrintMatrix(int size) {
            for (int i = 0; i < Math.Min(size, totalCostMatrix.LengthX); i++) {
                for (int j = 0; j < Math.Min(size, totalCostMatrix.LengthY); j++) {
                    Debug.Write(String.Format("{0:00000.00} ", totalCostMatrix[i, j]));
                }
                Debug.WriteLine("");
            }
        }
    }
}
