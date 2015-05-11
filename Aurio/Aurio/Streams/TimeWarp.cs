using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aurio.Streams {
    public class TimeWarp {
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
        public TimeSpan Offset {
            get { return To - From; }
        }

        public static double CalculateSampleRateRatio(TimeWarp mL, TimeWarp mH) {
            return (mH.To.Ticks - mL.To.Ticks) / (double)(mH.From.Ticks - mL.From.Ticks);
        }

        public override string ToString() {
            return String.Format("TimeMapping({0} -> {1})", From, To);
        }
    }
}
