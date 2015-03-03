using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AudioAlign.FFmpeg {
    public class FFmpegReader : IDisposable {

        private static IInteropWrapper interop;

        static FFmpegReader() {
            if (IntPtr.Size == 8) {
                interop = new Interop64Wrapper();
            }
            else {
                interop = new Interop32Wrapper();
            }
        }

        private bool disposed = false;
        private IntPtr instance = IntPtr.Zero;
        private OutputConfig outputConfig;

        public FFmpegReader(string filename) {
            instance = interop.stream_open(filename);
            
            IntPtr ocp = interop.stream_get_output_config(instance);
            outputConfig = (OutputConfig)Marshal.PtrToStructure(ocp, typeof(OutputConfig));
            
            Console.WriteLine(outputConfig.ToString());
        }

        public int ReadFrame() {
            return interop.stream_read_frame(instance);
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
                interop.stream_close(instance);
                instance = IntPtr.Zero;
            }
            disposed = true;

            // If it is available, make the call to the
            // base class's Dispose(Boolean) method
            //base.Dispose(disposing);
        }

        ~FFmpegReader() {
            Dispose(false);
        }

        #endregion
    }
}
