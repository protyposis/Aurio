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

namespace Aurio
{
    public static class TimeUtil
    {
        public static readonly long SECS_TO_TICKS;
        public static readonly long MILLISECS_TO_TICKS;

        static TimeUtil()
        {
            // 1 sec * 1000 = millisecs | * 1000 = microsecs | * 10 = ticks (100-nanosecond units)
            // http://msdn.microsoft.com/en-us/library/zz841zbz.aspx
            SECS_TO_TICKS = 1 * 1000 * 1000 * 10;
            MILLISECS_TO_TICKS = SECS_TO_TICKS / 1000;
        }

        public static long TimeSpanToBytes(
            TimeSpan timeSpan,
            Streams.AudioProperties audioProperties
        )
        {
            long bytes = (long)(
                timeSpan.TotalSeconds
                * audioProperties.SampleRate
                * audioProperties.SampleBlockByteSize
            );
            return bytes - (bytes % audioProperties.SampleBlockByteSize);
        }

        public static TimeSpan BytesToTimeSpan(long bytes, Streams.AudioProperties audioProperties)
        {
            return new TimeSpan(
                (long)(
                    (double)bytes
                    / audioProperties.SampleBlockByteSize
                    / audioProperties.SampleRate
                    * SECS_TO_TICKS
                )
            );
        }
    }
}
