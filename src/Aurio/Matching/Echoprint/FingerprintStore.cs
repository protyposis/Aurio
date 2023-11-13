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

using Aurio.Project;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Aurio.Matching.Echoprint
{
    public class FingerprintStore : Wang2003.FingerprintStore
    {
        public FingerprintStore(Profile profile)
        {
            // Precompute the threshold function
            var thresholdAccept = new double[profile.MatchingMaxFrames];
            var thresholdReject = new double[profile.MatchingMaxFrames];
            for (int i = 0; i < thresholdAccept.Length; i++)
            {
                thresholdAccept[i] = profile.ThresholdAccept.Calculate(i * profile.HashTimeScale);
                thresholdReject[i] = profile.ThresholdReject.Calculate(i * profile.HashTimeScale);
            }

            Initialize(
                profile,
                profile.MatchingMinFrames,
                profile.MatchingMaxFrames,
                thresholdAccept,
                thresholdReject,
                "FP-EP"
            );
        }
    }
}
