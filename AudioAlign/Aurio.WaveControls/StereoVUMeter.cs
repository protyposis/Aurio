using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace AudioAlign.WaveControls {
    public class StereoVUMeter : Control {
        
        public static readonly DependencyProperty AmplitudeLeftProperty;
        public static readonly DependencyProperty AmplitudeRightProperty;

        static StereoVUMeter() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(StereoVUMeter),
                new FrameworkPropertyMetadata(typeof(StereoVUMeter)));

            AmplitudeLeftProperty = DependencyProperty.Register("AmplitudeLeft", typeof(double), typeof(StereoVUMeter),
                new FrameworkPropertyMetadata(0.0d) { AffectsRender = true });

            AmplitudeRightProperty = DependencyProperty.Register("AmplitudeRight", typeof(double), typeof(StereoVUMeter),
                new FrameworkPropertyMetadata(0.0d) { AffectsRender = true });
        }

        public StereoVUMeter() {
        }

        public double AmplitudeLeft {
            get { return (double)GetValue(AmplitudeLeftProperty); }
            set { SetValue(AmplitudeLeftProperty, value); }
        }

        public double AmplitudeRight {
            get { return (double)GetValue(AmplitudeRightProperty); }
            set { SetValue(AmplitudeRightProperty, value); }
        }

        public void Reset() {
            ClearValue(AmplitudeLeftProperty);
            ClearValue(AmplitudeRightProperty);
        }
    }
}
