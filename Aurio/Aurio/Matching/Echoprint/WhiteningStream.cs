using Aurio.DataStructures;
using Aurio.Streams;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aurio.Matching.Echoprint {
    /// <summary>
    /// Whitens a stream by calculating an LPC filter block by block from the autocorrelation, 
    /// effectively removing stationary frequencies from the stream. This can be used to analyze
    /// nonstationary spectral changes.
    /// </summary>
    class WhiteningStream : AbstractAudioStreamWrapper {

        // config variables
        private int numPoles;
        private float decaySecs;
        private int blockLength;

        // block buffers
        private ByteBuffer blockInputBuffer;
        private ByteBuffer blockOutputBuffer;

        // processing variables
        private float alpha; // decay value for autocorrelation smoothing
        private float[] _R; // autocorrelation function
        private float[] _Xo; // carryover samples from the end of a block
        private float[] _ai; // predictor coefficients

        public WhiteningStream(IAudioStream sourceStream, int numPoles, float decaySecs, int blockLength)
            : base(sourceStream) {
                if (sourceStream.Properties.Channels != 1) {
                    throw new ArgumentException("source stream must be mono");
                }

                this.numPoles = numPoles;
                this.decaySecs = decaySecs;
                this.blockLength = blockLength;

                Init();
        }

        private void Init() {
            blockInputBuffer = new ByteBuffer(sourceStream.SampleBlockSize * blockLength);
            blockOutputBuffer = new ByteBuffer(blockInputBuffer.Capacity);

            alpha = 1.0f / decaySecs;

            _R = new float[numPoles + 1];
            _R[0] = 0.001f;

            _Xo = new float[numPoles + 1];
            _ai = new float[numPoles + 1];
        }

        /// <summary>
        /// Gets or sets the position in the stream. Setting means seeking.
        /// </summary>
        /// <remarks>
        /// Seeking restarts the whitening filter at the seek target index and will lead to 
        /// different sample output for the same global stream sample index. E.g., starting 
        /// at the beginning of the stream (index 0), the sample at index x s(x) has a different 
        /// value compared to when read after a seek to 0 < t < x because the whitening filter 
        /// will not be exactly the same.
        /// </remarks>
        public override long Position {
            get {
                return base.Position - blockOutputBuffer.Count;
            }
            set {
                if (value == base.Position) {
                    return;
                }

                base.Position = value;

                // Reset state after seek
                Init();
            }
        }

        public override int Read(byte[] buffer, int offset, int count) {
            // Process a block if output buffer is empty
            if (blockOutputBuffer.Empty) {
                blockInputBuffer.Clear();
                blockOutputBuffer.Clear();

                // Read a sample block
                int bytesRead = blockInputBuffer.ForceFill(sourceStream);
                if (bytesRead == 0) {
                    return 0; // EOS
                }

                if (bytesRead / sourceStream.SampleBlockSize > numPoles) {
                    // Whiten the sample block
                    unsafe {
                        fixed (byte* byteInBuffer = blockInputBuffer.Data, byteOutBuffer = blockOutputBuffer.Data) {
                            ProcessBlock((float*)byteInBuffer, (float*)byteOutBuffer, blockInputBuffer.Length / sourceStream.SampleBlockSize);
                        }
                    }
                }
                else {
                    // When there is not enough sample data left for whitening, just set output to zero
                    Array.Clear(blockOutputBuffer.Data, 0, blockOutputBuffer.Capacity);
                }
                blockOutputBuffer.Fill(blockInputBuffer.Length);
            }

            // Output whitened samples
            count = Math.Min(blockOutputBuffer.Count, count);
            Array.Copy(blockOutputBuffer.Data, blockOutputBuffer.Offset, buffer, offset, count);
            blockOutputBuffer.Read(count);

            return count;
        }
        /// <summary>
        /// Whitens a block of samples with a k-pole LPC filter by calculating the prediction filter P(z)
        /// from the autocorrelation of the first k samples with Durbin's recursion, then calculating 
        /// the estimated value of each sample through linear combination of preceding samples and the filter, 
        /// and subtracting the predicted sample value from the actual sample value to get the prediction error
        /// of each sample, which is the whitened signal.
        /// 
        /// Alternatively, the error filter A(z) that gets the whitenend signal could be calculated from the 
        /// prediction filter A(z) = 1-P(z), which could then be applied to get the whitenend signal directly 
        /// without the subtractions.
        /// 
        /// Linear Prediction Coding is described in:
        /// - K. M. M. Prabhu, Window Functions and Their Applications in Signal Processing, 2013, pp. 344
        /// - James Beauchamp, Analysis, Synthesis, and Perception of Musical Sounds, 2007, pp. 190
        /// - Freescale Semiconductor, Implementing the Levinson-Durbin Algorithm on the StarCore SC140/SC1400 Cores, Application Note AN2197, 2005, pp. 4
        /// - http://www.kvraudio.com/forum/viewtopic.php?t=159214
        /// </summary>
        /// <param name="samples">the input sample block</param>
        /// <param name="whitened">the output sample block</param>
        /// <param name="count">the size of the input and output sample blocks</param>
        private unsafe void ProcessBlock(float* samples, float* whitened, int count) {
            // calculate autocorrelation of current block
            for (int i = 0; i <= numPoles; i++) {
                float acc = 0;
                for (int j = i; j < count; j++) {
                    acc += samples[j] * samples[j - i];
                }
                // smoothed update (exponential moving average)
                _R[i] += alpha * (acc - _R[i]);
            }

            // calculate new filter coefficients
            // Durbin's recursion, per p. 411 of Rabiner & Schafer 1978
            float E = _R[0]; // prediction error
            for (int i = 1; i <= numPoles; i++) {
                float sumalphaR = 0;
                for (int j = 1; j < i; j++) {
                    sumalphaR += _ai[j] * _R[i - j];
                }
                float ki = (_R[i] - sumalphaR) / E;
                _ai[i] = ki;
                for (int j = 1; j <= i / 2; j++) {
                    float aj = _ai[j];
                    float aimj = _ai[i - j];
                    _ai[j] = aj - ki * aimj;
                    _ai[i - j] = aimj - ki * aj;
                }
                E = (1 - ki * ki) * E;
            }

            // calculate new output
            for (int i = 0; i < count; i++) {
                // Calculate predicted value of each sample
                float acc = 0;
                int minip = i;
                if (numPoles < minip) {
                    minip = numPoles;
                }
                // Because past samples need to be used to predict a sample, prediction of 
                // the first k samples requires samples from the end of the previous block.
                for (int j = i + 1; j <= numPoles; j++) {
                    acc += _ai[j] * _Xo[numPoles + i - j]; // preceding samples from previous block
                }
                for (int j = 1; j <= minip; j++) {
                    acc += _ai[j] * samples[i - j]; // preceding samples from current block
                }

                // Set whitened sample by subtracting the predicted sample value from the actual value
                whitened[i] = samples[i] - acc;
            }

            // save last few samples of input which are needed to estimate the first few samples of the next block
            for (int i = 0; i <= numPoles; i++) {
                _Xo[i] = samples[count - 1 - numPoles + i];
            }
        }
    }
}
