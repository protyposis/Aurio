using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioAlign.Audio.Matching.Echoprint {
    public class Profile {
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

        public Profile() {
            SamplingRate = 11025;

            double framesPerSecond = (double)SamplingRate / 8 / 4 / 8;
            MatchingMinFrames = (int)(framesPerSecond * 3);
            MatchingMaxFrames = (int)(framesPerSecond * 30);

            var threshold = new ExponentialDecayThreshold {
                Base = 0.5,
                WidthScale = 2,
                Height = 0.3
            };
            ThresholdAccept = threshold;
            ThresholdReject = new ExponentialDecayThreshold {
                Base = threshold.Base,
                WidthScale = threshold.WidthScale,
                Height = threshold.Height / 6
            };
        }

        public int SamplingRate { get; set; }

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
    }
}
