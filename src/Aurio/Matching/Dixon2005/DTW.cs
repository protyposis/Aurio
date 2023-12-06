//
// Aurio: Audio Processing, Analysis and Retrieval Library
// Copyright (C) 2010-2017  Mario Guggenberger <mg@protyposis.net>
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as
// published by the Free Software Foundation, either version 3 of the
// License, or (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
//
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Aurio.DataStructures.Matrix;
using Aurio.Resampler;
using Aurio.Streams;
using Aurio.TaskMonitor;

namespace Aurio.Matching.Dixon2005
{
    public class DTW
    {
        [DebuggerDisplay("Pair {i1} <-> {i2}")]
        public struct Pair
        {
            public int i1,
                i2;

            public Pair(int i1, int i2)
            {
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

        public delegate void OltwInitDelegate(
            int windowSize,
            IMatrix<double> cellCostMatrix,
            IMatrix<double> totalCostMatrix
        );
        public event OltwInitDelegate OltwInit;

        public delegate void OltwProgressDelegate(int i, int j, int minI, int minJ, bool force);
        public event OltwProgressDelegate OltwProgress;

        public DTW(TimeSpan searchWidth, ProgressMonitor progressMonitor)
        {
            this.searchWidth = searchWidth;
            this.progressMonitor = progressMonitor;
        }

        public virtual List<Tuple<TimeSpan, TimeSpan>> Execute(IAudioStream s1, IAudioStream s2)
        {
            s1 = PrepareStream(s1);
            s2 = PrepareStream(s2);

            int diagonalWidth = (int)(
                searchWidth.TotalSeconds
                * (1d * FrameReader.SAMPLERATE / FrameReader.WINDOW_HOP_SIZE)
            );

            // init ring buffers
            rb1 = new float[diagonalWidth][];
            rb1FrameCount = 0;
            rb2 = new float[diagonalWidth][];
            rb2FrameCount = 0;

            stream1FrameQueue = new BlockingCollection<float[]>(20);
            FrameReader stream1FrameReader = new FrameReader(s1);
            Task.Factory.StartNew(() =>
            {
                while (stream1FrameReader.HasNext())
                {
                    float[] frame = new float[FrameReader.FRAME_SIZE];
                    stream1FrameReader.ReadFrame(frame);
                    //Thread.Sleep(20);
                    stream1FrameQueue.Add(frame);
                }
                stream1FrameQueue.CompleteAdding();
            });

            stream2FrameQueue = new BlockingCollection<float[]>(20);
            FrameReader stream2FrameReader = new FrameReader(s2);
            Task.Factory.StartNew(() =>
            {
                while (stream2FrameReader.HasNext())
                {
                    float[] frame = new float[FrameReader.FRAME_SIZE];
                    stream2FrameReader.ReadFrame(frame);
                    //Thread.Sleep(20);
                    stream2FrameQueue.Add(frame);
                }
                stream2FrameQueue.CompleteAdding();
            });

            IProgressReporter progressReporter = progressMonitor.BeginTask("DTW aligning...", true);
            int n = stream1FrameReader.WindowCount;
            int m = stream2FrameReader.WindowCount;

            double deltaN;
            double deltaM;
            if (m > n)
            {
                deltaN = (double)(n - 1) / (m - 1);
                deltaM = 1d;
            }
            else if (m < n)
            {
                deltaN = 1d;
                deltaM = (double)(m - 1) / (n - 1);
            }
            else
            {
                deltaN = 1d;
                deltaM = 1d;
            }

            // NOTE the SparseMatrix is NOT USABLE for DTW -> OutOfMemoryException (1.3GB RAM) at a densely filled matrix of ~4000x3000
            IMatrix<double> totalCostMatrix = new PatchMatrix<double>(double.PositiveInfinity);
            IMatrix<double> cellCostMatrix = new PatchMatrix<double>(double.PositiveInfinity);

            // init matrix
            // NOTE do not explicitely init the PatchMatrix, otherwise the sparse matrix characteristic would
            //      be gone and the matrix would take up all the space like a standard matrix does
            totalCostMatrix[0, 0] = 0;
            cellCostMatrix[0, 0] = 0;

            double progressN = 0;
            double progressM = 0;
            int i,
                x = 0;
            int j,
                y = 0;

            FireOltwInit(diagonalWidth, cellCostMatrix, totalCostMatrix);

            while (x < n || y < m)
            {
                x = (int)progressN + 1;
                y = (int)progressM + 1;
                ReadFrames(x, y);

                i = Math.Max(x - diagonalWidth, 1);
                j = y;
                for (; i <= x; i++)
                {
                    double cost = CalculateCost(
                        rb1[(i - 1) % diagonalWidth],
                        rb2[(j - 1) % diagonalWidth]
                    );
                    totalCostMatrix[i, j] =
                        cost
                        + Min(
                            totalCostMatrix[i - 1, j],
                            totalCostMatrix[i, j - 1],
                            totalCostMatrix[i - 1, j - 1]
                        );
                    cellCostMatrix[i, j] = cost;
                }

                i = x;
                j = Math.Max(y - diagonalWidth, 1);
                for (; j <= y; j++)
                {
                    double cost = CalculateCost(
                        rb1[(i - 1) % diagonalWidth],
                        rb2[(j - 1) % diagonalWidth]
                    );
                    totalCostMatrix[i, j] =
                        cost
                        + Min(
                            totalCostMatrix[i - 1, j],
                            totalCostMatrix[i, j - 1],
                            totalCostMatrix[i - 1, j - 1]
                        );
                    cellCostMatrix[i, j] = cost;
                }

                FireOltwProgress(x, y, x, y, false);

                progressReporter.ReportProgress((double)x / n * 100);

                progressN += deltaN;
                progressM += deltaM;
            }
            FireOltwProgress(x, y, x, y, true);
            progressReporter.Finish();

            List<Pair> path = OptimalWarpingPath(totalCostMatrix);
            path.Reverse();

            return WarpingPathTimes(path, true);
        }

        private void ReadFrames(int x, int y)
        {
            if (rb1FrameCount < x)
            {
                for (; rb1FrameCount < x; rb1FrameCount++)
                {
                    if (stream1FrameQueue.IsCompleted)
                    {
                        rb1FrameCount--;
                        break;
                    }
                    rb1[rb1FrameCount % rb1.Length] = (stream1FrameQueue.Take());
                }
            }

            if (rb2FrameCount < y)
            {
                for (; rb2FrameCount < y; rb2FrameCount++)
                {
                    if (stream2FrameQueue.IsCompleted)
                    {
                        rb2FrameCount--;
                        break;
                    }
                    rb2[rb2FrameCount % rb2.Length] = (stream2FrameQueue.Take());
                }
            }
        }

        protected IAudioStream PrepareStream(IAudioStream stream)
        {
            if (stream.Properties.Channels > 1)
            {
                stream = new MonoStream(stream);
            }
            if (stream.Properties.SampleRate != FrameReader.SAMPLERATE)
            {
                stream = new ResamplingStream(
                    stream,
                    ResamplingQuality.Medium,
                    FrameReader.SAMPLERATE
                );
            }
            return stream;
        }

        public static List<Pair> OptimalWarpingPath(IMatrix<double> totalCostMatrix, int i, int j)
        {
            List<Pair> path = new List<Pair>();
            path.Add(new Pair(i, j));
            while (i > 0 || j > 0)
            {
                if (i == 0)
                {
                    j--;
                }
                else if (j == 0)
                {
                    i--;
                }
                else
                {
                    double min = Min(
                        totalCostMatrix[i - 1, j],
                        totalCostMatrix[i, j - 1],
                        totalCostMatrix[i - 1, j - 1]
                    );
                    if (min == totalCostMatrix[i - 1, j - 1])
                    {
                        i--;
                        j--;
                    }
                    else if (totalCostMatrix[i - 1, j] == min)
                    {
                        i--;
                    }
                    else
                    {
                        j--;
                    }
                }
                path.Add(new Pair(i, j));
            }

            return path;
        }

        public static List<Pair> OptimalWarpingPath(IMatrix<double> totalCostMatrix)
        {
            return OptimalWarpingPath(
                totalCostMatrix,
                totalCostMatrix.LengthX - 1,
                totalCostMatrix.LengthY - 1
            );
        }

        protected static double Min(double val1, double val2, double val3)
        {
            return Math.Min(val1, Math.Min(val2, val3));
        }

        /// <summary>
        /// Computes the distance between two audio frames.
        /// Dixon / Live Tracking of Musical Performances... / formula 4
        /// </summary>
        protected static double CalculateCost(float[] frame1, float[] frame2)
        {
            // taken from MATCH 0.9.2 at.ofai.music.match.PerformanceMatcher:804
            // and: https://code.soundsoftware.ac.uk/projects/match-vamp/repository/entry/Matcher.cpp
            double d = 0;
            double sum = 0;
            for (int i = 0; i < frame1.Length; i++)
            {
                d += Math.Abs(frame1[i] - frame2[i]);
                sum += frame1[i] + frame2[i];
            }
            if (sum == 0)
                return 0;

            double weight = (8 + Math.Log(sum)) / 10.0;

            if (weight < 0)
                weight = 0;
            else if (weight > 1)
                weight = 1;

            return (90d * d / sum * weight); // default scale = 90
        }

        protected static TimeSpan PositionToTimeSpan(long position)
        {
            return new TimeSpan(
                (long)Math.Round((double)position / FrameReader.SAMPLERATE * TimeUtil.SECS_TO_TICKS)
            );
        }

        protected static TimeSpan IndexToTimeSpan(int index)
        {
            return PositionToTimeSpan(index * FrameReader.WINDOW_HOP_SIZE);
        }

        protected static List<Tuple<TimeSpan, TimeSpan>> WarpingPathTimes(
            List<Pair> path,
            bool optimize
        )
        {
            if (optimize)
            {
                // average multiple continuous mappings from one sequence to one time point of the other sequence
                List<Pair> pairBuffer = new List<Pair>();
                List<Pair> cleanedPath = new List<Pair>();
                foreach (Pair p in path)
                {
                    if (pairBuffer.Count == 0)
                    {
                        pairBuffer.Add(p);
                    }
                    else
                    {
                        if (p.i1 == pairBuffer[^1].i1 && p.i2 == pairBuffer[^1].i2 + 1)
                        {
                            // pairs build a horizontal line
                            pairBuffer.Add(p);
                        }
                        else if (p.i2 == pairBuffer[^1].i2 && p.i1 == pairBuffer[^1].i1 + 1)
                        {
                            // pairs build a vertical line
                            pairBuffer.Add(p);
                        }
                        else
                        {
                            // direction change or vertical step
                            if (pairBuffer.Count == 1)
                            {
                                cleanedPath.Add(pairBuffer[0]);
                            }
                            else if (pairBuffer.Count > 1)
                            {
                                // averate the line to a single mapping point
                                cleanedPath.Add(
                                    new Pair(
                                        (int)pairBuffer.Average(bp => bp.i1),
                                        (int)pairBuffer.Average(bp => bp.i2)
                                    )
                                );
                            }
                            pairBuffer.Clear();
                            pairBuffer.Add(p);
                        }
                    }
                }
                cleanedPath.AddRange(pairBuffer);
                path = cleanedPath;

                // replace adjacent horizontal/vertical path segments with diagonal segments
                // HOW??
            }

            List<Tuple<TimeSpan, TimeSpan>> pathTimes = new List<Tuple<TimeSpan, TimeSpan>>();
            foreach (Pair pair in path)
            {
                Tuple<TimeSpan, TimeSpan> timePair = new Tuple<TimeSpan, TimeSpan>(
                    PositionToTimeSpan(pair.i1 * FrameReader.WINDOW_HOP_SIZE),
                    PositionToTimeSpan(pair.i2 * FrameReader.WINDOW_HOP_SIZE)
                );
                if (timePair.Item1 >= TimeSpan.Zero && timePair.Item2 >= TimeSpan.Zero)
                {
                    pathTimes.Add(timePair);
                }
            }

            return pathTimes;
        }

        protected void FireOltwInit(
            int windowSize,
            IMatrix<double> cellCostMatrix,
            IMatrix<double> totalCostMatrix
        )
        {
            OltwInit?.Invoke(windowSize, cellCostMatrix, totalCostMatrix);
        }

        protected void FireOltwProgress(int i, int j, int minI, int minJ, bool force)
        {
            OltwProgress?.Invoke(i, j, i, j, false);
        }
    }
}
