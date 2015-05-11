using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aurio.Audio.Streams;

namespace Aurio.Audio.Features.ContinuousFrequencyActivation {
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
