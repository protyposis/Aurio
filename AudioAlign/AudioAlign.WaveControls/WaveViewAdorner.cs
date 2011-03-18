using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Documents;
using System.Windows;
using System.Windows.Media;
using AudioAlign.Audio.Project;
using System.Windows.Data;
using AudioAlign.Audio;

namespace AudioAlign.WaveControls {
    class WaveViewAdorner : Adorner {

        public static readonly DependencyProperty VirtualViewportOffsetProperty =
            VirtualViewBase.VirtualViewportOffsetProperty.AddOwner(typeof(WaveViewAdorner),
            new FrameworkPropertyMetadata() { Inherits = true, AffectsRender = true });

        public static readonly DependencyProperty VirtualViewportWidthProperty =
            VirtualViewBase.VirtualViewportWidthProperty.AddOwner(typeof(WaveViewAdorner),
            new FrameworkPropertyMetadata() { Inherits = true, AffectsRender = true });

        public static readonly DependencyProperty TrackLengthProperty = 
            WaveView.TrackLengthProperty.AddOwner(typeof(WaveViewAdorner),
            new FrameworkPropertyMetadata { Inherits = true, AffectsRender = true });

        public static readonly DependencyProperty TrackOffsetProperty = 
            WaveView.TrackOffsetProperty.AddOwner(typeof(WaveViewAdorner),
            new FrameworkPropertyMetadata { Inherits = true, AffectsRender = true });

        private WaveView adornedWaveView;
        private List<TimeSpan> markers;
        private MultiTrackConnectionAdorner connectionAdorner;

        public WaveViewAdorner(WaveView adornedWaveView)
            : base(adornedWaveView) {
            this.adornedWaveView = adornedWaveView;

            Binding trackLengthBinding = new Binding("TrackLength") { Source = adornedWaveView };
            BindingOperations.SetBinding(this, TrackLengthProperty, trackLengthBinding);

            Binding trackOffsetBinding = new Binding("TrackOffset") { Source = adornedWaveView };
            BindingOperations.SetBinding(this, TrackOffsetProperty, trackOffsetBinding);

            markers = new List<TimeSpan>() { new TimeSpan(0, 0, 1), new TimeSpan(0, 1, 0) };
            double sampleLength = AudioUtil.CalculateSampleTicks(adornedWaveView.AudioTrack.Properties);
            for (int i = 0; i < 50; i++) {
                markers.Add(new TimeSpan((long)Math.Round(sampleLength * i)));
            }
        }

        public WaveViewAdorner(WaveView adornedWaveView, MultiTrackConnectionAdorner connectionAdorner)
            : this(adornedWaveView) {
                this.connectionAdorner = connectionAdorner;
        }

        protected override void OnRender(DrawingContext drawingContext) {
            Rect adornedElementRect = new Rect(this.AdornedElement.RenderSize);

            SolidColorBrush renderBrush = new SolidColorBrush(Colors.Green);
            renderBrush.Opacity = 0.2;
            Pen renderPen = new Pen(new SolidColorBrush(Colors.Navy), 1.5);
            double renderRadius = 5.0;

            drawingContext.DrawEllipse(renderBrush, renderPen, adornedElementRect.TopLeft, renderRadius, renderRadius);
            drawingContext.DrawEllipse(renderBrush, renderPen, adornedElementRect.TopRight, renderRadius, renderRadius);
            drawingContext.DrawEllipse(renderBrush, renderPen, adornedElementRect.BottomLeft, renderRadius, renderRadius);
            drawingContext.DrawEllipse(renderBrush, renderPen, adornedElementRect.BottomRight, renderRadius, renderRadius);


            AudioTrack audioTrack = adornedWaveView.AudioTrack;
            double trackStartOffset = adornedWaveView.VirtualToPhysicalIntervalOffset(audioTrack.Offset.Ticks) - 0.5d;
            double trackEndOffset = adornedWaveView.VirtualToPhysicalIntervalOffset(audioTrack.Offset.Ticks + audioTrack.Length.Ticks) - 0.5d;

            drawingContext.DrawLine(new Pen(Brushes.Red, 1d), new Point(trackStartOffset, 0), new Point(trackStartOffset, adornedElementRect.Height));
            drawingContext.DrawLine(new Pen(Brushes.Green, 1d), new Point(trackEndOffset, 0), new Point(trackEndOffset, adornedElementRect.Height));

            foreach (TimeSpan marker in markers) {
                double offset = adornedWaveView.VirtualToPhysicalIntervalOffset(audioTrack.Offset.Ticks + marker.Ticks) - 0.5d;
                drawingContext.DrawLine(new Pen(Brushes.Orange, 1d), new Point(offset, 0), new Point(offset, adornedElementRect.Height));
            }

            if (connectionAdorner != null) {
                connectionAdorner.InvalidateVisual();
            }
        }
    }
}
