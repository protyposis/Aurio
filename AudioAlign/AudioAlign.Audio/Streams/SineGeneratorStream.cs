using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioAlign.Audio.Streams {
    public class SineGeneratorStream : IAudioStream {

        private AudioProperties properties;
        private float frequency;
        private long length;
        private long position;

        public SineGeneratorStream(int sampleRate, float frequency, TimeSpan length) {
            this.properties = new AudioProperties(1, sampleRate, 32, AudioFormat.IEEE);
            this.frequency = frequency;
            this.length = TimeUtil.TimeSpanToBytes(length, properties);
        }

        public AudioProperties Properties {
            get { return properties; }
        }

        public long Length {
            get { return length; }
        }

        public long Position {
            get { return position; }
            set { position = value; }
        }

        public int SampleBlockSize {
            get { return properties.SampleBlockByteSize; }
        }

        public float Frequency {
            get { return frequency; }
        }

        public int Read(float[] buffer, int offset, int count) {
            float frequencyFactor = count / frequency;
            for (int x = 0; x < count; x++) {
                buffer[offset + x] = (float)Math.Sin((position + x) / frequencyFactor * Math.PI * 2);
            }
            return count;
        }

        public int Read(byte[] buffer, int offset, int count) {
            if (count % SampleBlockSize != 0) {
                throw new Exception("count is not aligned to the sample block size");
            }
            AudioBuffer audioBuffer = new AudioBuffer(buffer);
            return Read(audioBuffer.FloatData, offset / 4, count / 4);
        }
    }
}
