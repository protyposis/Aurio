using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aurio.Streams;

namespace Aurio.Features.ContinuousFrequencyActivation {
    class Binarizer : LocalPeakEmphasizer {

        private const float THRESHOLD = 0.1f;

        private float[] emphasizedPowerSpectrumBuffer;

        public Binarizer(IAudioStream stream)
            : base(stream) {
                emphasizedPowerSpectrumBuffer = new float[WindowSize / 2];
        }

        public override void ReadFrame(float[] binarizedPowerSpectrum) {
            base.ReadFrame(emphasizedPowerSpectrumBuffer);

            for (int i = 0; i < emphasizedPowerSpectrumBuffer.Length; i++) {
                binarizedPowerSpectrum[i] = emphasizedPowerSpectrumBuffer[i] > THRESHOLD ? 1 : 0;
            }
        }
    }
}
