using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;
using System.Globalization;
using System.ComponentModel;
using AudioAlign.Audio;

namespace AudioAlign.WaveControls {
    public class TimeScale: VirtualViewBase {

        private const int SCALE_HEIGHT = 10;
        private const int SCALE_FONT_SIZE = 8;
        private const string SCALE_TEXT_FORMAT = @"d\.hh\.mm\:ss\.fff";

        private readonly long[] TICK_LEVELS = { 
                                            10L * 1000,                         // MS
                                            10L * 1000 * 10,                    // 10MS
                                            10L * 1000 * 20,                    // 20MS
                                            10L * 1000 * 50,                    // 50MS
                                            10L * 1000 * 100,                   // 100MS
                                            10L * 1000 * 200,                   // 200MS
                                            10L * 1000 * 500,                   // 500MS
                                            10L * 1000 * 1000,                  // S
                                            10L * 1000 * 1000 * 2,              // 2S
                                            10L * 1000 * 1000 * 5,              // 5S
                                            10L * 1000 * 1000 * 10,             // 10S
                                            10L * 1000 * 1000 * 20,             // 20S
                                            10L * 1000 * 1000 * 30,             // 30S
                                            10L * 1000 * 1000 * 60,             // M
                                            10L * 1000 * 1000 * 60 * 2,         // 2M
                                            10L * 1000 * 1000 * 60 * 5,         // 5M
                                            10L * 1000 * 1000 * 60 * 10,        // 10M
                                            10L * 1000 * 1000 * 60 * 20,        // 20M
                                            10L * 1000 * 1000 * 60 * 30,        // 30M
                                            10L * 1000 * 1000 * 60 * 60,        // H
                                            10L * 1000 * 1000 * 60 * 60 * 2,    // 2H
                                            10L * 1000 * 1000 * 60 * 60 * 5     // 5H
                                        };

        static TimeScale() {
            WidthProperty.OverrideMetadata(typeof(TimeScale), new FrameworkPropertyMetadata(Double.NaN));
            HeightProperty.OverrideMetadata(typeof(TimeScale), new FrameworkPropertyMetadata(30d));

            ClipToBoundsProperty.OverrideMetadata(typeof(TimeScale), new FrameworkPropertyMetadata(true));
            ForegroundProperty.OverrideMetadata(typeof(TimeScale), new FrameworkPropertyMetadata(Brushes.Gray));
            BackgroundProperty.OverrideMetadata(typeof(TimeScale), new FrameworkPropertyMetadata(Brushes.White));
        }

        protected override void OnRender(System.Windows.Media.DrawingContext drawingContext) {
            base.OnRender(drawingContext);
            double actualWidth = ActualWidth;
            double actualHeight = ActualHeight;

            // draw background
            drawingContext.DrawRectangle(Background, null, new Rect(0, 0, actualWidth, actualHeight));

            Interval viewportInterval = new Interval(VirtualViewportOffset, VirtualViewportOffset + VirtualViewportWidth);
            double scale = actualWidth / viewportInterval.Length;
            long ticks = FindTicks(viewportInterval.Length, (int)(Math.Round(actualWidth / 20)));
            Interval viewportIntervalAligned = new Interval(
                viewportInterval.From - (viewportInterval.From % ticks), 
                viewportInterval.To + (viewportInterval.To % ticks));
            double drawingOffset = (viewportIntervalAligned.From - viewportInterval.From) * scale;

            GuidelineSet guidelineSet = new GuidelineSet();
            drawingContext.PushGuidelineSet(guidelineSet);

            // draw interval start & end time
            FormattedText formattedStartText = new FormattedText(
                        new TimeSpan(viewportInterval.From).ToString(SCALE_TEXT_FORMAT),
                        CultureInfo.CurrentUICulture, System.Windows.FlowDirection.LeftToRight,
                        new Typeface("Tahoma"), SCALE_FONT_SIZE, Brushes.Gray) { TextAlignment = TextAlignment.Left };
            drawingContext.DrawText(formattedStartText, new Point(1, 0));
            FormattedText formattedEndText = new FormattedText(
                        new TimeSpan(viewportInterval.To).ToString(SCALE_TEXT_FORMAT),
                        CultureInfo.CurrentUICulture, System.Windows.FlowDirection.LeftToRight,
                        new Typeface("Tahoma"), SCALE_FONT_SIZE, Brushes.Gray) { TextAlignment = TextAlignment.Right };
            drawingContext.DrawText(formattedEndText, new Point(actualWidth - 1, 0));

            // draw markers and time
            int timeDrawingRate = 5;
            long markerCount = (viewportIntervalAligned.From / ticks) % timeDrawingRate;
            for (long i = 0; i < viewportIntervalAligned.Length; i += ticks) {
                double markerHeight = actualHeight - (SCALE_HEIGHT / 2 * 1.5);
                double x = i * scale + drawingOffset;

                // draw time
                if (markerCount++ % timeDrawingRate == 0) {
                    markerHeight = actualHeight - SCALE_HEIGHT;
                    FormattedText formattedText = new FormattedText(
                        new TimeSpan(viewportIntervalAligned.From + i).ToString(SCALE_TEXT_FORMAT),
                        CultureInfo.CurrentUICulture, System.Windows.FlowDirection.LeftToRight,
                        new Typeface("Tahoma"), SCALE_FONT_SIZE, Foreground) { TextAlignment = TextAlignment.Center };
                    drawingContext.DrawText(formattedText, new Point(x, actualHeight - SCALE_HEIGHT - SCALE_FONT_SIZE * 1.2));
                }

                // draw marker
                guidelineSet.GuidelinesX.Add(x + 0.5);
                drawingContext.DrawLine(new Pen(Foreground, 1),
                        new Point(x, markerHeight), new Point(x, actualHeight));
            }

            // draw markers' bottom line
            guidelineSet.GuidelinesY.Add(actualHeight - 0.5);
            drawingContext.DrawLine(new Pen(Foreground, 1),
                        new Point(0, actualHeight - 1), new Point(actualWidth, actualHeight - 1));

            drawingContext.Pop();
        }

        private long FindTicks(long interval, long markers) {
            for (int x = 0; x < TICK_LEVELS.Length; x++) {
                if (interval / TICK_LEVELS[x] > markers) {
                    continue;
                }
                return TICK_LEVELS[x];
            }
            return TICK_LEVELS.Last();
        }
    }
}
