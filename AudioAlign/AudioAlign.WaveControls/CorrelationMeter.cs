using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;

namespace AudioAlign.WaveControls {
    [TemplatePart(Name = "PART_Indicator", Type = typeof(FrameworkElement))]
    [TemplatePart(Name = "PART_IndicatorContainer", Type = typeof(FrameworkElement))]
    public class CorrelationMeter : Control {

        public static DependencyProperty ValueProperty;

        static CorrelationMeter() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(CorrelationMeter), 
                new FrameworkPropertyMetadata(typeof(CorrelationMeter)));

            ValueProperty = DependencyProperty.Register("Value", typeof(double), typeof(CorrelationMeter),
                new FrameworkPropertyMetadata(0.0d, new PropertyChangedCallback(OnValueChanged)) { AffectsRender = true });
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            CorrelationMeter correlationMeter = d as CorrelationMeter;
            correlationMeter.UpdateValueIndicator();
        }

        private FrameworkElement valueIndicator;
        private FrameworkElement valueIndicatorContainer;

        public CorrelationMeter() {
            this.Loaded += new RoutedEventHandler(CorrelationMeter_Loaded);
        }

        private void CorrelationMeter_Loaded(object sender, RoutedEventArgs e) {
            valueIndicator = GetTemplateChild("PART_Indicator") as FrameworkElement;
            valueIndicatorContainer = GetTemplateChild("PART_IndicatorContainer") as FrameworkElement;
            UpdateValueIndicator();
        }

        public double Value {
            get { return (double)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        private void UpdateValueIndicator() {
            if (valueIndicator != null && valueIndicatorContainer != null) {
                valueIndicator.Width = valueIndicatorContainer.ActualWidth / 2 * (Value + 1);
            }
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo) {
            base.OnRenderSizeChanged(sizeInfo);
            UpdateValueIndicator();
        }
    }
}
