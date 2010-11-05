using System.IO;
using System;
using System.Linq;
using System.Diagnostics;
using System.Windows;
using System.Collections.Generic;
namespace AudioAlign.Audio {
    public class VisualizingAudioStream16: AudioStreamWrapper<float>, IAudioStream16 {

        private const int SAMPLES_PER_PEAK = 1024;
        private const int BUFFER_SIZE = 1024;

        private MemoryStream[] peakStreams;
        private float[][] buffer;

        public VisualizingAudioStream16(IAudioStream16 audioStream)
            : base(audioStream) {
            // init memory streams
            peakStreams = new MemoryStream[audioStream.Properties.Channels];
            for (int channel = 0; channel < audioStream.Properties.Channels; channel++) {
                peakStreams[channel] = new MemoryStream((int)(
                    audioStream.SampleCount / SAMPLES_PER_PEAK
                    * (audioStream.Properties.BitDepth / 8) * 2)); // * 2 for min and max value per peak
            }
            //init buffer
            buffer = AudioUtil.CreateArray<float>(audioStream.Properties.Channels, BUFFER_SIZE);

            GeneratePeaks();
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
            int samplesInRequestedInterval = AudioUtil.CalculateSamples(Properties, new TimeSpan(requestedInterval.Length));
            if (samplesInRequestedInterval < targetSamples) {
                throw new ArgumentException("the requested interval contains less samples than requested: " +
                    samplesInRequestedInterval + " < " + targetSamples);
            }

            double resampleFactor = samplesInRequestedInterval / targetSamples;
            Debug.WriteLine("VisualizingAudioStream16 resampleFactor: " + resampleFactor + " (" + samplesInRequestedInterval + "/" + targetSamples + ")");

            /*
             * STAGES:
             * - just return unresampled samples from file data CHECK
             * - return on the fly resampled samples from file data NO - DECEPTIVE VISUALS RESULTING
             * - return on the fly generated peaks from file data CHECK
             * - return precomputed peaks from peak data CHECK
             */

            if (resampleFactor < 2) {
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
                List<Point>[] samplesDownsampled = SamplesToPeaks(samples, downsamplingFactor);
                return samplesDownsampled;
            }
            else {
                peaks = true;
                readInterval = requestedInterval;
                return LoadPeaks(requestedInterval);
            }
            // TODO optionally introduce additional stage to return resampled peaks from peak data
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

        private static List<Point>[] SamplesToPeaks(List<float>[] samples, int downsamplingFactor) {
            int channels = samples.Length;
            int targetPoints = (int)Math.Ceiling((float)samples[0].Count / downsamplingFactor);
            List<Point>[] peakPoints = AudioUtil.CreateList<Point>(channels, targetPoints * 2);
            Point[][] tempPeakPoints = AudioUtil.CreateArray<Point>(channels, targetPoints * 2);

            for (int channel = 0; channel < channels; channel++) {
                List<float> temp = new List<float>(downsamplingFactor);
                int pointCount = 0;
                int sampleCount = 0;
                for (int sample = 0; sample < samples[0].Count; sample++) {
                    temp.Add(samples[channel][sample]);
                    if (++sampleCount % downsamplingFactor == 0 || sample + 1 == samples[0].Count) {
                        tempPeakPoints[channel][pointCount] = new Point(pointCount, temp.Min());
                        tempPeakPoints[channel][targetPoints * 2 - 1 - pointCount] = new Point(pointCount, temp.Max());
                        pointCount++;
                        temp.Clear();
                    }
                }
                peakPoints[channel].AddRange(tempPeakPoints[channel]);
            }
            return peakPoints;
        }

        private List<Point>[] LoadPeaks(Interval interval) {
            int channels = Properties.Channels;
            audioStream.TimePosition = new TimeSpan(interval.From);

            int peaks = AudioUtil.CalculateSamples(audioStream.Properties, new TimeSpan(interval.Length)) / SAMPLES_PER_PEAK;
            long peakStreamPosition = SamplePosition / SAMPLES_PER_PEAK * 2;
            Debug.WriteLine("LoadPeaks peakStreamPosition: " + peakStreamPosition);

            List<Point>[] peakPoints = AudioUtil.CreateList<Point>(channels, peaks * 2);
            Point[][] tempPeakPoints = AudioUtil.CreateArray<Point>(channels, peaks * 2);

            for (int channel = 0; channel < channels; channel++) {
                peakStreams[channel].Position = peakStreamPosition * 2;
                BinaryReader peakReader = new BinaryReader(peakStreams[channel]);
                for (int x = 0; x < peaks; x++) {
                    tempPeakPoints[channel][x] = new Point(x, peakReader.ReadSingle());
                    tempPeakPoints[channel][peaks * 2 - 1 - x] = new Point(x, peakReader.ReadSingle());
                }
                peakPoints[channel].AddRange(tempPeakPoints[channel]);
            }

            return peakPoints;
        }

        private void GeneratePeaks() {
            int bufferSize = 512;
            int channels = Properties.Channels;
            float[][] buffer = AudioUtil.CreateArray<float>(channels, bufferSize);
            int samplesRead;

            BinaryWriter[] peakWriters = new BinaryWriter[channels];
            for (int channel = 0; channel < channels; channel++) {
                peakWriters[channel] = new BinaryWriter(peakStreams[channel]);
            }

            List<float>[] minMax = AudioUtil.CreateList<float>(channels, SAMPLES_PER_PEAK);
            int sampleCount = 0;
            while ((samplesRead = audioStream.Read(buffer, bufferSize)) > 0) {
                for (int x = 0; x < samplesRead; x++) {
                    for (int channel = 0; channel < channels; channel++) {
                        minMax[channel].Add(buffer[channel][x]);
                    }
                    if (++sampleCount == SAMPLES_PER_PEAK) {
                        // write peak
                        for (int channel = 0; channel < channels; channel++) {
                            peakWriters[channel].Write(minMax[channel].Min());
                            peakWriters[channel].Write(minMax[channel].Max());
                            minMax[channel].Clear();
                        }
                        sampleCount = 0;
                    }
                }

                Debug.WriteLine((100.0f / SampleCount * SamplePosition) + "% of peaks generated...");
            }
            Debug.WriteLine("peak generation finished - " + (peakStreams[0].Length * channels) + " bytes");

            // TODO dispose / close / ??? the binary writers?
        }
    }
}
