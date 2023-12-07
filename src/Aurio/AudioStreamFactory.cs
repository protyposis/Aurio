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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Aurio.Project;
using Aurio.Streams;
using Aurio.TaskMonitor;
using NAudio.Wave;

namespace Aurio
{
    public static class AudioStreamFactory
    {
        private static BlockingCollection<Action> peakStoreQueue = new BlockingCollection<Action>();
        private static volatile int peakStoreQueueThreads = 0;
        private static readonly List<IAudioStreamFactory> factories =
            new List<IAudioStreamFactory>();

        static AudioStreamFactory()
        {
            AddFactory(new NAudioStreamFactory());
        }

        public static void AddFactory(IAudioStreamFactory factory)
        {
            if (!factories.Contains(factory))
            {
                factories.Add(factory);
            }
        }

        private static IAudioStream TryOpenSourceStream(
            FileInfo fileInfo,
            FileInfo proxyFileInfo = null
        )
        {
            if (factories.Count == 0)
            {
                throw new NotSupportedException("Cannot open file " + fileInfo.FullName);
            }

            var factoryEnumerator = factories.GetEnumerator();
            factoryEnumerator.MoveNext();

            while (true)
            {
                try
                {
                    var stream = factoryEnumerator.Current.OpenFile(fileInfo, proxyFileInfo);
                    return stream;
                }
                catch (Exception)
                {
                    if (!factoryEnumerator.MoveNext())
                    {
                        // Throw last exception if there is no more factory to try
                        throw;
                    }
                }
            }
        }

        public static IAudioStream FromFileInfo(FileInfo fileInfo, FileInfo proxyFileInfo = null)
        {
            return TryOpenSourceStream(fileInfo, proxyFileInfo);
        }

        public static IAudioStream FromFileInfoIeee32(
            FileInfo fileInfo,
            FileInfo proxyFileInfo = null
        )
        {
            return new IeeeStream(TryOpenSourceStream(fileInfo, proxyFileInfo));
        }

        public static VisualizingStream FromAudioTrackForGUI(AudioTrack audioTrack)
        {
            VisualizingStream visualizingStream = new VisualizingStream(
                audioTrack.CreateAudioStream(),
                CreatePeakStore(audioTrack, !audioTrack.Offline && audioTrack.TimeWarps.Count == 0)
            );

            // TODO if timewarps are added but total length stays the same, the peakstore still has to be refreshed
            audioTrack.LengthChanged += delegate(object sender, ValueEventArgs<TimeSpan> e)
            {
                visualizingStream.PeakStore = CreatePeakStore(audioTrack, false);
            };

            return visualizingStream;
        }

        /// <summary>
        /// Checks if a file has a supported format.
        /// </summary>
        /// <param name="fileName">the filename to check</param>
        /// <returns>true if the file is supported, else false</returns>
        public static bool IsSupportedFile(string fileName)
        {
            try
            {
                TryOpenSourceStream(new FileInfo(fileName));
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void IsSupportedFileOrThrow(string fileName)
        {
            TryOpenSourceStream(new FileInfo(fileName));
        }

        public static void WriteToFile(IAudioStream stream, string targetFileName)
        {
            WaveFileWriter.CreateWaveFile(targetFileName, new NAudioSinkStream(stream));
        }

        private static PeakStore CreatePeakStore(AudioTrack audioTrack, bool fileSupport)
        {
            IAudioStream audioInputStream = audioTrack.CreateAudioStream();

            PeakStore peakStore = new PeakStore(
                PeakStore.DefaultSamplesPerPeak,
                audioInputStream.Properties.Channels,
                (int)
                    Math.Ceiling(
                        (double)audioInputStream.Length
                            / audioInputStream.SampleBlockSize
                            / PeakStore.DefaultSamplesPerPeak
                    )
            );

            Action peakStoreFillAction = delegate
            {
                FillPeakStore(audioTrack, fileSupport, audioInputStream, peakStore);
                audioInputStream.Close();
            };

            // add task
            peakStoreQueue.Add(peakStoreFillAction);

            // create consumer/worker threads
            for (
                ;
                peakStoreQueueThreads < Math.Min(2, Environment.ProcessorCount);
                peakStoreQueueThreads++
            )
            {
                Task.Factory.StartNew(() =>
                {
                    // process peakstore actions as long as the queue is not empty
                    Debug.WriteLine("PeakStoreQueue thread started");
                    while (peakStoreQueue.Count > 0)
                    {
                        peakStoreQueue.Take().Invoke();
                    }
                    peakStoreQueueThreads--;
                    Debug.WriteLine("PeakStoreQueue thread stopped");
                });
            }

            return peakStore;
        }

        private static void FillPeakStore(
            AudioTrack audioTrack,
            bool fileSupport,
            IAudioStream audioInputStream,
            PeakStore peakStore
        )
        {
            bool peakFileLoaded = false;

            // search for existing peakfile
            if (audioTrack.HasPeakFile && fileSupport)
            {
                // load peakfile from disk
                try
                {
                    using var peakFileReadStream = File.OpenRead(audioTrack.PeakFile.FullName);
                    peakStore.ReadFrom(peakFileReadStream, audioTrack.FileInfo.LastWriteTimeUtc);
                    peakStore.CalculateScaledData(8, 6);
                    peakFileLoaded = true;
                }
                catch (Exception e)
                {
                    Console.WriteLine("peakfile read failed: " + e.Message);
                }
            }

            // generate peakfile
            if (!peakFileLoaded)
            {
                var progressMonitor = new ProgressMonitor();
                ProgressMonitor.GlobalInstance.AddChild(progressMonitor);
                IProgressReporter progressReporter = progressMonitor.BeginTask(
                    "Generating peaks for " + audioTrack.Name,
                    true
                );

                int progress = 0;
                progressMonitor.ProcessingProgressChanged += (
                    object sender,
                    ValueEventArgs<float> args
                ) =>
                {
                    int newProgress = (int)args.Value;
                    if (newProgress > progress)
                    {
                        peakStore.OnPeaksChanged();
                        progress = newProgress;
                    }
                };

                peakStore.Fill(audioInputStream, progressReporter);

                progressReporter.Finish();
                ProgressMonitor.GlobalInstance.RemoveChild(progressMonitor);

                if (fileSupport)
                {
                    // write peakfile to disk
                    try
                    {
                        FileStream peakOutputFile = File.OpenWrite(audioTrack.PeakFile.FullName);
                        peakStore.StoreTo(peakOutputFile, audioTrack.FileInfo.LastWriteTimeUtc);
                        peakOutputFile.Close();
                    }
                    catch (UnauthorizedAccessException e)
                    {
                        Debug.WriteLine("peak file writing failed: " + e.Message);
                    }
                }
            }

            peakStore.OnPeaksChanged();
        }
    }
}
