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
using System.Globalization;
using System.Diagnostics;
using AudioAlign.Audio;

namespace AudioAlign.WaveControls {
    public class WaveView : Control {

        public static readonly DependencyProperty VirtualHorizontalOffsetProperty;
        public static readonly DependencyProperty VirtualWidthProperty;
        public static readonly DependencyProperty WaveformBackgroundProperty;

        private const int BUFFER_SIZE = 1024;

        static WaveView() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(WaveView), new FrameworkPropertyMetadata(typeof(WaveView)));

            FrameworkPropertyMetadata virtualHorizontalOffsetMetadata = new FrameworkPropertyMetadata() { AffectsRender = true };
            FrameworkPropertyMetadata virtualWidthMetadata = new FrameworkPropertyMetadata() { AffectsRender = true };
            FrameworkPropertyMetadata waveformBackgroundMetadata = new FrameworkPropertyMetadata() { AffectsRender = true };

            VirtualHorizontalOffsetProperty = DependencyProperty.Register("VirtualHorizontalOffset", typeof(double), typeof(WaveView), virtualHorizontalOffsetMetadata);
            VirtualWidthProperty = DependencyProperty.Register("VirtualWidth", typeof(double), typeof(WaveView), virtualWidthMetadata);
            WaveformBackgroundProperty = DependencyProperty.Register("WaveformBackground", typeof(Brush), typeof(WaveView), waveformBackgroundMetadata);
        }

        private IAudioStream16 audioStream;
        private float[][] buffer;

        public WaveView() {
            //SetValue(RenderOptions.EdgeModeProperty, EdgeMode.Aliased);
        }

        public IAudioStream16 AudioStream {
            get { return audioStream; }
            set {
                audioStream = value;
                buffer = new float[audioStream.Properties.Channels][];
                for (int channel = 0; channel < audioStream.Properties.Channels; channel++) {
                    buffer[channel] = new float[BUFFER_SIZE];
                }
                VirtualWidth = audioStream.SampleCount;
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

        public Brush WaveformBackground {
            get { return (Brush)GetValue(WaveformBackgroundProperty); }
            set { SetValue(WaveformBackgroundProperty, value); }
        }

        protected override void OnRender(DrawingContext drawingContext) {
            base.OnRender(drawingContext);
            // draw background
            //drawingContext.DrawRectangle(Background, new Pen(), new Rect(0, 0, ActualWidth, ActualHeight));
            
            Rect viewport = CalculateViewport();

            if (audioStream != null) {
                long offset = (long)Math.Floor(viewport.Left);
                int width = (int)Math.Ceiling(ActualWidth) + 1; // +1 to avoid small gap on right border of graph (draws the graph over the right border)

                audioStream.SamplePosition = offset;
                if (audioStream.SamplePosition != offset) {
                    throw new Exception("WaveStream/WavFileReader BlockAlign violation");
                }
                long remainingSamples = (audioStream.SampleCount - audioStream.SamplePosition);
                width = remainingSamples < width ? (int)remainingSamples : width;

                drawingContext.DrawRectangle(WaveformBackground, null, new Rect(0, 0, width, ActualHeight));
                PaintWaveformBackground(viewport, drawingContext);

                Geometry waveForm = CreateWaveform(width);
                //waveForm.Transform = new ScaleTransform(2, 1);
                drawingContext.DrawGeometry(null, new Pen(Brushes.Black, 1), waveForm);
            }
            else {
                PaintWaveformBackground(viewport, drawingContext);
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

        private Geometry CreateWaveform(int samples) {
            int channels = audioStream.Properties.Channels;
            List<Point>[] linePoints = new List<Point>[channels];
            for (int channel = 0; channel < channels; channel++) {
                linePoints[channel] = new List<Point>(samples);
            }
            int totalSamplesRead = 0;
            double height = ActualHeight;
            double horizontalScale = height / ushort.MaxValue;

            while (totalSamplesRead < samples) {
                int samplesRead = audioStream.Read(buffer, BUFFER_SIZE);
                if (samplesRead == 0)
                    break;

                for (int x = 0; x < samplesRead; x++) {
                    for (int channel = 0; channel < channels; channel++) {
                        linePoints[channel].Add(new Point(totalSamplesRead, (height / 2) - (buffer[channel][x] * (height / 2))));
                    }
                    totalSamplesRead++;
                    if (totalSamplesRead == samples)
                        break;
                }
            }

            if (totalSamplesRead == 0) {
                return new PathGeometry();
            }
            else if (totalSamplesRead == 1) {
                // TODO auch vorheriges sample lesen um im Fall von einem angezeigten sample trotzdem eine Linie zw. 2 samples zeichen zu können
                return new PathGeometry();
            }
            else {
                PathGeometry geometry = new PathGeometry();
                for (int channel = 0; channel < channels; channel++) {
                    PathFigure pathFigure = new PathFigure();
                    pathFigure.IsClosed = false;
                    pathFigure.IsFilled = false;
                    pathFigure.StartPoint = linePoints[channel][0];
                    for (int x = 1; x < linePoints[channel].Count; x++) {
                        pathFigure.Segments.Add(new LineSegment(linePoints[channel][x], true));
                    }
                    geometry.Figures.Add(pathFigure);
                }
                geometry.Freeze();
                return geometry;

                // TODO testen ob diese Methode effizienter ist:
                //PathFigure pathFigure = new PathFigure();
                //pathFigure.Segments.Add(new PolyLineSegment(linePoints, true));
                //return geometry;
            }
        }
    }
}
