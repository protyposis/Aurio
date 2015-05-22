// 
// Aurio: Audio Processing, Analysis and Retrieval Library
// Copyright (C) 2010-2015  Mario Guggenberger <mg@protyposis.net>
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
using Aurio.Streams;

namespace Aurio.Features.ContinuousFrequencyActivation {
    class LocalPeakEmphasizer : PowerSpectrumCalculator {

        private const int LOCAL_WINDOW_SIZE = 21;
        private float[] powerSpectrumBuffer;

        public LocalPeakEmphasizer(IAudioStream stream)
            : base(stream) {
                powerSpectrumBuffer = new float[WindowSize / 2];
        }

        public override void ReadFrame(float[] emphasizedPowerSpectrum) {
            base.ReadFrame(powerSpectrumBuffer);

            int localWindowSizeHalf = LOCAL_WINDOW_SIZE / 2;
            float localWindowSizeFraction = 1f / LOCAL_WINDOW_SIZE;

            for (int i = 0; i < powerSpectrumBuffer.Length; i++) {
                // calculate running average
                float sum = 0;
                for (int j = i - localWindowSizeHalf; j <= i + localWindowSizeHalf; j++) {
                    sum += powerSpectrumBuffer[Math.Min(Math.Max(j, 0), powerSpectrumBuffer.Length - 1)];
                }
                emphasizedPowerSpectrum[i] = powerSpectrumBuffer[i] - localWindowSizeFraction * sum;
            }
        }
    }
}
