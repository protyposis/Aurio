using System.IO;
using System;
using System.Linq;
using System.Diagnostics;
using System.Windows;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
namespace AudioAlign.Audio {
    public class VisualizingAudioStream16: AudioStreamWrapper<float>, IAudioStream16 {

        private const int SAMPLES_PER_PEAK = 1024;
        private const int BUFFER_SIZE = 1024;

        private float[][] buffer;

        private PeakStore peakStore;
        private BinaryReader[] peakReaders;

        public VisualizingAudioStream16(IAudioStream16 audioStream, PeakStore peakStore)
            : base(audioStream) {
            // init memory streams
            this.peakStore = peakStore;
            this.peakReaders = peakStore.CreateMemoryStreams().WrapWithBinaryReaders();

            //init buffer
            buffer = AudioUtil.CreateArray<float>(audioStream.Properties.Channels, BUFFER_SIZE);
        }

        /// <summary>
        /// Reads audio data from the stream in a graphically representable form... samples oder peaks...
        /// ZoomFactor oder sowas ähnliches noch einbauen, damit ich entscheiden kann wie viele samples oder peaks returned werden sollen
        /// 
        /// If the requestedInterval is completely covered by the audio stream, it holds:
        /// * readInterval.From &lt;= requestedInterval.From && readInterval.To &gt;= readInterval
        /// because the readInterval borders are adjusted to the underlying samples/peaks.
        /// otherwise:
        /// * readInterval can end before the end of requestedInterval if the end of the audio stream has been reached
        /// </summary>
        /// <param name="requestedInterval">the time interval to read from the audio stream</param>
        /// <param name="targetSamples"></param>
        /// <param name="readInterval">the interval that has actually been read</param>
        /// <param name="peaks"></param>
        /// <returns></returns>
        public List<Point>[] Read(Interval requestedInterval, long targetSamples, out Interval readInterval, out bool peaks) {
            if (targetSamples == 0) {
                //throw new ArgumentException(targetSamples + " samples requested!?");
                peaks = false;
                readInterval = new Interval(requestedInterval.From, requestedInterval.From);
                return AudioUtil.CreateList<Point>(Properties.Channels, 0);
            }

            int samplesInRequestedInterval = AudioUtil.CalculateSamples(Properties, new TimeSpan(requestedInterval.Length));
            if (samplesInRequestedInterval < targetSamples) {
                throw new ArgumentException("the requested interval contains less samples than requested: " +
                    samplesInRequestedInterval + " < " + targetSamples);
            }

            double resampleFactor = samplesInRequestedInterval / targetSamples;
            //Debug.WriteLine("VisualizingAudioStream16 resampleFactor: " + resampleFactor + " (" + samplesInRequestedInterval + "/" + targetSamples + ")");

            /*
             * STAGES:
             * - return unresampled samples from file data CHECK
             * - return on the fly resampled samples from file data NO - DECEPTIVE VISUALS RESULTING
             * - return on the fly generated peaks from file data CHECK
             * - return precomputed & on the fly resampled peaks from peak data CHECK
             */

            if (resampleFactor <= 1) {
                peaks = false;
                audioStream.TimePosition = new TimeSpan(requestedInterval.From);
                List<Point>[] samples = SamplesToPoints(LoadSamples(AudioUtil.CalculateSamples(Properties, new TimeSpan(requestedInterval.Length))));
                readInterval = new Interval(requestedInterval.From, requestedInterval.From + (long)Math.Ceiling(samples[0].Count * AudioUtil.CalculateSampleTicks(Properties)));
                return samples;
            }
            else if (resampleFactor < SAMPLES_PER_PEAK) { // 
                // TODO if this is a performance bottleneck: load & resample peaks in one pass
                peaks = true;
                List<float>[] samples = LoadSamples(AudioUtil.CalculateSamples(Properties, new TimeSpan(requestedInterval.Length)));
                readInterval = new Interval(requestedInterval.From, requestedInterval.From + (long)Math.Ceiling(samples[0].Count * AudioUtil.CalculateSampleTicks(Properties)));
                int downsamplingFactor = (int)(samples[0].Count / targetSamples);
                return PeaksToPoints(SamplesToPeaks(samples, downsamplingFactor));
            }
            else {
                peaks = true;
                readInterval = requestedInterval;
                List<Peak>[] loadedPeaks = LoadPeaks(requestedInterval);
                int downsamplingFactor = (int)(loadedPeaks[0].Count / targetSamples);
                return PeaksToPoints(loadedPeaks, downsamplingFactor);
            }
        }

        private List<float>[] LoadSamples(int samples) {
            int channels = Properties.Channels;
            List<float>[] samplePoints = AudioUtil.CreateList<float>(channels, samples);
            int totalSamplesRead = 0;

            while (totalSamplesRead < samples) {
                int samplesRead = Read(buffer, BUFFER_SIZE);
                if (samplesRead == 0)
                    break;

                for (int x = 0; x < samplesRead; x++) {
                    for (int channel = 0; channel < channels; channel++) {
                        samplePoints[channel].Add(buffer[channel][x]);
                    }
                    totalSamplesRead++;
                    if (totalSamplesRead == samples)
                        break;
                }
            }

            return samplePoints;
        }

        private static List<Point>[] SamplesToPoints(List<float>[] samples) {
            int channels = samples.Length;
            List<Point>[] points = AudioUtil.CreateList<Point>(samples.Length, samples[0].Count);
            for (int channel = 0; channel < channels; channel++) {
                for (int x = 0; x < samples[0].Count; x++) {
                    points[channel].Add(new Point(x, samples[channel][x]));
                }
            }
            return points;
        }

        [Obsolete("downsampling is visually deceptive - generate peaks instead")]
        private static List<Point>[] SamplesToPoints(List<float>[] samples, int downsamplingFactor) {
            int channels = samples.Length;
            int targetPoints = (int)Math.Ceiling((float)samples[0].Count / downsamplingFactor);
            List<Point>[] points = AudioUtil.CreateList<Point>(samples.Length, samples[0].Count);

            for (int channel = 0; channel < channels; channel++) {
                List<float> temp = new List<float>(downsamplingFactor);
                int pointCount = 0;
                int sampleCount = 0;
                for (int x = 0; x < samples[0].Count; x++) {
                    temp.Add(samples[channel][x]);
                    if (++sampleCount % downsamplingFactor == 0) {
                        points[channel].Add(new Point(pointCount++, temp.Average()));
                        temp.Clear();
                    }
                }
            }
            return points;
        }

        private static List<Peak>[] SamplesToPeaks(List<float>[] samples, int downsamplingFactor) {
            int channels = samples.Length;
            int sourceSampleCount = samples[0].Count;
            int targetPeakCount = (int)Math.Ceiling((float)sourceSampleCount / downsamplingFactor);
            List<Peak>[] peaks = AudioUtil.CreateList<Peak>(channels, targetPeakCount);

            for (int channel = 0; channel < channels; channel++) {
                List<float> temp = new List<float>(downsamplingFactor);
                int pointCount = 0;
                int sampleCount = 0;
                for (int sample = 0; sample < sourceSampleCount; sample++) {
                    temp.Add(samples[channel][sample]);
                    if (++sampleCount % downsamplingFactor == 0 || sample + 1 == sourceSampleCount) {
                        peaks[channel].Add(new Peak(temp.Min(), temp.Max()));
                        pointCount++;
                        temp.Clear();
                    }
                }
            }
            return peaks;
        }

        private List<Peak>[] LoadPeaks(Interval interval) {
            int channels = Properties.Channels;
            audioStream.TimePosition = new TimeSpan(interval.From);

            int peakCount = AudioUtil.CalculateSamples(audioStream.Properties, new TimeSpan(interval.Length)) / SAMPLES_PER_PEAK;
            long peakStreamPosition = SamplePosition / SAMPLES_PER_PEAK * 8;

            //Debug.WriteLine("LoadPeaks peakCount: " + peakCount);
            //Debug.WriteLine("LoadPeaks peakStreamPosition: " + peakStreamPosition);

            List<Peak>[] peaks = AudioUtil.CreateList<Peak>(channels, peakCount);

            for (int channel = 0; channel < channels; channel++) {
                peakReaders[channel].BaseStream.Position = peakStreamPosition;
                for (int x = 0; x < peakCount; x++) {
                    peaks[channel].Add(peakReaders[channel].ReadPeak());
                }
            }

            return peaks;
        }

        private static List<Point>[] PeaksToPoints(List<Peak>[] peaks) {
            int channels = peaks.Length;
            int peakCount = peaks[0].Count;

            List<Point>[] peakPoints = AudioUtil.CreateList<Point>(channels, peakCount * 2);
            Point[][] tempPeakPoints = AudioUtil.CreateArray<Point>(channels, peakCount * 2);

            for (int channel = 0; channel < channels; channel++) {
                for (int x = 0; x < peakCount; x++) {
                    tempPeakPoints[channel][x] = new Point(x, peaks[channel][x].Min);
                    tempPeakPoints[channel][peakCount * 2 - 1 - x] = new Point(x, peaks[channel][x].Max);
                }
                peakPoints[channel].AddRange(tempPeakPoints[channel]);
            }

            return peakPoints;
        }

        private static List<Point>[] PeaksToPoints(List<Peak>[] peaks, int downsamplingFactor) {
            int channels = peaks.Length;
            int sourcePeakCount = peaks[0].Count;
            int targetPeakCount = (int)Math.Ceiling((float)sourcePeakCount / downsamplingFactor);

            List<Point>[] peakPoints = AudioUtil.CreateList<Point>(channels, targetPeakCount * 2);
            Point[][] tempPeakPoints = AudioUtil.CreateArray<Point>(channels, targetPeakCount * 2);


            for (int channel = 0; channel < channels; channel++) {
                List<float> min = new List<float>(downsamplingFactor);
                List<float> max = new List<float>(downsamplingFactor);

                int pointCount = 0;
                for (int peak = 0; peak < sourcePeakCount; peak++) {
                    min.Add(peaks[channel][peak].Min);
                    max.Add(peaks[channel][peak].Max);
                    if ((peak + 1) % downsamplingFactor == 0 || peak + 1 == sourcePeakCount) {
                        tempPeakPoints[channel][pointCount] = new Point(pointCount, min.Min());
                        tempPeakPoints[channel][targetPeakCount * 2 - 1 - pointCount] = new Point(pointCount, max.Max());
                        pointCount++;
                        max.Clear();
                        min.Clear();
                    }
                }
                peakPoints[channel].AddRange(tempPeakPoints[channel]);
            }

            return peakPoints;
        }
    }
}
