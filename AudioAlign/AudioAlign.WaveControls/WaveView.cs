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
using AudioAlign.Audio.Streams;

namespace AudioAlign.WaveControls {
    public partial class WaveView : VirtualViewBase {

        private VisualizingStream audioStream;

        // variables used for mouse dragging
        private bool dragging = false;
        private Point previousMousePosition;

        public WaveView() {
            // event gets triggered when ActualWidth or ActualHeight change
            SizeChanged += WaveView_SizeChanged;
        }

        public bool Antialiased {
            get { return ((EdgeMode)GetValue(RenderOptions.EdgeModeProperty)) != EdgeMode.Aliased; }
            set { SetValue(RenderOptions.EdgeModeProperty, !value ? EdgeMode.Aliased : EdgeMode.Unspecified); }
        }

        protected override void OnRender(DrawingContext drawingContext) {
            base.OnRender(drawingContext);
            bool debug = DebugOutput;

            if (audioStream != null) {
                Interval audioInterval = new Interval(TrackOffset, TrackOffset + audioStream.TimeLength.Ticks);
                Interval viewportInterval = VirtualViewportInterval;

                if (!audioInterval.Intersects(viewportInterval)) {
                    // audio track is outside the viewport
                    return;
                }

                Interval visibleAudioInterval = audioInterval.Intersect(viewportInterval);
                Interval audioToLoadInterval = visibleAudioInterval - TrackOffset;

                // align interval to samples
                Interval audioToLoadIntervalAligned = AudioUtil.AlignToSamples(audioToLoadInterval, audioStream.Properties);
                int samplesToLoad = AudioUtil.CalculateSamples(audioStream.Properties, new TimeSpan(audioToLoadIntervalAligned.Length)) + 1;
                double sampleLength = AudioUtil.CalculateSampleTicks(audioStream.Properties);

                // calculate drawing measures
                double viewportToDrawingScaleFactor = ActualWidth / VirtualViewportWidth;
                int drawingOffsetAligned = (int)((-(audioToLoadInterval.From - audioToLoadIntervalAligned.From)
                    + (visibleAudioInterval.From - viewportInterval.From)) * viewportToDrawingScaleFactor);
                int drawingWidthAligned = (int)((samplesToLoad - 1) * sampleLength * viewportToDrawingScaleFactor);
                int drawingOffset = (int)((visibleAudioInterval.From - viewportInterval.From) * viewportToDrawingScaleFactor);

                if (visibleAudioInterval.Length < sampleLength) {
                    drawingContext.DrawText(DebugText("VISIBLE INTERVAL WARNING: " + visibleAudioInterval.Length + " < SAMPLE LENGTH " + sampleLength), new Point(0, 0));
                    return;
                }

                if (drawingWidthAligned < 1) {
                    // the visible width of the track is too narrow to be drawn/visible
                    return;
                }

                // load audio samples
                DateTime beforeLoading = DateTime.Now;
                float[][] samples = null;
                int sampleCount = 0;
                if (RenderMode != WaveViewRenderMode.None) {
                    bool peaks = samplesToLoad > drawingWidthAligned;
                    // TODO don't recreate the array every time -> resize on demand
                    samples = AudioUtil.CreateArray<float>(audioStream.Properties.Channels, drawingWidthAligned * 2);
                    audioStream.TimePosition = new TimeSpan(audioToLoadIntervalAligned.From);

                    if (peaks) {
                        sampleCount = audioStream.ReadPeaks(samples, samplesToLoad, drawingWidthAligned);
                    }
                    else {
                        sampleCount = audioStream.ReadSamples(samples, samplesToLoad);
                    }

                    if (sampleCount <= 1) {
                        drawingContext.DrawText(DebugText("SAMPLE WARNING: " + sampleCount), new Point(0, 0));
                        return;
                    }
                }
                DateTime afterLoading = DateTime.Now;

                DateTime beforeDrawing = DateTime.Now;

                // draw background
                drawingContext.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, ActualWidth, ActualHeight));
                drawingContext.DrawRectangle(WaveformBackground, null, new Rect(drawingOffsetAligned, 0, drawingWidthAligned, ActualHeight));
                if (debug) {
                    drawingContext.DrawRectangle(null, new Pen(Brushes.Brown, 4), new Rect(drawingOffsetAligned, 0, drawingWidthAligned, ActualHeight));
                }

                // draw waveform guides & create drawing guidelines
                GuidelineSet guidelineSet = new GuidelineSet();
                drawingContext.PushGuidelineSet(guidelineSet);
                int channels = audioStream.Properties.Channels;
                double channelHeight = ActualHeight / channels;
                double channelHalfHeight = channelHeight / 2;
                for (int channel = 0; channel < channels; channel++) {
                    // waveform zero-line
                    guidelineSet.GuidelinesY.Add((channelHeight * channel + channelHalfHeight) + 0.5);
                    drawingContext.DrawLine(new Pen(Brushes.LightGray, 1),
                        new Point(drawingOffsetAligned, channelHeight * channel + channelHalfHeight),
                        new Point(drawingOffsetAligned + drawingWidthAligned, channelHeight * channel + channelHalfHeight));
                    // waveform spacers
                    if (channel > 0) {
                        guidelineSet.GuidelinesY.Add((channelHeight * channel) + 0.5);
                        drawingContext.DrawLine(new Pen(Brushes.DarkGray, 1),
                            new Point(drawingOffsetAligned, channelHeight * channel),
                            new Point(drawingOffsetAligned + drawingWidthAligned, channelHeight * channel));
                    }
                }
                drawingContext.Pop();

                // draw waveforms
                if (channelHeight >= 1) {
                    IWaveformRenderer renderer = null;
                    switch (RenderMode) {
                        case WaveViewRenderMode.None:
                            renderer = null;
                            break;
                        case WaveViewRenderMode.Bitmap:
                            renderer = new WaveformBitmapRenderer() { WaveformLine = WaveformLine as SolidColorBrush };
                            break;
                        case WaveViewRenderMode.Geometry:
                            renderer = new WaveformGeometryRenderer() { WaveformLine = WaveformLine as SolidColorBrush };
                            break;
                    }
                    if (renderer != null) {
                        for (int channel = 0; channel < channels; channel++) {
                            // calculate the balance factor for the first two channels only (balance only applies to stereo)
                            // TODO extend for multichannel (needs implementation of a multichannel balance adjustment control)
                            float balanceFactor = 1;
                            if(channel == 0) {
                                balanceFactor = AudioTrack.Balance < 0 ? 1 : 1 - AudioTrack.Balance;
                            } else if(channel == 1) {
                                balanceFactor = AudioTrack.Balance > 0 ? 1 : 1 + AudioTrack.Balance;
                            }

                            Drawing waveform = renderer.Render(samples[channel], sampleCount, drawingWidthAligned, (int)channelHeight, AudioTrack.Volume * balanceFactor);
                            DrawingGroup drawing = new DrawingGroup();
                            drawing.Children.Add(waveform);
                            drawing.Transform = new TranslateTransform((int)drawingOffsetAligned, (int)(channelHeight * channel));
                            drawingContext.DrawDrawing(drawing);
                        }
                    }
                }

                DateTime afterDrawing = DateTime.Now;

                // draw track name
                if (DrawTrackName) {
                    FormattedText formattedTrackName = new FormattedText(AudioTrack.FileInfo.Name, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), 10f, Brushes.White);
                    drawingContext.DrawRectangle(Brushes.Black, null, new Rect(4 + drawingOffset, 5, formattedTrackName.Width + 4, formattedTrackName.Height + 2));
                    drawingContext.DrawText(formattedTrackName, new Point(6 + drawingOffset, 6));
                }

                if (debug) {
                    // DEBUG OUTPUT
                    drawingContext.DrawText(DebugText(String.Format("source:" + "n/a" + " load:{0}ms render:{1}ms", (afterLoading - beforeLoading).TotalMilliseconds, (afterDrawing - beforeDrawing).TotalMilliseconds)),
                        new Point(0, 20));
                    drawingContext.DrawText(DebugText("visibleAudioInterval: " + visibleAudioInterval + ", audioToLoadInterval: " + audioToLoadInterval + ", audioToLoadIntervalAligned: " + audioToLoadIntervalAligned),
                        new Point(0, ActualHeight) + new Vector(0, -50));
                    drawingContext.DrawText(DebugText("Drawing Offset: " + drawingOffsetAligned + ", Width: " + drawingWidthAligned + ", ScalingFactor: " + viewportToDrawingScaleFactor + ", Samples: " + sampleCount + ", Peakratio 1:" + Math.Round(VirtualViewportWidth / sampleLength / ActualWidth, 2)),
                        new Point(0, ActualHeight) + new Vector(0, -40));
                }
            }

            if (debug) {
                // DEBUG OUTPUT
                drawingContext.DrawText(DebugText("ActualWidth: " + ActualWidth + ", ActualHeight: " + ActualHeight),
                    new Point(0, ActualHeight) + new Vector(0, -30));
                drawingContext.DrawText(DebugText("TrackLength: " + TrackLength + ", TrackOffset: " + TrackOffset),
                    new Point(0, ActualHeight) + new Vector(0, -20));
                drawingContext.DrawText(DebugText("ViewportOffset: " + VirtualViewportOffset + ", ViewportWidth: " + VirtualViewportWidth),
                    new Point(0, ActualHeight) + new Vector(0, -10));
            }
        }

        private FormattedText DebugText(string text) {
            return new FormattedText(text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
                new Typeface("Tahoma"), 8, Brushes.Black);
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e) {
            base.OnMouseDown(e);
            Point mouseDownPosition = Mouse.GetPosition(this);
            //Debug.WriteLine("WaveView OnMouseDown @ " + mouseDownPosition);
            dragging = true;
            previousMousePosition = mouseDownPosition;
            e.Handled = true;
        }

        protected override void OnMouseMove(MouseEventArgs e) {
            base.OnMouseMove(e);
            if (dragging) {
                if (e.LeftButton == MouseButtonState.Pressed) {
                    Point mouseMovePosition = Mouse.GetPosition(this);
                    //Debug.WriteLine("WaveView OnMouseMove @ " + mouseMovePosition);
                    double physicalDelta = mouseMovePosition.X - previousMousePosition.X;
                    previousMousePosition = mouseMovePosition;
                    TrackOffset += PhysicalToVirtualOffset((long)physicalDelta);
                    e.Handled = true;
                }
                else {
                    dragging = false;
                }
            }
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e) {
            base.OnMouseUp(e);
            Point mouseUpPosition = Mouse.GetPosition(this);
            //Debug.WriteLine("WaveView OnMouseUp @ " + mouseUpPosition);
            dragging = false;
            e.Handled = true;
        }

        protected override void OnMouseLeave(MouseEventArgs e) {
            base.OnMouseLeave(e);
            //Debug.WriteLine("WaveView OnMouseLeave");
            dragging = false;
            e.Handled = true;
        }
    }
}
