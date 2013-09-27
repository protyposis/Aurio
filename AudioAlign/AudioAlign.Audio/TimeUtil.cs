using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioAlign.Audio {
    public static class TimeUtil {

        public static readonly long SECS_TO_TICKS;
        public static readonly long MILLISECS_TO_TICKS;

        static TimeUtil() {
            // 1 sec * 1000 = millisecs | * 1000 = microsecs | * 10 = ticks (100-nanosecond units)
            // http://msdn.microsoft.com/en-us/library/zz841zbz.aspx
            SECS_TO_TICKS = 1 * 1000 * 1000 * 10;
            MILLISECS_TO_TICKS = SECS_TO_TICKS/1000;
        }

        public static long TimeSpanToBytes(TimeSpan timeSpan, Streams.AudioProperties audioProperties) {
            long bytes = (long)(timeSpan.TotalSeconds * audioProperties.SampleRate * audioProperties.SampleBlockByteSize);
            return bytes - (bytes % audioProperties.SampleBlockByteSize);
        }

        public static TimeSpan BytesToTimeSpan(long bytes, Streams.AudioProperties audioProperties) {
            return new TimeSpan((long)((double)bytes / audioProperties.SampleBlockByteSize / audioProperties.SampleRate * SECS_TO_TICKS));
        }
    }
}
