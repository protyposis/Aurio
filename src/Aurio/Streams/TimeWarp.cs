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

namespace Aurio.Streams
{
    public class TimeWarp
    {
        /// <summary>
        /// The byte position in the unwarped source stream.
        /// </summary>
        public TimeSpan From { get; set; }

        /// <summary>
        /// The warped byte position in the target stream.
        /// </summary>
        public TimeSpan To { get; set; }

        /// <summary>
        /// The position difference between the source and target stream.
        /// </summary>
        public TimeSpan Offset
        {
            get { return To - From; }
        }

        public static double CalculateSampleRateRatio(TimeWarp mL, TimeWarp mH)
        {
            return (mH.To.Ticks - mL.To.Ticks) / (double)(mH.From.Ticks - mL.From.Ticks);
        }

        public override string ToString()
        {
            return String.Format("TimeMapping({0} -> {1})", From, To);
        }
    }
}
