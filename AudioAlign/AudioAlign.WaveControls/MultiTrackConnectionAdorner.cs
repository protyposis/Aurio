using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Documents;
using System.Windows;
using System.Windows.Media;
using AudioAlign.Audio.Project;
using System.Windows.Controls;
using System.Diagnostics;

namespace AudioAlign.WaveControls {
    class MultiTrackConnectionAdorner : Adorner {

        private MultiTrackListBox multiTrackListBox;
        List<TimeSpan> test;

        public MultiTrackConnectionAdorner(UIElement adornedElement, MultiTrackListBox multiTrackListBox)
            : base(adornedElement) {
                this.multiTrackListBox = multiTrackListBox;
                test = new List<TimeSpan>() { new TimeSpan(0, 1, 0), new TimeSpan(0, 2, 0), new TimeSpan(0, 2, 30) };
        }

        protected override void OnRender(DrawingContext drawingContext) {
            List<WaveView> waveViews = new List<WaveView>();
            foreach (AudioTrack audioTrack in multiTrackListBox.Items) {
                ListBoxItem item = (ListBoxItem)multiTrackListBox.ItemContainerGenerator.ContainerFromItem(audioTrack);
                ContentPresenter itemContentPresenter = UIUtil.FindVisualChild<ContentPresenter>(item);
                DataTemplate itemDataTemplate = itemContentPresenter.ContentTemplate;
                WaveView waveView = (WaveView)itemDataTemplate.FindName("waveView", itemContentPresenter);
                waveViews.Add(waveView);
            }

            foreach (TimeSpan time in test) {
                for(int i1 = 0; i1 < waveViews.Count; i1++) {
                    WaveView waveView1 = waveViews[i1];
                    double x1 = waveView1.VirtualToPhysicalIntervalOffset(waveView1.AudioTrack.Offset.Ticks + time.Ticks);
                    Point origin1 = waveView1.TranslatePoint(new Point(0, 0), this);
                    
                    for (int i2 = i1 + 1; i2 < waveViews.Count; i2++) {
                        WaveView waveView2 = waveViews[i2];
                        double x2 = waveView2.VirtualToPhysicalIntervalOffset(waveView2.AudioTrack.Offset.Ticks + time.Ticks);
                        Point origin2 = waveView2.TranslatePoint(new Point(0, 0), this);

                        double y1 = 0, y2 = 0;
                        if (origin1.Y < origin2.Y) {
                            y1 = waveView1.ActualHeight;
                        }
                        else {
                            y2 = waveView2.ActualHeight;
                        }

                        if (waveView1 != waveView2) {
                            // calculate line color depending on similarity:
                            // http://www.objectdefinitions.com/odblog/2008/calculating-a-color-gradient/
                            // or: draw multiple lines (e.g. green, yellow, red) and vary the alpha intensity
                            drawingContext.DrawLine(new Pen(Brushes.Red, 3) { DashStyle = DashStyles.Dash, EndLineCap = PenLineCap.Triangle, StartLineCap = PenLineCap.Triangle },
                                new Point(x1 + origin1.X, y1 + origin1.Y),
                                new Point(x2 + origin2.X, y2 + origin2.Y));
                        }
                    }
                }
            }
        }
    }
}
