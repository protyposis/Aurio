using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AudioAlign.Audio.Streams;

namespace AudioAlign.Audio {
    public static class StreamUtil {

        public static int ForceRead(IAudioStream audioStream, byte[] buffer, int offset, int count) {
            int totalBytesRead = 0;
            int bytesRead = 0;

            while (count - totalBytesRead > 0 && (bytesRead = audioStream.Read(buffer, offset + totalBytesRead, count - totalBytesRead)) > 0) {
                totalBytesRead += bytesRead;
            }

            return totalBytesRead;
        }
    }
}
