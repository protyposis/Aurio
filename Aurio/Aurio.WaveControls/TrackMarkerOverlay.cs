// 
// Aurio: Audio Processing, Analysis and Retrieval Library
// Copyright (C) 2010-2017  Mario Guggenberger <mg@protyposis.net>
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as
// published by the Free Software Foundation, either version 3 of the
// License, or (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace Aurio.WaveControls {
    public class TrackMarkerOverlay : ContentOverlay {

        public static readonly DependencyProperty MarkersProperty;

        static TrackMarkerOverlay() {
            MarkersProperty = DependencyProperty.Register(
                "Markers", typeof(List<TrackMarker>), typeof(TrackMarkerOverlay),
                    new FrameworkPropertyMetadata { AffectsRender = true, DefaultValue = new List<TrackMarker>() });
        }

        public List<TrackMarker> Markers {
            get { return (List<TrackMarker>)GetValue(MarkersProperty); }
            set { SetValue(MarkersProperty, value); }
        }

        internal override void OnRenderOverlay(DrawingContext drawingContext) {
            Brush foregroundBrush = Foreground;
            Brush backgroundBrush = Background;
            Pen pen = new Pen(foregroundBrush, 1.0);
            Interval visibleInterval = VirtualViewportInterval;
            double textPadding = 3;
            double pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;

            foreach (var marker in Markers) {
                if(visibleInterval.Contains(marker.Position.Ticks)) {
                    double renderOffset = VirtualToPhysicalIntervalOffset(marker.Position.Ticks);

                    // Draw line marker
                    drawingContext.DrawLine(pen, new Point(renderOffset, 0), 
                        new Point(renderOffset, RenderSize.Height));

                    // Draw text label
                    FormattedText formattedText = new FormattedText(marker.Text,
                        CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
                        new Typeface("Tahoma"), 8, foregroundBrush, pixelsPerDip) { TextAlignment = TextAlignment.Left };

                    var textBgSize = new Size(formattedText.Width + 2 * textPadding, formattedText.Height + 2 * textPadding);
                    var textBgPosition = new Point(renderOffset + 2, RenderSize.Height - textBgSize.Height - 5);

                    var triangle = CreateTriangle(textBgSize.Height);
                    drawingContext.PushTransform(new TranslateTransform(textBgPosition.X, textBgPosition.Y));
                    drawingContext.DrawGeometry(foregroundBrush, pen, triangle);

                    drawingContext.PushTransform(new TranslateTransform(triangle.Bounds.Width, 0));
                    drawingContext.DrawRectangle(backgroundBrush, pen, new Rect(new Point(), textBgSize));

                    drawingContext.PushTransform(new TranslateTransform(3, 3));
                    drawingContext.DrawText(formattedText, new Point());

                    drawingContext.Pop(); // text
                    drawingContext.Pop(); // bg rect
                    drawingContext.Pop(); // bg triangle
                }
            }
        }

        private StreamGeometry CreateTriangle(double height) {
            Point left = new Point(0, height / 2);
            Point top = new Point(height / 3, 0);
            Point bottom = new Point(height / 3, height);
            StreamGeometry streamGeometry = new StreamGeometry();

            using (StreamGeometryContext geometryContext = streamGeometry.Open()) {
                geometryContext.BeginFigure(left, true, true);
                geometryContext.PolyLineTo(new PointCollection { top, bottom }, true, true);
            }

            streamGeometry.Freeze();

            return streamGeometry;
        }
    }
}
