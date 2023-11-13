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

using System.Diagnostics;

namespace Aurio.Matching.Wang2003
{
    [DebuggerDisplay("{Index}:{Peak1.Index} --({Distance})--> {Peak2.Index}")]
    public struct PeakPair
    {
        public int Index { get; set; }
        public Peak Peak1 { get; set; }
        public Peak Peak2 { get; set; }
        public int Distance { get; set; }
        public float AverageEnergy
        {
            get { return (Peak1.Value + Peak2.Value) / 2; }
        }

        public static uint PeakPairToHash(PeakPair pp)
        {
            // Put frequency bins and the distance each in one byte. The actual quantization
            // is configured through the parameters, e.g. the FFT window size determines the
            // number of frequency bins, and the size of the target zone determines the max
            // distance. Their max size can be anywhere in the range of a byte. if it should be
            // higher, a quantization step must be introduced (which will basically be a division).
            return (uint)(
                (byte)pp.Peak1.Index << 16 | (byte)pp.Peak2.Index << 8 | (byte)pp.Distance
            );
        }

        public static PeakPair HashToPeakPair(uint hash, int index)
        {
            // The inverse operation of the function above.
            return new PeakPair
            {
                Index = index,
                Peak1 = new Peak((int)(hash >> 16 & 0xFF), 0),
                Peak2 = new Peak((int)(hash >> 8 & 0xFF), 0),
                Distance = (int)(hash & 0xFF)
            };
        }
    }
}
