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
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Aurio.WaveControls
{
    [TemplatePart(Name = "PART_Indicator", Type = typeof(FrameworkElement))]
    [TemplatePart(Name = "PART_IndicatorContainer", Type = typeof(FrameworkElement))]
    public class CorrelationMeter : Control
    {
        public static DependencyProperty ValueProperty;

        static CorrelationMeter()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(CorrelationMeter),
                new FrameworkPropertyMetadata(typeof(CorrelationMeter))
            );

            ValueProperty = DependencyProperty.Register(
                "Value",
                typeof(double),
                typeof(CorrelationMeter),
                new FrameworkPropertyMetadata(0.0d, new PropertyChangedCallback(OnValueChanged))
                {
                    AffectsRender = true
                }
            );
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            CorrelationMeter correlationMeter = d as CorrelationMeter;
            correlationMeter.UpdateValueIndicator();
        }

        private FrameworkElement valueIndicator;
        private FrameworkElement valueIndicatorContainer;

        public CorrelationMeter()
        {
            this.Loaded += new RoutedEventHandler(CorrelationMeter_Loaded);
        }

        private void CorrelationMeter_Loaded(object sender, RoutedEventArgs e)
        {
            valueIndicator = GetTemplateChild("PART_Indicator") as FrameworkElement;
            valueIndicatorContainer =
                GetTemplateChild("PART_IndicatorContainer") as FrameworkElement;
            UpdateValueIndicator();
        }

        public double Value
        {
            get { return (double)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public void Reset()
        {
            ClearValue(ValueProperty);
        }

        private void UpdateValueIndicator()
        {
            if (valueIndicator != null && valueIndicatorContainer != null)
            {
                valueIndicator.Width = valueIndicatorContainer.ActualWidth / 2 * (Value + 1);
            }
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            UpdateValueIndicator();
        }
    }
}
