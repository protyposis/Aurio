//
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Aurio.WaveControls
{
    public class LogSlider : Slider
    {
        static LogSlider()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(LogSlider),
                new FrameworkPropertyMetadata(typeof(LogSlider))
            );
            MinimumProperty.OverrideMetadata(
                typeof(LogSlider),
                new FrameworkPropertyMetadata(1.0d)
            );
        }

        private static readonly DependencyProperty LogValueProperty = DependencyProperty.Register(
            "LogValue",
            typeof(double),
            typeof(LogSlider),
            new FrameworkPropertyMetadata(
                1.0d,
                new PropertyChangedCallback(OnLogValueChanged),
                new CoerceValueCallback(CoerceLogValue)
            )
        );

        private static void OnLogValueChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e
        )
        {
            LogSlider ctrl = (LogSlider)d;
            ctrl.OnLogValueChanged((double)e.OldValue, (double)e.NewValue);
        }

        private static object CoerceLogValue(DependencyObject d, object baseValue)
        {
            LogSlider ctrl = (LogSlider)d;
            double value = (double)baseValue;

            double min = ctrl.Minimum;
            if (value < min)
            {
                return min;
            }

            double max = ctrl.Maximum;
            if (value > max)
            {
                return max;
            }

            return value;
        }

        protected virtual void OnLogValueChanged(double oldValue, double newValue)
        {
            //Debug.WriteLine("OnLogValueChanged: " + oldValue + " -> " + newValue);
            Value = CalculateLogInv(newValue, Minimum, Maximum);
            //Debug.WriteLine("LogSlider LogValue/Value: " + LogValue + "/" + Value);
        }

        public double LogValue
        {
            get { return (double)GetValue(LogValueProperty); }
            set { SetValue(LogValueProperty, value); }
        }

        protected override void OnValueChanged(double oldValue, double newValue)
        {
            //Debug.WriteLine("OnValueChanged: " + oldValue + " -> " + newValue);
            base.OnValueChanged(oldValue, newValue);
            LogValue = CalculateLog(newValue, Minimum, Maximum);
            //Debug.WriteLine("LogSlider Value/LogValue: " + Value + "/" + LogValue);
        }

        /// <summary>
        /// Idea taken from: http://stackoverflow.com/questions/846221/logarithmic-slider
        /// </summary>
        private double CalculateLog(double value, double min, double max)
        {
            if (min == max)
            {
                return min;
            }

            var minv = Math.Log(min);
            var maxv = Math.Log(max);

            // calculate adjustment factor
            var scale = (maxv - minv) / (max - min);

            return Math.Exp(minv + scale * (value - min));
        }

        private double CalculateLogInv(double value, double min, double max)
        {
            if (min == max)
            {
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
