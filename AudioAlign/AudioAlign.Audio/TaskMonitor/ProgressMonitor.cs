using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Timers;

namespace AudioAlign.Audio.TaskMonitor {
    public class ProgressMonitor {

        private static ProgressMonitor singletonInstance = null;

        private List<ProgressReporter> reporters;
        private Dictionary<ProgressReporter, int> reporterProgress;

        private Timer timer;

        public event EventHandler ProcessingStarted;
        public event EventHandler<ValueEventArgs<float>> ProcessingProgressChanged;
        public event EventHandler ProcessingFinished;

        #region Child Monitor handling

        /* A child monitor serves to monitor a subset of progress reporters of a parent monitor. 
         * The parent monitor monitors all of its progress reporters and all progress reporters of
         * its child monitors.
         */

        private List<ProgressMonitor> childMonitors;

        private event EventHandler<ValueEventArgs<ProgressReporter>> TaskBegun;
        private event EventHandler<ValueEventArgs<ProgressReporter>> TaskEnded;

        #endregion

        public ProgressMonitor() {
            reporters = new List<ProgressReporter>();
            reporterProgress = new Dictionary<ProgressReporter, int>();
            timer = new Timer(100) { Enabled = false };
            timer.Elapsed += new ElapsedEventHandler(timer_Elapsed);
            childMonitors = new List<ProgressMonitor>();
        }

        public static ProgressMonitor GlobalInstance {
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

        [MethodImpl(MethodImplOptions.Synchronized)]
        private ProgressReporter BeginTask(ProgressReporter reporter) {
            if (reporters.Count == 0) {
                OnProcessingStarted();
            }
            reporters.Add(reporter);
            reporter.PropertyChanged += progressReporter_PropertyChanged;
            reporterProgress.Add(reporter, 0);
            OnTaskBegun(reporter);
            return reporter;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void EndTask(ProgressReporter reporter) {
            OnTaskEnded(reporter);
            reporter.PropertyChanged -= progressReporter_PropertyChanged;
            reporters.Remove(reporter);
            reporterProgress.Remove(reporter);
            if (reporters.Count == 0) {
                OnProcessingFinished();
            }
        }

        public string StatusMessage {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get {
                string message = "";
                if (reporters.Count == 1) {
                    message += reporters[0].Name;
                }
                else if (reporters.Count > 1) {
                    message += "(" + reporters.Count + " tasks) ";
                    for (int x = 0; x < reporters.Count; x++) {
                        message += reporters[x].Name;
                        if (x + 1 != reporters.Count) {
                            message += "; ";
                        }
                    }
                }
                return message;
            }
        }

        public bool Active {
            get { return reporters.Count > 0; }
        }

        private void progressReporter_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            ProgressReporter senderTaskStatus = (ProgressReporter)sender;
            if (reporterProgress[senderTaskStatus] != (int)senderTaskStatus.Progress) {
                reporterProgress[senderTaskStatus] = (int)senderTaskStatus.Progress;
                Debug.WriteLine(senderTaskStatus.Name + ": " + reporterProgress[senderTaskStatus] + "%");
            }
        }

        private void OnProcessingStarted() {
            if (TaskBegun == null) {
                timer.Enabled = true;
            }
            if(ProcessingStarted != null) {
                ProcessingStarted(this, EventArgs.Empty);
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private void OnProcessingProgressChanged() {
            if (ProcessingProgressChanged != null && reporters.Count > 0) {
                float progress = 0;
                foreach (ProgressReporter reporter in reporters) {
                    progress += (float)reporter.Progress;
                }
                progress /= reporters.Count;
                ProcessingProgressChanged(this, new ValueEventArgs<float>(progress));
            }
            if (childMonitors.Count > 0) {
                foreach (ProgressMonitor childMonitor in childMonitors) {
                    if (childMonitor.Active) {
                        childMonitor.OnProcessingProgressChanged();
                    }
                }
            }
        }

        private void OnProcessingFinished() {
            if (TaskEnded == null) {
                timer.Enabled = false;
            }
            if (ProcessingFinished != null) {
                ProcessingFinished(this, EventArgs.Empty);
            }
        }

        private void timer_Elapsed(object sender, ElapsedEventArgs e) {
            OnProcessingProgressChanged();
        }

        #region Child Monitor handling

        public void AddChild(ProgressMonitor childMonitor) {
            childMonitors.Add(childMonitor);
            childMonitor.TaskBegun += childMonitor_TaskBegun;
            childMonitor.TaskEnded += childMonitor_TaskEnded;
        }

        public bool RemoveChild(ProgressMonitor childMonitor) {
            if (childMonitors.Remove(childMonitor)) {
                childMonitor.TaskBegun -= childMonitor_TaskBegun;
                childMonitor.TaskEnded -= childMonitor_TaskEnded;
                return true;
            }
            return false;
        }

        private void childMonitor_TaskBegun(object sender, ValueEventArgs<ProgressReporter> e) {
            BeginTask(e.Value);
        }

        private void childMonitor_TaskEnded(object sender, ValueEventArgs<ProgressReporter> e) {
            EndTask(e.Value);
        }

        private void OnTaskBegun(ProgressReporter reporter) {
            if (TaskBegun != null) {
                TaskBegun(this, new ValueEventArgs<ProgressReporter>(reporter));
            }
        }

        private void OnTaskEnded(ProgressReporter reporter) {
            if (TaskEnded != null) {
                TaskEnded(this, new ValueEventArgs<ProgressReporter>(reporter));
            }
        }

        #endregion
    }
}
