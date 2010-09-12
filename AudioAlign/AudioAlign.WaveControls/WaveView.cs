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

        public static readonly DependencyProperty VirtualHorizontalOffsetProperty;
        public static readonly DependencyProperty VirtualWidthProperty;

        private const int BUFFER_SIZE = 512;
        private const int BYTES_PER_SAMPLE = 2;

        private const int BASE_SCALE = 96;

        static WaveView() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(WaveView), new FrameworkPropertyMetadata(typeof(WaveView)));

            FrameworkPropertyMetadata virtualHorizontalOffsetMetadata = new FrameworkPropertyMetadata() { AffectsRender = true };
            FrameworkPropertyMetadata virtualWidthMetadata = new FrameworkPropertyMetadata() { AffectsRender = true };

            VirtualHorizontalOffsetProperty = DependencyProperty.Register("VirtualHorizontalOffset", typeof(double), typeof(WaveView), virtualHorizontalOffsetMetadata);
            VirtualWidthProperty = DependencyProperty.Register("VirtualWidth", typeof(double), typeof(WaveView), virtualWidthMetadata);
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
                VirtualWidth = waveStream.Length / 2;
            }
        }

        public double VirtualHorizontalOffset {
            get { return (double)GetValue(VirtualHorizontalOffsetProperty); }
            set { SetValue(VirtualHorizontalOffsetProperty, value); }
        }

        public double VirtualWidth {
            get { return (double)GetValue(VirtualWidthProperty); }
            set { SetValue(VirtualWidthProperty, value); }
        }

        protected override void OnRender(DrawingContext drawingContext) {
            base.OnRender(drawingContext);
            Rect viewport = CalculateViewport();

            PaintWaveformBackground(viewport, drawingContext);

            if (waveStream != null) {
                long offset = (long)Math.Floor(viewport.Left);
                int width = (int)Math.Ceiling(ActualWidth) + 1; // +1 to avoid small gap on right border of graph (draws the graph over the right border)
                drawingContext.DrawGeometry(Brushes.Red, new Pen(Brushes.Black, 1), CreateWaveform(offset, width));
            }

            // DEBUG OUTPUT: VIEWPORT
            String viewportInfo = "Size: " + new Size(ActualWidth, ActualHeight) + " / " + "Viewport: " + viewport.ToString();
            drawingContext.DrawText(
                new FormattedText(
                    viewportInfo,
                    CultureInfo.CurrentUICulture,
                    System.Windows.FlowDirection.LeftToRight,
                    new Typeface("Tahoma"),
                    8,
                    Brushes.Black),
                new Point(0, ActualHeight) + new Vector(0, -10));
        }

        private Rect CalculateViewport() {
            return new Rect(VirtualHorizontalOffset, 0, ActualWidth, ActualHeight);
        }

        private void PaintWaveformBackground(Rect viewport, DrawingContext drawingContext) {
            drawingContext.DrawLine(new Pen(Brushes.Gray, 1), new Point(0, viewport.Height / 2), new Point(ActualWidth, viewport.Height / 2));
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
                    linePoints.Add(new Point(samplesRead, (height / 2) - (sample * horizontalScale)));
                    samplesRead++;
                    if (samplesRead == samples) break;
                }
            }

            if (samplesRead == 0) {
                return new PathGeometry();
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
