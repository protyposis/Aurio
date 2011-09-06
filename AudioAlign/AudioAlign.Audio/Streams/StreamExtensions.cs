using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace AudioAlign.Audio.Streams {
    public static class StreamExtensions {
        /// <summary>
        /// Searches for a stream of a given type in a hierarchy of nested streams.
        /// </summary>
        /// <typeparam name="T">the type of the stream to search for</typeparam>
        /// <param name="stream">a stream that may envelop a hierarchy of streams</param>
        /// <returns>the stream of the given type if found, else null</returns>
        public static T FindStream<T>(this IAudioStream stream) {
            FieldInfo fieldInfo = typeof(AbstractAudioStreamWrapper)
                .GetField("sourceStream", System.Reflection.BindingFlags.NonPublic);

            while (true) {
                if (stream is T) {
                    return (T)stream;
                }
                else if (stream is AbstractAudioStreamWrapper) {
                    stream = (IAudioStream)fieldInfo.GetValue(stream);
                }
                else { 
                    return default(T); 
                }
            }
        }
    }
}
