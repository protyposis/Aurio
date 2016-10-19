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

namespace Aurio.Features {
    /// <summary>
    /// Short-Time Fourier Tranformation
    /// </summary>
    public class STFT: StreamWindower {

        private float[] frameBuffer;
        private PFFFT.PFFFT fft;
        private bool normalizeTo_dB;

        // <summary>
        /// Initializes a new STFT for the specified stream with the specified window and hop size.
        /// </summary>
        /// <param name="stream">the stream to read the audio data to process from</param>
        /// <param name="windowSize">the window size in the dimension of samples</param>
        /// <param name="hopSize">the hop size in the dimension of samples</param>
        /// <param name="windowType">the type of the window function to apply</param>
        /// <param name="normalizeTo_dB">true if the FFT result should be normalized to dB scale, false if raw FFT magnitudes are desired</param>
        public STFT(IAudioStream stream, int windowSize, int hopSize, WindowType windowType, bool normalizeTo_dB)
            : base(stream, windowSize, hopSize, windowType) {
                frameBuffer = new float[WindowSize];
                fft = new PFFFT.PFFFT(WindowSize, PFFFT.Transform.Real);
                this.normalizeTo_dB = normalizeTo_dB;
        }

        public STFT(IAudioStream stream, int windowSize, int hopSize, WindowType windowType)
            : this(stream, windowSize, hopSize, windowType, true) {

        }

        public override void ReadFrame(float[] fftResult) {
            if (fftResult.Length != WindowSize / 2) {
                throw new ArgumentException("the provided FFT result array has an invalid size");
            }

            base.ReadFrame(frameBuffer);

            // do fourier transform
            fft.Forward(frameBuffer);

            // normalize fourier results
            if (normalizeTo_dB) {
                // TODO check if calculation corresponds to Haitsma & Kalker paper
                FFTUtil.Results(frameBuffer, fftResult);
            }
            else {
                //FFTUtil.CalculateMagnitudes(frameBuffer, fftResult);
                // TEMP TEST FOR OLTW
                FFTUtil.CalculateMagnitudesSquared(frameBuffer, fftResult);
            }

            OnFrameReadSTFT(fftResult);
        }

        //protected virtual void OnFrameSamplesRead(float[] frame) {
        //    // to be overridden
        //}

        protected virtual void OnFrameReadSTFT(float[] frame) {
            // to be overridden
        }
    }
}
