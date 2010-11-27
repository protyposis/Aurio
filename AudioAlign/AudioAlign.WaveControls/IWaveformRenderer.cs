using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace AudioAlign.WaveControls {
    interface IWaveformRenderer<out T> {
        T Render(List<Point> samples, int width, int height);
        void Draw(List<Point> samples, int width, int height, DrawingContext drawingContext, Point position);
    }
}
