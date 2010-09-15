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
        public static readonly DependencyProperty ZoomFactorProperty;

        private const int BUFFER_SIZE = 1024;

        static WaveView() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(WaveView), new FrameworkPropertyMetadata(typeof(WaveView)));

            FrameworkPropertyMetadata virtualHorizontalOffsetMetadata = new FrameworkPropertyMetadata() { AffectsRender = true };
            FrameworkPropertyMetadata virtualWidthMetadata = new FrameworkPropertyMetadata() { AffectsRender = true };
            FrameworkPropertyMetadata waveformBackgroundMetadata = new FrameworkPropertyMetadata() { AffectsRender = true };
            FrameworkPropertyMetadata zoomFactorMetadata = new FrameworkPropertyMetadata() { AffectsRender = true, DefaultValue = 1.0f };

            VirtualHorizontalOffsetProperty = DependencyProperty.Register("VirtualHorizontalOffset", typeof(double), typeof(WaveView), virtualHorizontalOffsetMetadata);
            VirtualWidthProperty = DependencyProperty.Register("VirtualWidth", typeof(double), typeof(WaveView), virtualWidthMetadata);
            WaveformBackgroundProperty = DependencyProperty.Register("WaveformBackground", typeof(Brush), typeof(WaveView), waveformBackgroundMetadata);
            ZoomFactorProperty = DependencyProperty.Register("ZoomFactor", typeof(float), typeof(WaveView), zoomFactorMetadata);
        }

        private IAudioStream16 audioStream;
        private float[][] buffer;

        public WaveView() {
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

        public bool Antialiased {
            get { return ((EdgeMode)GetValue(RenderOptions.EdgeModeProperty)) == EdgeMode.Aliased; }
            set { SetValue(RenderOptions.EdgeModeProperty, !value ? EdgeMode.Aliased : EdgeMode.Unspecified); }
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

        public float ZoomFactor {
            get { return (float)GetValue(ZoomFactorProperty); }
            set { SetValue(ZoomFactorProperty, value); }
        }

        protected override void OnRender(DrawingContext drawingContext) {
            base.OnRender(drawingContext);
            
            Rect viewport = CalculateViewport();

            if (audioStream != null) {
                long offset = (long)Math.Floor(viewport.Left);
                int width = (int)Math.Ceiling(ActualWidth); // +1 to avoid small gap on right border of graph (draws the graph over the right border)

                audioStream.SamplePosition = offset;
                if (audioStream.SamplePosition != offset) {
                    throw new Exception("WaveStream/WavFileReader BlockAlign violation");
                }
                long remainingSamples = (audioStream.SampleCount - audioStream.SamplePosition);
                width = remainingSamples < width ? (int)remainingSamples : width;

                int channels = audioStream.Properties.Channels;
                double channelHeight = viewport.Height / channels;
                double channelHalfHeight = channelHeight / 2;

                // draw background
                drawingContext.DrawRectangle(WaveformBackground, null, 
                    new Rect(0, 0, width, ActualHeight));

                // draw waveform guides
                for (int channel = 0; channel < channels; channel++) {
                    // waveform zero-line
                    drawingContext.DrawLine(new Pen(Brushes.LightGray, 1), 
                        new Point(0, channelHeight * channel + channelHalfHeight), 
                        new Point(width, channelHeight * channel + channelHalfHeight));
                    // waveform spacers
                    if (channel > 0) {
                        drawingContext.DrawLine(new Pen(Brushes.DarkGray, 1),
                            new Point(0, channelHeight * channel),
                            new Point(width, channelHeight * channel));
                    }
                }

                // draw waveforms
                int zoomedSamples = (int)Math.Ceiling(width / ZoomFactor) + 1;
                Geometry[] waveforms = CreateWaveforms(zoomedSamples);
                for (int channel = 0; channel < channels; channel++) {
                    if (waveforms[channel].IsFrozen)
                        continue;
                    TransformGroup transformGroup = new TransformGroup();
                    transformGroup.Children.Add(new ScaleTransform(ZoomFactor, channelHalfHeight * -1));
                    transformGroup.Children.Add(new TranslateTransform(0, channelHalfHeight + (channelHalfHeight * channel * 2)));
                    waveforms[channel].Transform = transformGroup;
                    drawingContext.DrawGeometry(null, new Pen(Brushes.CornflowerBlue, 1), waveforms[channel]);

                    // draw sample dots on high zoom factors
                    if (ZoomFactor > 10) {
                        float sampleDotSize = ZoomFactor < 30 ? ZoomFactor / 10 : 3;
                        PointCollection pointCollection = ((waveforms[channel] as PathGeometry)
                            .Figures[0].Segments[0] as PolyLineSegment).Points;
                        GeometryGroup geometryGroup = new GeometryGroup();
                        foreach (Point point in pointCollection) {
                            EllipseGeometry sampleDot = new EllipseGeometry(transformGroup.Transform(point), sampleDotSize, sampleDotSize);
                            geometryGroup.Children.Add(sampleDot);
                        }
                        drawingContext.DrawGeometry(Brushes.RoyalBlue, null, geometryGroup);
                    }
                }
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

        private Geometry[] CreateWaveforms(int samples) {
            int channels = audioStream.Properties.Channels;
            Geometry[] waveforms = new Geometry[channels];
            List<Point>[] linePoints = new List<Point>[channels];
            for (int channel = 0; channel < channels; channel++) {
                linePoints[channel] = new List<Point>(samples);
            }
            int totalSamplesRead = 0;

            while (totalSamplesRead < samples) {
                int samplesRead = audioStream.Read(buffer, BUFFER_SIZE);
                if (samplesRead == 0)
                    break;

                for (int x = 0; x < samplesRead; x++) {
                    for (int channel = 0; channel < channels; channel++) {
                        linePoints[channel].Add(new Point(totalSamplesRead, buffer[channel][x]));
                    }
                    totalSamplesRead++;
                    if (totalSamplesRead == samples)
                        break;
                }
            }

            if (totalSamplesRead < 2) {
                for (int channel = 0; channel < channels; channel++) {
                    waveforms[channel] = Geometry.Empty;
                }
            }
            else {
                for (int channel = 0; channel < channels; channel++) {
                    PathGeometry geometry = new PathGeometry();
                    PathFigure pathFigure = new PathFigure();
                    pathFigure.IsClosed = false;
                    pathFigure.IsFilled = false;
                    pathFigure.StartPoint = linePoints[channel][0];
                    pathFigure.Segments.Add(new PolyLineSegment(linePoints[channel], true)); // first point gets added a second time
                    geometry.Figures.Add(pathFigure);
                    //geometry.Freeze();
                    waveforms[channel] = geometry;
                }
            }
            return waveforms;
        }
    }
}
