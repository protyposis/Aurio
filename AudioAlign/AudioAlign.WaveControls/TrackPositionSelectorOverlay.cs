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
using System.Windows.Media;
using System.ComponentModel;

namespace AudioAlign.WaveControls {
    public class TrackPositionSelectorOverlay : VirtualContentViewBase {

        public static readonly DependencyPropertyKey PhysicalCaretOffsetPropertyKey;

        public static readonly DependencyProperty PhysicalCaretOffsetProperty;
        public static readonly DependencyProperty VirtualCaretOffsetProperty;
        public static readonly DependencyProperty CaretBrushProperty;

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

            CaretBrushProperty = DependencyProperty.Register("CaretBrush",
                typeof(Brush), typeof(TrackPositionSelectorOverlay),
                new FrameworkPropertyMetadata(Brushes.Black));
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

        [Bindable(true), Category("Brushes")]
        public Brush CaretBrush {
            get { return (Brush)GetValue(CaretBrushProperty); }
            set { SetValue(CaretBrushProperty, value); }
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
