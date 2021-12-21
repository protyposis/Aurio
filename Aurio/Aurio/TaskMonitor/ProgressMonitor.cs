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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Timers;
using System.ComponentModel;

namespace Aurio.TaskMonitor
{
    public sealed class ProgressMonitor
    {

        private class ProgressReporter : IProgressReporter
        {

            private ProgressMonitor monitor;
            private string name;
            private double progress;
            private bool isProgressReporting;
            private bool isFinished;
            private int prevProgress;
            private DateTime startTime;

            public event PropertyChangedEventHandler PropertyChanged;

            public ProgressReporter(ProgressMonitor monitor)
            {
                this.monitor = monitor;
                this.isProgressReporting = false;
                this.isFinished = false;
                this.prevProgress = -1;
                startTime = DateTime.Now;
            }

            public ProgressReporter(ProgressMonitor monitor, string name)
                : this(monitor)
            {
                this.name = name;
            }

            public ProgressReporter(ProgressMonitor monitor, string name, bool reportProgress)
                : this(monitor, name)
            {
                this.isProgressReporting = reportProgress;
            }

            public string Name
            {
                get { return name; }
            }

            public bool IsProgressReporting
            {
                get { return isProgressReporting; }
            }

            public double Progress
            {
                get { return progress; }
            }

            public void ReportProgress(double progress)
            {
                if (!isProgressReporting)
                {
                    throw new ArgumentException("this task doesn't support progress reporting");
                }

                if (progress < 0)
                {
                    progress = 0;
                }
                else if (progress > 100)
                {
                    progress = 100;
                }

                this.progress = progress;
                OnPropertyChanged("Progress");

                if (prevProgress != (int)progress)
                {
                    prevProgress = (int)progress;
                    //Debug.WriteLine("ProgressReporter::" + Name + ": " + prevProgress + "%"); // this immensely slows down debugging performance
                }
            }

            public void Finish()
            {
                monitor.EndTask(this);
                monitor = null;
                isFinished = true;
                Debug.WriteLine("ProgressReporter::" + name + " duration: " + (DateTime.Now - startTime));
            }

            public bool IsFinished
            {
                get { return isFinished; }
            }

            private void OnPropertyChanged(string propertyName)
            {
                PropertyChangedEventHandler handler = PropertyChanged;
                if (handler != null)
                {
                    handler(this, new PropertyChangedEventArgs(propertyName));
                }
            }
        }

        private static ProgressMonitor singletonInstance = null;
        private static Object lockObject = new Object();

        private List<ProgressReporter> reporters;

        private Timer timer;
        private string statusMessage;

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

        public ProgressMonitor()
        {
            reporters = new List<ProgressReporter>();
            timer = new Timer(100) { Enabled = false };
            timer.Elapsed += new ElapsedEventHandler(timer_Elapsed);
            childMonitors = new List<ProgressMonitor>();
        }

        public static ProgressMonitor GlobalInstance
        {
            get
            {
                if (singletonInstance == null)
                {
                    singletonInstance = new ProgressMonitor();
                }
                return singletonInstance;
            }
        }

        public IProgressReporter BeginTask(string taskName)
        {
            return BeginTask(new ProgressReporter(this, taskName));
        }

        public IProgressReporter BeginTask(string taskName, bool reportProgress)
        {
            return BeginTask(new ProgressReporter(this, taskName, reportProgress));
        }

        private ProgressReporter BeginTask(ProgressReporter reporter)
        {
            bool started = false;

            lock (lockObject)
            {
                if (reporters.Count == 0)
                {
                    started = true;
                }
                reporters.Add(reporter);
            }

            OnTaskBegun(reporter);

            if (started)
            {
                OnProcessingStarted();
            }

            return reporter;
        }

        private void EndTask(ProgressReporter reporter)
        {
            bool finished = false;

            OnTaskEnded(reporter);

            lock (lockObject)
            {
                reporters.Remove(reporter);
                if (reporters.Count == 0)
                {
                    finished = true;
                }
            }

            if (finished)
            {
                OnProcessingFinished();
            }
        }

        public string StatusMessage
        {
            get { return statusMessage; }
        }

        public bool Active
        {
            get { return reporters.Count > 0; }
        }

        private void UpdateStatusMessage()
        {
            string message = "";

            lock (lockObject)
            {
                if (reporters.Count == 1)
                {
                    message += reporters[0].Name;
                }
                else if (reporters.Count > 1)
                {
                    message += "(" + reporters.Count + " tasks) ";
                    for (int x = 0; x < reporters.Count; x++)
                    {
                        message += reporters[x].Name;
                        if (x + 1 != reporters.Count)
                        {
                            message += "; ";
                        }
                    }
                }
            }

            statusMessage = message;
        }

        private void OnProcessingStarted()
        {
            if (TaskBegun == null)
            {
                timer.Enabled = true;
            }
            if (ProcessingStarted != null)
            {
                ProcessingStarted(this, EventArgs.Empty);
            }
        }

        private void OnProcessingProgressChanged()
        {
            float progress = 0;
            int progressValueReporterCount = 0;

            lock (lockObject)
            {
                if (reporters.Count > 0)
                {
                    foreach (ProgressReporter reporter in reporters)
                    {
                        if (reporter.IsProgressReporting)
                        {
                            progress += (float)reporter.Progress;
                            progressValueReporterCount++;
                        }
                    }
                    progress /= progressValueReporterCount;
                }
            }

            if (progressValueReporterCount > 0 && ProcessingProgressChanged != null)
            {
                ProcessingProgressChanged(this, new ValueEventArgs<float>(progress));
            }

            if (childMonitors.Count > 0)
            {
                foreach (ProgressMonitor childMonitor in childMonitors)
                {
                    if (childMonitor.Active)
                    {
                        childMonitor.OnProcessingProgressChanged();
                    }
                }
            }
        }

        private void OnProcessingFinished()
        {
            if (TaskEnded == null)
            {
                timer.Enabled = false;
            }
            if (ProcessingFinished != null)
            {
                ProcessingFinished(this, EventArgs.Empty);
            }
        }

        private void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            OnProcessingProgressChanged();
        }

        #region Child Monitor handling

        public void AddChild(ProgressMonitor childMonitor)
        {
            childMonitors.Add(childMonitor);
            childMonitor.TaskBegun += childMonitor_TaskBegun;
            childMonitor.TaskEnded += childMonitor_TaskEnded;
        }

        public bool RemoveChild(ProgressMonitor childMonitor)
        {
            if (childMonitors.Remove(childMonitor))
            {
                childMonitor.TaskBegun -= childMonitor_TaskBegun;
                childMonitor.TaskEnded -= childMonitor_TaskEnded;
                return true;
            }
            return false;
        }

        private void childMonitor_TaskBegun(object sender, ValueEventArgs<ProgressReporter> e)
        {
            BeginTask(e.Value);
        }

        private void childMonitor_TaskEnded(object sender, ValueEventArgs<ProgressReporter> e)
        {
            EndTask(e.Value);
        }

        private void OnTaskBegun(ProgressReporter reporter)
        {
            UpdateStatusMessage();
            if (TaskBegun != null)
            {
                TaskBegun(this, new ValueEventArgs<ProgressReporter>(reporter));
            }
        }

        private void OnTaskEnded(ProgressReporter reporter)
        {
            UpdateStatusMessage();
            if (TaskEnded != null)
            {
                TaskEnded(this, new ValueEventArgs<ProgressReporter>(reporter));
            }
        }

        #endregion
    }
}
