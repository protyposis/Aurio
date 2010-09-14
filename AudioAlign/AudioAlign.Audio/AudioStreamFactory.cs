using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NAudio.Wave;

namespace AudioAlign.Audio {
    public static class AudioStreamFactory {
        public static IAudioStream16 FromFilename(string filename) {
            return new NAudio16BitWaveFileReaderWrapperStream(new WaveFileReader(filename));
        }
    }
}
