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
    public class OLTW2 : DTW {

        private const int MAX_RUN_COUNT = 3; // MaxRunCount
        private const int DIAG_COST_FACTOR = 1;
        private const int DIAG_LENGTH_FACTOR = 2;

        private enum Direction {
            None = 0,
            Row,
            Column,
            Both
        }

        private BlockingCollection<float[]> stream1FrameQueue;
        private BlockingCollection<float[]> stream2FrameQueue;

        //private PatchMatrix matrix;
        private RingBuffer<float[]> rb1;
        private int rb1FrameCount;
        private RingBuffer<float[]> rb2;
        private int rb2FrameCount;
        private int t; // pointer to the current position in series U
        private int j; // pointer to the current position in series V
        private Direction previous;
        private int runCount;
        private int c;

        List<float[]> s1Frames, s2Frames;
        IMatrix totalCostMatrix;
        IMatrix cellCostMatrix;
        int[] pathLengthRow, pathLengthCol;

        public OLTW2(TimeSpan searchWidth, ProgressMonitor progressMonitor)
            : base(searchWidth, progressMonitor) {
        }

        public OLTW2(ProgressMonitor progressMonitor)
            : this(new TimeSpan(0, 0, 10), progressMonitor) {
            // default contructor with default search width (500) like specified in the paper
        }

        public override List<Tuple<TimeSpan, TimeSpan>> Execute(IAudioStream s1, IAudioStream s2) {
            s1 = PrepareStream(s1);
            s2 = PrepareStream(s2);

            if (s1.Length > s2.Length) {
                s1 = new VolumeControlStream(s1) { Volume = 2.7f };
            }
            else {
                s2 = new VolumeControlStream(s2) { Volume = 2.7f };
            }

            IProgressReporter progressReporter;

            progressReporter = progressMonitor.BeginTask("OLTW analyzing stream 1..", true);
            FrameReader s1FrameReader = new FrameReader(s1);
            s1Frames = new List<float[]>(s1FrameReader.WindowCount);
            int s1FrameCount = 0;
            while (s1FrameReader.HasNext()) {
                float[] frame = new float[FrameReader.FRAME_SIZE];
                s1FrameReader.ReadFrame(frame);
                s1Frames.Add(frame);
                progressReporter.ReportProgress((double)++s1FrameCount / s1Frames.Capacity * 100);
            }
            progressReporter.Finish();

            progressReporter = progressMonitor.BeginTask("OLTW analyzing stream 2...", true);
            FrameReader s2FrameReader = new FrameReader(s2);
            s2Frames = new List<float[]>(s2FrameReader.WindowCount);
            int s2FrameCount = 0;
            while (s2FrameReader.HasNext()) {
                float[] frame = new float[FrameReader.FRAME_SIZE];
                s2FrameReader.ReadFrame(frame);
                s2Frames.Add(frame);
                progressReporter.ReportProgress((double)++s2FrameCount / s2Frames.Capacity * 100);
            }
            progressReporter.Finish();

            int c = (int)(this.searchWidth.TotalSeconds * (1d * FrameReader.SAMPLERATE / FrameReader.WINDOW_HOP_SIZE));
            c = Math.Min(c, Math.Min(s1FrameCount, s2FrameCount)); // reduce c to the shortest stream if necessary

            progressReporter = progressMonitor.BeginTask("OLTW initializing matrix...", false);
            //totalCostMatrix = new ArrayMatrix(double.PositiveInfinity, s1Frames.Count, s2Frames.Count);
            //cellCostMatrix = new ArrayMatrix(double.PositiveInfinity, s1Frames.Count, s2Frames.Count);
            //totalCostMatrix = new PatchMatrix(double.PositiveInfinity, 100);
            //cellCostMatrix = new PatchMatrix(double.PositiveInfinity, 100);
            totalCostMatrix = new DiagonalArrayMatrix(c + MAX_RUN_COUNT * c, double.PositiveInfinity);
            cellCostMatrix = new DiagonalArrayMatrix(c + MAX_RUN_COUNT * c, double.PositiveInfinity);

            totalCostMatrix[0, 0] = 0;
            cellCostMatrix[0, 0] = 0;
            // init square matrix
            for (int x = 0; x < c; x++) {
                for (int y = 0; y < c; y++) {
                    double cellCost = CalculateCost(s1Frames[x], s2Frames[y]);
                    if (x == 0 && y == 0) {
                        totalCostMatrix[x, y] = cellCost;
                    }
                    else if (x == 0) {
                        totalCostMatrix[x, y] = totalCostMatrix[x, y - 1] + cellCost;
                    }
                    else if (y == 0) {
                        totalCostMatrix[x, y] = totalCostMatrix[x - 1, y] + cellCost;
                    }
                    else {
                        totalCostMatrix[x, y] = Min(
                                totalCostMatrix[x, y - 1] + cellCost,
                                totalCostMatrix[x - 1, y] + cellCost,
                                totalCostMatrix[x - 1, y - 1] + DIAG_COST_FACTOR * cellCost);
                    }
                    cellCostMatrix[x, y] = cellCost;
                }
            }

            int i = c - 1;
            int j = c - 1;
            Direction previous = Direction.None;
            int maxRunCount = 0;

            pathLengthRow = new int[c + MAX_RUN_COUNT];
            pathLengthCol = new int[c + MAX_RUN_COUNT];
            int[] pathLengthRowPrev = new int[c + MAX_RUN_COUNT];
            int[] pathLengthColPrev = new int[c + MAX_RUN_COUNT];

            // init path lengths
            for (int x = 0; x < c; x++) {
                pathLengthRow[x] = GetPathLength(x, j);
            }
            for (int y = 0; y < c; y++) {
                pathLengthCol[y] = GetPathLength(i, y);
            }

            progressReporter.Finish();
            progressReporter = progressMonitor.BeginTask("OLTW aligning...", true);
            int totalFrames = s1Frames.Count + s2Frames.Count;

            FireOltwInit(c, cellCostMatrix, totalCostMatrix);

            int row = 0, col = 0, both = 0;
            while (i < s1Frames.Count - 1 || j < s2Frames.Count - 1) {
                // calculate temp. min cost path
                int xI = i;
                double xV = double.PositiveInfinity;
                for (int x = i - c + 1; x <= i; x++) {
                    double pC = totalCostMatrix[x, j] / pathLengthRow[x % pathLengthRow.Length];
                    if (pC <= xV) {
                        xV = pC;
                        xI = x;
                    }
                }

                int yI = j;
                double yV = double.PositiveInfinity;
                for (int y = j - c + 1; y <= j; y++) {
                    double pC = totalCostMatrix[i, y] / pathLengthCol[y % pathLengthRow.Length];
                    if (pC <= yV) {
                        yV = pC;
                        yI = y;
                    }
                }

                int minI, minJ;

                Direction r = Direction.None;
                if (xI == i && yI == j) {
                    // min path is in the corner
                    r = Direction.Both;
                    minI = i;
                    minJ = j;
                    both++;
                }
                else if (xV < yV) {
                    // min path was found in a row
                    r = Direction.Row;
                    minI = xI;
                    minJ = j;
                    row++;
                }
                else {
                    // min path is in the column
                    r = Direction.Column;
                    minI = i;
                    minJ = yI;
                    col++;
                }

                FireOltwProgress(i, j, minI, minJ, false);

                if (r == previous) {
                    maxRunCount++;
                }
                if (r == Direction.Both) {
                    maxRunCount = 0;
                }
                else if (maxRunCount >= MAX_RUN_COUNT) {
                    if (r == Direction.Row) {
                        r = Direction.Column;
                    }
                    else if (r == Direction.Column) {
                        r = Direction.Row;
                    }
                    maxRunCount = 0;
                }

                // add row
                if (j < s2Frames.Count - 1 && (r == Direction.Row || r == Direction.Both)) {
                    CommonUtil.Swap<int[]>(ref pathLengthRow, ref pathLengthRowPrev);
                    j++;
                    int start = i - c + 1;
                    for (int x = start; x <= i; x++) {
                        double cellCost = CalculateCost(s1Frames[x], s2Frames[j]);
                        if (x == 0) {
                            totalCostMatrix[x, j] = totalCostMatrix[x, j - 1] + cellCost;
                            pathLengthRow[x % pathLengthRow.Length] = pathLengthRowPrev[x % pathLengthRowPrev.Length] + 1;
                        }
                        else {
                            Direction direction;
                            totalCostMatrix[x, j] = Min(
                                totalCostMatrix[x - 1, j - 1],
                                    totalCostMatrix[x - 1, j],
                                    totalCostMatrix[x, j - 1],
                                    out direction);
                            totalCostMatrix[x, j] += (direction == Direction.Both) ? DIAG_COST_FACTOR * cellCost : cellCost;
                            if (direction == Direction.Row) pathLengthRow[x % pathLengthRow.Length] = pathLengthRow[(x - 1) % pathLengthRow.Length] + 1;
                            else if (direction == Direction.Column) pathLengthRow[x % pathLengthRow.Length] = pathLengthRowPrev[x % pathLengthRowPrev.Length] + 1;
                            else if (direction == Direction.Both) pathLengthRow[x % pathLengthRow.Length] = pathLengthRowPrev[(x - 1) % pathLengthRowPrev.Length] + DIAG_LENGTH_FACTOR;
                        }
                        cellCostMatrix[x, j] = cellCost;
                    }
                    pathLengthCol[j % pathLengthCol.Length] = pathLengthRow[i % pathLengthRow.Length];
                }
                // add column
                if (i < s1Frames.Count - 1 && (r == Direction.Column || r == Direction.Both)) {
                    CommonUtil.Swap<int[]>(ref pathLengthCol, ref pathLengthColPrev);
                    i++;
                    int start = j - c + 1;
                    for (int y = start; y <= j; y++) {
                        double cellCost = CalculateCost(s1Frames[i], s2Frames[y]);
                        if (y == 0) {
                            totalCostMatrix[i, y] = totalCostMatrix[i - 1, y] + cellCost;
                        }
                        else {
                            Direction direction;
                            totalCostMatrix[i, y] = Min(
                                totalCostMatrix[i - 1, y - 1],
                                    totalCostMatrix[i - 1, y],
                                    totalCostMatrix[i, y - 1],
                                    out direction);
                            totalCostMatrix[i, y] += (direction == Direction.Both) ? DIAG_COST_FACTOR * cellCost : cellCost;
                            if (direction == Direction.Column) pathLengthCol[y % pathLengthCol.Length] = pathLengthCol[(y - 1) % pathLengthCol.Length] + 1;
                            else if (direction == Direction.Row) pathLengthCol[y % pathLengthCol.Length] = pathLengthColPrev[y % pathLengthColPrev.Length] + 1;
                            else if (direction == Direction.Both) pathLengthCol[y % pathLengthCol.Length] = pathLengthColPrev[(y - 1) % pathLengthColPrev.Length] + DIAG_LENGTH_FACTOR;
                        }
                        cellCostMatrix[i, y] = cellCost;
                    }
                    pathLengthRow[i % pathLengthRow.Length] = pathLengthCol[j % pathLengthCol.Length];
                }

                previous = r;
                progressReporter.ReportProgress((i + j) / (double)totalFrames * 100);
                //Thread.Sleep(2);
            }
            FireOltwProgress(i, j, i, j, true);
            progressReporter.Finish();
            Debug.WriteLine("OLTW row {0} col {1} both {2}", row, col, both);

            List<Pair> path = OptimalWarpingPath(totalCostMatrix);
            path.Reverse();

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

        private int GetPathLength(int i, int j) {
            int length = 1;
            bool diagonal;
            while (i > 0 || j > 0) {
                diagonal = false;
                if (i == 0) {
                    j--;
                }
                else if (j == 0) {
                    i--;
                }
                else {
                    double min = Min(totalCostMatrix[i - 1, j], totalCostMatrix[i, j - 1], totalCostMatrix[i - 1, j - 1]);
                    if (min == totalCostMatrix[i - 1, j - 1]) {
                        i--;
                        j--;
                        diagonal = true;
                    }
                    else if (totalCostMatrix[i - 1, j] == min) {
                        i--;
                    }
                    else {
                        j--;
                    }
                }
                if (diagonal) length += DIAG_LENGTH_FACTOR;
                else length++;
            }
            return length;
        }

        private void DebugPrintMatrix(IMatrix matrix, int size) {
            for (int i = 0; i < Math.Min(size, matrix.LengthX); i++) {
                for (int j = 0; j < Math.Min(size, matrix.LengthY); j++) {
                    Debug.Write(String.Format("{0:000000.00} ", matrix[i, j]));
                }
                Debug.WriteLine("");
            }
        }

        private static double Min(double diag, double row, double col, out Direction direction) {
            double min = Math.Min(diag, Math.Min(row, col));
            if (min == diag) direction = Direction.Both;
            else if (min == row) direction = Direction.Row;
            else direction = Direction.Column;
            return min;
        }

        protected void DebugPrintMatrixHeadInfo(IMatrix matrix, int i, int j, int c) {
            
            int xI = -1;
            double xV = double.PositiveInfinity;
            Debug.Write("ROW:  ");
            for (int x = Math.Max(i - c, 0); x <= i; x++) {
                Debug.Write(String.Format("{0:000000.00} ", matrix[x, j]));
                if (matrix[x, j] <= xV) {
                    xV = matrix[x, j];
                    xI = x;
                }
            }
            Debug.WriteLine("");
            Debug.Write("COST: ");
            for (int x = Math.Max(i - c, 0); x <= i; x++) {
                Debug.Write(String.Format("{0:000000.00} ", pathLengthRow[x % pathLengthRow.Length]));
            }
            Debug.WriteLine("");
            Debug.Write("TOT:  ");
            for (int x = Math.Max(i - c, 0); x <= i; x++) {
                Debug.Write(String.Format("{0:000000.00} ", (int)(matrix[x, j] / (double)pathLengthRow[x % pathLengthRow.Length])));
            }
            Debug.WriteLine("");
            Debug.WriteLine("ROW min index {0} val {1}", xI, xV);

            
            int yI = -1;
            double yV = double.PositiveInfinity;
            Debug.Write("COL:  ");
            for (int y = Math.Max(j - c, 0); y <= j; y++) {
                Debug.Write(String.Format("{0:000000.00} ", matrix[i, y]));
                if (matrix[i, y] <= yV) {
                    yV = matrix[i, y];
                    yI = y;
                }
            }
            Debug.WriteLine("");
            Debug.Write("COST: ");
            for (int y = Math.Max(j - c, 0); y <= j; y++) {
                Debug.Write(String.Format("{0:000000.00} ", pathLengthCol[y % pathLengthCol.Length]));
            }
            Debug.WriteLine("");
            Debug.Write("TOT:  ");
            for (int y = Math.Max(j - c, 0); y <= j; y++) {
                Debug.Write(String.Format("{0:000000.00} ", (int)(matrix[i, y] / (double)pathLengthCol[y % pathLengthCol.Length])));
            }
            Debug.WriteLine("");
            Debug.WriteLine("COL min index {0} val {1}", yI, yV);
        }
    }
}
