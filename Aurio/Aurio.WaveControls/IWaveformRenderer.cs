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
