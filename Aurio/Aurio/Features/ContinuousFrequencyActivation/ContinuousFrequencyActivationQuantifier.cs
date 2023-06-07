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
using Aurio.Streams;

namespace Aurio.Features.ContinuousFrequencyActivation
{
    class ContinuousFrequencyActivationQuantifier : StrongPeakDetector
    {
        private float[] strongPeakValues; // buffer

        public ContinuousFrequencyActivationQuantifier(IAudioStream stream)
            : base(stream)
        {
            strongPeakValues = new float[WindowSize / 2];
        }

        public override void ReadFrame(float[] cfa)
        {
            base.ReadFrame(strongPeakValues);

            int peakCount = (int)strongPeakValues[strongPeakValues.Length - 1];
            Array.Sort<float>(strongPeakValues, 0, peakCount); // sort...
            Array.Reverse(strongPeakValues, 0, peakCount); // ...descending

            // sum largest peak values to characterize overall "peakiness"
            cfa[0] = 0;
            for (int i = 0; i < 5; i++)
            {
                cfa[0] += strongPeakValues[i];
            }
        }
    }
}
