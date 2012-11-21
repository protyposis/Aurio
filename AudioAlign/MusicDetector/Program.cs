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

            FileInfo errorLogFileInfo = new FileInfo("error.log");
            StreamWriter errorLogWriter = errorLogFileInfo.AppendText();

            foreach (var fi in scanQueue) {
                try {
                    if (fi.Exists && fi.Length > 0) {
                        new CFA(new AudioTrack(fi), CFA.DEFAULT_THRESHOLD, true, true).Run();
                    }
                    else {
                        throw new FileNotFoundException(String.Format("file '{0}' not existing or empty, skipping", fi.FullName));
                    }
                }
                catch (FileNotFoundException e) {
                    Console.WriteLine(DateTime.Now + " " + e.Message);
                    errorLogWriter.WriteLine(DateTime.Now + " " + e.Message);
                }
                catch (Exception e) {
                    Console.WriteLine(DateTime.Now + " " + e.ToString());
                    Console.WriteLine();

                    errorLogWriter.WriteLine(DateTime.Now + " " + fi.FullName);
                    errorLogWriter.WriteLine(e.ToString());
                }
            }

            errorLogWriter.Flush();
            errorLogWriter.Close();

        }
    }
}
