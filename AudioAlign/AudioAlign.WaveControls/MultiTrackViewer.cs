using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;

namespace AudioAlign.WaveControls {
    public class MultiTrackViewer : ContentControl {

        public static readonly DependencyProperty VerticalOffsetProperty;
        public static readonly DependencyProperty ScrollableHeightProperty;
        public static readonly DependencyProperty ViewportHeightProperty;

        public static readonly DependencyProperty ViewportOffsetProperty;
        public static readonly DependencyProperty ScrollableWidthProperty;
        public static readonly DependencyProperty ViewportWidthProperty;

        public static readonly DependencyProperty TestProperty;

        static MultiTrackViewer() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(MultiTrackViewer), new FrameworkPropertyMetadata(typeof(MultiTrackViewer)));

            VerticalOffsetProperty = ScrollViewer.VerticalOffsetProperty.AddOwner(typeof(MultiTrackViewer));
            ScrollableHeightProperty = ScrollViewer.ScrollableHeightProperty.AddOwner(typeof(MultiTrackViewer));
            ViewportHeightProperty = ScrollViewer.ViewportHeightProperty.AddOwner(typeof(MultiTrackViewer));

            ViewportOffsetProperty = VirtualViewBase.ViewportOffsetProperty.AddOwner(typeof(MultiTrackViewer));
            ScrollableWidthProperty = ScrollViewer.ScrollableWidthProperty.AddOwner(typeof(MultiTrackViewer));
            ViewportWidthProperty = VirtualViewBase.ViewportWidthProperty.AddOwner(typeof(MultiTrackViewer), new FrameworkPropertyMetadata() { Inherits = true });

            TestProperty = DependencyProperty.Register(
            "Test", typeof(long), typeof(MultiTrackViewer),
                new FrameworkPropertyMetadata { AffectsRender = true });
        }

        public double VerticalOffset {
            get { return (double)GetValue(VerticalOffsetProperty); }
            set { SetValue(VerticalOffsetProperty, value); }
        }

        public double ScrollableHeight {
            get { return (double)GetValue(ScrollableHeightProperty); }
            set { SetValue(ScrollableHeightProperty, value); }
        }

        public double ViewportHeight {
            get { return (double)GetValue(ViewportHeightProperty); }
            set { SetValue(ViewportHeightProperty, value); }
        }

        public long ViewportOffset {
            get { return (long)GetValue(ViewportOffsetProperty); }
            set { SetValue(ViewportOffsetProperty, value); }
        }

        public double ScrollableWidth {
            get { return (double)GetValue(ScrollableWidthProperty); }
            set { SetValue(ScrollableWidthProperty, value); }
        }

        public long ViewportWidth {
            get { return (long)GetValue(ViewportWidthProperty); }
            set { SetValue(ViewportWidthProperty, value); }
        }
    }
}
