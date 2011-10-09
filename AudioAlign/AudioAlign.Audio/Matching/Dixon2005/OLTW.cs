using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AudioAlign.Audio.Streams;
using AudioAlign.Audio.TaskMonitor;

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

        private IAudioStream stream1;
        private IAudioStream stream2;

        private FrameReader stream1FrameReader;
        private FrameReader stream2FrameReader;

        private SparseMatrix<double> matrix;
        private RingBuffer<float[]> rb1;
        private int rb1FrameCount;
        private RingBuffer<float[]> rb2;
        private int rb2FrameCount;
        private int t; // pointer to the current position in series U
        private int j; // pointer to the current position in series V
        private GetIncResult previous;
        private int runCount;
        private int c;

        public OLTW(IAudioStream s1, IAudioStream s2) : base(new TimeSpan(0), ProgressMonitor.GlobalInstance) {
            this.stream1 = s1;
            this.stream2 = s2;

            stream1FrameReader = new FrameReader(s1);
            stream2FrameReader = new FrameReader(s2);

            int rbCapacity = 100; // TODO make min size SEARCH_WIDTH
            matrix = new SparseMatrix<double>();
            rb1 = new RingBuffer<float[]>(rbCapacity);
            rb1FrameCount = 0;
            rb2 = new RingBuffer<float[]>(rbCapacity);
            rb2FrameCount = 0;
        }

        public void execute() {
            t = 0;
            j = 0;
            previous = GetIncResult.None;
            runCount = 0;
            c = SEARCH_WIDTH;

            // TODO optimize GetInc calls

            EvaluatePathCost(t, j);
            while (true) {
                if (GetInc(t, j) != GetIncResult.Column) {
                    t++;
                    for (int k = j - c + 1; k <= j; k++) {
                        if (k > 0) {
                            EvaluatePathCost(t, k);
                        }
                    }
                }
                if (GetInc(t, j) != GetIncResult.Row) {
                    j++;
                    for (int k = t - c + 1; k <= t; k++) {
                        if (k > 0) {
                            EvaluatePathCost(k, j);
                        }
                    }
                }
                if (GetInc(t, j) == previous) {
                    runCount++;
                }
                else {
                    runCount = 1;
                }
                if (GetInc(t, j) != GetIncResult.Both) {
                    previous = GetInc(t, j);
                }
            }
        }

        private void EvaluatePathCost(int t1, int t2) {
            if (rb1FrameCount < t1) {
                // read frames until position t
                for (; rb1FrameCount <= t1; rb1FrameCount++) {
                    if (!stream1FrameReader.HasNext()) {
                        rb1FrameCount--;
                        break;
                    }
                    float[] frame = new float[FrameReader.FRAME_SIZE];
                    stream1FrameReader.ReadFrame(frame);
                    rb1.Add(frame);
                }
            }

            if (rb2FrameCount < t2) {
                // read frames until position k
                for (; rb2FrameCount <= t2; rb2FrameCount++) {
                    if (!stream2FrameReader.HasNext()) {
                        rb2FrameCount--;
                        break;
                    }
                    float[] frame = new float[FrameReader.FRAME_SIZE];
                    stream2FrameReader.ReadFrame(frame);
                    rb2.Add(frame);
                }
            }

            if (rb1FrameCount - rb1.Count < t1) {
                throw new Exception("required frame is not in the buffer any more");
            }
            if (rb2FrameCount - rb2.Count < t2) {
                throw new Exception("required frame is not in the buffer any more");
            }

            float[] frame1 = rb1[t1 - (rb1FrameCount - rb1.Length)];
            float[] frame2 = rb2[t2 - (rb2FrameCount - rb2.Length)];
            matrix[t1, t2] = CalculateCost(frame1, frame2);
        }

        private GetIncResult GetInc(int t, int j) {
            if (t < c) {
                return GetIncResult.Both;
            }
            if (runCount > MAX_RUN_COUNT) {
                if (previous == GetIncResult.Row) {
                    return GetIncResult.Column;
                }
                else {
                    return GetIncResult.Row;
                }
            }

            // TODO understand pseudocode
            int x = 0, y = 0;

            if (x < t) {
                return GetIncResult.Row;
            }
            else if (y < j) {
                return GetIncResult.Column;
            }
            else {
                return GetIncResult.Both;
            }
        }
    }
}
