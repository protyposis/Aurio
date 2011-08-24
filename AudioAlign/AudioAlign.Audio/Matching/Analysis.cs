using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AudioAlign.Audio.Project;
using AudioAlign.Audio.Streams;
using System.Diagnostics;
using AudioAlign.Audio.TaskMonitor;
using System.Threading.Tasks;

namespace AudioAlign.Audio.Matching {
    public class Analysis {

        public static void AnalyzeAlignment(TrackList<AudioTrack> audioTracks, TimeSpan windowLength, TimeSpan intervalLength, int sampleRate, ProgressMonitor progressMonitor) {
            if (audioTracks.Count < 2) {
                // there must be at least 2 tracks, otherwise there's nothing to compare
                return;
            }
            if (intervalLength < windowLength) {
                throw new ArgumentException("intervalLength must be at least as long as the windowLength");
            }

            IProgressReporter reporter = progressMonitor.BeginTask("Analyzing alignment...", true);

            List<IAudioStream> streams = new List<IAudioStream>(audioTracks.Count);
            TimeSpan start = TimeSpan.MaxValue;
            TimeSpan end = TimeSpan.MinValue;
            foreach (AudioTrack audioTrack in audioTracks) {
                streams.Add(CrossCorrelation.PrepareStream(audioTrack.CreateAudioStream(), 22050));
                if (audioTrack.Offset < start) {
                    start = audioTrack.Offset;
                }
                if (audioTrack.Offset + audioTrack.Length > end) {
                    end = audioTrack.Offset + audioTrack.Length;
                }
            }

            long[] streamOffsets = new long[audioTracks.Count];
            for (int i = 0; i < audioTracks.Count; i++) {
                streamOffsets[i] = TimeUtil.TimeSpanToBytes(audioTracks[i].Offset - start, streams[0].Properties);
            }

            int windowLengthInBytes = (int)TimeUtil.TimeSpanToBytes(windowLength, streams[0].Properties);
            int windowLengthInSamples = windowLengthInBytes / streams[0].Properties.SampleBlockByteSize;
            long intervalLengthInBytes = TimeUtil.TimeSpanToBytes(intervalLength, streams[0].Properties);
            long analysisIntervalLength = TimeUtil.TimeSpanToBytes(end - start, streams[0].Properties);

            byte[] x = new byte[windowLengthInBytes];
            byte[] y = new byte[windowLengthInBytes];
            long positionX;
            long positionY;
            double sumNegative = 0;
            double sumPositive = 0;
            int countNegative = 0;
            int countPositive = 0;
            double min = float.MaxValue;
            double max = float.MinValue;
            int measurementPoints = 0;
            unsafe {
                fixed (byte* xB = &x[0], yB = &y[0]) {
                    float* xF = (float*)xB;
                    float* yF = (float*)yB;
                    for (long position = 0; position < analysisIntervalLength; position += intervalLengthInBytes) {
                        Debug.WriteLine("Analyzing {0} / {1}", position, analysisIntervalLength);
                        // at each position in the analysis interval, compare each stream with each other
                        for (int i = 0; i < streams.Count; i++) {
                            positionX = position - streamOffsets[i];
                            if (positionX >= 0 && positionX < streams[i].Length) {
                                streams[i].Position = positionX;
                                StreamUtil.ForceRead(streams[i], x, 0, windowLengthInBytes);
                                for (int j = i + 1; j < streams.Count; j++) {
                                    positionY = position - streamOffsets[j];
                                    if (positionY >= 0 && positionY < streams[j].Length) {
                                        streams[j].Position = positionY;
                                        StreamUtil.ForceRead(streams[j], y, 0, windowLengthInBytes);

                                        double val = CrossCorrelation.Correlate(xF, yF, windowLengthInSamples);
                                        if (val > 0) {
                                            sumPositive += val;
                                            countPositive++;
                                        }
                                        else {
                                            sumNegative += val;
                                            countNegative++;
                                        }
                                        if (min > val) {
                                            min = val;
                                        }
                                        if (max < val) {
                                            max = val;
                                        }
                                        Debug.WriteLine("{0,2}->{1,2}: {2}", i, j, val);
                                        measurementPoints++;
                                    }
                                }
                            }
                        }
                        reporter.ReportProgress((double)position / analysisIntervalLength * 100);
                    }
                }
            }
            reporter.Finish();
            Debug.WriteLine("Finished. sum: {0}, sum+: {1}, sum-: {2}, sumAbs: {3}, avg: {4}, avg+: {5}, avg-: {6}, avgAbs: {7}, min: {8}, max: {9}, points: {10}", 
                sumPositive + sumNegative, sumPositive, sumNegative, sumPositive + (sumNegative * -1), 
                (sumPositive + sumNegative) / (countPositive + countNegative), sumPositive / countPositive,
                sumNegative / countNegative, (sumPositive + (sumNegative * -1)) / (countPositive + countNegative),
                min, max, measurementPoints);
            double score = (sumPositive + (sumNegative * -1)) / measurementPoints;
            Debug.WriteLine("Score: {0} => {1}%", score, Math.Round(score * 100));
        }

        public static void AnalyzeAlignmentAsync(TrackList<AudioTrack> audioTracks, TimeSpan windowLength, TimeSpan intervalLength, int sampleRate, ProgressMonitor progressMonitor) {
            Task.Factory.StartNew(() => {
                AnalyzeAlignment(audioTracks, windowLength, intervalLength, sampleRate, progressMonitor);
            });
        }
    }
}
