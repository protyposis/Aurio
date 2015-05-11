using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace Aurio.Audio.Streams {
    public static class StreamExtensions {
        /// <summary>
        /// Searches for a stream of a given type in a hierarchy of nested streams.
        /// </summary>
        /// <typeparam name="T">the type of the stream to search for</typeparam>
        /// <param name="stream">a stream that may envelop a hierarchy of streams</param>
        /// <returns>the stream of the given type if found, else null</returns>
        public static T FindStream<T>(this IAudioStream stream) {
            FieldInfo fieldInfo = typeof(AbstractAudioStreamWrapper)
                .GetField("sourceStream", BindingFlags.Instance | BindingFlags.NonPublic);

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

        /// <summary>
        /// Returns the direct source of a stream.
        /// </summary>
        /// <param name="stream">the stream of which the source is asked for</param>
        /// <returns>the source stream of the stream if existing, else null</returns>
        public static IAudioStream GetSourceStream(this IAudioStream stream) {
            if (!(stream is AbstractAudioStreamWrapper)) {
                return null;
            }

            FieldInfo fieldInfo = typeof(AbstractAudioStreamWrapper)
                .GetField("sourceStream", BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            return (IAudioStream)fieldInfo.GetValue(stream);
        }

        /// <summary>
        /// Goes through a hierarchy of nested streams and prints their type and properties to the console. 
        /// Useful for debugging.
        /// </summary>
        /// <param name="stream">the stream whose hierarchy should be printed</param>
        public static void PrintStreamHierarchy(this IAudioStream stream) {
            Console.WriteLine(stream.GetType().Name + " " + stream.Properties + " " + stream.Length + "@" + stream.Position);
            var source = GetSourceStream(stream);
            if (source != null) {
                PrintStreamHierarchy(source);
            }
        }
    }
}
