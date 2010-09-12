using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using NAudio.Wave;
using System.Globalization;
using System.Diagnostics;

namespace AudioAlign.WaveControls {
    public class WaveView : Control {

        public static readonly DependencyProperty ParentViewportHorizontalOffsetProperty;
        public static readonly DependencyProperty ParentViewportWidthProperty;

        private const int BUFFER_SIZE = 512;
        private const int BYTES_PER_SAMPLE = 2;

        private const int BASE_SCALE = 96;

        static WaveView() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(WaveView), new FrameworkPropertyMetadata(typeof(WaveView)));

            FrameworkPropertyMetadata viewportHorizontalOffsetMetadata = new FrameworkPropertyMetadata() { AffectsRender = true };
            FrameworkPropertyMetadata viewportWidthMetadata = new FrameworkPropertyMetadata() { AffectsRender = true };

            ParentViewportHorizontalOffsetProperty = DependencyProperty.Register("ParentViewportHorizontalOffset", typeof(double), typeof(WaveView), viewportHorizontalOffsetMetadata);
            ParentViewportWidthProperty = DependencyProperty.Register("ParentViewportWidth", typeof(double), typeof(WaveView), viewportWidthMetadata);
        }

        private WaveStream waveStream;
        private byte[] buffer = new byte[BUFFER_SIZE];

        public WaveView() {
            //SetValue(RenderOptions.EdgeModeProperty, EdgeMode.Aliased);
        }

        public WaveStream WaveStream {
            get { return waveStream; }
            set { 
                waveStream = value;
                Width = waveStream.Length / 2;
            }
        }

        public double ParentViewportHorizontalOffset {
            get { return (double)GetValue(ParentViewportHorizontalOffsetProperty); }
            set { SetValue(ParentViewportHorizontalOffsetProperty, value); }
        }

        public double ParentViewportWidth {
            get { return (double)GetValue(ParentViewportWidthProperty); }
            set { SetValue(ParentViewportWidthProperty, value); }
        }

        protected override void OnRender(DrawingContext drawingContext) {
            base.OnRender(drawingContext);
            Rect viewport = CalculateViewport();

            PaintWaveformBackground(viewport, drawingContext);

            if (waveStream != null) {
                long offset = (long)Math.Floor(viewport.Left);
                int width = (int)Math.Ceiling(viewport.Width) + 1;
                drawingContext.DrawGeometry(Brushes.Red, new Pen(Brushes.Black, 1), CreateWaveform(offset, width));
            }

            // DEBUG OUTPUT: VIEWPORT
            drawingContext.DrawText(
                new FormattedText(
                    "Viewport: " + viewport.ToString(),
                    CultureInfo.CurrentUICulture,
                    System.Windows.FlowDirection.LeftToRight,
                    new Typeface("Tahoma"),
                    8,
                    Brushes.Black),
                viewport.BottomLeft + new Vector(0, -10));
        }

        private Rect CalculateViewport() {
            return new Rect(ParentViewportHorizontalOffset, 0, ParentViewportWidth, ActualHeight);
        }

        private void PaintWaveformBackground(Rect viewport, DrawingContext drawingContext) {
            drawingContext.DrawLine(new Pen(Brushes.Gray, 1), new Point(viewport.Left, viewport.Top + viewport.Height / 2), new Point(viewport.Right, viewport.Top + viewport.Height / 2));
            drawingContext.DrawLine(new Pen(Brushes.Red, 3), viewport.TopLeft, viewport.BottomLeft);
        }

        private Geometry CreateWaveform(long offset, int samples) {
            List<Point> linePoints = new List<Point>(samples);
            int samplesRead = 0;
            double height = ActualHeight;
            double horizontalScale = height / ushort.MaxValue;

            waveStream.Position = offset * BYTES_PER_SAMPLE;
            while (samplesRead < samples) {
                int bytesRead = waveStream.Read(buffer, 0, BUFFER_SIZE);
                if(bytesRead == 0) break;

                for (int x = 0; x < bytesRead; x += BYTES_PER_SAMPLE * 2) {
                    short sample = (short)((buffer[x + 1] << 8) | buffer[x + 0]);
                    linePoints.Add(new Point(offset + samplesRead, (height / 2) - (sample * horizontalScale)));
                    samplesRead++;
                    if (samplesRead == samples) break;
                }
            }

            if (samplesRead == 0) {
                throw new NotImplementedException();
            }
            else if (samplesRead == 1) {
                throw new NotImplementedException();
            }
            else {
                PathFigure pathFigure = new PathFigure();
                pathFigure.IsClosed = false;
                pathFigure.IsFilled = false;
                pathFigure.StartPoint = linePoints[0];
                for (int x = 1; x < linePoints.Count; x++) {
                    pathFigure.Segments.Add(new LineSegment(linePoints[x], true));
                }
                PathGeometry geometry = new PathGeometry();
                geometry.Figures.Add(pathFigure);
                return geometry;

                // TODO testen ob diese Methode effizienter ist:
                //PathFigure pathFigure = new PathFigure();
                //pathFigure.Segments.Add(new PolyLineSegment(linePoints, true));
                //return geometry;
            }
        }
    }
}
