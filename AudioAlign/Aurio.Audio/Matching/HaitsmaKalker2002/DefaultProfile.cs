using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aurio.Audio.Matching.HaitsmaKalker2002 {
    /// <summary>
    /// Implements the default fingerprint frequency mapping profile as described in the paper.
    /// </summary>
    class DefaultProfile : Profile {

        protected double[] frequencyBands;

        public DefaultProfile() {
            Name = "Haitsma&Kalker default";

            FrameSize = 2048;
            FrameStep = 64;
            SampleRate = 5512;

            MinFrequency = 300;
            MaxFrequency = 2000;
            FrequencyBands = 33;

            FlipWeakestBits = 3;

            HashTimeScale = 1d / SampleRate * FrameStep;

            this.frequencyBands = FFTUtil.CalculateFrequencyBoundariesLog(MinFrequency, MaxFrequency, FrequencyBands);
        }

        public override void MapFrequencies(float[] inputBins, float[] outputBins) {
            // sum up the frequency bins
            // TODO check energy computation formula from paper
            // TODO index-mapping can be precomputed -> CHECKED slower than now!?!
            double bandWidth = SampleRate / 2d / inputBins.Length;
            for (int x = 0; x < frequencyBands.Length - 1; x++) {
                outputBins[x] = 0;
                int lowerIndex = (int)(frequencyBands[x] / bandWidth);
                int upperIndex = (int)(frequencyBands[x + 1] / bandWidth);
                for (int y = lowerIndex; y < upperIndex; y++) {
                    outputBins[x] += inputBins[y];
                }
            }
        }
    }
}
