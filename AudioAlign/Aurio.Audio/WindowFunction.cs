using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioAlign.Audio {
    public class WindowFunction {

        private float[] window;

        internal WindowFunction(float[] window, WindowType type) {
            this.window = window;
            this.Type = type;
        }

        public WindowType Type { get; private set; }
        public int Size { get { return window.Length; } }

        public void Apply(float[] values) {
            WindowUtil.Apply(values, window);
        }
    }
}
