using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioAlign.Audio {
    internal abstract class AbstractAudioStream<T>: IAudioStream<T> {

        #region IAudioStream<T> Members

        public AudioProperties Properties {
            get;
            protected set;
        }

        public TimeSpan TimeLength {
            get;
            protected set;
        }

        public abstract TimeSpan TimePosition {
            get;
            set;
        }

        public long SampleCount {
            get;
            protected set;
        }

        public abstract long SamplePosition {
            get;
            set;
        }

        public abstract TimeSpan Read(T[][] sampleBuffer, TimeSpan timeToRead);

        public abstract int Read(T[][] sampleBuffer, int samplesToRead);

        #endregion
    }
}
