using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioAlign.LibSampleRate {
    public class SampleRateConverter : IDisposable {

        private static IInteropWrapper interop;

        static SampleRateConverter() {
            if (IntPtr.Size == 8) {
                interop = new Interop64Wrapper();
            }
            else {
                interop = new Interop32Wrapper();
            }
        }

        private bool disposed = false;
        private IntPtr srcState = IntPtr.Zero;
        private SRC_DATA srcData;
        private int error;
        private int channels;
        private double ratio;
        private double bufferedSamples;

        public SampleRateConverter(ConverterType type, int channels) {
            srcState = interop.src_new(type, channels, out error);
            ThrowExceptionForError(error);
            srcData = new SRC_DATA();

            SetRatio(1d);

            this.channels = channels;
            this.bufferedSamples = 0;
        }

        #region IDisposable & destructor

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// http://www.codeproject.com/KB/cs/idisposable.aspx
        /// </summary>
        protected virtual void Dispose(bool disposing) {
            if (!disposed) {
                if (disposing) {
                    // Dispose managed resources.
                }

                // There are no unmanaged resources to release, but
                // if we add them, they need to be released here.
                srcState = interop.src_delete(srcState);
                if (srcState != IntPtr.Zero) {
                    throw new Exception("could not delete the sample rate converter");
                }
            }
            disposed = true;

            // If it is available, make the call to the
            // base class's Dispose(Boolean) method
            //base.Dispose(disposing);
        }

        ~SampleRateConverter() {
            Dispose(false);
        }

        #endregion

        /// <summary>
        /// Gets the number of bytes buffered by the SRC. Buffering may happen since the SRC may read more
        /// data than it outputs during one #Process call.
        /// </summary>
        public int BufferedBytes {
            get { return (int)(bufferedSamples * 4); }
        }

        public void Reset() {
            error = interop.src_reset(srcState);
            ThrowExceptionForError(error);
            bufferedSamples = 0;
        }

        public void SetRatio(double ratio) {
            SetRatio(ratio, true);
        }

        public void SetRatio(double ratio, bool step) {
            if (step) {
                // force the ratio for the next #Process call instead of linearly interpolating from the previous
                // ratio to the current ratio
                error = interop.src_set_ratio(srcState, ratio);
                ThrowExceptionForError(error);
            }
            this.ratio = ratio;
        }

        public static bool CheckRatio(double ratio) {
            return interop.src_is_valid_ratio(ratio) == 1;
        }

        public void Process(byte[] input, int inputOffset, int inputLength,
            byte[] output, int outputOffset, int outputLength,
            bool endOfInput, out int inputLengthUsed, out int outputLengthGenerated) {
            unsafe {
                fixed (byte* inputBytes = &input[inputOffset], outputBytes = &output[outputOffset]) {
                    Process((float*)inputBytes, inputLength / 4, (float*)outputBytes, outputLength / 4, endOfInput,
                        out inputLengthUsed, out outputLengthGenerated);
                    inputLengthUsed *= 4;
                    outputLengthGenerated *= 4;
                }
            }
        }

        public void Process(float[] input, int inputOffset, int inputLength,
            float[] output, int outputOffset, int outputLength,
            bool endOfInput, out int inputLengthUsed, out int outputLengthGenerated) {
            unsafe {
                fixed (float* inputFloats = &input[inputOffset], outputFloats = &output[outputOffset]) {
                    Process(inputFloats, inputLength, outputFloats, outputLength, endOfInput, 
                        out inputLengthUsed, out outputLengthGenerated);
                }
            }
        }

        private unsafe void Process(float* input, int inputLength, float* output, int outputLength,
            bool endOfInput, out int inputLengthUsed, out int outputLengthGenerated) {
            srcData.data_in = input;
            srcData.data_out = output;
            srcData.end_of_input = endOfInput ? 1 : 0;
            srcData.input_frames = inputLength / channels;
            srcData.output_frames = outputLength / channels;
            srcData.src_ratio = ratio;

            error = interop.src_process(srcState, ref srcData);
            ThrowExceptionForError(error);

            inputLengthUsed = srcData.input_frames_used * channels;
            outputLengthGenerated = srcData.output_frames_gen * channels;

            bufferedSamples += inputLengthUsed - (outputLengthGenerated / ratio);
        }

        private void ThrowExceptionForError(int error) {
            if (error != 0) {
                throw new Exception(interop.src_strerror(error));
            }
        }
    }
}
