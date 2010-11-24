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
                new FrameworkPropertyMetadata { Inherits = true, AffectsRender = true,
                PropertyChangedCallback = OnViewportOffsetChanged});

        public static readonly DependencyProperty VirtualViewportWidthProperty = DependencyProperty.Register(
            "VirtualViewportWidth", typeof(long), typeof(VirtualViewBase),
                new FrameworkPropertyMetadata { Inherits = true, AffectsRender = true, 
                    PropertyChangedCallback = OnViewportWidthChanged, DefaultValue = 1000L });

        private static void OnViewportOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            VirtualViewBase ctrl = (VirtualViewBase)d;
            ctrl.OnViewportOffsetChanged((long)e.OldValue, (long)e.NewValue);
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

        public static long PhysicalToVirtualOffset(Interval virtualViewportInterval, double controlWidth, double physicalOffset) {
            long visibleIntervalOffset = (long)Math.Round(virtualViewportInterval.Length / controlWidth * physicalOffset);
            return virtualViewportInterval.From + visibleIntervalOffset;
        }

        public static double VirtualToPhysicalOffset(Interval virtualViewportInterval, double controlWidth, long virtualOffset) {
            virtualOffset -= virtualViewportInterval.From;
            double physicalOffset = controlWidth / virtualViewportInterval.Length * virtualOffset;
            return physicalOffset;
        }

        public long PhysicalToVirtualOffset(double physicalOffset) {
            return PhysicalToVirtualOffset(VirtualViewportInterval, ActualWidth, physicalOffset);
        }

        public double VirtualToPhysicalOffset(long virtualOffset) {
            return VirtualToPhysicalOffset(VirtualViewportInterval, ActualWidth, virtualOffset);
        }

        protected virtual void OnViewportOffsetChanged(long oldValue, long newValue) { }
        protected virtual void OnViewportWidthChanged(long oldValue, long newValue) {}
    }
}
