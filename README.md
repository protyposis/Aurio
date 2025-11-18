# Aurio: Audio Stream Processing & Retrieval for .NET

Aurio is an open source .NET audio library for stream processing, analysis and retrieval.

<!-- nuget-exclude:start -->
<p align="center">
  <img src="aurio-icon.png" alt="Aurio logo" width="100"/>
</p>
<!-- nuget-exclude:end -->

## Features

* 32-bit floating point audio stream processing engine
* File I/O (using NAudio and FFmpeg)
* FFT and iFFT (using Exocortex.DSP, FftSharp, FFTW, PFFFT) 
* Resampling (using NAudio, libsamplerate, Soxr)
* Windowing, overlap-adding, STFT, iSTFT
* Chroma
* Dynamic Time Warping
* On-line Time Warping (Dixon, Simon. "Live tracking of musical performances using on-line time warping." Proceedings of the 8th International Conference on Digital Audio Effects. 2005.)
* Fingerprinting
  * Haitsma, Jaap, and Ton Kalker. "A highly robust audio fingerprinting system." ISMIR. 2002.
  * Wang, Avery. "An Industrial Strength Audio Search Algorithm." ISMIR. 2003.
  * [Echoprint](http://echoprint.me/codegen) (Ellis, Daniel PW, Brian Whitman, and Alastair Porter. "Echoprint: An open music identification service." ISMIR. 2011.)
  * AcoustID [Chromaprint](https://acoustid.org/chromaprint)
* Audio playback
* UI widgets

All audio processing (incl. fingerprinting) is stream-based and supports processing of arbitrarily long streams at constant memory use.

<!-- nuget-exclude:start -->
## Getting Started

The easiest way of using Aurio is installing the [packages](#packages) through [NuGet](https://www.nuget.org/packages?q=aurio). For usage, check the [code examples](#examples) and [example applications](#example-applications).
<!-- nuget-exclude:end -->

## Packages

* `Aurio`: The core library.
* `Aurio.Windows`: Audio player and MP3 decoder. Windows only.
* `Aurio.WaveControls`: WPF controls and utilities for UIs. Windows only.
  * Spectrogram / Chromagram View, Spectrum / Graph View, VU Meter, Correlometer, Time Scale, Wave View

### Decoders

| Name | Formats | Description | License |
| --- | ----------- | --- | --- |
| `Aurio` (core) | PCM Wave | Managed [NAudio](https://github.com/naudio/NAudio) decoder. | MIT |
| `Aurio.Windows` | MP3 | Uses Windows's ACM codec through [NAudio](https://github.com/naudio/NAudio). Windows only. | MIT |
| `Aurio.FFmpeg` | many | Decodes a very wide range of media container formats and codecs through [FFmpeg](https://ffmpeg.org/). Windows and Linux. | LGPL |

### Resamplers

| Name | Description | Variable Rate Support | License |
| --- | ----------- | --- | --- |
| `Aurio` (core) | Managed [NAudio](https://github.com/naudio/NAudio) WDL resampler. Recommended for cross-platform use. | yes | MIT |
| `Aurio.LibSampleRate` | Native [libsamplerate](https://github.com/libsndfile/libsamplerate) (a.k.a. Secret Rabbit Code) library via [LibSampleRate.NET](https://github.com/protyposis/LibSampleRate.NET). Windows only*. | yes | BSD |
| `Aurio.Soxr` | Native [SoX Resampler](https://sourceforge.net/projects/soxr/) library. Windows only*. | yes (depending on config) | LGPL |

(*) Linux binary not integrated yet.

### FFTs

| Name | Description | In-Place Transform | Inverse Transform | License |
| --- | ----------- | --- | --- | --- |
| `Aurio.Exocortex` | [Exocortex.DSP](https://benhouston3d.com/dsp/) library. Fastest managed FFT, recommended for cross-platform use. | yes | yes | BSD |
| `Aurio.FftSharp` | [FftSharp](https://github.com/swharden/FftSharp/) library. Much slower than Exocortex. | no | yes | MIT |
| `Aurio.FFTW` | Native [FFTW](https://www.fftw.org/) (Fastest Fourier Transform in the West) library. Much faster than the managed implementations. Windows only*. | yes | no | GPL |
| `Aurio.PFFFT` | Native [PFFFT](https://bitbucket.org/jpommier/pffft/) (pretty Fast FFT) library. Even faster than FFTW, recommended for high-performance use. Windows only*. | yes | yes | FFTPACK |

(*) Linux binary not integrated yet.

<!-- nuget-exclude:start -->
Run the `Aurio.Test.FFTBenchmark` tool for a more detailed performance comparison, or see native benchmark results [here](https://github.com/hayguen/pffft_benchmarks), [here](https://www.fftw.org/speed/), and [here](https://bitbucket.org/jpommier/pffft/).


## What's new

See [CHANGELOG](CHANGELOG.md).


## Development

### Requirements

* Windows: Visual Studio 2022 (with CMake tools)
* Linux: Ubuntu 22.04, CMake, Ninja
* .NET SDK 6.0

### Build Instructions

Open `./nativesrc` and `./src/Aurio.sln` in Visual Studio, or check the [CI workflow](.github/workflows/ci.yml) for the Windows and Linux CLI build command sequence.
<!-- nuget-exclude:end -->

## Documentation

Not available yet. If you have any questions, feel free to open an issue!


## Examples

### Select Decoders

```csharp
using Aurio;

// The stream factory automatically selects an appropriate decoder stream
// for a given input. Multiple decoders can be added. The PCM Wave decoder
// is added by default.

// Add Aurio.Windows MP3 decoder
AudioStreamFactory.AddFactory(new Windows.NAudioStreamFactory());
// Add Aurio.FFmpeg decoder
AudioStreamFactory.AddFactory(new FFmpeg.FFmpegAudioStreamFactory());

var stream = AudioStreamFactory.FromFileInfo(new FileInfo("./media.file"));

// Alternatively, decoders can be directly used without the factory
var stream = FFmpegSourceStream(new FileInfo("./media.file"));
```

### Select Resampler

```csharp
using Aurio.Resampler;
using Aurio.Streams;

// Only one resampler can be selected at a time.

// Use managed NAudio resampler from core
ResamplerFactory.Factory = new Aurio.NAudioWdlResamplerFactory();
// or Aurio.Soxr
ResamplerFactory.Factory = new Aurio.Soxr.ResamplerFactory();
// or Aurio.LibSampleRate
ResamplerFactory.Factory = new Aurio.LibSampleRate.ResamplerFactory();

// Needs resampler factory, throws otherwise
var stream = new ResamplingStream(...);
```

### Select FFT

```csharp
using Aurio;
using Aurio.FFT;

// Only one FFT can be selected at a time.

// Use Aurio.Exocortex
FFTFactory.Factory = new Exocortex.FFTFactory();
// or Aurio.Exocortex
FFTFactory.Factory = new FftSharp.FFTFactory();
// or Aurio.FFTW
FFTFactory.Factory = new FFTW.FFTFactory();
// or Aurio.PFFFT
FFTFactory.Factory = new PFFFT.FFTFactory();

// Needs FFT factory, throws otherwise
var stft = new STFT(...);
```

### Stream Processing

```csharp
// Read MKV movie with surround audio
var sourceStream = new FFmpegSourceStream(new FileInfo("media.mkv"));
// Convert to 32-bit
var ieee32BitStream = new IeeeStream(sourceStream);
// Downmix to stereo
var downmixStream = new SurroundDownmixStream(ieee32BitStream);
// Downmix to mono
var monoStream = new MonoStream(downmixStream);
// Concatenate with other streams
var concatStream = new ConcatenationStream(monoStream, anotherStream, ...);
// Turn volume down to 50%
var volumeStream = new VolumeControlStream(concatStream) { Volume = 0.5f, Balance = 1, Mute = false };
// Mix with another stream
var mixerStream = new MixerStream(concatStream.Properties.Channels, concatStream.Properties.SampleRate);
mixerStream.Add(volumeStream);
mixerStream.Add(yetAnotherStream);
// Skip the first 10 samples
var cropStream = new CropStream(mixerStream, mixerStream.Properties.SampleBlockByteSize * 10, mixerStream.Length);
// Clip samples at max volume
var clipStream = new VolumeClipStream(cropStream);
// Downsample to telephone sound quality
var resamplingStream = new ResamplingStream(clipStream, ResamplingQuality.Low, 8000);
// Write it to a WAV fil
var sinkStream = new NAudioSinkStream(resamplingStream);
WaveFileWriter.CreateWaveFile("telephone-audio.wav", sinkStream);
```

### STFT

```csharp
// Setup STFT with a window size of 100ms and an overlap of 50ms
var source = AudioStreamFactory.FromFileInfoIeee32(new FileInfo("audio.wav"));
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

### FFT Equalizer

```csharp
var source = new IeeeStream(...);
var target = new MemoryWriterStream(new System.IO.MemoryStream(), source.Properties);

var windowSize = 512;
var hopSize = windowSize/2+1; // for COLA condition
var window = WindowType.Hann;
var stft = new STFT(source, windowSize, hopSize, window);
var istft = new InverseSTFT(target, windowSize, hopSize, window);
var spectrum = new float[windowSize/2];

while (stft.HasNext()) {
    stft.ReadFrame(spectrum);
    // manipulate spectrum
    istft.WriteFrame(spectrum);
}
istft.Flush();
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

<!-- nuget-exclude:start -->
## Example Applications

Aurio comes with a few tools and test applications that can be taken as a reference:

* Test applications
  * `Aurio.FFmpeg.Test`: Decodes audio to `.wav` files or video to `.png` frame images.
  * `Aurio.Test.FFTBenchmark`: Measures the execution time of all supported FFT implementations at various input lengths.
  * `Aurio.Test.FingerprintingBenchmark`: Runs a file through all fingerprinting algorithms and measures the required time.
  * `Aurio.Test.FingerprintingHaitsmaKalker2002`: Fingerprints files and builds a hash store to match the fingerprinted files. The matches can be browsed and fingerprints inspected.
  * `Aurio.Test.FingerprintingWang2003`: Fingerprints a file and displays a live spectrogram and constellation map.
  * `Aurio.Test.HugeControlRendering`: Test bed for WPF waveform drawing.
  * `Aurio.Test.MultitrackPlayback`: A multitrack audio player with a simple user interface.
  * `Aurio.Test.RealtimeFingerprinting`: Fingerprints a real-time live audio stream.
  * `Aurio.Test.ResamlingStream`: Test bed for dynamic resampling.
  * `Aurio.Test.Streams`: A simple stream processing demo.
  * `Aurio.Test.WaveViewControl`: Test bed for the `WaveView` WPF control.
* Tools
  * `BatchResampler`: Resamples wave files according to a configuration file.
  * `MusicDetector`: Analyzes audio files for music content.
  * `WaveCutter`: Cuts a files into slices of random length.

Aurio has originally been developed for [AudioAlign](https://github.com/protyposis/AudioAlign), a tool to research automated synchronization of overlapping audio and video recordings. It uses most functionality of Aurio and its sources can also be used as an implementation reference.

## Publications

> Mario Guggenberger. 2015. [Aurio: Audio Processing, Analysis and Retrieval](http://protyposis.net/publications/). In Proceedings of the 23rd ACM international conference on Multimedia (MM '15). ACM, New York, NY, USA, 705-708. DOI=http://dx.doi.org/10.1145/2733373.2807408
<!-- nuget-exclude:end -->

## Support

For questions and issues, please open an issue on the issue tracker. Commercial support, development
and consultation is available through [Protyposis Multimedia Solutions](https://protyposis.com).

## Patents

Please be aware that this library may contain code covered by patents. Users are advised to seek legal counsel to ensure compliance with all relevant patent laws and licensing requirements. For example, there are patents covering the fingerprinting methods by Haitsma & Kalker, Wang, and Echoprint. Their usage may therefore be severely limited.

## License

Copyright (C) 2010-2023 Mario Guggenberger <mg@protyposis.net>.
This project is released under the terms of the GNU Affero General Public License. See `LICENSE` for details. The library can be built to be free of any copyleft requirements; get in touch if the AGPL does not suit your needs.
