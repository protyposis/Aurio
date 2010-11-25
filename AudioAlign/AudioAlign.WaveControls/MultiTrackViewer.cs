using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Data;
using System.Diagnostics;

namespace AudioAlign.WaveControls {
    [TemplatePart(Name = "PART_TimeScale", Type = typeof(TimeScale))]
    [TemplatePart(Name = "PART_TrackListBox", Type = typeof(MultiTrackListBox))]
    public class MultiTrackViewer : VirtualViewBase {

        public static readonly DependencyProperty VirtualCaretOffsetProperty;

        static MultiTrackViewer() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(MultiTrackViewer), 
                new FrameworkPropertyMetadata(typeof(MultiTrackViewer)));

            VirtualCaretOffsetProperty = CaretOverlay.VirtualCaretOffsetProperty
                .AddOwner(typeof(MultiTrackViewer), new FrameworkPropertyMetadata() { 
                    Inherits = true, CoerceValueCallback = CaretOverlay.CoerceVirtualCaretOffset 
                });
        }

        public MultiTrackViewer() {
            this.Loaded += new RoutedEventHandler(MultiTrackViewer_Loaded);
        }

        private void MultiTrackViewer_Loaded(object sender, RoutedEventArgs e) {
            AddHandler(CaretOverlay.PositionSelectedEvent, new CaretOverlay.PositionEventHandler(MultiTrackViewer_CaretPositionSelected));
            AddHandler(CaretOverlay.IntervalSelectedEvent, new CaretOverlay.IntervalEventHandler(MultiTrackViewer_CaretIntervalSelected));
        }

        private void MultiTrackViewer_CaretPositionSelected(object sender, CaretOverlay.PositionEventArgs e) {
            //Debug.WriteLine("MultiTrackViewer CaretPositionSelected @ " + e.Position);
            SetValue(VirtualCaretOffsetProperty, PhysicalToVirtualOffset(VirtualViewportInterval, e.SourceInterval, e.Position));
            e.Handled = true;
        }

        private void MultiTrackViewer_CaretIntervalSelected(object sender, CaretOverlay.IntervalEventArgs e) {
            //Debug.WriteLine("MultiTrackViewer CaretIntervalSelected {0} -> {1} ", e.From, e.To);
            e.Handled = true;
        }

        public long VirtualCaretOffset {
            get { return (long)GetValue(VirtualCaretOffsetProperty); }
            set { SetValue(VirtualCaretOffsetProperty, value); }
        }

        public ItemCollection Items {
            get {
                ItemsControl itemsControl = GetTemplateChild("PART_TrackListBox") as ItemsControl;
                return itemsControl.Items;
            }
        }

        /// <summary>
        /// Preview event is used because the bubbling mousewheel event (ehich is already handled at this time)
        /// arrives after the listbox has done it's work on the event - which we want to avoid.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPreviewMouseWheel(System.Windows.Input.MouseWheelEventArgs e) {
            base.OnPreviewMouseWheel(e);
            //Debug.WriteLine("MultiTrackViewer OnPreviewMouseWheel: " + e.Delta);

            // add/remove percentage for a zoom command
            double scalePercentage = 0.25d;

            // calculate new viewport width
            long currentViewportWidth = VirtualViewportWidth;
            long newViewportWidth = (long)(e.Delta < 0 ? 
                currentViewportWidth * (1 + scalePercentage) : 
                currentViewportWidth * (1 - scalePercentage));
            //Debug.WriteLine("MultiTrackViewer viewport width change: {0} -> {1}", currentViewportWidth, newViewportWidth);
            
            // calculate new viewport offset (don't care about the valid offset range here - it's handled by the property value coercion)
            long viewportWidthDelta = currentViewportWidth - newViewportWidth;
            long newViewportOffset = VirtualViewportOffset + viewportWidthDelta / 2;

            // set new values
            VirtualViewportOffset = newViewportOffset;
            VirtualViewportWidth = newViewportWidth;

            e.Handled = true;
        }

        //protected override void OnViewportWidthChanged(long oldValue, long newValue) {
        //    long viewportWidthDifference = newValue - oldValue;
        //    long viewportOffset = VirtualViewportOffset;
        //    long caretPosition = VirtualCaretOffset;
        //}
    }
}
