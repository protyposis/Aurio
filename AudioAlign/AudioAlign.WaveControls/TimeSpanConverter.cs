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

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            string format = parameter as string;
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
            return null;
        }

        #endregion
    }
}
