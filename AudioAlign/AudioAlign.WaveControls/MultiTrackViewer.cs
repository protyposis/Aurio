using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;

namespace AudioAlign.WaveControls {
    public class MultiTrackViewer : Control {

        //public static readonly DependencyProperty VerticalOffsetProperty;
        //public static readonly DependencyProperty ScrollableHeightProperty;
        //public static readonly DependencyProperty ViewportHeightProperty;

        public static readonly DependencyProperty VirtualViewportOffsetProperty;
        //public static readonly DependencyProperty ScrollableWidthProperty;
        public static readonly DependencyProperty VirtualViewportWidthProperty;

        static MultiTrackViewer() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(MultiTrackViewer), new FrameworkPropertyMetadata(typeof(MultiTrackViewer)));

            //VerticalOffsetProperty = ScrollViewer.VerticalOffsetProperty.AddOwner(typeof(MultiTrackViewer));
            //ScrollableHeightProperty = ScrollViewer.ScrollableHeightProperty.AddOwner(typeof(MultiTrackViewer));
            //ViewportHeightProperty = ScrollViewer.ViewportHeightProperty.AddOwner(typeof(MultiTrackViewer));

            VirtualViewportOffsetProperty = VirtualViewBase.VirtualViewportOffsetProperty.AddOwner(typeof(MultiTrackViewer));
            //ScrollableWidthProperty = ScrollViewer.ScrollableWidthProperty.AddOwner(typeof(MultiTrackViewer));
            VirtualViewportWidthProperty = VirtualViewBase.VirtualViewportWidthProperty.AddOwner(typeof(MultiTrackViewer), new FrameworkPropertyMetadata() { Inherits = true });
        }

        //public double VerticalOffset {
        //    get { return (double)GetValue(VerticalOffsetProperty); }
        //    set { SetValue(VerticalOffsetProperty, value); }
        //}

        //public double ScrollableHeight {
        //    get { return (double)GetValue(ScrollableHeightProperty); }
        //    set { SetValue(ScrollableHeightProperty, value); }
        //}

        //public double ViewportHeight {
        //    get { return (double)GetValue(ViewportHeightProperty); }
        //    set { SetValue(ViewportHeightProperty, value); }
        //}

        public long VirtualViewportOffset {
            get { return (long)GetValue(VirtualViewportOffsetProperty); }
            set { SetValue(VirtualViewportOffsetProperty, value); }
        }

        //public double ScrollableWidth {
        //    get { return (double)GetValue(ScrollableWidthProperty); }
        //    set { SetValue(ScrollableWidthProperty, value); }
        //}

        public long VirtualViewportWidth {
            get { return (long)GetValue(VirtualViewportWidthProperty); }
            set { SetValue(VirtualViewportWidthProperty, value); }
        }

        public ItemCollection Items {
            get {
                ItemsControl itemsControl = GetTemplateChild("PART_TrackListBox") as ItemsControl;
                return itemsControl.Items;
            }
        }
    }
}
