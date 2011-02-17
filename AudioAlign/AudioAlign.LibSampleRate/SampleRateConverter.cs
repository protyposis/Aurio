using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioAlign.LibSampleRate {
    public class SampleRateConverter : IDisposable {

        private bool disposed = false;
        private IntPtr srcState = IntPtr.Zero;
        private int channels;
        private double ratio;

        public SampleRateConverter(ConverterType type, int channels) {
            int error = 0;
            srcState = Interop.src_new(type, channels, out error);
            ThrowExceptionForError(error);

            SetRatio(1d);

            this.channels = channels;
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
                srcState = Interop.src_delete(srcState);
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

        public void Reset() {
            int error = Interop.src_reset(srcState);
            ThrowExceptionForError(error);
        }

        public void SetRatio(double ratio) {
            int error = Interop.src_set_ratio(srcState, ratio);
            ThrowExceptionForError(error);
            this.ratio = ratio;
        }

        public bool CheckRatio(double ratio) {
            return Interop.src_is_valid_ratio(ratio) == 1;
        }

        public void Process(byte[] input, int inputOffset, int inputLength,
            byte[] output, int outputOffset, int outputLength,
            bool endOfStream, out int inputLengthUsed, out int outputLengthGenerated) {
            SRC_DATA data = new SRC_DATA();
            unsafe {
                fixed (byte* inputBytes = &input[inputOffset], outputBytes = &output[outputOffset]) {
                    data.data_in = (float*)inputBytes;
                    data.data_out = (float*)outputBytes;
                    data.end_of_input = endOfStream ? 1 : 0;
                    data.input_frames = inputLength / 4 / channels;
                    data.output_frames = outputLength / 4 / channels;
                    data.src_ratio = ratio;

                    int error = Interop.src_process(srcState, ref data);
                    ThrowExceptionForError(error);

                    inputLengthUsed = data.input_frames_used * 4 * channels;
                    outputLengthGenerated = data.output_frames_gen * 4 * channels;
                }
            }
        }

        public void Process(float[] input, int inputOffset, int inputLength,
            float[] output, int outputOffset, int outputLength,
            bool endOfStream, out int inputLengthUsed, out int outputLengthGenerated) {
            SRC_DATA data = new SRC_DATA();
            unsafe {
                fixed (float* inputFloats = &input[inputOffset], outputFloats = &output[outputOffset]) {
                    data.data_in = inputFloats;
                    data.data_out = outputFloats;
                    data.end_of_input = endOfStream ? 1 : 0;
                    data.input_frames = inputLength / channels;
                    data.output_frames = outputLength / channels;

                    int error = Interop.src_process(srcState, ref data);
                    ThrowExceptionForError(error);

                    inputLengthUsed = data.input_frames_used * channels;
                    outputLengthGenerated = data.output_frames_gen * channels;
                }
            }
        }

        private void ThrowExceptionForError(int error) {
            if (error != 0) {
                throw new Exception(Interop.src_strerror(error));
            }
        }
    }
}
