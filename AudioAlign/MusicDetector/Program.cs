using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using MusicDetector.ContinuousFrequencyActivation;
using AudioAlign.Audio.Project;

namespace MusicDetector {
    class Program {
        static void Main(string[] args) {

            if (args.Length == 0) {
                Console.WriteLine("no file(s) specified");
                return;
            }

            Queue<FileInfo> scanQueue = new Queue<FileInfo>();

            foreach (string file in args) {
                if (file.Contains("*")) {
                    int slashIndex = file.LastIndexOf(@"\");
                    var di = new DirectoryInfo(slashIndex > -1 ? file.Substring(0, slashIndex) : ".");
                    foreach (var fi in di.GetFiles(file.Substring(slashIndex > -1 ? slashIndex + 1 : 0), SearchOption.TopDirectoryOnly)) {
                        scanQueue.Enqueue(fi);
                    }
                }
                else {
                    scanQueue.Enqueue(new FileInfo(file));
                }
            }

            foreach (var fi in scanQueue) {
                if (fi.Exists) {
                    new CFA(new AudioTrack(fi), 1.0f, true).Run();
                }
                else {
                    Console.WriteLine("file '{0}' does not exist, skip", fi.FullName);
                }
            }

        }
    }
}
