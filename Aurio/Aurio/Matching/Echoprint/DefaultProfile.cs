using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aurio.Matching.Echoprint {
    class DefaultProfile : Profile {
        public DefaultProfile() {
            Name = "Echoprint default";

            SamplingRate = 11025;

            WhiteningNumPoles = 40;
            WhiteningDecaySecs = 8;
            WhiteningBlockLength = 10000; // almost 1 sec

            OnsetRmsHopSize = 4;
            OnsetRmsWindowSize = 8;
            OnsetRmsWindowType = WindowType.Hann;
            OnsetMinDistance = 128;
            OnsetTargetDistance = 345; // 345 ~= 1 sec (11025 / 8 [SubbandAnalyzer] / 4 [RMS / adaptiveOnsets()] ~= 345 frames per second)
            OnsetOverfact = 1.1; // paper says 1.05 but Echoprint sources say 1.1

            HashTimeQuantizationFactor = 8;

            double framesPerSecond = (double)SamplingRate / SampleToHashQuantizationFactor;
            MatchingMinFrames = (int)(framesPerSecond * 3);
            MatchingMaxFrames = (int)(framesPerSecond * 60);

            var threshold = new ExponentialDecayThreshold {
                Base = 0.5,
                WidthScale = 2,
                Height = 0.12
            };
            ThresholdAccept = threshold;
            ThresholdReject = new ExponentialDecayThreshold {
                Base = threshold.Base,
                WidthScale = threshold.WidthScale,
                Height = threshold.Height / 3
            };

            HashTimeScale = 1d / SamplingRate * SampleToHashQuantizationFactor;
        }
    }
}
