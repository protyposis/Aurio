// 
// Aurio: Audio Processing, Analysis and Retrieval Library
// Copyright (C) 2010-2015  Mario Guggenberger <mg@protyposis.net>
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
using NAudio.Wave;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
using Aurio.Project;
using Aurio.TaskMonitor;
using Aurio.Streams;
using System.Collections.Concurrent;

namespace Aurio {
    public static class AudioStreamFactory {

        private const int SAMPLES_PER_PEAK = 256;

        private static BlockingCollection<Action> peakStoreQueue = new BlockingCollection<Action>();
        private static volatile int peakStoreQueueThreads = 0;

        private static WaveStream OpenFile(FileInfo fileInfo) {
            if (fileInfo.Extension.Equals(".wav")) {
                return new WaveFileReader(fileInfo.FullName);
            }
            else if (fileInfo.Extension.Equals(".mp3")) {
                return new Mp3FileReader(fileInfo.FullName);
            }
            return null;
        }

        private static IAudioStream TryOpenSourceStream(FileInfo fileInfo) {
            WaveStream waveStream;
            if ((waveStream = OpenFile(fileInfo)) != null) {
                return new NAudioSourceStream(waveStream);
            }
            else {
                try {
                    return new FFmpegSourceStream(fileInfo);
                }
                catch (FFmpegSourceStream.FileNotSeekableException) {
                    /* 
                     * This exception gets thrown is a file is not seekable and therefore cannot
                     * provide all the functionality that is needed for an IAudioStream, although
                     * the problem could be solved by creating a seek index. See FFmpegSourceStream
                     * for further information.
                     * 
                     * For now, we create a WAV proxy file, because it is easier (consumes 
                     * additional space though).
                     */
                    Console.WriteLine("File not seekable, creating proxy file...");
                    return TryOpenSourceStream(FFmpegSourceStream.CreateWaveProxy(fileInfo));
                }
                catch (DllNotFoundException) {
                    Console.WriteLine("Cannot open file through FFmpeg: DLL missing");
                }
                catch {
                    // file probably unsupported
                    Console.WriteLine("unsupported file format");
                }
            }
            return null;
        }

        public static IAudioStream FromFileInfo(FileInfo fileInfo) {
            return TryOpenSourceStream(fileInfo);
        }

        public static IAudioStream FromFileInfoIeee32(FileInfo fileInfo) {
            return new IeeeStream(TryOpenSourceStream(fileInfo));
        }

        public static VisualizingStream FromAudioTrackForGUI(AudioTrack audioTrack) {
            VisualizingStream visualizingStream = 
                new VisualizingStream(audioTrack.CreateAudioStream(),
                    CreatePeakStore(audioTrack, audioTrack.TimeWarps.Count == 0));

            // TODO if timewarps are added but total length stays the same, the peakstore still has to be refreshed
            audioTrack.LengthChanged += delegate(object sender, ValueEventArgs<TimeSpan> e) {
                visualizingStream.PeakStore = CreatePeakStore(audioTrack, false);
            };

            return visualizingStream;
        }

        /// <summary>
        /// Checks if a file has a supported format.
        /// </summary>
        /// <param name="fileName">the filename to check</param>
        /// <returns>true if the file is supported, else false</returns>
        public static bool IsSupportedFile(string fileName) {
            return TryOpenSourceStream(new FileInfo(fileName)) != null;
        }

        public static void WriteToFile(IAudioStream stream, string targetFileName) {
            WaveFileWriter.CreateWaveFile(targetFileName, new NAudioSinkStream(stream));
        }

        private static PeakStore CreatePeakStore(AudioTrack audioTrack, bool fileSupport) {
            IAudioStream audioInputStream = audioTrack.CreateAudioStream();

            PeakStore peakStore = new PeakStore(SAMPLES_PER_PEAK, audioInputStream.Properties.Channels,
                (int)Math.Ceiling((float)audioInputStream.Length / audioInputStream.SampleBlockSize / SAMPLES_PER_PEAK));

            Action peakStoreFillAction = delegate {
                FillPeakStore(audioTrack, fileSupport, audioInputStream, peakStore);
            };

            // add task
            peakStoreQueue.Add(peakStoreFillAction);

            // create consumer/worker threads
            for (; peakStoreQueueThreads < Math.Min(2, Environment.ProcessorCount); peakStoreQueueThreads++) {
                Task.Factory.StartNew(() => {
                    // process peakstore actions as long as the queue is not empty
                    Debug.WriteLine("PeakStoreQueue thread started");
                    while (peakStoreQueue.Count > 0) {
                        peakStoreQueue.Take().Invoke();
                    }
                    peakStoreQueueThreads--;
                    Debug.WriteLine("PeakStoreQueue thread stopped");
                });
            }

            return peakStore;
        }

        private static void FillPeakStore(AudioTrack audioTrack, bool fileSupport, IAudioStream audioInputStream, PeakStore peakStore) {
            bool peakFileLoaded = false;

            // search for existing peakfile
            if (audioTrack.HasPeakFile && fileSupport) {
                // load peakfile from disk
                try {
                    peakStore.ReadFrom(File.OpenRead(audioTrack.PeakFile.FullName), audioTrack.FileInfo.LastWriteTimeUtc);
                    peakStore.CalculateScaledData(8, 6);
                    peakFileLoaded = true;
                }
                catch (Exception e) {
                    Console.WriteLine("peakfile read failed: " + e.Message);
                }
            }

            // generate peakfile
            if(!peakFileLoaded) {
                int channels = peakStore.Channels;
                byte[] buffer = new byte[65536 * audioInputStream.SampleBlockSize];
                float[] min = new float[channels];
                float[] max = new float[channels];
                BinaryWriter[] peakWriters = peakStore.CreateMemoryStreams().WrapWithBinaryWriters();

                IProgressReporter progressReporter = ProgressMonitor.GlobalInstance.BeginTask("Generating peaks for " + audioTrack.FileInfo.Name, true);
                DateTime startTime = DateTime.Now;
                int sampleBlockCount = 0;
                int peakCount = 0;
                int bytesRead;
                long totalSampleBlocks = audioInputStream.Length / audioInputStream.SampleBlockSize;
                long totalSamplesRead = 0;
                int progress = 0;

                for (int i = 0; i < channels; i++) {
                    min[i] = float.MaxValue;
                    max[i] = float.MinValue;
                }

                unsafe {
                    fixed (byte* bufferB = &buffer[0]) {
                        float* bufferF = (float*)bufferB;
                        int samplesRead;
                        int samplesProcessed;
                        bool peakStoreFull = false;

                        while ((bytesRead = StreamUtil.ForceRead(audioInputStream, buffer, 0, buffer.Length)) > 0) {
                            samplesRead = bytesRead / audioInputStream.Properties.SampleByteSize;
                            samplesProcessed = 0;

                            do {
                                for (int channel = 0; channel < channels; channel++) {
                                    if (min[channel] > bufferF[samplesProcessed]) {
                                        min[channel] = bufferF[samplesProcessed];
                                    }
                                    if (max[channel] < bufferF[samplesProcessed]) {
                                        max[channel] = bufferF[samplesProcessed];
                                    }
                                    samplesProcessed++;
                                    totalSamplesRead++;
                                }

                                if (++sampleBlockCount % SAMPLES_PER_PEAK == 0 || sampleBlockCount == totalSampleBlocks) {
                                    // write peak
                                    peakCount++;
                                    for (int channel = 0; channel < channels; channel++) {
                                        peakWriters[channel].Write(new Peak(min[channel], max[channel]));
                                        // add last sample of previous peak as first sample of current peak to make consecutive peaks overlap
                                        // this gives the impression of a continuous waveform
                                        min[channel] = max[channel] = bufferF[samplesProcessed - channels];
                                    }
                                    //sampleBlockCount = 0;
                                }

                                if (sampleBlockCount == totalSampleBlocks && samplesProcessed < samplesRead) {
                                    // There's no more space for more peaks
                                    // TODO how to handle this case? why is there still audio data left?
                                    Console.WriteLine("peakstore full, but there are samples left ({0} < {1})", samplesProcessed, samplesRead);
                                    peakStoreFull = true;
                                    break;
                                }
                            }
                            while (samplesProcessed < samplesRead);

                            progressReporter.ReportProgress(100.0f / audioInputStream.Length * audioInputStream.Position);
                            if ((int)(100.0f / audioInputStream.Length * audioInputStream.Position) > progress) {
                                progress = (int)(100.0f / audioInputStream.Length * audioInputStream.Position);
                                peakStore.OnPeaksChanged();
                            }

                            if (peakStoreFull) {
                                break;
                            }
                        }
                    }
                }

                Debug.WriteLine("generating downscaled peaks...");
                peakStore.CalculateScaledData(8, 6);

                Debug.WriteLine("peak generation finished - " + (DateTime.Now - startTime) + ", " + (peakWriters[0].BaseStream.Length * channels) + " bytes");
                progressReporter.Finish();

                if (fileSupport) {
                    // write peakfile to disk
                    try {
                        FileStream peakOutputFile = File.OpenWrite(audioTrack.PeakFile.FullName);
                        peakStore.StoreTo(peakOutputFile, audioTrack.FileInfo.LastWriteTimeUtc);
                        peakOutputFile.Close();
                    }
                    catch (UnauthorizedAccessException e) {
                        Debug.WriteLine("peak file writing failed: " + e.Message);
                    }
                }
            }
            peakStore.OnPeaksChanged();
        }
    }
}
