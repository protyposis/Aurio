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

        public enum OutputFormat {
            Raw,
            Magnitudes,
            MagnitudesSquared,
            MagnitudesAndPhases,
            Decibel
        }

        private float[] frameBuffer;
        private PFFFT.PFFFT fft;
        private OutputFormat outputFormat;

        /// <summary>
        /// Initializes a new STFT for the specified stream with the specified window, hop, and FFT size.
        /// An FFT size larger than the window size can be used to increase frequency resolution. Window will be right-padded with zeros for FFT.
        /// </summary>
        /// <param name="stream">the stream to read the audio data to process from</param>
        /// <param name="windowSize">the window size in the dimension of samples</param>
        /// <param name="hopSize">the hop size in the dimension of samples</param>
        /// <param name="fftSize">the FFT size, must be >= windowSize</param>
        /// <param name="windowType">the type of the window function to apply</param>
        /// <param name="normalizeTo_dB">true if the FFT result should be normalized to dB scale, false if raw FFT magnitudes are desired</param>
        public STFT(IAudioStream stream, int windowSize, int hopSize, int fftSize, WindowType windowType, OutputFormat outputFormat)
            : base(stream, windowSize, hopSize, windowType) {
                if(fftSize < windowSize) {
                    throw new ArgumentOutOfRangeException("fftSize must be >= windowSize");
                }
                frameBuffer = new float[fftSize];
                Array.Clear(frameBuffer, 0, frameBuffer.Length); // init with zeros (assure zero padding)
                fft = new PFFFT.PFFFT(fftSize, PFFFT.Transform.Real);
                this.outputFormat = outputFormat;
        }

        /// <summary>
        /// Initializes a new STFT for the specified stream with the specified window and hop size.
        /// </summary>
        /// <param name="stream">the stream to read the audio data to process from</param>
        /// <param name="windowSize">the window size in the dimension of samples</param>
        /// <param name="hopSize">the hop size in the dimension of samples</param>
        /// <param name="windowType">the type of the window function to apply</param>
        /// <param name="normalizeTo_dB">true if the FFT result should be normalized to dB scale, false if raw FFT magnitudes are desired</param>
        public STFT(IAudioStream stream, int windowSize, int hopSize, WindowType windowType, OutputFormat outputFormat)
            : this(stream, windowSize, hopSize, windowSize, windowType, outputFormat) {
        }

        public override void ReadFrame(float[] fftResult) {
            // Check output array size
            switch (outputFormat) {
                case OutputFormat.Decibel:
                case OutputFormat.Magnitudes:
                case OutputFormat.MagnitudesSquared:
                    if (fftResult.Length != fft.Size / 2) {
                        throw new ArgumentException("the provided FFT result array has an invalid size");
                    }
                    break;
                default:
                    if (fftResult.Length != fft.Size) {
                        throw new ArgumentException("the provided FFT result array has an invalid size");
                    }
                    break;
            }

            base.ReadFrame(frameBuffer);

            // do fourier transform
            fft.Forward(frameBuffer);

            // normalize fourier results
            switch (outputFormat) {
                case OutputFormat.Decibel:
                    // TODO check if calculation corresponds to Haitsma & Kalker paper
                    FFTUtil.Results(frameBuffer, fftResult);
                    break;
                case OutputFormat.Magnitudes:
                    FFTUtil.CalculateMagnitudes(frameBuffer, fftResult);
                    break;
                case OutputFormat.MagnitudesSquared:
                    // TODO check if consumers of this mode really want it or if they want unsquared magnitudes instead 
                    // (e.g. OLTW; this code path returns CalculateMagnitudesSquared for some time, as a temp test for OLTW, 
                    // but originally returned CalculateMagnitudes)
                    FFTUtil.CalculateMagnitudesSquared(frameBuffer, fftResult);
                    break;
                case OutputFormat.MagnitudesAndPhases:
                    FFTUtil.CalculateMagnitudes(frameBuffer, fftResult, 0);
                    FFTUtil.CalculatePhases(frameBuffer, fftResult, WindowSize / 2);
                    break;
                case OutputFormat.Raw:
                    // Nothing to do here, result is already in raw format, just copy it to output buffer
                    Buffer.BlockCopy(frameBuffer, 0, fftResult, 0, WindowSize * 4);
                    break;
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
