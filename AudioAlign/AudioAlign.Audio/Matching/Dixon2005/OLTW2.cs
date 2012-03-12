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

        private enum GetIncResult {
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
        private GetIncResult previous;
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

            s1Frames = new List<float[]>((int)(s1.Length / s1.SampleBlockSize / FrameReader.WINDOW_HOP_SIZE));
            FrameReader s1FrameReader = new FrameReader(s1);
            while (s1FrameReader.HasNext()) {
                float[] frame = new float[FrameReader.FRAME_SIZE];
                s1FrameReader.ReadFrame(frame);
                s1Frames.Add(frame);
            }

            s2Frames = new List<float[]>((int)(s2.Length / s2.SampleBlockSize / FrameReader.WINDOW_HOP_SIZE));
            FrameReader s2FrameReader = new FrameReader(s2);
            while (s2FrameReader.HasNext()) {
                float[] frame = new float[FrameReader.FRAME_SIZE];
                s2FrameReader.ReadFrame(frame);
                s2Frames.Add(frame);
            }

            totalCostMatrix = new ArrayMatrix(double.PositiveInfinity, s1Frames.Count, s2Frames.Count);
            cellCostMatrix = new ArrayMatrix(double.PositiveInfinity, s1Frames.Count, s2Frames.Count);
            for (int a = 0; a < totalCostMatrix.LengthX; a++) {
                for (int b = 0; b < totalCostMatrix.LengthY; b++) {
                    totalCostMatrix[a, b] = double.PositiveInfinity;
                    cellCostMatrix[a, b] = double.PositiveInfinity;
                }
            }


            int c = (int)(this.searchWidth.TotalSeconds * (1d * FrameReader.SAMPLERATE / FrameReader.WINDOW_HOP_SIZE));

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
            GetIncResult previous = GetIncResult.None;
            int maxRunCount = 0;

            pathLengthRow = new int[c + MAX_RUN_COUNT];
            pathLengthCol = new int[c + MAX_RUN_COUNT];
            int[] pathLengthRowPrev = new int[c + MAX_RUN_COUNT];
            int[] pathLengthColPrev = new int[c + MAX_RUN_COUNT];

            // init path lengths
            for (int x = 0; x < c; x++) {
                pathLengthRow[x] = GetPathLength(x, j); //OptimalWarpingPath(totalCostMatrix, x, j).Count;
            }
            for (int y = 0; y < c; y++) {
                pathLengthCol[y] = GetPathLength(i, y); //OptimalWarpingPath(totalCostMatrix, i, y).Count;
            }

            IProgressReporter progressReporter = progressMonitor.BeginTask("OLTW", true);
            int totalFrames = s1Frames.Count + s2Frames.Count;

            FireOltwInit(c, cellCostMatrix, totalCostMatrix);

            int row = 0, col = 0, both = 0;
            while (i < s1Frames.Count - 1 || j < s2Frames.Count - 1) {
                // calculate temp. min cost path
                int xI = i;
                double xV = double.PositiveInfinity;
                for (int x = i - c + 1; x <= i; x++) {
                    //double pC = EvaluatePathCost(x, j);
                    //double pC = costMatrix[x, j] / GetPathLength(x, j);
                    //double pC = totalCostMatrix[x, j];
                    double pC = totalCostMatrix[x, j] / pathLengthRow[x % pathLengthRow.Length];
                    if (pC <= xV) {
                        xV = pC;
                        xI = x;
                    }
                }

                int yI = j;
                double yV = double.PositiveInfinity;
                for (int y = j - c + 1; y <= j; y++) {
                    //double pC = EvaluatePathCost(i, y);
                    //double pC = costMatrix[i, y] / GetPathLength(i, y);
                    //double pC = totalCostMatrix[i, y];
                    double pC = totalCostMatrix[i, y] / pathLengthCol[y % pathLengthRow.Length];
                    if (pC <= yV) {
                        yV = pC;
                        yI = y;
                    }
                }

                int minI, minJ;

                GetIncResult r = GetIncResult.None;
                if (xI == i && yI == j) {
                    // min path is in the corner
                    r = GetIncResult.Both;
                    minI = i;
                    minJ = j;
                    both++;
                }
                else if (xV < yV) {
                    // min path was found in a row
                    r = GetIncResult.Row;
                    minI = xI;
                    minJ = j;
                    row++;
                }
                else {
                    // min path is in the column
                    r = GetIncResult.Column;
                    minI = i;
                    minJ = yI;
                    col++;
                }

                FireOltwProgress(i, j, minI, minJ, false);

                if (r == previous) {
                    maxRunCount++;
                }
                if (r != GetIncResult.Both && maxRunCount >= MAX_RUN_COUNT) {
                    if (r == GetIncResult.Row) {
                        r = GetIncResult.Column;
                    }
                    else if (r == GetIncResult.Column) {
                        r = GetIncResult.Row;
                    }
                    maxRunCount = 0;
                }
                previous = r;

                // add row
                if (j < s2Frames.Count - 1 && (r == GetIncResult.Row || r == GetIncResult.Both)) {
                    CommonUtil.Swap<int[]>(ref pathLengthRow, ref pathLengthRowPrev);
                    j++;
                    int start = i - c + 1;
                    for (int x = start; x <= i; x++) {
                        double cellCost = CalculateCost(s1Frames[x], s2Frames[j]);
                        if (x == 0) {
                            totalCostMatrix[x, j] = totalCostMatrix[x, j - 1] + cellCost;
                            pathLengthRow[x % pathLengthRow.Length] = pathLengthRowPrev[x % pathLengthRowPrev.Length] + 1;
                            //if (pathLengthRow[x % pathLengthRow.Length] != GetPathLength(x, j)) {
                            //    Debug.WriteLine("AAAH");
                            //}
                        }
                        else {
                            GetIncResult direction;
                            totalCostMatrix[x, j] = Min(
                                totalCostMatrix[x - 1, j - 1],
                                    totalCostMatrix[x - 1, j],
                                    totalCostMatrix[x, j - 1],
                                    out direction);
                            totalCostMatrix[x, j] += (direction == GetIncResult.Both) ? DIAG_COST_FACTOR * cellCost : cellCost;
                            if (direction == GetIncResult.Row) pathLengthRow[x % pathLengthRow.Length] = pathLengthRow[(x - 1) % pathLengthRow.Length] + 1;
                            else if (direction == GetIncResult.Column) pathLengthRow[x % pathLengthRow.Length] = pathLengthRowPrev[x % pathLengthRowPrev.Length] + 1;
                            else if (direction == GetIncResult.Both) pathLengthRow[x % pathLengthRow.Length] = pathLengthRowPrev[(x - 1) % pathLengthRowPrev.Length] + 2;
                            //pathLengthRow[x % pathLengthRow.Length] = 1 + Min(
                            //    pathLengthRow[(x - 1) % pathLengthRow.Length],
                            //    pathLengthRowPrev[x % pathLengthRowPrev.Length],
                            //    pathLengthRowPrev[(x - 1) % pathLengthRowPrev.Length]);
                            //if (pathLengthRow[x % pathLengthRow.Length] != GetPathLength(x, j)) {
                            //    Debug.WriteLine("AAAH");
                            //}
                        }
                        cellCostMatrix[x, j] = cellCost;
                        //pathLengthRow[x % pathLengthRow.Length] = GetPathLength(x, j);
                    }
                    pathLengthCol[j % pathLengthCol.Length] = pathLengthRow[i % pathLengthRow.Length];
                    //if (pathLengthCol[j % pathLengthCol.Length] != GetPathLength(i, j)) {
                    //    Debug.WriteLine("AAAH");
                    //}
                    //pathLengthCol[j % pathLengthCol.Length] = GetPathLength(i, j);
                }
                // add column
                if (i < s1Frames.Count - 1 && (r == GetIncResult.Column || r == GetIncResult.Both)) {
                    CommonUtil.Swap<int[]>(ref pathLengthCol, ref pathLengthColPrev);
                    i++;
                    int start = j - c + 1;
                    for (int y = start; y <= j; y++) {
                        double cellCost = CalculateCost(s1Frames[i], s2Frames[y]);
                        if (y == 0) {
                            totalCostMatrix[i, y] = totalCostMatrix[i - 1, y] + cellCost;
                            //pathLengthCol[y % pathLengthCol.Length] = pathLengthColPrev[y % pathLengthColPrev.Length] + 1;
                        }
                        else {
                            GetIncResult direction;
                            totalCostMatrix[i, y] = Min(
                                totalCostMatrix[i - 1, y - 1],
                                    totalCostMatrix[i - 1, y],
                                    totalCostMatrix[i, y - 1],
                                    out direction);
                            totalCostMatrix[i, y] += (direction == GetIncResult.Both) ? DIAG_COST_FACTOR * cellCost : cellCost;
                            if (direction == GetIncResult.Column) pathLengthCol[y % pathLengthCol.Length] = pathLengthCol[(y - 1) % pathLengthCol.Length] + 1;
                            else if (direction == GetIncResult.Row) pathLengthCol[y % pathLengthCol.Length] = pathLengthColPrev[y % pathLengthColPrev.Length] + 1;
                            else if (direction == GetIncResult.Both) pathLengthCol[y % pathLengthCol.Length] = pathLengthColPrev[(y - 1) % pathLengthColPrev.Length] + 2;
                        }
                        cellCostMatrix[i, y] = cellCost;
                        //pathLengthCol[y % pathLengthCol.Length] = GetPathLength(i, y);
                    }
                    pathLengthRow[i % pathLengthRow.Length] = pathLengthCol[j % pathLengthCol.Length];
                    //pathLengthRow[i % pathLengthRow.Length] = GetPathLength(i, j);
                }

                progressReporter.ReportProgress((i + j) / (double)totalFrames * 100);
                Thread.Sleep(2);
            }
            FireOltwProgress(i, j, i, j, true);
            progressReporter.Finish();
            Debug.WriteLine("OLTW row {0} col {1} both {2}", row, col, both);

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

        private double EvaluatePathCost(int t1, int t2) {
            if(t1 == 0 && t2 == 0) {
                return 0;
            }

            //double cost = CalculateCost(s1Frames[t1], s2Frames[t2]);
            double cost = cellCostMatrix[t1, t2];

            if (cost == double.PositiveInfinity) {
                return cost;
            }

            double pathCost1 = EvaluatePathCost(t1, t2 - 1) + cost;
            double pathCost2 = EvaluatePathCost(t1 - 1, t2) + cost;
            double pathCost3 = EvaluatePathCost(t1 - 1, t2 - 1) + 2 * cost;

            return Min(pathCost1, pathCost2, pathCost3);
        }

        private int GetPathLength(int i, int j) {
            // TODO OptimalWarpingPath kopieren und so umschreiben dass keine objekte erzeugt werden, dann testen ob schneller
            //return OptimalWarpingPath(matrix, t1, t2).Count;

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

        public static List<Pair> OptimalWarpingPath(IMatrix totalCostMatrix, int i, int j) {
            List<Pair> path = new List<Pair>();
            path.Add(new Pair(i, j));
            while (i > 0 || j > 0) {
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
                    }
                    else if (totalCostMatrix[i - 1, j] == min) {
                        i--;
                    }
                    else {
                        j--;
                    }
                }
                path.Add(new Pair(i, j));
            }
            path.Reverse();
            return path;
        }

        private List<Pair> OptimalWarpingPath(IMatrix totalCostMatrix) {
            return OptimalWarpingPath(totalCostMatrix, totalCostMatrix.LengthX - 1, totalCostMatrix.LengthY - 1);
        }

        //private GetIncResult GetInc(int t, int j) {
        //    // for the first c steps just proceed diagonally and build a square matrix
        //    if (t < c) {
        //        return GetIncResult.Both;
        //    }

        //    if (runCount >= MAX_RUN_COUNT) { // ERROR in paper: ">" results in 4 similar runs in a row instead of 3
        //        if (previous == GetIncResult.Row) {
        //            return GetIncResult.Column;
        //        }
        //        else if(previous == GetIncResult.Column) {
        //            return GetIncResult.Row;
        //        }
        //    }

        //    // x = argmin(pathCost(t,l|*))
        //    // y = argmin(pathCost(k|*,j))
        //    double minValX, minValY;
        //    int x = ArgminRow(t, j, out minValX);
        //    int y = ArgminCol(t, j, out minValY);

        //    // NOTE the following block is taken from: Dixon / Live Tracking of Musical...
        //    //      and has been enhanced with the minVal* comparisons since since it would otherwise wrongly prefer columns
        //    if (minValX < minValY && x < t) {
        //        return GetIncResult.Column;
        //    }
        //    else if (minValY < minValX && y < j) {
        //        return GetIncResult.Row;
        //    }
        //    else {
        //        return GetIncResult.Both;
        //    }
        //}

        //private int ArgminRow(int row, int col, out double minVal) {
        //    minVal = double.MaxValue;
        //    int minIndex = 0;
        //    for (int i = Math.Max(row - c, 0); i < row; i++) {
        //        double val = totalCostMatrix[i, col];
        //        if (val < minVal) {
        //            minVal = val;
        //            minIndex = i;
        //        }
        //    }
        //    return minIndex;
        //}

        //private int ArgminCol(int row, int col, out double minVal) {
        //    minVal = double.MaxValue;
        //    int minIndex = 0;
        //    for (int i = Math.Max(col - c, 0); i < col; i++) {
        //        double val = totalCostMatrix[row, i];
        //        if (val < minVal) {
        //            minVal = val;
        //            minIndex = i;
        //        }
        //    }
        //    return minIndex;
        //}

        private void DebugPrintMatrix(IMatrix matrix, int size) {
            for (int i = 0; i < Math.Min(size, matrix.LengthX); i++) {
                for (int j = 0; j < Math.Min(size, matrix.LengthY); j++) {
                    Debug.Write(String.Format("{0:000000.00} ", matrix[i, j]));
                }
                Debug.WriteLine("");
            }
        }

        protected double CalculateCost(float[] frame1, float[] frame2) {
            // taken from MATCH 0.9.2 at.ofai.music.match.PerformanceMatcher:804
            // NOTE sum of ABS values is the same as the square root of the squared values, BUT FASTER
            double result = 0;
            for (int i = 0; i < frame1.Length; i++) {
                result += Math.Abs(frame1[i] - frame2[i]);
            }
            result = result * 0.001d; // default scaling factor 90
            return result;
        }

        protected static double Min(double val1, double val2, double val3) {
            return Math.Min(val1, Math.Min(val2, val3));
        }

        protected static int Min(int val1, int val2, int val3) {
            return Math.Min(val1, Math.Min(val2, val3));
        }

        private static double Min(double diag, double row, double col, out GetIncResult direction) {
            double min = Math.Min(diag, Math.Min(row, col));
            if (min == diag) direction = GetIncResult.Both;
            else if (min == row) direction = GetIncResult.Row;
            else direction = GetIncResult.Column;
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
