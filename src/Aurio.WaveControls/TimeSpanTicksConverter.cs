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
using System.Windows.Data;

namespace Aurio.WaveControls
{
    /// <summary>
    /// Converts between TimeSpan structs and long integers (represented by TimeSpan.Ticks).
    /// </summary>
    public class TimeSpanTicksConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(
            object value,
            Type targetType,
            object parameter,
            System.Globalization.CultureInfo culture
        )
        {
            if (value is TimeSpan)
            {
                long returnValue = ((TimeSpan)value).Ticks;

                if (targetType == typeof(double))
                    return (double)returnValue;

                return returnValue;
            }
            else if (value is long || value is double)
            {
                return new TimeSpan(System.Convert.ToInt64(value));
            }

            return null;
        }

        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            System.Globalization.CultureInfo culture
        )
        {
            return Convert(value, targetType, parameter, culture);
        }

        #endregion
    }
}
