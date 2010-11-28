using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace AudioAlign.Audio.TaskMonitor {
    public class ProgressReporter {

        private string name;
        private double progress;
        private bool isProgressReporting;

        public event PropertyChangedEventHandler PropertyChanged;

        public ProgressReporter() {
            this.isProgressReporting = false;
        }

        public ProgressReporter(string name)
            : this() {
                this.name = name;
        }

        public ProgressReporter(string name, bool reportProgress)
            : this(name) {
                this.isProgressReporting = reportProgress;
        }

        public string Name {
            get { return name; }
        }

        public bool IsProgressReporting {
            get { return isProgressReporting; }
        }

        public double Progress {
            get { return progress; }
        }

        public void ReportProgress(double progress) {
            if (!isProgressReporting) {
                throw new ArgumentException("this task doesn't support progress reporting");
            }

            if (progress < 0 || progress > 100) {
                throw new ArgumentException("invalid progress " + progress + " (must be a value between 0 and 100)");
            }
            this.progress = progress;
            OnPropertyChanged("Progress");
        }

        protected void OnPropertyChanged(string propertyName) {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
