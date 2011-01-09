using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using System.ComponentModel;

namespace AudioAlign.WaveControls {
    public class ExtendedSlider : Slider {

        private static readonly DependencyProperty DefaultValueProperty = DependencyProperty.Register(
            "DefaultValue", typeof(double), typeof(ExtendedSlider), 
            new FrameworkPropertyMetadata(0.0d, 
                new PropertyChangedCallback(OnDefaultValueChanged), 
                new CoerceValueCallback(CoerceDefaultValue)));

        private static void OnDefaultValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            ExtendedSlider ctrl = (ExtendedSlider)d;
            ctrl.OnDefaultValueChanged((double)e.OldValue, (double)e.NewValue);
        }

        private static object CoerceDefaultValue(DependencyObject d, object baseValue) {
            ExtendedSlider ctrl = (ExtendedSlider)d;
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

        public ExtendedSlider() {
            this.MouseDoubleClick += new System.Windows.Input.MouseButtonEventHandler(ExtendedSlider_MouseDoubleClick);
        }

        private void ExtendedSlider_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) {
            Value = DefaultValue;
        }

        protected virtual void OnDefaultValueChanged(double oldValue, double newValue) {
        }

        [Bindable(true), Category("Common")]
        public double DefaultValue {
            get { return (double)GetValue(DefaultValueProperty); }
            set { SetValue(DefaultValueProperty, value); }
        }

    }
}
