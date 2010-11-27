using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows;

namespace AudioAlign.WaveControls {
    class WaveformGeometryRenderer : IWaveformRenderer {
        #region IWaveformRenderer Members

        public Drawing Render(List<Point> samples, int width, int height) {
            SolidColorBrush WaveformFill = Brushes.LightBlue;
            SolidColorBrush WaveformLine = Brushes.CornflowerBlue;
            SolidColorBrush WaveformSamplePoint = Brushes.RoyalBlue;
            DrawingGroup waveformDrawing = new DrawingGroup();

            bool peaks = samples.Count > width; // for peaks, samples.Count should actually be width*2, but in any case it's > width

            Geometry audioform = peaks ? CreatePeakform(samples) : CreateWaveform(samples);
            TransformGroup transformGroup = new TransformGroup();
            transformGroup.Children.Add(new ScaleTransform(width / audioform.Bounds.Width, height / 2 * -1));
            transformGroup.Children.Add(new TranslateTransform(0, height / 2));
            audioform.Transform = transformGroup;

            waveformDrawing.Children.Add(new GeometryDrawing(WaveformFill, new Pen(WaveformLine, 1), audioform));

            if (!peaks) {
                // draw sample dots on high zoom factors
                float zoomFactor = (float)(width / samples.Count);
                if (zoomFactor > 0.05) {
                    float sampleDotSize = zoomFactor < 30 ? zoomFactor / 10 : 3;
                    GeometryGroup geometryGroup = new GeometryGroup();
                    foreach (Point point in samples) {
                        EllipseGeometry sampleDot = new EllipseGeometry(audioform.Transform.Transform(point), sampleDotSize, sampleDotSize);
                        geometryGroup.Children.Add(sampleDot);
                    }
                    waveformDrawing.Children.Add(new GeometryDrawing(WaveformSamplePoint, null, geometryGroup));
                }
            }

            return waveformDrawing;
        }

        #endregion

        private Geometry CreateWaveform(List<Point> samplePoints) {
            if (samplePoints.Count() < 2) { // cannot draw a line if I have just one point
                return Geometry.Empty;
            }
            else {
                PathGeometry waveformGeometry = new PathGeometry();
                PathFigure pathFigure = new PathFigure();
                pathFigure.IsClosed = false;
                pathFigure.IsFilled = false;
                pathFigure.StartPoint = samplePoints[0];
                pathFigure.Segments.Add(new PolyLineSegment(samplePoints, true)); // first point gets added a second time
                waveformGeometry.Figures.Add(pathFigure);
                return waveformGeometry;
            }
        }

        private Geometry CreatePeakform(List<Point> peakLines) {
            PathGeometry peakformGeometry = new PathGeometry();
            PathFigure pathFigure = new PathFigure();
            pathFigure.IsClosed = true;
            pathFigure.IsFilled = true;
            pathFigure.StartPoint = peakLines[0];
            pathFigure.Segments.Add(new PolyLineSegment(peakLines, true)); // first point gets added a second time
            peakformGeometry.Figures.Add(pathFigure);
            return peakformGeometry;
        }
    }
}
