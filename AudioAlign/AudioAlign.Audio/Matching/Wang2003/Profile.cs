using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioAlign.Audio.Matching.Wang2003 {
    /// <summary>
    ///  The basic configuration profile for the Wang fingerprinting algorithm.
    ///  
    ///  This profile contains settings for configuring fingerprint generation
    ///  and lookup.
    /// </summary>
    public class Profile {

        public Profile() {
            SamplingRate = 11025;
            WindowSize = 512;
            HopSize = 256;
            SpectrumTemporalSmoothingCoefficient = 0.05f;
            SpectrumSmoothingLength = 0;
            PeaksPerFrame = 3;
            PeakFanout = 5;
            TargetZoneDistance = 2;
            TargetZoneLength = 30;
            TargetZoneWidth = 63;
        }

        /// <summary>
        /// The sampling rate at which the STFT is taken to generate the hashes.
        /// </summary>
        public int SamplingRate { get; set; }

        /// <summary>
        /// The STFT window size in samples, which is the double of the spectral frame resolution.
        /// </summary>
        public int WindowSize { get; set; }

        /// <summary>
        /// The distance in samples from one STFT window to the next. Can be overlapping.
        /// </summary>
        public int HopSize { get; set; }

        /// <summary>
        /// The alpha coefficient for the exponential moving average of the spectrum bins over time.
        /// Subtracting this average from a spectral frame results in the residual spectrum from
        /// which the peaks are extracted.
        /// </summary>
        public float SpectrumTemporalSmoothingCoefficient { get; set; }

        /// <summary>
        /// The length in bins of the simple moving average filter to smooth a spectral frame.
        /// Can be used to low pass filter the spectrum and get rid of small peaks, but also 
        /// shifts the peaks a bit. Set to zero to disable.
        /// </summary>
        public int SpectrumSmoothingLength { get; set; }

        /// <summary>
        /// The maximum number of peaks to extract from a spectral frame.
        /// </summary>
        public int PeaksPerFrame { get; set; }

        /// <summary>
        /// The maximum number of peak pairs to generate for a peak. The peak pairs are what ultimately
        /// results in the fingerprint hashes.
        /// </summary>
        public int PeakFanout { get; set; }

        /// <summary>
        /// The temporal distance in frames from a peak to its target zone whose peaks are taken to build peak pairs.
        /// </summary>
        public int TargetZoneDistance { get; set; }

        /// <summary>
        /// The temporal length in frames of the target peak zone.
        /// </summary>
        public int TargetZoneLength { get; set; }

        /// <summary>
        /// The spectral width in peaks of the target peak zone.
        /// </summary>
        public int TargetZoneWidth { get; set; }
    }
}
