using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aurio.Streams;
using Aurio.TaskMonitor;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Threading;
using Aurio.DataStructures.Matrix;

namespace Aurio.Matching.Dixon2005 {
    /// <summary>
    /// On-line Time Warping
    /// - Dixon, Simon. "An On-Line Time Warping Algorithm for Tracking Musical Performances." IJCAI. 2005.
    /// - Dixon, Simon, and Gerhard Widmer. "MATCH: A Music Alignment Tool Chest." ISMIR. 2005.
    /// </summary>
    public class OLTW2 : DTW {

        private const int MAX_RUN_COUNT = 3;
        private const int DIAG_COST_FACTOR = 1;

        private enum Direction {
            None = 0,
            Row,
            Column,
            Both
        }

        private List<float[]> s1Frames, s2Frames;
        private IMatrix<double> totalCostMatrix;
        private IMatrix<double> cellCostMatrix;
        private int[] pathLengthRow, pathLengthCol;

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

            totalCostMatrix = new PatchMatrix<double>(double.PositiveInfinity, 100);
            cellCostMatrix = new PatchMatrix<double>(double.PositiveInfinity, 100);

            int i = 0, j = 0; // position of the "head" in each step
            int minI, minJ; // position of the cell with the min path cost in each step
            Direction r = Direction.None;
            Direction previous = Direction.None;
            int runCount = 0;
            pathLengthRow = new int[c + MAX_RUN_COUNT];
            pathLengthCol = new int[c + MAX_RUN_COUNT];
            int[] pathLengthRowPrev = new int[c + MAX_RUN_COUNT];
            int[] pathLengthColPrev = new int[c + MAX_RUN_COUNT];
            int totalFrames = s1Frames.Count + s2Frames.Count;

            progressReporter = progressMonitor.BeginTask("OLTW aligning...", true);

            // init
            totalCostMatrix[0, 0] = cellCostMatrix[0, 0] = CalculateCost(s1Frames[0], s2Frames[0]);
            FireOltwInit(c, cellCostMatrix, totalCostMatrix);

            while (i < s1Frames.Count - 1 || j < s2Frames.Count - 1) {
                if (i < c) { // build initial square matrix
                    r = Direction.Both;
                    minI = i;
                    minJ = j;
                }
                else { // calculate temp. min cost path
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

                    if (xI == i && yI == j) {
                        // min path is in the corner
                        r = Direction.Both;
                        minI = i;
                        minJ = j;
                    }
                    else if (xV < yV) {
                        // min path was found in a row
                        r = Direction.Row;
                        minI = xI;
                        minJ = j;
                    }
                    else {
                        // min path is in the column
                        r = Direction.Column;
                        minI = i;
                        minJ = yI;
                    }
                }

                FireOltwProgress(i, j, minI, minJ, false);

                if (r == previous) {
                    runCount++;
                }
                if (r == Direction.Both) {
                    runCount = 0;
                }
                else if (runCount >= MAX_RUN_COUNT) {
                    if (r == Direction.Row) {
                        r = Direction.Column;
                    }
                    else if (r == Direction.Column) {
                        r = Direction.Row;
                    }
                    runCount = 0;
                }

                // add row
                if (j < s2Frames.Count - 1 && (r == Direction.Row || r == Direction.Both)) {
                    CommonUtil.Swap<int[]>(ref pathLengthRow, ref pathLengthRowPrev);
                    j++;
                    for (int x = Math.Max(i - c + 1, 0); x <= i; x++) {
                        double cellCost = CalculateCost(s1Frames[x], s2Frames[j]);
                        if (x == 0) {
                            totalCostMatrix[x, j] = totalCostMatrix[x, j - 1] + cellCost;
                        }
                        else {
                            totalCostMatrix[x, j] = Min(
                                totalCostMatrix[x - 1, j - 1] + DIAG_COST_FACTOR * cellCost,
                                totalCostMatrix[x - 1, j] + cellCost,
                                totalCostMatrix[x, j - 1] + cellCost);
                        }
                        cellCostMatrix[x, j] = cellCost;
                        pathLengthRow[x % pathLengthRow.Length] = GetPathLength(x, j);
                    }
                    pathLengthCol[j % pathLengthCol.Length] = pathLengthRow[i % pathLengthRow.Length];
                }

                // add column
                if (i < s1Frames.Count - 1 && (r == Direction.Column || r == Direction.Both)) {
                    CommonUtil.Swap<int[]>(ref pathLengthCol, ref pathLengthColPrev);
                    i++;
                    for (int y = Math.Max(j - c + 1, 0); y <= j; y++) {
                        double cellCost = CalculateCost(s1Frames[i], s2Frames[y]);
                        if (y == 0) {
                            totalCostMatrix[i, y] = totalCostMatrix[i - 1, y] + cellCost;
                        }
                        else {
                            totalCostMatrix[i, y] = Min(
                                totalCostMatrix[i - 1, y - 1] + DIAG_COST_FACTOR * cellCost, 
                                totalCostMatrix[i - 1, y] + cellCost,
                                totalCostMatrix[i, y - 1] + cellCost);
                        }
                        cellCostMatrix[i, y] = cellCost;
                        pathLengthCol[y % pathLengthCol.Length] = GetPathLength(i, y);
                    }
                    pathLengthRow[i % pathLengthRow.Length] = pathLengthCol[j % pathLengthCol.Length];
                }

                previous = r;
                progressReporter.ReportProgress((i + j) / (double)totalFrames * 100);
                //Thread.Sleep(2);
            }
            FireOltwProgress(i, j, i, j, true);
            progressReporter.Finish();

            List<Pair> path = OptimalWarpingPath(totalCostMatrix);
            path.Reverse();

            return WarpingPathTimes(path, true);
        }

        private int GetPathLength(int i, int j) {
            return 1 + i + j;
        }

        private void DebugPrintMatrix(IMatrix<double> matrix, int size) {
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

        protected void DebugPrintMatrixHeadInfo(IMatrix<double> matrix, int i, int j, int c) {
            
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
