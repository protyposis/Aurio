using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using AudioAlign.Audio;

namespace AudioAlign.WaveControls {
    public class VirtualViewBase: Control {

        public static readonly DependencyProperty VirtualViewportOffsetProperty = DependencyProperty.Register(
            "VirtualViewportOffset", typeof(long), typeof(VirtualViewBase),
                new FrameworkPropertyMetadata { Inherits = true, AffectsRender = true });

        public static readonly DependencyProperty VirtualViewportWidthProperty = DependencyProperty.Register(
            "VirtualViewportWidth", typeof(long), typeof(VirtualViewBase),
                new FrameworkPropertyMetadata { Inherits = true, AffectsRender = true, 
                    PropertyChangedCallback = OnViewportWidthChanged, 
                    CoerceValueCallback = CoerceViewportWidth, DefaultValue = (long)1000000 });

        private static object CoerceViewportWidth(DependencyObject d, object value) {
            long viewportWidth = (long)value;
            // avoid negative length
            return viewportWidth >= 0 ? viewportWidth : 0;
        }

        private static void OnViewportWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            VirtualViewBase ctrl = (VirtualViewBase)d;
            ctrl.OnViewportWidthChanged((long)e.OldValue, (long)e.NewValue);
        }

        public long VirtualViewportOffset {
            get { return (long)GetValue(VirtualViewportOffsetProperty); }
            set { SetValue(VirtualViewportOffsetProperty, value); }
        }

        public long VirtualViewportWidth {
            get { return (long)GetValue(VirtualViewportWidthProperty); }
            set { SetValue(VirtualViewportWidthProperty, value); }
        }

        public Interval VirtualViewportInterval {
            get { return new Interval(VirtualViewportOffset, VirtualViewportOffset + VirtualViewportWidth); }
        }

        protected virtual void OnViewportWidthChanged(long oldValue, long newValue) {}
    }
}
