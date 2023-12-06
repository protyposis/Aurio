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
using Aurio.FFT;
using Aurio.Streams;

namespace Aurio
{
    public class FFTAnalyzer
    {
        public event EventHandler<ValueEventArgs<float[]>> WindowAnalyzed;

        private object lockObject = new object();

        private float[] inputBuffer;
        private int inputBufferFillLevel;
        private float[] outputBuffer;
        private WindowFunction windowFunction;
        private IFFT fft;
        private float windowFunctionNormalizationDecibelOffset;

        public FFTAnalyzer(int windowSize)
        {
            WindowSize = windowSize;
            fft = FFTFactory.CreateInstance(windowSize);
            windowFunctionNormalizationDecibelOffset = 0;
        }

        /// <summary>
        /// Gets or sets the size of the FFT window. Must be a power of 2.
        /// </summary>
        public int WindowSize
        {
            set
            {
                lock (lockObject)
                {
                    if (Math.Log(value, 2) % 1 != 0)
                    {
                        throw new Exception("size not a power of 2: " + value);
                    }
                    inputBuffer = new float[value];
                    inputBufferFillLevel = 0;
                    outputBuffer = new float[value / 2];
                }
            }
            get { return inputBuffer.Length; }
        }

        public WindowFunction WindowFunction
        {
            get { return windowFunction; }
            set
            {
                windowFunction = value;
                CalculateWindowFunctionNormalizationOffset();
            }
        }

        /// <summary>
        /// Calculates a normalization offset for the current window function. Every window function leads to
        /// a different FFT result peak value, and since the peak value of each windowed FFT means 0dB, this
        /// offset can be used to adjust the calculated dB values.
        /// </summary>
        private void CalculateWindowFunctionNormalizationOffset()
        {
            if (windowFunction == null)
            {
                windowFunctionNormalizationDecibelOffset = 0;
            }
            else
            {
                SineGeneratorStream sine = new SineGeneratorStream(1024, 16, new TimeSpan(0, 0, 1));
                float[] input = new float[WindowSize];
                float[] output = new float[input.Length / 2];
                WindowFunction wf = WindowUtil.GetFunction(windowFunction.Config);

                sine.Read(input, 0, input.Length);
                wf.Apply(input);
                fft.Forward(input);
                int maxIndex = FFTUtil.Results(input, output);
                float maxValue = output[maxIndex];
                windowFunctionNormalizationDecibelOffset = 1f - maxValue;
            }
        }

        public void PutSamples(float[] samples)
        {
            //Debug.WriteLine("PutSamples: input samples: " + samples.Length);

            lock (lockObject)
            {
                int inputSamplesPosition = 0;

                while (inputSamplesPosition < samples.Length)
                {
                    int length = Math.Min(
                        WindowSize - inputBufferFillLevel,
                        samples.Length - inputSamplesPosition
                    );
                    Array.Copy(
                        samples,
                        inputSamplesPosition,
                        inputBuffer,
                        inputBufferFillLevel,
                        length
                    );
                    inputSamplesPosition += length;
                    inputBufferFillLevel += length;

                    //Debug.WriteLine("PutSamples: copied " + length + "samples, bufferlevel: " + windowBufferFillLevel + ", inputpos: " + inputSamplesPosition);

                    if (inputBufferFillLevel == WindowSize)
                    {
                        CalculateFFT();
                        inputBufferFillLevel = 0;
                        //Debug.WriteLine("PutSamples: buffer full -> FFT performed");
                    }
                }
            }

            //Debug.WriteLine("PutSamples finished");
        }

        public void Reset()
        {
            // recreate buffer (new buffer is all zeroed)
            WindowSize = WindowSize;
        }

        private void CalculateFFT()
        {
            WindowFunction?.Apply(inputBuffer);
            fft.Forward(inputBuffer);
            FFTUtil.Results(inputBuffer, outputBuffer, windowFunctionNormalizationDecibelOffset);
            OnWindowAnalyzed();
        }

        private void OnWindowAnalyzed()
        {
            WindowAnalyzed?.Invoke(
                this,
                new ValueEventArgs<float[]>((float[])outputBuffer.Clone())
            );
        }
    }
}
