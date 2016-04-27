using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aurio {
    public static class TimeSpanExtensions {

        public static TimeSpan Divide(this TimeSpan ts, double divisor) {
            return new TimeSpan((long)(ts.Ticks / divisor));
        }

        public static TimeSpan Multiply(this TimeSpan ts, double multiplicand) {
            return new TimeSpan((long)(ts.Ticks * multiplicand));
        }
    }
}
