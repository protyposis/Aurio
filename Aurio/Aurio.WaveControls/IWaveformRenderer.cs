using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace Aurio.WaveControls {
    interface IWaveformRenderer {
        /// <summary>
        /// Renders a list of points (samples/peaks) to a drawing. Depending on the input parameters
        /// the resulting drawing contains a waveform or a peakform.
        /// </summary>
        /// <param name="samples">the samples or peaks to draw</param>
        /// <param name="peaks">true is the supplied list contains peaks, else false for samples</param>
        /// <param name="width">the width of the resulting drawing</param>
        /// <param name="height">the height of the resulting drawing</param>
        /// <param name="volume">volume scale factor, default value is 1.0f</param>
        /// <returns></returns>
        Drawing Render(float[] sampleData, int sampleCount, int width, int height, float volume);
    }
}
