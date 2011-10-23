using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Data;
using System.Diagnostics;
using AudioAlign.Audio;
using System.Windows.Documents;
using AudioAlign.Audio.Project;
using System.Windows.Media;
using System.Windows.Input;
using AudioAlign.Audio.Matching;
using System.Collections.ObjectModel;

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

        private MultiTrackListBox multiTrackListBox;
        MultiTrackConnectionAdorner multiTrackConnectionAdorner;

        public MultiTrackViewer() {
            this.Loaded += new RoutedEventHandler(MultiTrackViewer_Loaded);
        }

        private void MultiTrackViewer_Loaded(object sender, RoutedEventArgs e) {
            AddHandler(CaretOverlay.PositionSelectedEvent, new CaretOverlay.PositionEventHandler(MultiTrackViewer_CaretPositionSelected));
            AddHandler(CaretOverlay.IntervalSelectedEvent, new CaretOverlay.IntervalEventHandler(MultiTrackViewer_CaretIntervalSelected));
            AddHandler(WaveView.TrackOffsetChangedEvent, new RoutedEventHandler(MultiTrackViewer_WaveViewTrackOffsetChanged));
            multiTrackListBox = (MultiTrackListBox)GetTemplateChild("PART_TrackListBox");

            StackPanel itemContainer = UIUtil.FindVisualChild<StackPanel>(multiTrackListBox);
            multiTrackConnectionAdorner = new MultiTrackConnectionAdorner(itemContainer, multiTrackListBox);
            AdornerLayer.GetAdornerLayer(itemContainer).Add(multiTrackConnectionAdorner);
        }

        private void MultiTrackViewer_CaretPositionSelected(object sender, CaretOverlay.PositionEventArgs e) {
            //Debug.WriteLine("MultiTrackViewer CaretPositionSelected @ " + e.Position);
            SetValue(VirtualCaretOffsetProperty, PhysicalToVirtualIntervalOffset(VirtualViewportInterval, e.SourceInterval, e.Position));
            e.Handled = true;
        }

        private void MultiTrackViewer_CaretIntervalSelected(object sender, CaretOverlay.IntervalEventArgs e) {
            //Debug.WriteLine("MultiTrackViewer CaretIntervalSelected {0} -> {1} ", e.From, e.To);
            e.Handled = true;
        }

        private void MultiTrackViewer_WaveViewTrackOffsetChanged(object sender, RoutedEventArgs e) {
            RefreshAdornerLayer();
        }

        public long VirtualCaretOffset {
            get { return (long)GetValue(VirtualCaretOffsetProperty); }
            set { SetValue(VirtualCaretOffsetProperty, value); }
        }

        public ItemCollection Items {
            get { return multiTrackListBox.Items; }
        }

        public object SelectedItem {
            get { return multiTrackListBox.SelectedItem; }
        }

        public ObservableCollection<Match> Matches {
            get { return multiTrackConnectionAdorner.Matches; }
        }

        public Match SelectedMatch {
            get { return multiTrackConnectionAdorner.SelectedMatch; }
            set { multiTrackConnectionAdorner.SelectedMatch = value; }
        }

        public void RefreshAdornerLayer() {
            if (multiTrackConnectionAdorner != null) {
                multiTrackConnectionAdorner.InvalidateVisual();
            }
        }

        public void Display(TimeSpan time, bool adjustCaret) {
            VirtualViewportOffset = time.Ticks - VirtualViewportWidth / 2;
            if (adjustCaret) {
                VirtualCaretOffset = time.Ticks;
            }
        }

        public void FitTracksVertically(double minHeight) {
            double targetHeight = 
                (multiTrackListBox.ActualHeight - UIUtil.FindVisualChild<TimeScale>(multiTrackListBox).ActualHeight) / 
                multiTrackListBox.Items.Count;
            targetHeight = Math.Max(targetHeight, minHeight);
            foreach (AudioTrack track in multiTrackListBox.Items) {
                ListBoxItem listBoxItem = multiTrackListBox.ItemContainerGenerator.ContainerFromItem(track) as ListBoxItem;
                if (listBoxItem != null) {
                    ResizeDecorator resizeDecorator = UIUtil.FindVisualChild<ResizeDecorator>(listBoxItem);
                    resizeDecorator.Height = targetHeight;
                }
            }
        }

        /// <summary>
        /// Preview event is used because the bubbling mousewheel event (which is already handled at this time)
        /// arrives after the listbox has done it's work on the event - which we want to avoid.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPreviewMouseWheel(System.Windows.Input.MouseWheelEventArgs e) {
            base.OnPreviewMouseWheel(e);

            // if the ctrl-key is held, we scroll the viewport
            if (Keyboard.Modifiers == ModifierKeys.Control) {
                double scrollPercentage = 0.01d;
                double scrollOffset = ActualWidth * scrollPercentage * (e.Delta / Mouse.MouseWheelDeltaForOneLine);
                VirtualViewportOffset += PhysicalToVirtualOffset(scrollOffset);
                e.Handled = true;
                return;
            }
            // else the viewport gets zoomed

            //Debug.WriteLine("MultiTrackViewer OnPreviewMouseWheel: " + e.Delta + " (" + e.Delta / Mouse.MouseWheelDeltaForOneLine + " lines)");

            // add/remove percentage for a zoom command
            double scalePercentage = 0.20d;
            bool zoomToCaret = true;

            Interval currentViewportInterval = VirtualViewportInterval;

            // calculate new viewport width
            long newViewportWidth = (long)(e.Delta < 0 ?
                currentViewportInterval.Length * (1 + scalePercentage) :
                currentViewportInterval.Length * (1 - scalePercentage));
            //Debug.WriteLine("MultiTrackViewer viewport width change: {0} -> {1}", currentViewportWidth, newViewportWidth);
            VirtualViewportWidth = newViewportWidth; // force coercion
            newViewportWidth = VirtualViewportWidth; // get coerced value
            
            // calculate new viewport offset (don't care about the valid offset range here - it's handled by the property value coercion)
            long viewportWidthDelta = currentViewportInterval.Length - newViewportWidth;
            long newViewportOffset;
            if (zoomToCaret) {
                // zoom the viewport and move it towards the caret
                long caretPosition = VirtualCaretOffset;
                if (!currentViewportInterval.Contains(caretPosition)) {
                    // if caret is out of the viewport, just skip there
                    newViewportOffset = caretPosition - newViewportWidth / 2;
                }
                else {
                    // if caret is visible, approach it smoothly
                    // TODO simplify the following calculation
                    newViewportOffset = currentViewportInterval.From + viewportWidthDelta / 2;
                    long caretTargetPosition = newViewportOffset + newViewportWidth / 2;
                    long caretPositionsDelta = caretPosition - caretTargetPosition;
                    newViewportOffset += caretPositionsDelta / 2;
                }
            }
            else {
                // straight viewport zoom
                newViewportOffset = VirtualViewportOffset + viewportWidthDelta / 2;
            }

            // set new values
            VirtualViewportOffset = newViewportOffset;
            VirtualViewportWidth = newViewportWidth;

            e.Handled = true;
        }

        protected override void OnViewportOffsetChanged(long oldValue, long newValue) {
            base.OnViewportOffsetChanged(oldValue, newValue);
            RefreshAdornerLayer();
        }

        protected override void OnViewportWidthChanged(long oldValue, long newValue) {
            base.OnViewportWidthChanged(oldValue, newValue);
            RefreshAdornerLayer();
        }
    }
}
