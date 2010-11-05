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
    public partial class WaveView : VirtualViewBase {

        private bool debug = false;
        private VisualizingAudioStream16 audioStream;

        public WaveView() {
            // event gets triggered when ActualWidth or ActualHeight change
            SizeChanged += WaveView_SizeChanged;
        }

        public VisualizingAudioStream16 AudioStream {
            get { return audioStream; }
            set {
                audioStream = value;
                TrackLength = audioStream.TimeLength.Ticks;
            }
        }

        public bool Antialiased {
            get { return ((EdgeMode)GetValue(RenderOptions.EdgeModeProperty)) != EdgeMode.Aliased; }
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

                double sampleLength = AudioUtil.CalculateSampleTicks(audioStream.Properties);
                Interval visibleAudioInterval = audioInterval.Intersect(viewportInterval);
                Interval audioToLoadInterval = visibleAudioInterval - TrackOffset;

                // align interval to samples
                Interval audioToLoadIntervalAligned = AudioUtil.AlignToSamples(audioToLoadInterval, audioStream.Properties);
                int samplesToLoad = AudioUtil.CalculateSamples(audioStream.Properties, new TimeSpan(audioToLoadIntervalAligned.Length));

                // calculate drawing measures
                double viewportToDrawingScaleFactor = ActualWidth / ViewportWidth;
                double drawingOffset = ((audioToLoadIntervalAligned.From - audioToLoadInterval.From) + (visibleAudioInterval.From - viewportInterval.From)) * viewportToDrawingScaleFactor;
                double drawingWidth = (samplesToLoad - 1) * sampleLength * viewportToDrawingScaleFactor;

                // load audio samples
                audioStream.TimePosition = new TimeSpan(audioToLoadIntervalAligned.From);
                bool peaks;
                Interval readInterval;
                List<Point>[] samples = audioStream.Read(audioToLoadIntervalAligned, samplesToLoad > drawingWidth ? (int)drawingWidth : samplesToLoad, out readInterval, out peaks);
                int samplesLoaded = peaks ? samples[0].Count / 2 : samples[0].Count;

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

                if (samplesLoaded <= 1) {
                    drawingContext.DrawText(DebugText("SAMPLE WARNING: " + samplesLoaded), new Point(0, 0));
                    return;
                }

                // draw background
                drawingContext.DrawRectangle(WaveformBackground, null, new Rect(drawingOffset, 0, drawingWidth, ActualHeight));
                if (debug) {
                    drawingContext.DrawRectangle(null, new Pen(Brushes.Brown, 4), new Rect(drawingOffset, 0, drawingWidth, ActualHeight));
                }

                GuidelineSet guidelineSet = new GuidelineSet();
                drawingContext.PushGuidelineSet(guidelineSet);

                // draw waveform guides & create drawing guidelines
                int channels = audioStream.Properties.Channels;
                double channelHeight = ActualHeight / channels;
                double channelHalfHeight = channelHeight / 2;
                for (int channel = 0; channel < channels; channel++) {
                    // waveform zero-line
                    guidelineSet.GuidelinesY.Add((channelHeight * channel + channelHalfHeight) + 0.5);
                    drawingContext.DrawLine(new Pen(Brushes.LightGray, 1),
                        new Point(drawingOffset, channelHeight * channel + channelHalfHeight),
                        new Point(drawingOffset + drawingWidth, channelHeight * channel + channelHalfHeight));
                    // waveform spacers
                    if (channel > 0) {
                        guidelineSet.GuidelinesY.Add((channelHeight * channel) + 0.5);
                        drawingContext.DrawLine(new Pen(Brushes.DarkGray, 1),
                            new Point(drawingOffset, channelHeight * channel),
                            new Point(drawingOffset + drawingWidth, channelHeight * channel));
                    }
                }

                // draw waveforms
                Geometry[] audioforms = peaks ? 
                    CreatePeakforms(samples) : 
                    CreateWaveforms(samples);
                for (int channel = 0; channel < channels; channel++) {
                    TransformGroup transformGroup = new TransformGroup();
                    transformGroup.Children.Add(new ScaleTransform(drawingWidth / audioforms[channel].Bounds.Width, channelHalfHeight * -1));
                    transformGroup.Children.Add(new TranslateTransform(drawingOffset, channelHalfHeight + (channelHalfHeight * channel * 2)));
                    audioforms[channel].Transform = transformGroup;
                    drawingContext.DrawGeometry(WaveformFill, new Pen(WaveformLine, 1), audioforms[channel]);

                    if (!peaks) {
                        // draw sample dots on high zoom factors
                        float zoomFactor = (float)(drawingWidth / samplesLoaded);
                        if (zoomFactor > 0.05) {
                            float sampleDotSize = zoomFactor < 30 ? zoomFactor / 10 : 3;
                            GeometryGroup geometryGroup = new GeometryGroup();
                            foreach (Point point in samples[channel]) {
                                EllipseGeometry sampleDot = new EllipseGeometry(transformGroup.Transform(point), sampleDotSize, sampleDotSize);
                                geometryGroup.Children.Add(sampleDot);
                            }
                            drawingContext.DrawGeometry(WaveformSamplePoint, null, geometryGroup);
                        }
                    }
                }

                drawingContext.Pop();

                if (debug) {
                    // DEBUG OUTPUT
                    drawingContext.DrawText(DebugText("visibleAudioInterval: " + visibleAudioInterval + ", audioToLoadInterval: " + audioToLoadInterval + ", audioToLoadIntervalAligned: " + audioToLoadIntervalAligned),
                        new Point(0, ActualHeight) + new Vector(0, -50));
                    drawingContext.DrawText(DebugText("Drawing Offset: " + drawingOffset + ", Width: " + drawingWidth + ", ScalingFactor: " + viewportToDrawingScaleFactor + ", Samples: " + samplesLoaded),
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

        private Geometry[] CreateWaveforms(List<Point>[] samplePoints) {
            int channels = samplePoints.Length;
            Geometry[] waveforms = new Geometry[channels];

            if (samplePoints[0].Count() < 2) {
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

        private Geometry[] CreatePeakforms(List<Point>[] peakLines) {
            int channels = peakLines.Length;
            Geometry[] peakforms = new Geometry[channels];

            for (int channel = 0; channel < channels; channel++) {
                PathGeometry geometry = new PathGeometry();
                PathFigure pathFigure = new PathFigure();
                pathFigure.IsClosed = true;
                pathFigure.IsFilled = true;
                pathFigure.StartPoint = peakLines[channel][0];
                pathFigure.Segments.Add(new PolyLineSegment(peakLines[channel], true)); // first point gets added a second time
                geometry.Figures.Add(pathFigure);
                peakforms[channel] = geometry;
            }

            return peakforms;
        }

        private FormattedText DebugText(string text) {
            return new FormattedText(text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, 
                new Typeface("Tahoma"), 8, Brushes.Black);
        }
    }
}
