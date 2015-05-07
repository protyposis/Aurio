using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioAlign.Audio.Matching.HaitsmaKalker2002 {
    /// <summary>
    /// Profile interface for the fingerprint generator.
    /// </summary>
    public abstract class Profile {

        public string Name { get; protected set; }

        public int FrameSize { get; set; }

        public int FrameStep { get; set; }

        public int SampleRate { get; set; }

        public int MinFrequency { get; set; }

        public int MaxFrequency { get; set; }

        public int FrequencyBands { get; set; }

        public int FlipWeakestBits { get; set; }

        /// <summary>
        /// Maps the frequency bins from the FFT result to the target frequency bins that the fingerprint
        /// will be generated from.
        /// </summary>
        /// <param name="inputBins">FFT result bins</param>
        /// <param name="outputBins">target bins</param>
        public abstract void MapFrequencies(float[] inputBins, float[] outputBins);
    }
}
