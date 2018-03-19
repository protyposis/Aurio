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

namespace Aurio.LibSampleRate
{
    public class Resampler : IResampler
    {
        private readonly SampleRateConverter _src;

        public Resampler(ResamplingQuality quality, int channels, double sampleRateRatio)
        {
            Quality = quality;
            Channels = channels;
            Ratio = sampleRateRatio;

            ConverterType ct = ConverterType.SRC_ZERO_ORDER_HOLD;

            switch (quality)
            {
                case ResamplingQuality.VeryLow:
                    ct = ConverterType.SRC_ZERO_ORDER_HOLD; break;
                case ResamplingQuality.Low:
                    ct = ConverterType.SRC_LINEAR; break;
                case ResamplingQuality.Medium:
                    ct = ConverterType.SRC_SINC_FASTEST; break;
                case ResamplingQuality.High:
                    ct = ConverterType.SRC_SINC_MEDIUM_QUALITY; break;
                case ResamplingQuality.VeryHigh:
                    ct = ConverterType.SRC_SINC_BEST_QUALITY; break;
                case ResamplingQuality.VariableRate:
                    ct = ConverterType.SRC_SINC_MEDIUM_QUALITY; break;
            }

            _src = new SampleRateConverter(ct, channels);
            _src.SetRatio(sampleRateRatio);
        }

        public ResamplingQuality Quality { get; private set; }

        public int Channels { get; private set; }

        public double Ratio { get; private set; }

        public bool VariableRate
        {
            get
            {
                return true;
            }
        }

        public void SetRatio(double ratio, int transitionLength)
        {
            // TODO how to handle the transition length?
            _src.SetRatio(ratio);
            Ratio = ratio;
        }

        public double GetOutputDelay()
        {
            // TODO can we calculate the delay? Or how should we handle this case?
            return -1;
        }

        public void Clear()
        {
            _src.Reset();
        }

        public void Process(byte[] input, int inputOffset, int inputLength,
            byte[] output, int outputOffset, int outputLength,
            bool endOfInput, out int inputLengthUsed, out int outputLengthGenerated)
        {
            _src.Process(input, inputOffset, inputLength,
                    output, outputOffset, outputLength, 
                    endOfInput, out inputLengthUsed, out outputLengthGenerated);
        }

        public void Dispose()
        {
            _src.Dispose();
        }

        ~Resampler()
        {
            Dispose();
        }
    }
}
