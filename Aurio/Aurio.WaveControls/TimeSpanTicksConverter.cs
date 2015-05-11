using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace Aurio.WaveControls {
    /// <summary>
    /// Converts between TimeSpan structs and long integers (represented by TimeSpan.Ticks).
    /// </summary>
    public class TimeSpanTicksConverter : IValueConverter {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            if (value is TimeSpan) {
                return ((TimeSpan)value).Ticks;
            }
            else if (value is long) {
                return new TimeSpan((long)value);
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            return Convert(value, targetType, parameter, culture);
        }

        #endregion
    }
}
