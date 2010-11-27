using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace AudioAlign.WaveControls {
    interface IWaveformRenderer {
        Drawing Render(List<Point> samples, int width, int height);
    }
}
