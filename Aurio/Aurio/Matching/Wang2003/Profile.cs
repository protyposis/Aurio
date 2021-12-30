// 
// Aurio: Audio Processing, Analysis and Retrieval Library
// Copyright (C) 2010-2017  Mario Guggenberger <mg@protyposis.net>
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as
// published by the Free Software Foundation, either version 3 of the
// License, or (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aurio.Matching.Wang2003
{
    /// <summary>
    ///  The basic configuration profile for the Wang fingerprinting algorithm.
    ///  
    ///  This profile contains settings for configuring fingerprint generation
    ///  and lookup.
    /// </summary>
    public abstract class Profile : IProfile
    {

        public interface IThreshold
        {
            /// <summary>
            /// Returns, for a given x (time), a threshold value in the range of [0,1].
            /// </summary>
            /// <param name="x">the time instant to calculate the threshold for</param>
            /// <returns>the threshold for the given time instant</returns>
            double Calculate(double x);
        }

        /// <summary>
        /// An exponentially decaying threshold in the form of y=b^x.
        /// </summary>
        public class ExponentialDecayThreshold : IThreshold
        {
            public double Base { get; set; }
            public double WidthScale { get; set; }
            public double Height { get; set; }

            public double Calculate(double x)
            {
                return Math.Pow(Base, x / WidthScale) * Height;
            }
        }

        /// <summary>
        /// The name of this profile.
        /// </summary>
        public string Name { get; protected set; }

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
        /// The spectral width in frequency bins (peak indices) of the target peak zone.
        /// </summary>
        public int TargetZoneWidth { get; set; }

        /// <summary>
        /// The minimum length in frames to classify a match.
        /// </summary>
        public int MatchingMinFrames { get; set; }

        /// <summary>
        /// The maximum number of frames to scan for a match.
        /// </summary>
        public int MatchingMaxFrames { get; set; }

        /// <summary>
        /// The threshold that a match candidates rate needs to exceed to be classified as a match.
        /// </summary>
        public IThreshold ThresholdAccept { get; set; }

        /// <summary>
        /// The threshold that rejects a match candidate and stops the matching process if its matching rate drops below.
        /// </summary>
        public IThreshold ThresholdReject { get; set; }

        public double HashTimeScale => 1d / SamplingRate * HopSize;
    }
}
