using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using StringPtr = System.IntPtr;
using SoxrInstance = System.IntPtr;
using SoxrError = System.IntPtr;
using System.Runtime.InteropServices;

namespace AudioAlign.Soxr {
    public class SoxResampler : IDisposable {

        private SoxrInstance soxr;
        private bool variableRate;
        private int channels;

        /// <summary>
        /// Creates a new resampler with the supplied quality parameters. 
        /// 
        /// Can be used to create a variable-rate resampler by supplying SOXR_HQ as recipe 
        /// and SOXR_VR as flag. For a variable resampler, inputRate/outputRate must equate
        /// to the maximum IO ratio that will be set by calling SetIoRatio. To set the initial
        /// ratio, call SetIoRatio with a transitionLength of 0 on the created instance.
        /// </summary>
        /// <param name="inputRate"></param>
        /// <param name="outputRate"></param>
        /// <param name="channels"></param>
        /// <param name="qualityRecipe"></param>
        /// <param name="qualityFlags"></param>
        /// <exception cref="SoxrException">when parameters are incorrect; see error message for details</exception>
        public SoxResampler(double inputRate, double outputRate, int channels, 
            QualityRecipe qualityRecipe, QualityFlags qualityFlags) {

            SoxrError error = SoxrError.Zero;

            // Apply the default configuration as per soxr.c
            InteropWrapper.SoxrIoSpec ioSpec = InteropWrapper.soxr_io_spec(Datatype.SOXR_FLOAT32_I, Datatype.SOXR_FLOAT32_I);
            InteropWrapper.SoxrQualitySpec qSpec = InteropWrapper.soxr_quality_spec(qualityRecipe, qualityFlags);
            InteropWrapper.SoxrRuntimeSpec rtSpec = InteropWrapper.soxr_runtime_spec(1);

            if (qualityFlags == QualityFlags.SOXR_VR) {
                if (qualityRecipe != QualityRecipe.SOXR_HQ) {
                    throw new SoxrException("Invalid parameters: variable rate resampling only works with the HQ recipe");
                }
                variableRate = true;
            }
            
            soxr = InteropWrapper.soxr_create(inputRate, outputRate, (uint)channels,
                out error, ref ioSpec, ref qSpec, ref rtSpec);
            
            if (error != SoxrError.Zero) {
                throw new SoxrException(GetError(error));
            }

            this.channels = channels;
        }

        /// <summary>
        /// Creates a new resampler instance with the supplied quality.
        /// </summary>
        /// <param name="inputRate"></param>
        /// <param name="outputRate"></param>
        /// <param name="channels"></param>
        /// <exception cref="SoxrException">when parameters are incorrect; see error message for details</exception>
        public SoxResampler(double inputRate, double outputRate, int channels, QualityRecipe qualityRecipe) :
            this(inputRate, outputRate, channels,
            qualityRecipe, QualityFlags.SOXR_ROLLOFF_SMALL) { }

        /// <summary>
        /// Creates a new resampler instance with the default configuration.
        /// </summary>
        /// <param name="inputRate"></param>
        /// <param name="outputRate"></param>
        /// <param name="channels"></param>
        /// <exception cref="SoxrException">when parameters are incorrect; see error message for details</exception>
        public SoxResampler(double inputRate, double outputRate, int channels) :
            this(inputRate, outputRate, channels,
            QualityRecipe.SOXR_HQ, QualityFlags.SOXR_ROLLOFF_SMALL) { }

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
        /// <exception cref="SoxrException">if an error happens during processing, see error message for details</exception>
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
        /// Transitions to a new resampling ratio for variable-rate resampling over the given length.
        /// Set the length to 0 for an instant change.
        /// </summary>
        /// <param name="ratio">the new resampling ratio</param>
        /// <param name="transitionLength">the length over which to linearly transition to the new ratio</param>
        /// <exception cref="SoxrException">when the resampler is not configured for variable-rate resampling</exception>
        public void SetIoRatio(double ratio, int transitionLength) {
            if (!variableRate) {
                throw new SoxrException("Illegal call: set_io_ratio only works in variable-rate resampling mode");
            }

            SoxrError error = InteropWrapper.soxr_set_io_ratio(soxr, ratio, (uint)transitionLength);

            if (error != SoxrError.Zero) {
                throw new SoxrException("Error changing IO ratio: " + GetError(error));
            }
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
        /// <exception cref="SoxrException">when clearing fails, see error message for details</exception>
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
