using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace AudioAlign.Audio {
    public class FFTAnalyzer {

        public event EventHandler<ValueEventArgs<float[]>> WindowAnalyzed;

        private object lockObject = new object();

        private float[] inputBuffer;
        private int inputBufferFillLevel;
        private float[] outputBuffer;

        public FFTAnalyzer(int windowSize) {
            WindowSize = windowSize;
        }

        /// <summary>
        /// Gets or sets the size of the FFT window. Must be a power of 2.
        /// </summary>
        public int WindowSize {
            set {
                lock (lockObject) {
                    if (Math.Log(value, 2) % 1 != 0) {
                        throw new Exception("size not a power of 2: " + value);
                    }
                    inputBuffer = new float[value];
                    inputBufferFillLevel = 0;
                    outputBuffer = new float[value / 2];
                }
            }
            get { return inputBuffer.Length; }
        }

        public WindowFunction WindowFunction { get; set; }

        public void PutSamples(float[] samples) {
            //Debug.WriteLine("PutSamples: input samples: " + samples.Length);

            lock (lockObject) {
                int inputSamplesPosition = 0;

                while (inputSamplesPosition < samples.Length) {
                    int length = Math.Min(WindowSize - inputBufferFillLevel, samples.Length - inputSamplesPosition);
                    Array.Copy(samples, inputSamplesPosition, inputBuffer, inputBufferFillLevel, length);
                    inputSamplesPosition += length;
                    inputBufferFillLevel += length;

                    //Debug.WriteLine("PutSamples: copied " + length + "samples, bufferlevel: " + windowBufferFillLevel + ", inputpos: " + inputSamplesPosition);

                    if (inputBufferFillLevel == WindowSize) {
                        CalculateFFT();
                        inputBufferFillLevel = 0;
                        //Debug.WriteLine("PutSamples: buffer full -> FFT performed");
                    }
                }
            }

            //Debug.WriteLine("PutSamples finished");
        }

        public void Reset() {
            // recreate buffer (new buffer is all zeroed)
            WindowSize = WindowSize;
        }

        private void CalculateFFT() {
            if (WindowFunction != null) {
                WindowFunction.Apply(inputBuffer);
            }
            FFTUtil.FFT(inputBuffer);
            FFTUtil.Results(inputBuffer, outputBuffer);
            OnWindowAnalyzed();
        }

        private void OnWindowAnalyzed() {
            if (WindowAnalyzed != null) {
                WindowAnalyzed(this, new ValueEventArgs<float[]>((float[])outputBuffer.Clone()));
            }
        }
    }
}
