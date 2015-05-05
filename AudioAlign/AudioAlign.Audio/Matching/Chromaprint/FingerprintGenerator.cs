using AudioAlign.Audio.DataStructures;
using AudioAlign.Audio.Project;
using AudioAlign.Audio.Streams;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioAlign.Audio.Matching.Chromaprint {
    /// <summary>
    /// Chromaprint fingerprint generator as described in:
    /// - https://oxygene.sk/2010/07/introducing-chromaprint/
    /// - https://oxygene.sk/2011/01/how-does-chromaprint-work/
    /// - https://bitbucket.org/acoustid/chromaprint/
    /// </summary>
    public class FingerprintGenerator {

        private static readonly uint[] grayCodeMapping = { 0, 1, 3, 2 };
        private Profile profile;

        public event EventHandler<SubFingerprintEventArgs> SubFingerprintCalculated;
        public event EventHandler Completed;

        public FingerprintGenerator(Profile profile) {
            this.profile = profile;
        }

        public void Generate(AudioTrack track) {
            IAudioStream audioStream = new ResamplingStream(
                new MonoStream(AudioStreamFactory.FromFileInfoIeee32(track.FileInfo)),
                ResamplingQuality.Medium, profile.SamplingRate);

            var chroma = new Chroma(audioStream, profile.WindowSize, profile.HopSize, profile.WindowType,
                profile.ChromaMinFrequency, profile.ChromaMaxFrequency, false);


            float[] chromaFrame;
            var chromaBuffer = new RingBuffer<float[]>(profile.ChromaFilterCoefficients.Length);
            var chromaFilterCoefficients = profile.ChromaFilterCoefficients;
            var filteredChromaFrame = new double[Chroma.Bins];
            var classifiers = profile.Classifiers;
            var maxFilterWidth = classifiers.Max(c => c.Filter.Width);
            var integralImage = new IntegralImage(maxFilterWidth, Chroma.Bins);
            int index = 0;
            int indices = chroma.WindowCount;
            while (chroma.HasNext()) {
                // Get chroma frame buffer
                // When the chroma buffer is full, we can take and reuse the oldest array
                chromaFrame = chromaBuffer.Count == chromaBuffer.Length ? chromaBuffer[0] : new float[Chroma.Bins];

                // Read chroma frame into buffer
                chroma.ReadFrame(chromaFrame);

                // ChromaFilter
                chromaBuffer.Add(chromaFrame);
                if (chromaBuffer.Count < chromaBuffer.Length) {
                    // Wait for the buffer to fill completely for the filtering to start
                    continue;
                }
                Array.Clear(filteredChromaFrame, 0, filteredChromaFrame.Length);
                for (int i = 0; i < chromaFilterCoefficients.Length; i++) {
                    var frame = chromaBuffer[i];
                    for (int j = 0; j < frame.Length; j++) {
                        filteredChromaFrame[j] += frame[j] * chromaFilterCoefficients[i];
                    }
                }

                // ChromaNormalizer
                double euclideanNorm = 0;
                for (int i = 0; i < filteredChromaFrame.Length; i++) {
                    var value = filteredChromaFrame[i];
                    euclideanNorm += value * value;
                }
                euclideanNorm = Math.Sqrt(euclideanNorm);
                if (euclideanNorm < profile.ChromaNormalizationThreshold) {
                    Array.Clear(filteredChromaFrame, 0, filteredChromaFrame.Length);
                }
                else {
                    for (int i = 0; i < filteredChromaFrame.Length; i++) {
                        filteredChromaFrame[i] /= euclideanNorm;
                    }
                }

                // ImageBuilder
                // ... just add one feature vector after another as rows to the image
                integralImage.AddColumn(filteredChromaFrame);

                // FingerprintCalculator
                if (integralImage.Columns < maxFilterWidth) {
                    // Wait for the image to fill completely before subfingerprints can be generated
                    continue;
                }
                // Calculate subfingerprint
                uint subFingerprint = 0;
                for (int i = 0; i < classifiers.Length; i++) {
                    subFingerprint = (subFingerprint << 2) | grayCodeMapping[classifiers[i].Classify(integralImage, 0)];
                }
                // We have a SubFingerprint@frameTime
                if (SubFingerprintCalculated != null) {
                    SubFingerprintCalculated(this, new SubFingerprintEventArgs(track, new SubFingerprint(subFingerprint), index, indices, false));
                }

                index++;
            }

            if (Completed != null) {
                Completed(this, EventArgs.Empty);
            }
        }

        public static TimeSpan SubFingerprintIndexToTimeSpan(Profile profile, int index) {
            return new TimeSpan((long)Math.Round((double)index * profile.HopSize / profile.SamplingRate * TimeUtil.SECS_TO_TICKS));
        }

        public static int TimeStampToSubFingerprintIndex(Profile profile, TimeSpan timeSpan) {
            return (int)Math.Round((double)timeSpan.Ticks / TimeUtil.SECS_TO_TICKS * profile.SamplingRate / profile.HopSize);
        }
    }
}
