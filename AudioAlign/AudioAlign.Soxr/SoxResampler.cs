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
    public class SoxResampler {

        private SoxrInstance soxr;

        public SoxResampler(double inputRate, double outputRate, int channels) {
            SoxrError error = SoxrError.Zero;
            
            soxr = InteropWrapper.soxr_create(inputRate, outputRate, (uint)channels, 
                out error, SoxrIoSpec.Zero, SoxrQualitySpec.Zero, SoxrRuntimeSpec.Zero);
            
            if (error != SoxrError.Zero) {
                throw new SoxrException(GetError(error));
            }
        }

        public string Version {
            get { 
                StringPtr ptr = InteropWrapper.soxr_version();
                return Marshal.PtrToStringAnsi(ptr);
            }
        }

        public string Engine {
            get {
                StringPtr ptr = InteropWrapper.soxr_engine(soxr);
                return Marshal.PtrToStringAnsi(ptr);
            }
        }

        private string GetError(SoxrError error) {
            if (error == SoxrError.Zero) {
                return null;
            }
            else {
                return Marshal.PtrToStringAnsi(error);
            }
        }

        public string GetError() {
            SoxrError ptr = InteropWrapper.soxr_error(soxr);
            return GetError(ptr);
        }
    }
}
