using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using AudioAlign.Audio;

namespace AudioAlign.WaveControls {
    public class LinearDecibelConverter : IValueConverter {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            if (value == null || !(value is double))
                return double.NaN;
            
            // linear to decibel
            return VolumeUtil.LinearToDecibel((double)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            if (value == null || !(value is double))
                return double.NaN;

            // decibel to linear
            return VolumeUtil.DecibelToLinear((double)value);
        }

        #endregion
    }
}
