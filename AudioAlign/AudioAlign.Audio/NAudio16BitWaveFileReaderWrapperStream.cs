using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NAudio.Wave;

namespace AudioAlign.Audio {
    internal class NAudio16BitWaveFileReaderWrapperStream: AbstractAudioStream<float>, IAudioStream16 {
        private WaveFileReader waveFileReader;

        public NAudio16BitWaveFileReaderWrapperStream(WaveFileReader waveFileReader) {
            this.waveFileReader = waveFileReader;
            Properties = new AudioProperties(
                waveFileReader.WaveFormat.Channels, 
                waveFileReader.WaveFormat.SampleRate, 
                waveFileReader.WaveFormat.BitsPerSample);

            TimeLength = waveFileReader.TotalTime;
            TimePosition = waveFileReader.CurrentTime;
            SampleCount = ByteToSamplePosition(waveFileReader.Length);
            SamplePosition = ByteToSamplePosition(waveFileReader.Position);
        }

        public override TimeSpan TimePosition {
            get { return waveFileReader.CurrentTime; }
            set { waveFileReader.CurrentTime = value; }
        }

        public override long SamplePosition {
            get { return ByteToSamplePosition(waveFileReader.Position); }
            set { waveFileReader.Position = SampleToBytePosition(value); }
        }

        // TODO direkt auf byteStream operieren
        public override TimeSpan Read(float[][] sampleBuffer, TimeSpan timeToRead) {
            waveFileReader.Read(sampleBuffer, AudioUtil.CalculateSamples(Properties, timeToRead));
            return timeToRead;
        }

        // TODO direkt auf byteStream operieren
        public override int Read(float[][] sampleBuffer, int samplesToRead) {
            return waveFileReader.Read(sampleBuffer, samplesToRead);
        }

        private long ByteToSamplePosition(long bytePosition) {
            return bytePosition / (Properties.BitDepth / 8) / Properties.Channels;
        }

        private long SampleToBytePosition(long samplePosition) {
            return samplePosition * (Properties.BitDepth / 8) * Properties.Channels;
        }
    }
}
