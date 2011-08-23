using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioAlign.Audio.TaskMonitor {
    public interface IProgressReporter {
        string Name { get; }
        bool IsProgressReporting { get; }
        double Progress { get; }
        void ReportProgress(double progress);
        void Finish();
        bool IsFinished { get; }
    }
}
