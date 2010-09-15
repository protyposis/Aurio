using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace AudioAlign.WaveControls {
    public class LogTransformConverter: IValueConverter {
        #region IValueConverter Members

        /// <summary>
        /// Converts a linear scale to a logarithmic scale.
        /// 
        /// Parameter example:
        /// "0 100 0.01 50"
        /// Maps the linear range from 0 to 100 to the logarithmic range from 0.01 to 50.
        /// </summary>
        /// <param name="value">the value to convert</param>
        /// <param name="targetType"></param>
        /// <param name="parameter">the scale mapping in the form of a string containing 4 double values divided by spaces</param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            string[] strings = ((string)parameter).Split(new string[] {" "}, StringSplitOptions.RemoveEmptyEntries);
            double[] numbers = new double[4];
            for (int x = 0; x < numbers.Length; x++) {
                numbers[x] = double.Parse(strings[x], culture);
            }
            return logslider((double)value, numbers[0], numbers[1], numbers[2], numbers[3]);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            throw new NotImplementedException();
        }

        #endregion

        /// <summary>
        /// Idea taken from: http://stackoverflow.com/questions/846221/logarithmic-slider
        /// </summary>
        private double logslider(double value, double fromL, double fromH, double toL, double toH) {
            // value will be between fromL and fromH
            var min = fromL;
            var max = fromH;

            // The result should be between toL an toH
            var minv = Math.Log(toL);
            var maxv = Math.Log(toH);

            // calculate adjustment factor
            var scale = (maxv-minv) / (max-min);

            return Math.Exp(minv + scale*(value-min));
        }
    }
}
