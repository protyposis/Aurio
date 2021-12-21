// 
// Aurio: Audio Processing, Analysis and Retrieval Library
// Copyright (C) 2010-2018  Mario Guggenberger <mg@protyposis.net>
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

using Aurio.Resampler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aurio.Soxr
{
    public class Resampler : IResampler
    {
        private readonly SoxResampler _soxr;

        public Resampler(ResamplingQuality quality, int channels, double sampleRateRatio)
        {
            Quality = quality;
            Channels = channels;
            Ratio = sampleRateRatio;

            QualityRecipe qr = QualityRecipe.SOXR_HQ;
            QualityFlags qf = QualityFlags.SOXR_ROLLOFF_SMALL;

            switch (quality)
            {
                case ResamplingQuality.VeryLow:
                    qr = QualityRecipe.SOXR_QQ; break;
                case ResamplingQuality.Low:
                    qr = QualityRecipe.SOXR_LQ; break;
                case ResamplingQuality.Medium:
                    qr = QualityRecipe.SOXR_MQ; break;
                case ResamplingQuality.High:
                    qr = QualityRecipe.SOXR_HQ; break;
                case ResamplingQuality.VeryHigh:
                    qr = QualityRecipe.SOXR_VHQ; break;
                case ResamplingQuality.VariableRate:
                    qr = QualityRecipe.SOXR_HQ; qf = QualityFlags.SOXR_VR; break;
            }

            double inputRate = 1;
            double outputRate = sampleRateRatio;

            if (qf == QualityFlags.SOXR_VR)
            {
                // set max variable rate
                inputRate = 10.0;
                outputRate = 1.0;
            }

            _soxr = new SoxResampler(inputRate, outputRate, channels, qr, qf);
        }

        public ResamplingQuality Quality { get; private set; }

        public int Channels { get; private set; }

        public double Ratio { get; private set; }

        public bool VariableRate
        {
            get
            {
                return _soxr.VariableRate;
            }
        }

        public void SetRatio(double ratio, int transitionLength)
        {
            _soxr.SetRatio(ratio, transitionLength);
            Ratio = ratio;
        }

        public double GetOutputDelay()
        {
            return _soxr.GetOutputDelay();
        }

        public void Clear()
        {
            _soxr.Clear();
        }

        public void Process(byte[] input, int inputOffset, int inputLength,
            byte[] output, int outputOffset, int outputLength,
            bool endOfInput, out int inputLengthUsed, out int outputLengthGenerated)
        {
            _soxr.Process(input, inputOffset, inputLength,
                    output, outputOffset, outputLength,
                    endOfInput, out inputLengthUsed, out outputLengthGenerated);
        }

        public void Dispose()
        {
            _soxr.Dispose();
        }

        ~Resampler()
        {
            Dispose();
        }
    }
}
