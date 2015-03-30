using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioAlign.Soxr {
    public class SoxrException : Exception {
        public SoxrException() : base() { }
        public SoxrException(string message) : base(message) { }
        public SoxrException(string message, Exception innerException) : base(message, innerException) { }
    }
}
