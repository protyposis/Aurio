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
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Aurio.WaveControls
{
    public class ExtendedSlider : FormattedAutoTooltipSlider
    {
        private static readonly DependencyProperty DefaultValueProperty =
            DependencyProperty.Register(
                "DefaultValue",
                typeof(double),
                typeof(ExtendedSlider),
                new FrameworkPropertyMetadata(
                    0.0d,
                    new PropertyChangedCallback(OnDefaultValueChanged),
                    new CoerceValueCallback(CoerceDefaultValue)
                )
            );

        private static void OnDefaultValueChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e
        )
        {
            ExtendedSlider ctrl = (ExtendedSlider)d;
            ctrl.OnDefaultValueChanged((double)e.OldValue, (double)e.NewValue);
        }

        private static object CoerceDefaultValue(DependencyObject d, object baseValue)
        {
            ExtendedSlider ctrl = (ExtendedSlider)d;
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

        public ExtendedSlider()
            : base()
        {
            this.Loaded += new RoutedEventHandler(ExtendedSlider_Loaded);
        }

        private void ExtendedSlider_Loaded(object sender, RoutedEventArgs e)
        {
            Thumb thumb = GetThumb(this);
            if (thumb != null)
            {
                thumb.MouseDoubleClick += new System.Windows.Input.MouseButtonEventHandler(
                    thumb_MouseDoubleClick
                );
            }
        }

        void thumb_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Value = DefaultValue;
        }

        protected virtual void OnDefaultValueChanged(double oldValue, double newValue) { }

        [Bindable(true), Category("Common")]
        public double DefaultValue
        {
            get { return (double)GetValue(DefaultValueProperty); }
            set { SetValue(DefaultValueProperty, value); }
        }

        /// <summary>
        /// Gets the Thumb of a Slider.
        /// Source: http://stackoverflow.com/questions/3233000/get-the-thumb-of-a-slider
        /// </summary>
        /// <param name="slider"></param>
        /// <returns></returns>
        private static Thumb GetThumb(Slider slider)
        {
            var track = slider.Template.FindName("PART_Track", slider) as Track;
            return track == null ? null : track.Thumb;
        }
    }
}
