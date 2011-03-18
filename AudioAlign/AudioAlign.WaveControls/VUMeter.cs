using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;

namespace AudioAlign.WaveControls {
    [TemplatePart(Name = "PART_Indicator", Type = typeof(FrameworkElement))]
    [TemplatePart(Name = "PART_IndicatorContainer", Type = typeof(FrameworkElement))]
    public class VUMeter : Control {

        public const int MIN_DB = -60;
        public const int MAX_DB = 0;

        private static readonly DependencyPropertyKey DecibelPropertyKey;
        private static readonly DependencyPropertyKey IsOverdrivenPropertyKey;

        public static readonly DependencyProperty AmplitudeProperty;
        public static readonly DependencyProperty DecibelProperty;
        public static readonly DependencyProperty IsOverdrivenProperty;

        static VUMeter() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(VUMeter), 
                new FrameworkPropertyMetadata(typeof(VUMeter)));

            AmplitudeProperty = DependencyProperty.Register("Amplitude", typeof(double), typeof(VUMeter),
                new FrameworkPropertyMetadata(0.0d, new PropertyChangedCallback(OnAmplitudeChanged)) { AffectsRender = true });

            DecibelPropertyKey = DependencyProperty.RegisterReadOnly("Decibel", typeof(double), typeof(VUMeter), 
                new FrameworkPropertyMetadata(double.NegativeInfinity, new PropertyChangedCallback(OnDecibelChanged)));
            DecibelProperty = DecibelPropertyKey.DependencyProperty;

            IsOverdrivenPropertyKey = DependencyProperty.RegisterReadOnly("IsOverdriven", typeof(bool), typeof(VUMeter),
                new FrameworkPropertyMetadata(false));
            IsOverdrivenProperty = IsOverdrivenPropertyKey.DependencyProperty;
        }

        private static void OnAmplitudeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            VUMeter vuMeter = d as VUMeter;
            double newValue = (double)e.NewValue;
            if (newValue == double.NegativeInfinity) {
                vuMeter.Decibel = double.NegativeInfinity;
            }
            else {
                vuMeter.Decibel = 20 * Math.Log10(newValue);
            }
        }

        private static void OnDecibelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            VUMeter vuMeter = d as VUMeter;
            vuMeter.UpdateVolumeIndicator();
        }

        private FrameworkElement volumeIndicator;
        private FrameworkElement volumeIndicatorContainer;

        public VUMeter() {
            this.Loaded += new RoutedEventHandler(VUMeter_Loaded);
        }

        private void VUMeter_Loaded(object sender, RoutedEventArgs e) {
            volumeIndicator = GetTemplateChild("PART_Indicator") as FrameworkElement;
            volumeIndicatorContainer = GetTemplateChild("PART_IndicatorContainer") as FrameworkElement;
            UpdateVolumeIndicator();
        }

        public double Amplitude {
            get { return (double)GetValue(AmplitudeProperty); }
            set { SetValue(AmplitudeProperty, value); }
        }

        public double Decibel {
            get { return (double)GetValue(DecibelProperty); }
            private set { SetValue(DecibelPropertyKey, value); }
        }

        public bool IsOverdriven {
            get { return (bool)GetValue(IsOverdrivenProperty); }
            private set { SetValue(IsOverdrivenPropertyKey, value); }
        }

        private void UpdateVolumeIndicator() {
            double decibel = Decibel;

            if (volumeIndicator != null && volumeIndicatorContainer != null) {
                double percentage = 0;

                if (decibel > double.NegativeInfinity) {
                    if (decibel > 0) {
                        percentage = 1;
                    }
                    else {
                        percentage = (Decibel - MIN_DB) / (MAX_DB - MIN_DB);
                    }
                }

                volumeIndicator.Height = volumeIndicatorContainer.ActualHeight * (1 - percentage);
            }

            IsOverdriven = decibel > 0;
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo) {
            base.OnRenderSizeChanged(sizeInfo);
            UpdateVolumeIndicator();
        }
    }
}
