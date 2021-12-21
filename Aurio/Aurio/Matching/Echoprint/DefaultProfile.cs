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

namespace Aurio.Matching.Echoprint
{
    class DefaultProfile : Profile
    {
        public DefaultProfile()
        {
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

            var threshold = new ExponentialDecayThreshold
            {
                Base = 0.5,
                WidthScale = 2,
                Height = 0.12
            };
            ThresholdAccept = threshold;
            ThresholdReject = new ExponentialDecayThreshold
            {
                Base = threshold.Base,
                WidthScale = threshold.WidthScale,
                Height = threshold.Height / 3
            };

            HashTimeScale = 1d / SamplingRate * SampleToHashQuantizationFactor;
        }
    }
}
