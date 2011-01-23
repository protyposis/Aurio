using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NAudio.Wave;
using System.Reflection;

namespace AudioAlign.Audio.NAudio {
    public class ExtendedWaveMixerStream32 : WaveMixerStream32 {
        
        /// <summary>
        /// This is a copy of the same field in the superclass. Since the superclass' field is private I
        /// cannot access it and need to create a second reference to it in this class through reflection.
        /// </summary>
        private List<WaveStream> inputStreams;

        private FieldInfo lengthField;

        public ExtendedWaveMixerStream32() : base() {
            InitializeSuperclassReferences();
        }

        public ExtendedWaveMixerStream32(IEnumerable<WaveStream> inputStreams, bool autoStop)
            : base(inputStreams, autoStop) {
            InitializeSuperclassReferences();
        }

        private void InitializeSuperclassReferences() {
            FieldInfo fi = typeof(WaveMixerStream32).GetField("inputStreams", BindingFlags.NonPublic | BindingFlags.Instance);
            inputStreams = (List<WaveStream>)fi.GetValue(this);
            if (inputStreams == null) {
                throw new NullReferenceException("could not reference the input streams collection");
            }

            lengthField = typeof(WaveMixerStream32).GetField("length", BindingFlags.NonPublic | BindingFlags.Instance);
            if (lengthField == null) {
                throw new NullReferenceException("could not reference the length field");
            }
        }

        public void UpdateLength() {
            long length = 0;
            foreach (WaveStream inputStream in inputStreams) {
                length = Math.Max(length, inputStream.Length);
            }
            lengthField.SetValue(this, length);
            if (this.Length != length) {
                throw new Exception("could not set the length");
            }
        }
    }
}
