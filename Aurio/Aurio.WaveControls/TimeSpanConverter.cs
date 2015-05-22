// 
// Aurio: Audio Processing, Analysis and Retrieval Library
// Copyright (C) 2010-2015  Mario Guggenberger <mg@protyposis.net>
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Data;

namespace Aurio.WaveControls {
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

            try {
                if (targetType == typeof(long)) {
                    return TimeSpan.ParseExact(input, format, null).Ticks;
                }
                else if (targetType == typeof(TimeSpan)) {
                    return TimeSpan.ParseExact(input, format, null);
                }
            }
            catch (Exception e) {
                return new ValidationResult(false, e.Message);
            }

            return null;
        }

        #endregion
    }
}
