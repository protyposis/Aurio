using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using System.Diagnostics;

namespace AudioAlign.WaveControls {
    public class LogSlider: Slider {

        static LogSlider() {
            MinimumProperty.OverrideMetadata(typeof(LogSlider), new FrameworkPropertyMetadata(1.0d));
        }

        private static readonly DependencyProperty LogValueProperty =
                DependencyProperty.Register(
                        "LogValue",
                        typeof(double),
                        typeof(LogSlider),
                        new FrameworkPropertyMetadata(
                            1.0d, 
                            new PropertyChangedCallback(OnLogValueChanged),
                            new CoerceValueCallback(CoerceLogValue)));

        private static void OnLogValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            LogSlider ctrl = (LogSlider)d;
            ctrl.OnLogValueChanged((double)e.OldValue, (double)e.NewValue);
        }

        private static object CoerceLogValue(DependencyObject d, object baseValue) {
            LogSlider ctrl = (LogSlider)d;
            double value = (double)baseValue;

            double min = ctrl.Minimum;
            if (value < min) {
                return min;
            }

            double max = ctrl.Maximum;
            if (value > max) {
                return max;
            }

            return value;
        }

        protected virtual void OnLogValueChanged(double oldValue, double newValue) {
            //Debug.WriteLine("OnLogValueChanged: " + oldValue + " -> " + newValue);
            Value = CalculateLogInv(newValue, Minimum, Maximum);
            //Debug.WriteLine("LogSlider LogValue/Value: " + LogValue + "/" + Value);
        }

        public double LogValue {
            get { return (double)GetValue(LogValueProperty); }
            set { SetValue(LogValueProperty, value); }
        }

        protected override void OnValueChanged(double oldValue, double newValue) {
            //Debug.WriteLine("OnValueChanged: " + oldValue + " -> " + newValue);
            base.OnValueChanged(oldValue, newValue);
            LogValue = CalculateLog(newValue, Minimum, Maximum);
            //Debug.WriteLine("LogSlider Value/LogValue: " + Value + "/" + LogValue);
        }

        /// <summary>
        /// Idea taken from: http://stackoverflow.com/questions/846221/logarithmic-slider
        /// </summary>
        private double CalculateLog(double value, double min, double max) {
            if (min == max) {
                return min;
            }

            var minv = Math.Log(min);
            var maxv = Math.Log(max);

            // calculate adjustment factor
            var scale = (maxv - minv) / (max - min);

            return Math.Exp(minv + scale * (value - min));
        }

        private double CalculateLogInv(double value, double min, double max) {
            if (min == max) {
                return min;
            }

            var minv = Math.Log(min);
            var maxv = Math.Log(max);

            // calculate adjustment factor
            var scale = (maxv - minv) / (max - min);

            return ((Math.Log(value) - minv) / scale) + min;
        }
    }
}
