using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioAlign.Audio.Matching.HaitsmaKalker2002 {
    /// <summary>
    /// Profile interface for the fingerprint generator.
    /// </summary>
    public interface IProfile {
        string Name { get; }
        int FrameSize { get; }
        int FrameStep { get; }
        int SampleRate { get; }

        /// <summary>
        /// Maps the frequency bins from the FFT result to the target frequency bins that the fingerprint
        /// will be generated from.
        /// </summary>
        /// <param name="inputBins">FFT result bins</param>
        /// <param name="outputBins">target bins</param>
        void MapFrequencies(float[] inputBins, float[] outputBins);
    }
}
