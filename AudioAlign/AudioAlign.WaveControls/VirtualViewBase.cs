using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using AudioAlign.Audio;
using System.Diagnostics;

namespace AudioAlign.WaveControls {
    public class VirtualViewBase: Control {

        public static readonly DependencyProperty VirtualViewportOffsetProperty = DependencyProperty.Register(
            "VirtualViewportOffset", typeof(long), typeof(VirtualViewBase),
                new FrameworkPropertyMetadata {
                    Inherits = true, AffectsRender = true,
                    CoerceValueCallback = CoerceVirtualViewportOffset,
                    PropertyChangedCallback = OnViewportOffsetChanged
                });

        public static readonly DependencyProperty VirtualViewportWidthProperty = DependencyProperty.Register(
            "VirtualViewportWidth", typeof(long), typeof(VirtualViewBase),
                new FrameworkPropertyMetadata {
                    Inherits = true, AffectsRender = true,
                    CoerceValueCallback = CoerceVirtualViewportWidth,
                    PropertyChangedCallback = OnViewportWidthChanged, DefaultValue = 1000L
                });

        public static readonly DependencyProperty DebugOutputProperty = DependencyProperty.Register(
            "DebugOutput", typeof(bool), typeof(VirtualViewBase),
                new FrameworkPropertyMetadata {
                    Inherits = true, AffectsRender = true, DefaultValue = false
                });

        private static object CoerceVirtualViewportOffset(DependencyObject d, object value) {
            long newValue = (long)value;
            
            long lowerBound = 0L;
            if (newValue < lowerBound) {
                return lowerBound;
            }

            return newValue;
        }

        private static object CoerceVirtualViewportWidth(DependencyObject d, object value) {
            //Debug.WriteLine("CoerceVirtualViewportWidth @ " + ((FrameworkElement)d).Name);
            long newValue = (long)value;
            if (newValue < 1) {
                return 1L;
            }
            return newValue;
        }

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

        public bool DebugOutput {
            get { return (bool)GetValue(DebugOutputProperty); }
            set { SetValue(DebugOutputProperty, value); }
        }

        public Interval VirtualViewportInterval {
            get { return new Interval(VirtualViewportOffset, VirtualViewportOffset + VirtualViewportWidth); }
        }

        public static long PhysicalToVirtualOffset(long virtualViewportWidth, double controlWidth, double physicalOffset) {
            return (long)Math.Round(virtualViewportWidth / controlWidth * physicalOffset);
        }

        public static double VirtualToPhysicalOffset(long virtualViewportWidth, double controlWidth, long virtualOffset) {
            return controlWidth / virtualViewportWidth * virtualOffset;
        }

        public static long PhysicalToVirtualIntervalOffset(Interval virtualViewportInterval, double controlWidth, double physicalOffset) {
            long visibleIntervalOffset = (long)Math.Round(virtualViewportInterval.Length / controlWidth * physicalOffset);
            return virtualViewportInterval.From + visibleIntervalOffset;
        }

        public static double VirtualToPhysicalIntervalOffset(Interval virtualViewportInterval, double controlWidth, long virtualOffset) {
            virtualOffset -= virtualViewportInterval.From;
            double physicalOffset = controlWidth / virtualViewportInterval.Length * virtualOffset;
            return physicalOffset;
        }

        public long PhysicalToVirtualOffset(double physicalOffset) {
            return PhysicalToVirtualOffset(VirtualViewportWidth, ActualWidth, physicalOffset);
        }

        public double VirtualToPhysicalOffset(long virtualOffset) {
            return VirtualToPhysicalOffset(VirtualViewportWidth, ActualWidth, virtualOffset);
        }

        public long PhysicalToVirtualIntervalOffset(double physicalOffset) {
            return PhysicalToVirtualIntervalOffset(VirtualViewportInterval, ActualWidth, physicalOffset);
        }

        public double VirtualToPhysicalIntervalOffset(long virtualOffset) {
            return VirtualToPhysicalIntervalOffset(VirtualViewportInterval, ActualWidth, virtualOffset);
        }

        protected virtual void OnViewportOffsetChanged(long oldValue, long newValue) { }
        protected virtual void OnViewportWidthChanged(long oldValue, long newValue) {}
    }
}
