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
