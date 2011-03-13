using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows;

namespace AudioAlign.WaveControls {
    class WaveformGeometryRenderer : IWaveformRenderer {

        public WaveformGeometryRenderer() {
            WaveformFill = Brushes.LightBlue;
            WaveformLine = Brushes.CornflowerBlue;
            WaveformSamplePoint = Brushes.RoyalBlue;
        }

        public SolidColorBrush WaveformFill { get; set; }
        public SolidColorBrush WaveformLine { get; set; }
        public SolidColorBrush WaveformSamplePoint { get; set; }

        #region IWaveformRenderer Members

        public Drawing Render(float[] sampleData, int sampleCount, int width, int height) {
            bool peaks = sampleCount >= width;
            DrawingGroup waveformDrawing = new DrawingGroup();

            Geometry audioform = peaks ? CreatePeakform(sampleData, sampleCount) : CreateWaveform(sampleData, sampleCount);
            TransformGroup transformGroup = new TransformGroup();
            transformGroup.Children.Add(new ScaleTransform(width / audioform.Bounds.Width, height / 2 * -1));
            transformGroup.Children.Add(new TranslateTransform(0, height / 2));
            audioform.Transform = transformGroup;

            waveformDrawing.Children.Add(new GeometryDrawing(WaveformFill, new Pen(WaveformLine, 1), audioform));

            if (!peaks) {
                // draw sample dots on high zoom factors
                float zoomFactor = (float)(width / sampleCount);
                if (zoomFactor > 0.05) {
                    float sampleDotSize = zoomFactor < 30 ? zoomFactor / 10 : 3;
                    GeometryGroup geometryGroup = new GeometryGroup();
                    for(int x = 0; x < sampleCount; x++) {
                        EllipseGeometry sampleDot = new EllipseGeometry(audioform.Transform.Transform(new Point(x, sampleData[x])), sampleDotSize, sampleDotSize);
                        geometryGroup.Children.Add(sampleDot);
                    }
                    waveformDrawing.Children.Add(new GeometryDrawing(WaveformSamplePoint, null, geometryGroup));
                }
            }

            return waveformDrawing;
        }

        #endregion

        private Geometry CreateWaveform(float[] sampleData, int sampleCount) {
            if (sampleData.Length < 2) { // cannot draw a line if I have just one point
                return Geometry.Empty;
            }
            else {
                PathGeometry waveformGeometry = new PathGeometry();
                PathFigure pathFigure = new PathFigure();
                pathFigure.IsClosed = false;
                pathFigure.IsFilled = false;
                pathFigure.StartPoint = new Point(0, sampleData[0]);
                for (int x = 1; x < sampleCount; x++) {
                    pathFigure.Segments.Add(new LineSegment(new Point(x, sampleData[x]), true));
                }
                waveformGeometry.Figures.Add(pathFigure);
                return waveformGeometry;
            }
        }

        private Geometry CreatePeakform(float[] sampleData, int sampleCount) {
            PathGeometry peakformGeometry = new PathGeometry();
            PathFigure pathFigure = new PathFigure();
            pathFigure.IsClosed = true;
            pathFigure.IsFilled = true;
            pathFigure.StartPoint = new Point(0, sampleData[0]);
            for (int x = 1; x < sampleData.Length / 2; x++) {
                pathFigure.Segments.Add(new LineSegment(new Point(x, sampleData[x * 2]), true));
            }
            for (int x = sampleData.Length / 2 - 1; x >= 0; x--) {
                pathFigure.Segments.Add(new LineSegment(new Point(x, sampleData[x * 2 + 1]), true));
            }
            peakformGeometry.Figures.Add(pathFigure);
            return peakformGeometry;
        }
    }
}
