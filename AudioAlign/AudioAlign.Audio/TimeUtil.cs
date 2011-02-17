using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioAlign.Audio {
    public static class TimeUtil {

        public static long TimeSpanToBytes(TimeSpan timeSpan, Streams.AudioProperties audioProperties) {
            int sampleBlockSize = audioProperties.SampleByteSize * audioProperties.Channels;
            long bytes = (long)(timeSpan.TotalSeconds * audioProperties.SampleRate * sampleBlockSize);
            return bytes - (bytes % sampleBlockSize);
        }

        public static TimeSpan BytesToTimeSpan(long bytes, Streams.AudioProperties audioProperties) {
            return new TimeSpan(0, 0, (int)(bytes / audioProperties.SampleBlockByteSize));
        }
    }
}
