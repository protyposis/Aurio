using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioAlign.Audio.Matching.Echoprint {
    public abstract class Profile {
        public interface IThreshold {
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
        public class ExponentialDecayThreshold : IThreshold {
            public double Base { get; set; }
            public double WidthScale { get; set; }
            public double Height { get; set; }

            public double Calculate(double x) {
                return Math.Pow(Base, x / WidthScale) * Height;
            }
        }

        /// <summary>
        /// The name of this profile.
        /// </summary>
        public string Name { get; protected set; }

        /// <summary>
        /// The sampling rate as input to the whitening stage.
        /// </summary>
        public int SamplingRate { get; set; }

        /// <summary>
        /// The resolution in poles of the whitening LPC spectral envelope.
        /// </summary>
        public int WhiteningNumPoles { get; set; }

        /// <summary>
        /// The decay rate in seconds of the whitening envelope.
        /// </summary>
        public float WhiteningDecaySecs { get; set; }

        /// <summary>
        /// The length of a block in samples for which a whitening envelope is calculated and applied.
        /// This is basically the inverse frequency of how often the envelope is refreshed.
        /// </summary>
        public int WhiteningBlockLength { get; set; }

        /// <summary>
        /// The number of subbands the sectrum is divided in the subband analysis stage. This value
        /// is fixed because of the hardcoded filter bank coefficients.
        /// </summary>
        public readonly int SubBands = 8;

        /// <summary>
        /// The hop since in samples for the RMS subsampling in the onset detector.
        /// </summary>
        public int OnsetRmsHopSize { get; set; }

        /// <summary>
        /// The window size in samples for the RMS subsampling in the onset detector.
        /// </summary>
        public int OnsetRmsWindowSize { get; set; }

        /// <summary>
        /// The type of the window to use for the RMS subsampling in the onset detector.
        /// </summary>
        public WindowType OnsetRmsWindowType { get; set; }

        /// <summary>
        /// The minimum distance in RMS samples to keep between two onsets.
        /// </summary>
        public int OnsetMinDistance { get; set; }

        /// <summary>
        /// The target distance in RMS samples between onsets.
        /// </summary>
        public int OnsetTargetDistance { get; set; }

        /// <summary>
        /// The onset detection threshold boost factor to apply after a successful onset detection.
        /// </summary>
        public double OnsetOverfact { get; set; }

        /// <summary>
        /// The factor by which the onset times are quantized during hash generation.
        /// </summary>
        public double HashTimeQuantizationFactor { get; set; }

        /// <summary>
        /// The minimum length in frames to classify a match.
        /// </summary>
        public int MatchingMinFrames { get; set; }

        /// <summary>
        /// The maximum number of frames to scan for a match.
        /// </summary>
        public int MatchingMaxFrames { get; set; }

        /// <summary>
        /// The threshold that a match candidate's rate needs to exceed to be classified as a match.
        /// </summary>
        public IThreshold ThresholdAccept { get; set; }

        /// <summary>
        /// The threshold that rejects a match candidate and stops the matching process if it's matching rate drops below.
        /// </summary>
        public IThreshold ThresholdReject { get; set; }

        /// <summary>
        /// The factor to convert the input sample time scale into the output hash time scale.
        /// </summary>
        public double SampleToHashQuantizationFactor {
            get { return SubBands * OnsetRmsHopSize * HashTimeQuantizationFactor; }
        }
    }
}
