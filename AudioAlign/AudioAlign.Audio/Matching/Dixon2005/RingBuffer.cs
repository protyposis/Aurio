using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioAlign.Audio.Matching.Dixon2005 {
    class RingBuffer<T> {

        private T[] buffer;
        private int bufferSize;
        private int bufferStart;
        private int bufferFillLevel;

        public RingBuffer(int size) {
            this.buffer = new T[size];
            this.bufferSize = size;
            this.bufferStart = 0;
            this.bufferFillLevel = 0;
        }

        public int Length {
            get { return bufferSize; }
        }

        public int Count {
            get { return bufferFillLevel; }
        }

        public void Add(T data) {
            buffer[bufferStart] = data;
            bufferStart = (bufferStart + 1) % bufferSize;
            if (bufferFillLevel < bufferSize) {
                bufferFillLevel++;
            }
        }

        public T this[int index] {
            get {
                if(index < 0 || index >= bufferFillLevel) {
                    throw new IndexOutOfRangeException();
                }
                int realIndex = (index + bufferStart + (bufferSize - bufferFillLevel)) % bufferSize;
                return buffer[realIndex];
            }
        }
    }
}
