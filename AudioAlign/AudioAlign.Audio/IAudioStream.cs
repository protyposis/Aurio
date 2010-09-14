using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioAlign.Audio {
    /// <summary>
    /// A generic audio stream.
    /// </summary>
    /// <typeparam name="T">The type of the samples of the audio stream. Must be at least as wide as the 
    /// bitrate of the stream is, otherwise unexpected things may happen (clipping, in the best case).</typeparam>
    public interface IAudioStream<T> {
        /// <summary>
        /// Returns the properties of the audio stream (e.g. sample rate, bit rate, number of channels).
        /// </summary>
        AudioProperties Properties { get; }

        /// <summary>
        /// Gets the total length of the audio stream in units of time.
        /// </summary>
        TimeSpan TimeLength { get; }
        
        /// <summary>
        /// Gets or sets the current position in the stream in units of time.
        /// </summary>
        TimeSpan TimePosition { get; set; }
        
        /// <summary>
        /// Gets the total length of the audio stream in units of samples.
        /// </summary>
        long SampleCount { get; }

        /// <summary>
        /// Gets or sets the current position in the audio stream in units of samples.
        /// </summary>
        long SamplePosition { get; set; }

        /// <summary>
        /// Reads samples of a time span at the current position in the audio stream. It isn't guaranteed 
        /// that the requested time span will be completely read (e.g. in case of an EOF, or for reasons of 
        /// the underlying layer), therefore the method returns the time span that has actually been read.
        /// 
        /// The layout of the buffer is the following: [channelNumber, sampleNumber]
        /// </summary>
        /// <param name="sampleBuffer">the buffer to fill with samples</param>
        /// <param name="timeToRead">the number of samples to read in units of time</param>
        /// <returns>the number of samples read, in units of time</returns>
        TimeSpan Read(T[][] sampleBuffer, TimeSpan timeToRead);

        /// <summary>
        /// Reads samples at the current position in the audio stream. It isn't guaranteed that all requested
        /// samples will be read (e.g. in case of an EOF, or for reasons of the underlying layer), therefore
        /// the method returns the number of samples that have actually been read.
        /// 
        /// The layout of the buffer is the following: [channelNumber, sampleNumber]
        /// </summary>
        /// <param name="sampleBuffer">the buffer to fill with samples</param>
        /// <param name="samplesToRead">the number of samples to read</param>
        /// <returns>the number of samples read</returns>
        int Read(T[][] sampleBuffer, int samplesToRead);
    }
}
