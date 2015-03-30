using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SoxrInstance = System.IntPtr;
using SoxrError = System.IntPtr;
using SoxrIoSpec = System.IntPtr;
using SoxrQualitySpec = System.IntPtr;
using SoxrRuntimeSpec = System.IntPtr;

namespace AudioAlign.Soxr {
    public class SoxResampler {

        private SoxrInstance soxr;

        public SoxResampler(double inputRate, double outputRate, int channels) {
            soxr = InteropWrapper.soxr_create(inputRate, outputRate, (uint)channels, SoxrError.Zero, SoxrIoSpec.Zero , SoxrQualitySpec.Zero, SoxrRuntimeSpec.Zero);
        }

        public string Version {
            get { return InteropWrapper.soxr_version(); }
        }
    }
}
