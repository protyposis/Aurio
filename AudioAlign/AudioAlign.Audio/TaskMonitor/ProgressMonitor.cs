using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;

namespace AudioAlign.Audio.TaskMonitor {
    public class ProgressMonitor {

        private static ProgressMonitor singletonInstance = null;

        private List<ProgressReporter> reporters;

        private ProgressMonitor() {
            reporters = new List<ProgressReporter>();
        }

        public static ProgressMonitor Instance {
            get {
                if (singletonInstance == null) {
                    singletonInstance = new ProgressMonitor();
                }
                return singletonInstance;
            }
        }

        public ProgressReporter BeginTask(string taskName) {
            return BeginTask(new ProgressReporter(taskName));
        }

        public ProgressReporter BeginTask(string taskName, bool reportProgress) {
            return BeginTask(new ProgressReporter(taskName, reportProgress));
        }

        private ProgressReporter BeginTask(ProgressReporter reporter) {
            reporters.Add(reporter);
            reporter.PropertyChanged += progressReporter_PropertyChanged;
            return reporter;
        }

        public void EndTask(ProgressReporter reporter) {
            reporter.PropertyChanged -= progressReporter_PropertyChanged;
            reporters.Remove(reporter);
        }

        private void progressReporter_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            ProgressReporter senderTaskStatus = (ProgressReporter)sender;
            Debug.WriteLine(senderTaskStatus.Name + ": " + Math.Round(senderTaskStatus.Progress, 2) +"%");
        }
    }
}
