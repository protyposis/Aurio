using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.ComponentModel;

namespace AudioAlign.WaveControls
{
    public partial class WaveView {

        public static readonly DependencyProperty WaveformBackgroundProperty;
        public static readonly DependencyProperty WaveformLineProperty;
        public static readonly DependencyProperty WaveformFillProperty;
        public static readonly DependencyProperty WaveformSamplePointProperty;
        public static readonly DependencyProperty TrackLengthProperty;
        public static readonly DependencyProperty TrackOffsetProperty;
        public static readonly DependencyProperty ViewportOffsetProperty;
        public static readonly DependencyProperty ViewportWidthProperty;

        private static readonly DependencyPropertyKey TrackScrollLengthPropertyKey; // TrackLength - ViewportWidth
        public static readonly DependencyProperty TrackScrollLengthProperty;

        private static readonly DependencyPropertyKey ViewportZoomPropertyKey; // ActualWidth / ViewportWidth
        public static readonly DependencyProperty ViewportZoomProperty;

        static WaveView() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(WaveView),
                new FrameworkPropertyMetadata(typeof(WaveView)));

            WaveformBackgroundProperty = DependencyProperty.Register("WaveformBackground", typeof(Brush), typeof(WaveView), 
                new FrameworkPropertyMetadata { DefaultValue = Brushes.White, AffectsRender = true });

            WaveformLineProperty = DependencyProperty.Register("WaveformLine", typeof(Brush), typeof(WaveView),
                new FrameworkPropertyMetadata { DefaultValue = Brushes.CornflowerBlue, AffectsRender = true });

            WaveformFillProperty = DependencyProperty.Register("WaveformFill", typeof(Brush), typeof(WaveView),
                new FrameworkPropertyMetadata { DefaultValue = Brushes.LightBlue, AffectsRender = true });

            WaveformSamplePointProperty = DependencyProperty.Register("WaveformSamplePoint", typeof(Brush), typeof(WaveView),
                new FrameworkPropertyMetadata { DefaultValue = Brushes.RoyalBlue, AffectsRender = true });

            TrackLengthProperty = DependencyProperty.Register("TrackLength", typeof(long), typeof(WaveView),
                new FrameworkPropertyMetadata { AffectsRender = true, 
                    PropertyChangedCallback = OnTrackLengthChanged });

            TrackOffsetProperty = DependencyProperty.Register("TrackOffset", typeof(long), typeof(WaveView),
                new FrameworkPropertyMetadata { AffectsRender = true });

            ViewportOffsetProperty = DependencyProperty.Register("ViewportOffset", typeof(long), typeof(WaveView),
                new FrameworkPropertyMetadata { AffectsRender = true });

            ViewportWidthProperty = DependencyProperty.Register("ViewportWidth", typeof(long), typeof(WaveView),
                new FrameworkPropertyMetadata { AffectsRender = true, 
                    PropertyChangedCallback = OnViewportWidthChanged, 
                    CoerceValueCallback = CoerceViewportWidth, DefaultValue = (long)100 });

            TrackScrollLengthPropertyKey = DependencyProperty.RegisterReadOnly("TrackScrollLength", typeof(long), typeof(WaveView),
                new FrameworkPropertyMetadata());
            TrackScrollLengthProperty = TrackScrollLengthPropertyKey.DependencyProperty;

            ViewportZoomPropertyKey = DependencyProperty.RegisterReadOnly("ViewportZoom", typeof(float), typeof(WaveView),
                new FrameworkPropertyMetadata());
            ViewportZoomProperty = ViewportZoomPropertyKey.DependencyProperty;

            //ActualWidthProperty.OverrideMetadata(typeof(WaveView),
            //    new FrameworkPropertyMetadata(0.0, OnActualWidthChanged));
        }

        private static void OnTrackLengthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            UpdateTrackScrollLength(d);
        }

        private static void OnViewportWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            UpdateTrackScrollLength(d);
            UpdateViewportZoom(d);
        }

        //private static void OnActualWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        //    UpdateViewportZoom(d);
        //}

        private static object CoerceTrackLength(DependencyObject d, object value) {
            long trackLength = (long)value;
            // avoid negative length
            return trackLength >= 0 ? trackLength : 0;
        }

        private static object CoerceViewportWidth(DependencyObject d, object value) {
            long viewportWidth = (long)value;
            // avoid negative length
            return viewportWidth >= 0 ? viewportWidth : 0;
        }

        private static void UpdateTrackScrollLength(DependencyObject d) {
            long trackLength = (long)d.GetValue(TrackLengthProperty);
            long viewportWidth = (long)d.GetValue(ViewportWidthProperty);
            d.SetValue(TrackScrollLengthPropertyKey, trackLength - viewportWidth);
        }

        private static void UpdateViewportZoom(DependencyObject d) {
            long viewportWidth = (long)d.GetValue(ViewportWidthProperty);
            double actualWidth = (double)d.GetValue(ActualWidthProperty);
            d.SetValue(ViewportZoomPropertyKey, (float)(actualWidth / viewportWidth));
        }

        [Bindable(true), Category("Brushes")]
        public Brush WaveformBackground {
            get { return (Brush)GetValue(WaveformBackgroundProperty); }
            set { SetValue(WaveformBackgroundProperty, value); }
        }

        [Bindable(true), Category("Brushes")]
        public Brush WaveformLine {
            get { return (Brush)GetValue(WaveformLineProperty); }
            set { SetValue(WaveformLineProperty, value); }
        }

        [Bindable(true), Category("Brushes")]
        public Brush WaveformFill {
            get { return (Brush)GetValue(WaveformFillProperty); }
            set { SetValue(WaveformFillProperty, value); }
        }

        [Bindable(true), Category("Brushes")]
        public Brush WaveformSamplePoint {
            get { return (Brush)GetValue(WaveformSamplePointProperty); }
            set { SetValue(WaveformSamplePointProperty, value); }
        }

        public long TrackLength {
            get { return (long)GetValue(TrackLengthProperty); }
            set { SetValue(TrackLengthProperty, value); }
        }

        public long TrackOffset {
            get { return (long)GetValue(TrackOffsetProperty); }
            set { SetValue(TrackOffsetProperty, value); }
        }

        public long ViewportOffset {
            get { return (long)GetValue(ViewportOffsetProperty); }
            set { SetValue(ViewportOffsetProperty, value); }
        }

        public long ViewportWidth {
            get { return (long)GetValue(ViewportWidthProperty); }
            set { SetValue(ViewportWidthProperty, value); }
        }

        public long TrackScrollLength {
            get { return (long)GetValue(TrackScrollLengthProperty); }
        }

        public float ViewportZoom {
            get { return (float)GetValue(ViewportZoomProperty); }
        }

        private void WaveView_SizeChanged(object sender, SizeChangedEventArgs e) {
            if (e.WidthChanged && e.PreviousSize.Width > 0) {
                ViewportWidth = (long)(ViewportWidth * e.NewSize.Width / e.PreviousSize.Width);
            }
            UpdateViewportZoom(this);
            //InvalidateVisual();
        }
    }
}
