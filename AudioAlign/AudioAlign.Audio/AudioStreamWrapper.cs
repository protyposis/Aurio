using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioAlign.Audio {
    public class AudioStreamWrapper<T>: IAudioStream<T> {

        protected IAudioStream<T> audioStream;

        public AudioStreamWrapper(IAudioStream<T> audioStream) {
            this.audioStream = audioStream;
        }

        #region IAudioStream<T> Members

        public virtual AudioProperties Properties {
            get { return audioStream.Properties; }
        }

        public virtual TimeSpan TimeLength {
            get { return audioStream.TimeLength; }
        }

        public virtual TimeSpan TimePosition {
            get { return audioStream.TimePosition; }
            set { audioStream.TimePosition = value; }
        }

        public virtual long SampleCount {
            get { return audioStream.SampleCount; }
        }

        public virtual long SamplePosition {
            get { return audioStream.SamplePosition; }
            set { audioStream.SamplePosition = value; }
        }

        public virtual TimeSpan Read(T[][] sampleBuffer, TimeSpan timeToRead) {
            return audioStream.Read(sampleBuffer, timeToRead);
        }

        public virtual int Read(T[][] sampleBuffer, int samplesToRead) {
            return audioStream.Read(sampleBuffer, samplesToRead);
        }

        #endregion
    }
}
