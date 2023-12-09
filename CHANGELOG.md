# Changelog

All notable changes to this project will be documented in this file. See [commit-and-tag-version](https://github.com/absolute-version/commit-and-tag-version) for commit guidelines.

## [4.1.0](https://github.com/protyposis/Aurio/compare/v4.0.0...v4.1.0) (2023-12-09)


### Features

* bump Soxr to 0.1.3 ([5908909](https://github.com/protyposis/Aurio/commit/5908909f9e99f75169b2cdf25f6762cd5059ba90))
* validate availability of native Windows libraries ([55db90b](https://github.com/protyposis/Aurio/commit/55db90b430b853c52099bcb308b80f024e4247e7))

## 4.0.0 (2023-12-07)

### Features

* upgrade to .NET 6
* faster fingerprint matching
* custom audio proxy files
* `PeakPairsGenerated` event
* draw peak pairs into spectrogram
* `Aurio` core cross-platform support
* `Aurio.Windows` for Windows dependencies
* FFT benchmarking application
* FftSharp integration
* FFmpeg Linux support
* remove unnecessary .NET Core test application
* remove x86 support
* upgrade FFmpeg to 6.0
* Wang guessed settings profile
* `WdlResampler` flushing
* support skipping missing files when loading project
* `DummyAudioTrack` as placeholder for missing backing file
* custom audio proxy files
* create `Interval` from `TimeSpan`s
* optional progress callback on `MatchProcessor.WindowFilter`
* `FFTUtil.CalculateFrequencyBinIndex`
* trigger progress events only if there is actual progress to report
* `Aurio.FFmpeg.Test` cross-platform support and dry-run mode
* periodic Hamming, sine, periodic sine windows
* consolidate window parameters in `WindowConfig`
* `StreamUtil.Read*` bytes or samples to array
* square-root windows
* `FFTUtil.CalculateNextPowerOf2` and `IsPowerOf2`
* create memory source stream from sample array
* `DCOffsetStream`
* `FixedLengthFifoStream`
* use wave view with stream instead of track
* overlap-add visualizer

### Bug Fixes

* analysis of multiple files
* audio stream creation without proxy
* silent spectrum frames not drawn
* incorrect stream windower last frame detection
* missing waveform view after reload
* progress event without reported progress
* status message update before `ProcessingStarted` event
* stream factory not accessible
* update peak file when audio file has changed
* wave controls rendered empty
* status message update before `ProcessingStarted` event
* progress events without reported progress
* periodic Hann window incorrectly applied
* misleading representation of clipped samples by bitmap waveform renderer

## 2018-12-04 d01cb3f
* Introduced `IAudioStreamFactory` pattern that allows to register external audio stream factories in `AudioStreamFactory` to read streams
* Removed FFmpeg dependency from core
  * Moved `FFmpegSourceStream` to `Aurio.FFmpeg`
  * Added `FFmpegAudioStreamFactory` to `Aurio.FFmpeg`
* Updated test apps and tools with factory initializations

## 2018-03-22 5af5339
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

## 2017-10-15 14c7c92
* FFmpeg updated to 3.3.3
* `SurroundDownmixStream` for 4.0/5.1/7.1 to stereo downmixing
* Validate matches before executing alignment and throw exception when an invalid match sequence is detected
* Add progress callback to `FingerprintStore`s match finder
* Various bug fixes

## 2017-02-06 3e703cd

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

## 2016-03-01 fe49ea5

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
