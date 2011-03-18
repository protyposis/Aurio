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
using AudioAlign.Audio.Matching;
using System.Collections.ObjectModel;

namespace AudioAlign.WaveControls {
    class MultiTrackConnectionAdorner : Adorner {

        private MultiTrackListBox multiTrackListBox;
        private SolidColorBrush brushGreen, brushYellow, brushRed;

        public MultiTrackConnectionAdorner(UIElement adornedElement, MultiTrackListBox multiTrackListBox)
            : base(adornedElement) {
                this.multiTrackListBox = multiTrackListBox;
                Matches = new ObservableCollection<Match>();

                brushGreen = Brushes.Green;
                brushYellow = Brushes.Yellow;
                brushRed = Brushes.Red;

                Matches.Add(new Match() { Track1Time = new TimeSpan(0, 1, 0), Track2Time = new TimeSpan(0, 2, 0), Similarity = 1 });
                Matches.Add(new Match() { Track1Time = new TimeSpan(0, 1, 10), Track2Time = new TimeSpan(0, 2, 10), Similarity = 0.75f });
                Matches.Add(new Match() { Track1Time = new TimeSpan(0, 1, 20), Track2Time = new TimeSpan(0, 2, 20), Similarity = 0.5f });
                Matches.Add(new Match() { Track1Time = new TimeSpan(0, 1, 30), Track2Time = new TimeSpan(0, 2, 30), Similarity = 0.25f });
                Matches.Add(new Match() { Track1Time = new TimeSpan(0, 1, 40), Track2Time = new TimeSpan(0, 2, 40), Similarity = 0 });
        }

        public ObservableCollection<Match> Matches {
            get;
            private set;
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

            foreach (Match match in Matches) {
                for(int i1 = 0; i1 < waveViews.Count; i1++) {
                    WaveView waveView1 = waveViews[i1];
                    double x1 = waveView1.VirtualToPhysicalIntervalOffset(waveView1.AudioTrack.Offset.Ticks + match.Track1Time.Ticks);
                    Point origin1 = waveView1.TranslatePoint(new Point(0, 0), this);
                    
                    for (int i2 = i1 + 1; i2 < waveViews.Count; i2++) {
                        WaveView waveView2 = waveViews[i2];
                        double x2 = waveView2.VirtualToPhysicalIntervalOffset(waveView2.AudioTrack.Offset.Ticks + match.Track2Time.Ticks);
                        Point origin2 = waveView2.TranslatePoint(new Point(0, 0), this);

                        double y1 = 0, y2 = 0;
                        if (origin1.Y < origin2.Y) {
                            y1 = waveView1.ActualHeight;
                        }
                        else {
                            y2 = waveView2.ActualHeight;
                        }

                        if (waveView1 != waveView2) {
                            // calculate brush colors depending on match similarity
                            if (match.Similarity < 0.5f) {
                                brushRed = SetAlpha(brushRed, (byte)(255 * (1 - 2 * match.Similarity)));
                                brushYellow = SetAlpha(brushYellow, (byte)(255 * (2 * match.Similarity)));
                                brushGreen = SetAlpha(brushGreen, 0);
                            }
                            else {
                                brushRed = SetAlpha(brushRed, 0);
                                brushYellow = SetAlpha(brushYellow, (byte)(255 * (1 - 2 * (match.Similarity - 0.5))));
                                brushGreen = SetAlpha(brushGreen, (byte)(255 * (2 * (match.Similarity - 0.5))));
                            }

                            Point p1 = new Point(x1 + origin1.X, y1 + origin1.Y);
                            Point p2 = new Point(x2 + origin2.X, y2 + origin2.Y);

                            // draw 3 stacked lines for the 3 basic colors
                            // depending on their alpha values the resulting visible line will be gradually different
                            drawingContext.DrawLine(new Pen(brushRed, 3) { DashStyle = DashStyles.Dash, EndLineCap = PenLineCap.Triangle, StartLineCap = PenLineCap.Triangle },
                                p1, p2);
                            drawingContext.DrawLine(new Pen(brushYellow, 3) { DashStyle = DashStyles.Dash, EndLineCap = PenLineCap.Triangle, StartLineCap = PenLineCap.Triangle },
                                p1, p2);
                            drawingContext.DrawLine(new Pen(brushGreen, 3) { DashStyle = DashStyles.Dash, EndLineCap = PenLineCap.Triangle, StartLineCap = PenLineCap.Triangle },
                                p1, p2);
                        }
                    }
                }
            }
        }

        private static SolidColorBrush SetAlpha(SolidColorBrush brush, byte alpha) {
            return new SolidColorBrush(new Color() {
                R = brush.Color.R,
                G = brush.Color.G,
                B = brush.Color.B,
                A = alpha
            });
        }
    }
}
