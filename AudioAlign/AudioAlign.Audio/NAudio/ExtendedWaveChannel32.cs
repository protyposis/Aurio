using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NAudio.Wave;
using System.Reflection;

namespace AudioAlign.Audio.NAudio {
    public class ExtendedWaveChannel32 : WaveChannel32 {

        /// <summary>
        /// This is a copy of the same field in the superclass.
        /// </summary>
        private WaveStream sourceStream;

        private FieldInfo lengthField;

        public ExtendedWaveChannel32(WaveStream sourceStream)
            : base(sourceStream) {
            this.sourceStream = sourceStream;
            InitializeSuperclassReferences();
        }

        public ExtendedWaveChannel32(WaveStream sourceStream, float volume, float pan)
            : base(sourceStream, volume, pan) {
            this.sourceStream = sourceStream;
            InitializeSuperclassReferences();
        }

        private void InitializeSuperclassReferences() {
            lengthField = typeof(WaveChannel32).GetField("length", BindingFlags.NonPublic | BindingFlags.Instance);
            if (lengthField == null) {
                throw new NullReferenceException("could not reference the length field");
            }
        }

        public void UpdateLength() {
            // length calculation copied from WaveChannel32 constructor
            int destBytesPerSample = 8;
            int sourceBytesPerSample = sourceStream.WaveFormat.Channels * sourceStream.WaveFormat.BitsPerSample / 8;
            long sourceSamples = sourceStream.Length / sourceBytesPerSample;
            long length = sourceSamples * destBytesPerSample; // output is stereo

            lengthField.SetValue(this, length);
            if (this.Length != length) {
                throw new Exception("could not set the length");
            }
        }
    }
}
