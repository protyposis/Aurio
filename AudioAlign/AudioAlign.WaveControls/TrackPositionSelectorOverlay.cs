using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using System.Diagnostics;
using System.Windows.Input;
using System.Windows.Threading;
using System.Threading;

namespace AudioAlign.WaveControls {
    public class TrackPositionSelectorOverlay : VirtualViewBase {

        public static readonly DependencyPropertyKey PhysicalCaretOffsetPropertyKey;
        public static readonly DependencyProperty PhysicalCaretOffsetProperty;

        public static readonly DependencyProperty VirtualCaretOffsetProperty;

        static TrackPositionSelectorOverlay() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TrackPositionSelectorOverlay), 
                new FrameworkPropertyMetadata(typeof(TrackPositionSelectorOverlay)));

            PhysicalCaretOffsetPropertyKey = DependencyProperty.RegisterReadOnly("PhysicalCaretOffset", 
                typeof(double), typeof(TrackPositionSelectorOverlay), 
                new FrameworkPropertyMetadata(new PropertyChangedCallback(OnPhysicalCaretOffsetChanged)));
            PhysicalCaretOffsetProperty = PhysicalCaretOffsetPropertyKey.DependencyProperty;

            VirtualCaretOffsetProperty = DependencyProperty.Register("VirtualCaretOffset",
                typeof(long), typeof(TrackPositionSelectorOverlay), 
                new FrameworkPropertyMetadata(new PropertyChangedCallback(OnVirtualCaretOffsetChanged)));
        }

        private static void OnPhysicalCaretOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            TrackPositionSelectorOverlay positionSelector = d as TrackPositionSelectorOverlay;
            double newValue = (double)e.NewValue;
            positionSelector.VirtualCaretOffset = positionSelector.PhysicalToVirtualOffset(newValue);
        }

        private static void OnVirtualCaretOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            long newValue = (long)e.NewValue;
        }

        private Point mouseDownPosition;

        public double PhysicalCaretOffset {
            get { return (double)GetValue(PhysicalCaretOffsetProperty); }
            private set { SetValue(PhysicalCaretOffsetPropertyKey, value); }
        }

        public long VirtualCaretOffset {
            get { return (long)GetValue(VirtualCaretOffsetProperty); }
            private set { SetValue(VirtualCaretOffsetProperty, value); }
        }

        protected override void OnPreviewMouseDown(System.Windows.Input.MouseButtonEventArgs e) {
            base.OnPreviewMouseDown(e);
            Debug.WriteLine("TrackPositionSelectorOverlay OnPreviewMouseDown @ " + Mouse.GetPosition(this));
            mouseDownPosition = Mouse.GetPosition(this);
        }

        protected override void OnPreviewMouseUp(System.Windows.Input.MouseButtonEventArgs e) {
            base.OnPreviewMouseUp(e);
            Debug.WriteLine("TrackPositionSelectorOverlay OnPreviewMouseUp @ " + Mouse.GetPosition(this));

            if (Mouse.GetPosition(this) == mouseDownPosition) {
                // pseudo click event
                //e.Handled = true;
                Debug.WriteLine("TrackPositionSelectorOverlay PseudoClick @ " + mouseDownPosition);
                PhysicalCaretOffset = mouseDownPosition.X;
            }
        }

        protected override void OnViewportWidthChanged(long oldValue, long newValue) {
            base.OnViewportWidthChanged(oldValue, newValue);
            PhysicalCaretOffset = VirtualToPhysicalOffset(VirtualCaretOffset);
        }
    }
}
