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
    public partial class WaveView : Control {

        private const int BUFFER_SIZE = 1024;

        private IAudioStream16 audioStream;
        private float[][] buffer;

        public WaveView() {
            // event gets triggered when ActualWidth or ActualHeight change
            SizeChanged += WaveView_SizeChanged;
        }

        public IAudioStream16 AudioStream {
            get { return audioStream; }
            set {
                audioStream = value;
                buffer = AudioUtil.CreateArray<float>(audioStream.Properties.Channels, BUFFER_SIZE);
                TrackLength = audioStream.SampleCount;
            }
        }

        public bool Antialiased {
            get { return ((EdgeMode)GetValue(RenderOptions.EdgeModeProperty)) == EdgeMode.Aliased; }
            set { SetValue(RenderOptions.EdgeModeProperty, !value ? EdgeMode.Aliased : EdgeMode.Unspecified); }
        }

        protected override void OnRender(DrawingContext drawingContext) {
            base.OnRender(drawingContext);
            
            Rect viewport = CalculateViewport();

            if (audioStream != null) {
                long offset = (long)Math.Floor(viewport.Left);
                int width = (int)Math.Ceiling(ActualWidth);

                audioStream.SamplePosition = offset;
                if (audioStream.SamplePosition != offset) {
                    throw new Exception("WaveStream/WavFileReader BlockAlign violation");
                }
                int zoomedSamples = (int)ViewportWidth;

                int channels = audioStream.Properties.Channels;
                double channelHeight = viewport.Height / channels;
                double channelHalfHeight = channelHeight / 2;

                List<Point>[] samples = LoadSamples(zoomedSamples);
                zoomedSamples = samples[0].Count;

                // draw background
                drawingContext.DrawRectangle(WaveformBackground, null,
                    new Rect(0, 0, zoomedSamples * ViewportZoom, ActualHeight));

                // draw waveform guides
                for (int channel = 0; channel < channels; channel++) {
                    // waveform zero-line
                    drawingContext.DrawLine(new Pen(Brushes.LightGray, 1), 
                        new Point(0, channelHeight * channel + channelHalfHeight),
                        new Point(zoomedSamples * ViewportZoom, channelHeight * channel + channelHalfHeight));
                    // waveform spacers
                    if (channel > 0) {
                        drawingContext.DrawLine(new Pen(Brushes.DarkGray, 1),
                            new Point(0, channelHeight * channel),
                            new Point(zoomedSamples * ViewportZoom, channelHeight * channel));
                    }
                }

                // draw waveforms
                Geometry[] waveforms = CreateWaveforms(samples);
                for (int channel = 0; channel < channels; channel++) {
                    if (waveforms[channel].IsFrozen)
                        continue;
                    TransformGroup transformGroup = new TransformGroup();
                    transformGroup.Children.Add(new ScaleTransform(ViewportZoom, channelHalfHeight * -1));
                    transformGroup.Children.Add(new TranslateTransform(0, channelHalfHeight + (channelHalfHeight * channel * 2)));
                    waveforms[channel].Transform = transformGroup;
                    drawingContext.DrawGeometry(null, new Pen(Brushes.CornflowerBlue, 1), waveforms[channel]);

                    // draw sample dots on high zoom factors
                    if (ViewportZoom > 10) {
                        float sampleDotSize = ViewportZoom < 30 ? ViewportZoom / 10 : 3;
                        GeometryGroup geometryGroup = new GeometryGroup();
                        foreach (Point point in samples[channel]) {
                            EllipseGeometry sampleDot = new EllipseGeometry(transformGroup.Transform(point), sampleDotSize, sampleDotSize);
                            geometryGroup.Children.Add(sampleDot);
                        }
                        drawingContext.DrawGeometry(Brushes.RoyalBlue, null, geometryGroup);
                    }
                }
            }

            // DEBUG OUTPUT: VIEWPORT
            String viewportInfo = "Size: " + new Size(ActualWidth, ActualHeight)
                + " / " + "Viewport: " + viewport + " / " + "Zoom: " + ViewportZoom;
            drawingContext.DrawText(
                new FormattedText(viewportInfo, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
                    new Typeface("Tahoma"), 8, Brushes.Black),
                new Point(0, ActualHeight) + new Vector(0, -10));
        }

        private Rect CalculateViewport() {
            return new Rect(ViewportOffset, 0, ActualWidth, ActualHeight);
        }

        private List<Point>[] LoadSamples(int samples) {
            int channels = audioStream.Properties.Channels;
            List<Point>[] samplePoints = AudioUtil.CreateList<Point>(channels, samples);
            int totalSamplesRead = 0;

            while (totalSamplesRead < samples) {
                int samplesRead = audioStream.Read(buffer, BUFFER_SIZE);
                if (samplesRead == 0)
                    break;

                for (int x = 0; x < samplesRead; x++) {
                    for (int channel = 0; channel < channels; channel++) {
                        samplePoints[channel].Add(new Point(totalSamplesRead, buffer[channel][x]));
                    }
                    totalSamplesRead++;
                    if (totalSamplesRead == samples)
                        break;
                }
            }

            return samplePoints;
        }

        private Geometry[] CreateWaveforms(List<Point>[] samplePoints) {
            int channels = samplePoints.Length;
            Geometry[] waveforms = new Geometry[channels];

            if (samplePoints[0].Count < 2) {
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
                    pathFigure.StartPoint = samplePoints[channel][0];
                    pathFigure.Segments.Add(new PolyLineSegment(samplePoints[channel], true)); // first point gets added a second time
                    geometry.Figures.Add(pathFigure);
                    //geometry.Freeze();
                    waveforms[channel] = geometry;
                }
            }
            return waveforms;
        }
    }
}
