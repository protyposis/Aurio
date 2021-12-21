// 
// Aurio: Audio Processing, Analysis and Retrieval Library
// Copyright (C) 2010-2017  Mario Guggenberger <mg@protyposis.net>
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as
// published by the Free Software Foundation, either version 3 of the
// License, or (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace Aurio.Streams
{
    public static class StreamExtensions
    {
        /// <summary>
        /// Searches for a stream of a given type in a hierarchy of nested streams.
        /// </summary>
        /// <typeparam name="T">the type of the stream to search for</typeparam>
        /// <param name="stream">a stream that may envelop a hierarchy of streams</param>
        /// <returns>the stream of the given type if found, else null</returns>
        public static T FindStream<T>(this IAudioStream stream)
        {
            FieldInfo fieldInfo = typeof(AbstractAudioStreamWrapper)
                .GetField("sourceStream", BindingFlags.Instance | BindingFlags.NonPublic);

            while (true)
            {
                if (stream is T)
                {
                    return (T)stream;
                }
                else if (stream is AbstractAudioStreamWrapper)
                {
                    stream = (IAudioStream)fieldInfo.GetValue(stream);
                }
                else
                {
                    return default(T);
                }
            }
        }

        /// <summary>
        /// Returns the direct source of a stream.
        /// </summary>
        /// <param name="stream">the stream of which the source is asked for</param>
        /// <returns>the source stream of the stream if existing, else null</returns>
        public static IAudioStream GetSourceStream(this IAudioStream stream)
        {
            if (!(stream is AbstractAudioStreamWrapper))
            {
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
        public static void PrintStreamHierarchy(this IAudioStream stream)
        {
            Console.WriteLine(stream.GetType().Name + " " + stream.Properties + " " + stream.Length + "@" + stream.Position);
            var source = GetSourceStream(stream);
            if (source != null)
            {
                PrintStreamHierarchy(source);
            }
        }
    }
}
