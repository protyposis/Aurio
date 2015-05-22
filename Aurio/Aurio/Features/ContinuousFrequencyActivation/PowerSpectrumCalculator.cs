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
using Aurio.Matching;
using Aurio;
using Aurio.Streams;

namespace Aurio.Features.ContinuousFrequencyActivation {
    class PowerSpectrumCalculator : STFT {

        private const WindowType WINDOW_TYPE = WindowType.Hann; // "Hanning" window
        private const int WINDOW_SIZE = 1024; // samples = ~ 100ms @ 11kHz
        private const int HOP_SIZE = 256; // samples = 75%

        public PowerSpectrumCalculator(IAudioStream stream)
            : base(stream, WINDOW_SIZE, HOP_SIZE, WINDOW_TYPE, true) {
            // nothing to do here
        }

        public override void ReadFrame(float[] powerSpectrum) {
            // the STFT result equals the power spectrum in the paper
            base.ReadFrame(powerSpectrum);
        }
    }
}
