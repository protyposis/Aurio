using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using Aurio.Audio;

namespace Aurio.WaveControls {
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

    public class DecibelStringConverter : IValueConverter {

        private const string NEG_INFINITY = "-∞";

        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            double dB = (double)value;

            // double to string
            if (dB == double.NegativeInfinity) {
                return NEG_INFINITY;
            }
            else {
                return dB.ToString("0.0");
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            double dB = double.NaN;

            // string to double
            if (value != null && value is string) {
                if ((string)value == NEG_INFINITY) {
                    dB = double.NegativeInfinity;
                }
                else {
                    dB = double.Parse((string)value);
                }
            }

            return dB;

        }

        #endregion
    }

    public class LinearDecibelStringConverter : IValueConverter {

        private static readonly LinearDecibelConverter linearDecibelConverter = new LinearDecibelConverter();
        private static readonly DecibelStringConverter decibelStringConverter = new DecibelStringConverter();

        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            double dB = (double)linearDecibelConverter.Convert(value, targetType, parameter, culture);
            return decibelStringConverter.Convert(dB, targetType, parameter, culture);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            double dB = (double)decibelStringConverter.ConvertBack(value, targetType, parameter, culture);
            return linearDecibelConverter.ConvertBack(dB, targetType, parameter, culture);

        }

        #endregion
    }
}
