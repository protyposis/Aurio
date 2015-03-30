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

        public SoxResampler(double inputRate, double outputRate, int channels) {
            SoxrError error = SoxrError.Zero;
            
            soxr = InteropWrapper.soxr_create(inputRate, outputRate, (uint)channels, 
                out error, SoxrIoSpec.Zero, SoxrQualitySpec.Zero, SoxrRuntimeSpec.Zero);
            
            if (error != SoxrError.Zero) {
                throw new SoxrException(GetError(error));
            }
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
