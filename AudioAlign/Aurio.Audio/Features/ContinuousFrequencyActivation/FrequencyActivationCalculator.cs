using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aurio.Audio.Streams;

namespace Aurio.Audio.Features.ContinuousFrequencyActivation {
    class FrequencyActivationCalculator : Binarizer {

        private const int BLOCK_SIZE = 100; // F, frames = ~ 2,6 secs
        private const int BLOCK_OVERLAP = 50; // 50%

        private Queue<float[]> binarizedPowerSpectrumBlock;
        private long frameCount;

        public FrequencyActivationCalculator(IAudioStream stream)
            : base(stream) {
                binarizedPowerSpectrumBlock = new Queue<float[]>(BLOCK_SIZE);
                frameCount = 0;
        }

        public override int WindowCount {
            get { return (int)Math.Ceiling((float)base.WindowCount / BLOCK_OVERLAP) - 1; }
        }

        public override void ReadFrame(float[] frequencyActivation) {
            //while ((frameCount < base.WindowCount /* == base.HasNext() */ && frameCount < BLOCK_SIZE) || (frameCount < base.WindowCount && frameCount % BLOCK_OVERLAP != 0)) {
            //    ReadBinaryFrame();
            //}
            do {
                ReadBinaryFrame();
            } while ((frameCount < base.WindowCount /* == base.HasNext() */ && frameCount < BLOCK_SIZE) || (frameCount < base.WindowCount && frameCount % BLOCK_OVERLAP != 0));

            Array.Clear(frequencyActivation, 0, frequencyActivation.Length);
            foreach (float[] binarizedPowerSpectrum in binarizedPowerSpectrumBlock) {
                for (int i = 0; i < binarizedPowerSpectrum.Length; i++) {
                    frequencyActivation[i] += binarizedPowerSpectrum[i];
                }
            }
            // normalize
            for (int i = 0; i < frequencyActivation.Length; i++) {
                frequencyActivation[i] /= BLOCK_SIZE;
            }
        }

        private void ReadBinaryFrame() {
            float[] binarizedPowerSpectrum;

            if (binarizedPowerSpectrumBlock.Count < BLOCK_SIZE) {
                binarizedPowerSpectrum = new float[WindowSize / 2];
            }
            else {
                // reuse the oldest frame buffer
                binarizedPowerSpectrum = binarizedPowerSpectrumBlock.Dequeue();
            }

            base.ReadFrame(binarizedPowerSpectrum);
            binarizedPowerSpectrumBlock.Enqueue(binarizedPowerSpectrum);
            frameCount++;
        }
    }
}
