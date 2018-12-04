Aurio: Audio Fingerprinting & Retrieval for .NET
================================================

Aurio is a .NET library that focuses on audio processing, analysis, media synchronization and media retrieval and implements various audio fingerprinting methods. It has been developed for research purposes and is a by-product of the media synchronization application [AudioAlign](https://github.com/protyposis/AudioAlign).


Features
--------

* 32-bit floating point audio processing engine
* File I/O through NAudio and FFmpeg
* Audio playback through NAudio
* FFT/iFFT through PFFFT, FFTW (optional) and Exocortex.DSP (optional)
* Resampling through Soxr and SecretRabbitCode/libsamplerate (optional)
* Stream windowing and overlap-adding
* STFT, inverse STFT
* Chroma
* Dynamic Time Warping
* On-line Time Warping (Dixon, Simon. "Live tracking of musical performances using on-line time warping." Proceedings of the 8th International Conference on Digital Audio Effects. 2005.)
* Fingerprinting
 *  Haitsma, Jaap, and Ton Kalker. "A highly robust audio fingerprinting system." ISMIR. 2002.
 *  Wang, Avery. "An Industrial Strength Audio Search Algorithm." ISMIR. 2003.
 *  [Echoprint](http://echoprint.me/codegen) (Ellis, Daniel PW, Brian Whitman, and Alastair Porter. "Echoprint: An open music identification service." ISMIR. 2011.)
 *  AcoustID [Chromaprint](https://acoustid.org/chromaprint)

All audio processing (incl. fingerprinting) is stream-based and supports processing of arbitrarily long streams at constant memory usage. All fingerprinting methods are implemented from scratch, not ports from existing libraries, while keeping compatibility where possible.

Aurio.WaveControls provides WPF widgets for user interfaces:

* Spectrogram / Chromagram View
* Spectrum / Graph View
* VU Meter
* Correlometer
* Time Scale
* Wave View


What's new
----------

### 2018-12-04 d01cb3f
* Introduced `IAudioStreamFactory` pattern that allows to register external audio stream factories in `AudioStreamFactory` to read streams
* Removed FFmpeg dependency from core
  * Moved `FFmpegSourceStream` to `Aurio.FFmpeg`
  * Added `FFmpegAudioStreamFactory` to `Aurio.FFmpeg`
* Updated test apps and tools with factory initializations

### 2018-03-22 5af5339
* Converted all Aurio library packages to .NET Standard 2.0 for .NET Core 2.0 compatibility
  * Updated WPF GUI apps from NET Framework 4.0 to 4.6.2 for .NET Standard 2.0 compatibility
  * Requires Visual Studio 2017
* Added support for realtime (live) stream processing
  * `CircularMemoryWriterStream` with read and write support of audio data into a circular buffer
  * `BlockingFixedLengthFifoStream` with blocking read and write support of audio data into a fixed-length FIFO buffer (converts pull-based stream processing into a push-based approach suitable for realtime applications)
  * `Aurio.Test.RealtimeFingerprinting`: .NET Core realtime audio fingerprinting example/demo app  
* Removed resampler dependency from core
  * Added `Aurio.Resampler.ResamplerFactory` that can be configured with various resampler implementations
  * Implemented `ResamplerFactory` for NAudio WdlResampler (managed code, recommended for .NET Core cross-plattform deployment), LibSampleRate, and Soxr (recommended for quality and speed)
* Removed FFT dependency from core
  * Added `Aurio.FFT.FFTFactory` that can be configured with various FFT implementations
  * Implemented `FFTFactory` for Exocortex.DSP (managed code, recommended for .NET Core cross-plattform deployment), FFTW, and PFFFT (recommended for quality and speed) 
* Moved SQLite dependency (`SQLiteCollisionMap`) from core to optional `Aurio.Matching.SQLite` package 
* Added `FingerprintStore.FindMatchesFromExternalSubFingerprints` to match external fingerprints with the contents of a store
* Parameterized `StreamWindower`/`STFT`/`FingerprintGenerator` buffer size (to minimize processing latency)
* Parameterized `FingerprintGenerator` update event interval
* Fixed FFmpeg postbuild library copy command 

### 2017-10-15 14c7c92
* FFmpeg updated to 3.3.3
* `SurroundDownmixStream` for 4.0/5.1/7.1 to stereo downmixing
* Validate matches before executing alignment and throw exception when an invalid match sequence is detected
* Add progress callback to `FingerprintStore`s match finder
* Various bug fixes

### 2017-02-06 3e703cd

* `Complex` type
* Various `StreamWindower` `OutputFormat`s: `Raw`, `Magnitudes`, `MagnitudesSquared`, `MagnitudesAndPhases`, and `Decibel`
* Overlap-adder `OLA`
* Inverse STFT `InverseSTFT`
* Writable audio streams
  * `IAudioWriterStream` interface
  * `MemoryWriterStream` that writes audio to memory
* `Aurio.WaveControls`:
  * `CaretOverlay`, `MultiplicationConverter` migrated from AudioAlign
  * `TrackMarkerOverlay` to add markers with labels onto audio tracks
* Inverse FFT transformation in `Aurio.PFFFT`
* Concatenated `AudioTrack`s (audio tracks consisting of multiple files)
* Seek indizes for FFmpeg input
  * Seek index support in `Aurio.FFmpeg.Proxy` and `Aurio.FFmpeg`'s `FFmpegReader`
  * Automatic seek index generation in `FFmpegSourceStream` for streams that do not natively support seeking
* Windowing moved from `STFT` to `StreamWindower`
* NAudio updated to fix Windows 10 high CPU utilization during playback
* Various unit tests
* Many bug fixes, optimizations, and enhancements

### 2016-03-01 fe49ea5

* Support for Visual Studio 2015
* FFmpeg setup helper script
* FFmpeg updated to 2.8.2
* Decoding of various files formats through FFmpeg
  * Direct processing of audio from compressed formats and (video) containers
  * Video decoding (see Aurio.FFmpeg.Test program)
  * Automatic creation of wav proxy files for audio formats that don't allow seeking (shn, ape)
* Stream input support in FFmpeg decoder (required a file input before)
* Close-method and IDisposable implementation in audio streams to free resources
* Close all audio streams after use
* `MultiStream` to concatenate files/streams for decoding (e.g. vob files) 
* Support additional video files (ts, mkv, mov) for automatic EDL export wav proxy replacement
* Fix release config postbuild event commands
* Fix FFmpeg proxy compile error in VS2013


Support
-------

For questions and issues, please open an issue on the issue tracker. Commercial support, development
and consultation is available through [Protyposis Multimedia Solutions](https://protyposis.com).

Requirements
------------

* Windows
* Visual Studio 2017
* .NET Standard 2.0 / .NET Core 2.0 (.NET Framework 4.6.2 for WPF apps)

Aurio has been developed for Windows but since only a small part depends on the OS, it should be portable to the Mono platform and other OS'.


Build Instructions
------------------

Aurio comes with all required dependencies except for FFmpeg (which would blow up the repo size too much). See `libs\ffmpeg\ffmpeg-prepare.txt` for instructions on how to download and where to put FFmpeg.

Building works as easy as building any other Visual Studio solution and should work without problems if all dependencies have been set up correctly. Open the Visual Studio solution `Aurio\Aurio.sln` and build the `Aurio` project and optionally the `Aurio.WaveControls` project. The compiled DLLs will be built in each project's `bin` folder.


Documentation
-------------

Not available yet. If you have any questions, feel free to open an issue!


Publications
------------

> Mario Guggenberger. 2015. [Aurio: Audio Processing, Analysis and Retrieval](http://protyposis.net/publications/). In Proceedings of the 23rd ACM international conference on Multimedia (MM '15). ACM, New York, NY, USA, 705-708. DOI=http://dx.doi.org/10.1145/2733373.2807408


Examples
--------

### Reading, Processing & Writing

```csharp
/* Read a high definition MKV video file with FFmpeg,
 * convert it to telephone sound quality,
 * and write it to a WAV file with NAudio. */
var sourceStream = new FFmpegSourceStream(new FileInfo("high-definition-video.mkv"));
var ieee32BitStream = new IeeeStream(sourceStream);
var monoStream = new MonoStream(ieee32BitStream);
var resamplingStream = new ResamplingStream(monoStream, ResamplingQuality.Low, 8000);
var sinkStream = new NAudioSinkStream(resamplingStream);
WaveFileWriter.CreateWaveFile("telephone-audio.wav", sinkStream);
```

### Short-time Fourier Transform

```csharp
// Setup STFT with a window size of 100ms and an overlap of 50ms
var source = AudioStreamFactory.FromFileInfoIeee32(new FileInfo("somefilecontainingaudio.ext"));
var windowSize = source.Properties.SampleRate/10;
var hopSize = windowSize/2;
var stft = new STFT(source, windowSize, hopSize, WindowType.Hann);
var spectrum = new float[windowSize/2];

// Read all frames and get their spectrum
while (stft.HasNext()) {
    stft.ReadFrame(spectrum);
    // do something with the spectrum (e.g. build spectrogram)
}
```

### Generate fingerprints

```csharp
// Setup the source (AudioTrack is Aurio's internal representation of an audio file)
var audioTrack = new AudioTrack(new FileInfo("somefilecontainingaudio.ext"));

// Setup the fingerprint generator (each fingerprinting algorithms has its own namespace but works the same)
var defaultProfile = FingerprintGenerator.GetProfiles()[0]; // the first one is always the default profile
var generator = new FingerprintGenerator(defaultProfile);

// Setup the generator event listener
generator.SubFingerprintsGenerated += (sender, e) => {
    // Print the hashes
    e.SubFingerprints.ForEach(sfp => Console.WriteLine("{0,10}: {1}", sfp.Index, sfp.Hash));
};

// Generate fingerprints for the whole track
generator.Generate(audioTrack);
```

### Fingerprinting & Matching

```csharp
// Setup the sources
var audioTrack1 = new AudioTrack(new FileInfo("somefilecontainingaudio1.ext"));
var audioTrack2 = new AudioTrack(new FileInfo("somefilecontainingaudio2.ext"));

// Setup the fingerprint generator
var defaultProfile = FingerprintGenerator.GetProfiles()[0];
var generator = new FingerprintGenerator(defaultProfile);

// Create a fingerprint store
var store = new FingerprintStore(defaultProfile);

// Setup the generator event listener (a subfingerprint is a hash with its temporal index)
generator.SubFingerprintsGenerated += (sender, e) => {
    var progress = (double)e.Index / e.Indices;
    var hashes = e.SubFingerprints.Select(sfp => sfp.Hash);
    store.Add(e);
};

// Generate fingerprints for both tracks
generator.Generate(audioTrack1);
generator.Generate(audioTrack2);

// Check if tracks match
if (store.FindAllMatches().Count > 0) {
   Console.WriteLine("overlap detected!");
}
```

### Multitrack audio playback

```csharp
var drumTrack = new AudioTrack(new FileInfo("drums.wav"));
var guitarTrack = new AudioTrack(new FileInfo("guitar.wav"));
var vocalTrack = new AudioTrack(new FileInfo("vocals.wav"));
var band = new TrackList<AudioTrack>(new[] {drumTrack, guitarTrack, vocalTrack});

new MultitrackPlayer(band).Play();
```

Example Applications
--------------------

Aurio comes with a few tools and test applications that can be taken as a reference:

* Tests
  * **Aurio.FFmpeg.Test** decodes audio to wav files or video to frame images
  * **Aurio.Test.FingerprintingBenchmark** runs a file through all fingerprinting algorithms and measures the required time.
  * **Aurio.Test.FingerprintingHaitsmaKalker2002** fingerprints files and builds a hash store to match the fingerprinted files. The matches can be browsed and fingerprints inspected.
  * **Aurio.Test.FingerprintingWang2003** display the spectrogram and constellation map while fingerprinting a file.
  * **Aurio.Test.MultitrackPlayback** is a multitrack audio player with a simple user interface.
  * **Aurio.Test.RealtimeFingerprinting** fingerprints a realtime live audio stream.
  * **Aurio.Test.ResamlingStream** is a test bed for dynamic resampling.
  * **Aurio.Test.WaveViewControl** is a test bed for the WaveView WPF control.
* Tools
  * **BatchResampler** resamples wave files according to a configuration file.
  * **MusicDetector** analyzes audio files for music content.
  * **WaveCutter** cuts a number of concatenated files into slices of random length.

Aurio has originally been developed for [AudioAlign](https://github.com/protyposis/AudioAlign), a tool to automatically synchronize overlapping audio and video recordings, which uses almost all functionality of Aurio. Its sources are also available and can be used as a implementation reference.


Patents
-------

The fingerprinting methods by Haitsma&Kalker and Wang are protected worldwide, and Echoprint is protected in the US by patents. Their usage is therefore severely limited. In Europe, patented methods can be used privately or for research purposes.


License
-------

Copyright (C) 2010-2017 Mario Guggenberger <mg@protyposis.net>.
This project is released under the terms of the GNU Affero General Public License. See `LICENSE` for details. The library can be built to be free of any copyleft requirements; get in touch if the AGPL does not suit your needs.
