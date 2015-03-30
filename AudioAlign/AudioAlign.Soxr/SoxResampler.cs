using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using StringPtr = System.IntPtr;
using SoxrInstance = System.IntPtr;
using SoxrError = System.IntPtr;
using SoxrIoSpec = System.IntPtr;
using SoxrQualitySpec = System.IntPtr;
using SoxrRuntimeSpec = System.IntPtr;
using System.Runtime.InteropServices;

namespace AudioAlign.Soxr {
    public class SoxResampler : IDisposable {

        private SoxrInstance soxr;
        private int channels;

        public SoxResampler(double inputRate, double outputRate, int channels) {
            SoxrError error = SoxrError.Zero;
            
            soxr = InteropWrapper.soxr_create(inputRate, outputRate, (uint)channels, 
                out error, SoxrIoSpec.Zero, SoxrQualitySpec.Zero, SoxrRuntimeSpec.Zero);
            
            if (error != SoxrError.Zero) {
                throw new SoxrException(GetError(error));
            }

            this.channels = channels;
        }

        /// <summary>
        /// Returns the libsoxr version.
        /// </summary>
        public string Version {
            get { 
                StringPtr ptr = InteropWrapper.soxr_version();
                return Marshal.PtrToStringAnsi(ptr);
            }
        }

        /// <summary>
        /// Returns the name of the active resampling engine.
        /// </summary>
        public string Engine {
            get {
                StringPtr ptr = InteropWrapper.soxr_engine(soxr);
                return Marshal.PtrToStringAnsi(ptr);
            }
        }

        public double GetOutputDelay() {
            return InteropWrapper.soxr_delay(soxr);
        }

        /// <summary>
        /// Resamples input data to output data according to the configuration of the resampler.
        /// 
        /// The input and output data is expected to be interleaved with the number of channels
        /// supplied in the constructor.
        /// 
        /// All data sizes are byte sizes. E.g. to get the number of output samples, divide the
        /// output length by 4 (to get the number of float samples) and then by the number of 
        /// channels to get the number of samples for a single channel.
        /// </summary>
        /// <param name="input">a byte array of interleaved float samples</param>
        /// <param name="inputOffset">the byte offset in the input array</param>
        /// <param name="inputLength">the byte length available in the input array</param>
        /// <param name="output">a byte array of interleaved float samples</param>
        /// <param name="outputOffset">the byte offset in the output array</param>
        /// <param name="outputLength">the byte length available in the output array</param>
        /// <param name="endOfInput">set to true if there is no more input data</param>
        /// <param name="inputLengthUsed">the number of bytes read</param>
        /// <param name="outputLengthGenerated">the number of bytes output</param>
        public void Process(byte[] input, int inputOffset, int inputLength,
            byte[] output, int outputOffset, int outputLength,
            bool endOfInput, out int inputLengthUsed, out int outputLengthGenerated) {

            // Only 32-bit float samples are supported
            int sampleBlockByteSize = 4 * channels;

            uint ilen = (uint)(inputLength / sampleBlockByteSize);
            uint olen = (uint)(outputLength / sampleBlockByteSize);
            uint idone = 0;
            uint odone = 0;

            unsafe {
                fixed (byte* inputBytes = &input[inputOffset], outputBytes = &output[outputOffset]) {
                    SoxrError error = InteropWrapper.soxr_process(soxr, 
                        endOfInput ? null : inputBytes, ilen, out idone, 
                        outputBytes, olen, out odone);

                    if (error != SoxrError.Zero) {
                        throw new SoxrException("Processing failed: " + GetError(error));
                    }
                }
            }

            inputLengthUsed = (int)(idone * sampleBlockByteSize);
            outputLengthGenerated = (int)(odone * sampleBlockByteSize);
        }

        /// <summary>
        /// Converts an error pointer to an error message.
        /// </summary>
        /// <param name="error">the error pointer to convert to the error message</param>
        /// <returns>An error message, or null if no error reported</returns>
        private string GetError(SoxrError error) {
            if (error == SoxrError.Zero) {
                return null;
            }
            else {
                return Marshal.PtrToStringAnsi(error);
            }
        }

        /// <summary>
        /// Returns the most recent error message.
        /// </summary>
        public string GetError() {
            SoxrError ptr = InteropWrapper.soxr_error(soxr);
            return GetError(ptr);
        }

        /// <summary>
        /// Clear internal state (e.g. buffered data) for a fresh resampling session, 
        /// but keeping the same configuration.
        /// </summary>
        public void Clear() {
            SoxrError error = InteropWrapper.soxr_clear(soxr);

            if (error != SoxrError.Zero) {
                throw new SoxrException("Cannot clear state: " + GetError(error));
            }
        }

        /// <summary>
        /// Deletes the current resampler instance and frees its memory. 
        /// To be called on destruction.
        /// </summary>
        private void Delete() {
            // Check if an instance is existing and only delete it if this is the case, avoiding
            // an exception if called multiple times (i.e. by Dispose and the destructor).
            if (soxr != SoxrInstance.Zero) {
                InteropWrapper.soxr_delete(soxr);
                soxr = SoxrInstance.Zero;
            }
        }

        ~SoxResampler() {
            Delete();
        }

        public void Dispose() {
            Delete();
        }
    }
}
