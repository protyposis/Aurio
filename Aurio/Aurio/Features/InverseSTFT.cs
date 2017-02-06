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
using Aurio.Streams;

namespace Aurio.Features {
    /// <summary>
    /// Inverse Short-Time Fourier Tranformation.
    /// Takes a number of raw FFT frames and converts them to a continuous audio stream by using the overlap-add method.
    /// </summary>
    public class InverseSTFT : OLA {

        private float[] frameBuffer;
        private PFFFT.PFFFT fft;
        private WindowFunction synthesisWindow;

        public InverseSTFT(IAudioWriterStream stream, int windowSize, int hopSize, int fftSize, WindowType windowType, float windowNormalizationFactor) 
            : base(stream, windowSize, hopSize) {
            if (fftSize < windowSize) {
                throw new ArgumentOutOfRangeException("fftSize must be >= windowSize");
            }
            frameBuffer = new float[fftSize];
            fft = new PFFFT.PFFFT(fftSize, PFFFT.Transform.Real);
            synthesisWindow = WindowUtil.GetFunction(windowType, windowSize, windowNormalizationFactor);
        }

        public InverseSTFT(IAudioWriterStream stream, int windowSize, int hopSize, WindowType windowType)
            : this(stream, windowSize, hopSize, windowSize, windowType, 1.0f) {
        }

        /// <summary>
        /// Writes a raw FFT result frame into the output audio stream.
        /// </summary>
        /// <param name="fftResult">raw FFT frame as output by STFT in OutputFormat.Raw mode</param>
        public override void WriteFrame(float[] fftResult) {
            if (fftResult.Length != fft.Size) {
                throw new ArgumentException("the provided FFT result array has an invalid size");
            }

            OnFrameWrittenInverseSTFT(fftResult);

            // do inverse fourier transform
            fft.Backward(fftResult, frameBuffer);

            // Apply synthesis window
            synthesisWindow.Apply(frameBuffer);

            base.WriteFrame(frameBuffer);
        }

        /// <summary>
        /// Flushes remaining buffered data to the output audio stream.
        /// </summary>
        public override void Flush() {
            base.Flush();
        }

        protected virtual void OnFrameWrittenInverseSTFT(float[] frame) {
            // to be overridden
        }
    }
}
