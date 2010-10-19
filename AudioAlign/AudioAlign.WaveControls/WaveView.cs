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

        private bool debug = false;
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
                TrackLength = audioStream.TimeLength.Ticks;
            }
        }

        public bool Antialiased {
            get { return ((EdgeMode)GetValue(RenderOptions.EdgeModeProperty)) == EdgeMode.Aliased; }
            set { SetValue(RenderOptions.EdgeModeProperty, !value ? EdgeMode.Aliased : EdgeMode.Unspecified); }
        }

        public bool DebugMode {
            get { return debug; }
            set { debug = value; InvalidateVisual(); }
        }

        protected override void OnRender(DrawingContext drawingContext) {
            base.OnRender(drawingContext);

            if (audioStream != null) {
                Interval audioInterval = new Interval(TrackOffset, TrackOffset + audioStream.TimeLength.Ticks);
                Interval viewportInterval = new Interval(ViewportOffset, ViewportOffset + ViewportWidth);

                if (!audioInterval.Intersects(viewportInterval)) {
                    Debug.WriteLine("nothing to draw!");
                    return;
                }

                float sampleLength = AudioUtil.CalculateSampleTicks(audioStream.Properties);
                Interval visibleAudioInterval = audioInterval.Intersect(viewportInterval);
                Interval audioToLoadInterval = visibleAudioInterval - TrackOffset;

                // align interval to samples
                Interval audioToLoadIntervalAligned = new Interval(
                    (long)(audioToLoadInterval.From - ((double)audioToLoadInterval.From % sampleLength)),
                    (long)(audioToLoadInterval.To + ((double)audioToLoadInterval.To % sampleLength)));

                // load audio samples
                audioStream.TimePosition = new TimeSpan(audioToLoadIntervalAligned.From);
                List<Point>[] samples = LoadSamples(AudioUtil.CalculateSamples(audioStream.Properties, new TimeSpan(audioToLoadIntervalAligned.Length)) + 1);
                int sampleCount = samples[0].Count;

                // calculate drawing measures
                double viewportToDrawingScaleFactor = ActualWidth / ViewportWidth;
                double drawingOffset = ((audioToLoadIntervalAligned.From - audioToLoadInterval.From) + (visibleAudioInterval.From - viewportInterval.From)) * viewportToDrawingScaleFactor;
                double drawingWidth = (sampleCount - 1) * sampleLength * viewportToDrawingScaleFactor;

                //Debug.WriteLine("sampleCount:                  " + sampleCount);
                //Debug.WriteLine("sampleLength:                 " + sampleLength);
                //Debug.WriteLine("viewportToDrawingScaleFactor: " + viewportToDrawingScaleFactor);

                //Debug.WriteLine("audioInterval:                " + audioInterval);
                //Debug.WriteLine("viewportInterval:             " + viewportInterval);
                //Debug.WriteLine("visibleAudioInterval:         " + visibleAudioInterval);
                //Debug.WriteLine("audioToLoadInterval:          " + audioToLoadInterval + " (" + audioToLoadInterval.Length / sampleLength + ")");
                //Debug.WriteLine("audioToLoadIntervalAligned:   " + audioToLoadIntervalAligned + " (" + audioToLoadIntervalAligned.Length / sampleLength + ")");

                //Debug.WriteLine((audioToLoadIntervalAligned.From - visibleAudioInterval.From) + " * " + viewportToDrawingScaleFactor + " = " + drawingOffset);
                //Debug.WriteLine((visibleAudioInterval.From - ViewportOffset) + " * " + viewportToDrawingScaleFactor + " = " + drawingOffset);
                //Debug.WriteLine(sampleCount + " samples, drawingWidth: " + audioToLoadIntervalAligned.Length + " * " + viewportToDrawingScaleFactor + " = " + drawingWidth);

                if (sampleCount <= 1) {
                    drawingContext.DrawText(DebugText("SAMPLE WARNING: " + sampleCount), new Point(0, 0));
                    return;
                }

                // draw background
                drawingContext.DrawRectangle(WaveformBackground, null, new Rect(drawingOffset, 0, drawingWidth, ActualHeight));
                if (debug) {
                    drawingContext.DrawRectangle(null, new Pen(Brushes.Brown, 4), new Rect(drawingOffset, 0, drawingWidth, ActualHeight));
                }

                // draw waveform guides
                int channels = audioStream.Properties.Channels;
                double channelHeight = ActualHeight / channels;
                double channelHalfHeight = channelHeight / 2;
                for (int channel = 0; channel < channels; channel++) {
                    // waveform zero-line
                    drawingContext.DrawLine(new Pen(Brushes.LightGray, 1),
                        new Point(drawingOffset, channelHeight * channel + channelHalfHeight),
                        new Point(drawingOffset + drawingWidth, channelHeight * channel + channelHalfHeight));
                    // waveform spacers
                    if (channel > 0) {
                        drawingContext.DrawLine(new Pen(Brushes.DarkGray, 1),
                            new Point(drawingOffset, channelHeight * channel),
                            new Point(drawingOffset + drawingWidth, channelHeight * channel));
                    }
                }

                if (sampleCount > drawingWidth * 2) {
                    Geometry[] peakforms = CreatePeakforms(SamplesToPeaks(samples, (int)drawingWidth));
                    for (int channel = 0; channel < channels; channel++) {
                        TransformGroup transformGroup = new TransformGroup();
                        transformGroup.Children.Add(new ScaleTransform(drawingWidth / peakforms[channel].Bounds.Width, channelHalfHeight * -1));
                        transformGroup.Children.Add(new TranslateTransform(drawingOffset, channelHalfHeight + (channelHalfHeight * channel * 2)));
                        peakforms[channel].Transform = transformGroup;
                        drawingContext.DrawGeometry(Brushes.LightBlue, new Pen(Brushes.CornflowerBlue, 1), peakforms[channel]);
                    }
                }
                else {
                    // draw waveforms
                    Geometry[] waveforms = CreateWaveforms(samples);
                    for (int channel = 0; channel < channels; channel++) {
                        TransformGroup transformGroup = new TransformGroup();
                        transformGroup.Children.Add(new ScaleTransform(drawingWidth / waveforms[channel].Bounds.Width, channelHalfHeight * -1));
                        transformGroup.Children.Add(new TranslateTransform(drawingOffset, channelHalfHeight + (channelHalfHeight * channel * 2)));
                        waveforms[channel].Transform = transformGroup;
                        drawingContext.DrawGeometry(null, new Pen(Brushes.CornflowerBlue, 1), waveforms[channel]);

                        // draw sample dots on high zoom factors
                        float zoomFactor = (float)(ActualWidth / sampleCount);
                        if (zoomFactor > 2) {
                            float sampleDotSize = zoomFactor < 30 ? zoomFactor / 10 : 3;
                            GeometryGroup geometryGroup = new GeometryGroup();
                            foreach (Point point in samples[channel]) {
                                EllipseGeometry sampleDot = new EllipseGeometry(transformGroup.Transform(point), sampleDotSize, sampleDotSize);
                                geometryGroup.Children.Add(sampleDot);
                            }
                            drawingContext.DrawGeometry(Brushes.RoyalBlue, null, geometryGroup);
                        }
                    }
                }

                if (debug) {
                    // DEBUG OUTPUT
                    drawingContext.DrawText(DebugText("Drawing Offset: " + drawingOffset + ", Width: " + drawingWidth + ", ScalingFactor: " + viewportToDrawingScaleFactor + ", Samples: " + sampleCount),
                        new Point(0, ActualHeight) + new Vector(0, -40));
                }
            }

            if (debug) {
                // DEBUG OUTPUT
                drawingContext.DrawText(DebugText("ActualWidth: " + ActualWidth + ", ActualHeight: " + ActualHeight),
                    new Point(0, ActualHeight) + new Vector(0, -30));
                drawingContext.DrawText(DebugText("TrackLength: " + TrackLength + ", TrackOffset: " + TrackOffset),
                    new Point(0, ActualHeight) + new Vector(0, -20));
                drawingContext.DrawText(DebugText("ViewportOffset: " + ViewportOffset + ", ViewportWidth: " + ViewportWidth),
                    new Point(0, ActualHeight) + new Vector(0, -10));
            }
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

        private Geometry[] CreatePeakforms(List<PointPair>[] peakLines) {
            int channels = peakLines.Length;
            Geometry[] peakforms = new Geometry[channels];

            for (int channel = 0; channel < channels; channel++) {
                List<Point> peakPoints = new List<Point>(peakLines.Length * 2);
                for (int x = 0; x < peakLines[channel].Count; x++) {
                    peakPoints.Add(peakLines[channel][x].Point1);
                }
                for (int x = peakLines[channel].Count - 1; x >= 0; x--) {
                    peakPoints.Add(peakLines[channel][x].Point2);
                }

                PathGeometry geometry = new PathGeometry();
                PathFigure pathFigure = new PathFigure();
                pathFigure.IsClosed = true;
                pathFigure.IsFilled = true;
                pathFigure.StartPoint = peakPoints[0];
                pathFigure.Segments.Add(new PolyLineSegment(peakPoints, true)); // first point gets added a second time
                geometry.Figures.Add(pathFigure);
                peakforms[channel] = geometry;

                //GeometryGroup geometry = new GeometryGroup();
                //foreach (PointPair pp in peakLines[channel]) {
                //    geometry.Children.Add(new LineGeometry(pp.Point1, pp.Point2));
                //}
                //peakforms[channel] = geometry;
            }

            return peakforms;
        }

        private List<PointPair>[] SamplesToPeaks(List<Point>[] samplePoints, int width) {
            int channels = samplePoints.Length;
            List<PointPair>[] peakLines = AudioUtil.CreateList<PointPair>(channels, width);

            if (width == 0) {
                return peakLines;
            }

            double samplesPerPeak = samplePoints[0].Count / (double)width;
            int samplesPerPeakCeiling = (int)Math.Ceiling(samplesPerPeak);
            bool samplesPerPeakIsInteger = samplesPerPeak == samplesPerPeakCeiling; // is the number of samples per peak an integer or a floating point number?
            List<double>[] minMax = AudioUtil.CreateList<double>(channels, samplesPerPeakCeiling);

            int minMaxCount = 0;
            int numPeaks = 0;
            for (int x = 0; x < samplePoints[0].Count; x++) {
                for (int channel = 0; channel < channels; channel++) {
                    minMax[channel].Add(samplePoints[channel][x].Y);
                }
                minMaxCount++;
                if (minMaxCount % samplesPerPeakCeiling == 0) {
                    for (int channel = 0; channel < channels; channel++) {
                        double min = minMax[channel].Min();
                        double max = minMax[channel].Max();
                        peakLines[channel].Add(new PointPair(numPeaks, max, numPeaks, min));
                        minMax[channel].Clear();
                    }
                    numPeaks++;
                    if (!samplesPerPeakIsInteger) {
                        // in case of a floating point sample per peak ratio, the sample counts to the actual
                        // and the next peak
                        // go one step back in the cycle and continue with next peak
                        x--;
                    }
                }
            }

            return peakLines;
        }

        private FormattedText DebugText(string text) {
            return new FormattedText(text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, 
                new Typeface("Tahoma"), 8, Brushes.Black);
        }
    }

    internal struct PointPair {

        private Point p1, p2;

        public PointPair(double x1, double y1, double x2, double y2) {
            p1 = new Point(x1, y1);
            p2 = new Point(x2, y2);
        }

        public PointPair(Point p1, Point p2) {
            this.p1 = p1;
            this.p2 = p2;
        }

        public Point Point1 {
            get { return p1; }
            set { p1 = value; }
        }

        public Point Point2 {
            get { return p2; }
            set { p2 = value; }
        }
    }
}
