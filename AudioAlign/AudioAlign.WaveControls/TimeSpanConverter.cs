using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace AudioAlign.WaveControls {
    /// <summary>
    /// Converts a TimeSpan struct to its string representative by formatting it with the format specified by the parameter.
    /// </summary>
    public class TimeSpanConverter : IValueConverter {
        #region IValueConverter Members

        public static readonly string DEFAULT_FORMAT = "d\\.hh\\:mm\\:ss\\.fffffff";

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            string format = parameter as string ?? DEFAULT_FORMAT;
            TimeSpan timeSpan = new TimeSpan();

            if ((value as TimeSpan?) != null) {
                timeSpan = (TimeSpan)value;
            }
            else if ((value as long?) != null) {
                long ticks = (long)value;
                timeSpan = new TimeSpan(ticks);
            }

            return format != null ? timeSpan.ToString(format) : timeSpan.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            string input = (string)value;
            string format = parameter as string ?? DEFAULT_FORMAT;

            if (targetType == typeof(long)) {
                return TimeSpan.ParseExact(input, format, null).Ticks;
            }
            else if (targetType == typeof(TimeSpan)) {
                return TimeSpan.ParseExact(input, format, null);
            }

            return null;
        }

        #endregion
    }
}
