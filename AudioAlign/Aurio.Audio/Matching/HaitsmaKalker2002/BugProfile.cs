using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aurio.Audio.Matching.HaitsmaKalker2002 {
    /// <summary>
    /// Implements the buggy profile that I implemented at first, which works much better for
    /// some live recordings than the default profile, but doesn't work at all in other cases where
    /// the default profile works great.
    /// </summary>
    class BugProfile : DefaultProfile {

        public BugProfile() : base() {
            Name = "Buggy";
        }

        public override void MapFrequencies(float[] inputBins, float[] outputBins) {
            float bandWidth = SampleRate / inputBins.Length;
            for (int x = 0; x < frequencyBands.Length - 1; x++) {
                outputBins[x] = 0;
                int lowerIndex = (int)(frequencyBands[x] / bandWidth);
                int upperIndex = (int)(frequencyBands[x + 1] / bandWidth);
                for (int y = lowerIndex; y <= upperIndex; y++) {
                    outputBins[x] += inputBins[x];
                }
            }
        }
    }
}
