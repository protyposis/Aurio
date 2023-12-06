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
using Aurio.Streams;

namespace Aurio.Features.ContinuousFrequencyActivation
{
    class FrequencyActivationCalculator : Binarizer
    {
        private const int BLOCK_SIZE = 100; // F, frames = ~ 2,6 secs
        private const int BLOCK_OVERLAP = 50; // 50%

        private Queue<float[]> binarizedPowerSpectrumBlock;
        private long frameCount;

        public FrequencyActivationCalculator(IAudioStream stream)
            : base(stream)
        {
            binarizedPowerSpectrumBlock = new Queue<float[]>(BLOCK_SIZE);
            frameCount = 0;
        }

        public override int WindowCount
        {
            get { return (int)Math.Ceiling((float)base.WindowCount / BLOCK_OVERLAP) - 1; }
        }

        public override void ReadFrame(float[] frequencyActivation)
        {
            //while ((frameCount < base.WindowCount /* == base.HasNext() */ && frameCount < BLOCK_SIZE) || (frameCount < base.WindowCount && frameCount % BLOCK_OVERLAP != 0)) {
            //    ReadBinaryFrame();
            //}
            do
            {
                ReadBinaryFrame();
            } while (
                (
                    frameCount < base.WindowCount /* == base.HasNext() */
                    && frameCount < BLOCK_SIZE
                ) || (frameCount < base.WindowCount && frameCount % BLOCK_OVERLAP != 0)
            );

            Array.Clear(frequencyActivation, 0, frequencyActivation.Length);
            foreach (float[] binarizedPowerSpectrum in binarizedPowerSpectrumBlock)
            {
                for (int i = 0; i < binarizedPowerSpectrum.Length; i++)
                {
                    frequencyActivation[i] += binarizedPowerSpectrum[i];
                }
            }
            // normalize
            for (int i = 0; i < frequencyActivation.Length; i++)
            {
                frequencyActivation[i] /= BLOCK_SIZE;
            }
        }

        private void ReadBinaryFrame()
        {
            float[] binarizedPowerSpectrum;

            if (binarizedPowerSpectrumBlock.Count < BLOCK_SIZE)
            {
                binarizedPowerSpectrum = new float[WindowSize / 2];
            }
            else
            {
                // reuse the oldest frame buffer
                binarizedPowerSpectrum = binarizedPowerSpectrumBlock.Dequeue();
            }

            base.ReadFrame(binarizedPowerSpectrum);
            binarizedPowerSpectrumBlock.Enqueue(binarizedPowerSpectrum);
            frameCount++;
        }
    }
}
