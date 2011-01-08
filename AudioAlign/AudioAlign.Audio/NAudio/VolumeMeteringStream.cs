using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NAudio.Wave;

namespace AudioAlign.Audio.NAudio {
    /// <summary>
    /// basic metering stream
    /// n.b. does not close its source stream
    /// 
    /// taken from NAudio's NAudioDemo application
    /// </summary>
    public class VolumeMeteringStream : WaveStream {
        public WaveStream SourceStream { get; private set; }
        public int SamplesPerNotification { get; set; }

        float[] maxSamples;
        int sampleCount;

        public event EventHandler<StreamVolumeEventArgs> StreamVolume;

        public VolumeMeteringStream(WaveStream sourceStream) :
            this(sourceStream, sourceStream.WaveFormat.SampleRate / 10) {
        }

        public VolumeMeteringStream(WaveStream sourceStream, int samplesPerNotification) {
            SourceStream = sourceStream;
            if (sourceStream.WaveFormat.BitsPerSample != 32)
                throw new ArgumentException("Metering Stream expects 32 bit floating point audio", "sourceStream");
            maxSamples = new float[sourceStream.WaveFormat.Channels];
            this.SamplesPerNotification = samplesPerNotification;
        }

        public override WaveFormat WaveFormat {
            get { return SourceStream.WaveFormat; }
        }

        public override long Length {
            get { return SourceStream.Length; }
        }

        public override long Position {
            get { return SourceStream.Position; }
            set { SourceStream.Position = value; }
        }

        public override int Read(byte[] buffer, int offset, int count) {
            int bytesRead = SourceStream.Read(buffer, offset, count);
            ProcessData(buffer, offset, bytesRead);
            return bytesRead;
        }

        private void ProcessData(byte[] buffer, int offset, int count) {
            int index = 0;
            while (index < count) {
                for (int channel = 0; channel < maxSamples.Length; channel++) {
                    float sampleValue = Math.Abs(BitConverter.ToSingle(buffer, offset + index));
                    maxSamples[channel] = Math.Max(maxSamples[channel], sampleValue);
                    index += 4;
                }
                sampleCount++;
                if (sampleCount >= SamplesPerNotification) {
                    RaiseStreamVolumeNotification();
                    sampleCount = 0;
                    Array.Clear(maxSamples, 0, maxSamples.Length);

                }

            }
        }

        private void RaiseStreamVolumeNotification() {
            if (StreamVolume != null) {
                StreamVolume(this, new StreamVolumeEventArgs() { MaxSampleValues = (float[])maxSamples.Clone() });
            }
        }
    }

    public class StreamVolumeEventArgs : EventArgs {
        public float[] MaxSampleValues { get; set; }
    }
}
