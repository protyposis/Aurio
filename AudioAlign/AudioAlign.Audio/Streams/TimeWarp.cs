using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioAlign.Audio.Streams {
    public class TimeWarp {
        /// <summary>
        /// The byte position in the unwarped source stream.
        /// </summary>
        public long From { get; set; }

        /// <summary>
        /// The warped byte position in the target stream.
        /// </summary>
        public long To { get; set; }

        /// <summary>
        /// The position difference between the source and target stream.
        /// </summary>
        public long Offset {
            get { return To - From; }
        }

        public static double CalculateSampleRateRatio(TimeWarp mL, TimeWarp mH) {
            return (mH.To - mL.To) / (double)(mH.From - mL.From);
        }

        public override string ToString() {
            return String.Format("TimeMapping({0} -> {1})", From, To);
        }
    }
}
