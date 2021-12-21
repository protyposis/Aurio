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
    class DefaultProfile : Profile
    {
        public DefaultProfile()
        {
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

            var threshold = new ExponentialDecayThreshold
            {
                Base = 0.5,
                WidthScale = 2,
                Height = 0.3
            };
            ThresholdAccept = threshold;
            ThresholdReject = new ExponentialDecayThreshold
            {
                Base = threshold.Base,
                WidthScale = threshold.WidthScale,
                Height = threshold.Height / 6
            };

            HashTimeScale = 1d / SamplingRate * HopSize;
        }
    }
}
