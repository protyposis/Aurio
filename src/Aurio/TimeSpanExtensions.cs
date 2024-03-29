﻿//
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

namespace Aurio
{
    public static class TimeSpanExtensions
    {
        public static TimeSpan Divide(this TimeSpan ts, double divisor)
        {
            return new TimeSpan((long)(ts.Ticks / divisor));
        }

        public static TimeSpan Multiply(this TimeSpan ts, double multiplicand)
        {
            return new TimeSpan((long)(ts.Ticks * multiplicand));
        }
    }
}
