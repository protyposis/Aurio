using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aurio.Matching.Wang2003 {
    class DefaultProfile : Profile {
        public DefaultProfile() {
            Name = "Wang03 guessed default";

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

            double framesPerSecond = (double)SamplingRate / HopSize;
            MatchingMinFrames = 10;
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

            HashTimeScale = 1d / SamplingRate * HopSize;
        }
    }
}
