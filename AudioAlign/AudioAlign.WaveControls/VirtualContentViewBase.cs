using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using AudioAlign.Audio;

namespace AudioAlign.WaveControls {
    public class VirtualContentViewBase : ContentControl {

        public static readonly DependencyProperty VirtualViewportOffsetProperty =
            VirtualViewBase.VirtualViewportOffsetProperty.AddOwner(typeof(VirtualContentViewBase),
            new FrameworkPropertyMetadata() { Inherits = true, PropertyChangedCallback = OnViewportOffsetChanged });

        public static readonly DependencyProperty VirtualViewportWidthProperty =
            VirtualViewBase.VirtualViewportWidthProperty.AddOwner(typeof(VirtualContentViewBase),
            new FrameworkPropertyMetadata() { Inherits = true, PropertyChangedCallback = OnViewportWidthChanged });

        public static readonly DependencyProperty DebugOutputProperty =
            VirtualViewBase.DebugOutputProperty.AddOwner(typeof(VirtualContentViewBase),
            new FrameworkPropertyMetadata() { Inherits = true });

        private static void OnViewportOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            VirtualContentViewBase ctrl = (VirtualContentViewBase)d;
            ctrl.OnViewportOffsetChanged((long)e.OldValue, (long)e.NewValue);
        }

        private static void OnViewportWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            VirtualContentViewBase ctrl = (VirtualContentViewBase)d;
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

        public long PhysicalToVirtualOffset(double physicalOffset) {
            return VirtualViewBase.PhysicalToVirtualOffset(VirtualViewportInterval, ActualWidth, physicalOffset);
        }

        public double VirtualToPhysicalOffset(long virtualOffset) {
            return VirtualViewBase.VirtualToPhysicalOffset(VirtualViewportInterval, ActualWidth, virtualOffset);
        }

        protected virtual void OnViewportOffsetChanged(long oldValue, long newValue) { }
        protected virtual void OnViewportWidthChanged(long oldValue, long newValue) { }
    }
}
