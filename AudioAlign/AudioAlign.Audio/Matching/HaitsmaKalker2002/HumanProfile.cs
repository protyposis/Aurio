using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioAlign.Audio.Matching.HaitsmaKalker2002 {
    class HumanProfile : IProfile {

        protected const int FRAME_SIZE = 16384;
        protected const int FRAME_STEP = 512;
        protected const int SAMPLERATE = 44100;

        protected const int FREQ_MIN = 80;
        protected const int FREQ_MAX = 16000;
        protected const int FREQ_BANDS = 33;

        protected double[] frequencyBands;

        public HumanProfile() {
            this.frequencyBands = FFTUtil.CalculateFrequencyBoundariesLog(FREQ_MIN, FREQ_MAX, FREQ_BANDS);
        }

        public virtual string Name { 
            get { return "Human (slow!)"; }
        }

        public int FrameSize {
            get { return FRAME_SIZE; }
        }

        public int FrameStep {
            get { return FRAME_STEP; }
        }
        public int SampleRate {
            get { return SAMPLERATE; }
        }

        public virtual void MapFrequencies(float[] inputBins, float[] outputBins) {
            // sum up the frequency bins
            // TODO check energy computation formula from paper
            // TODO index-mapping can be precomputed -> CHECKED slower than now!?!
            double bandWidth = SAMPLERATE / 2d / inputBins.Length;
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
