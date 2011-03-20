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
using AudioAlign.Audio;

namespace AudioAlign.WaveControls {
    class MultiTrackConnectionAdorner : Adorner {

        private MultiTrackListBox multiTrackListBox;
        private SolidColorBrush brushGreen, brushYellow, brushRed;

        public MultiTrackConnectionAdorner(UIElement adornedElement, MultiTrackListBox multiTrackListBox)
            : base(adornedElement) {
                this.multiTrackListBox = multiTrackListBox;
                Matches = new ObservableCollection<Match>();
                Matches.CollectionChanged += Matches_CollectionChanged;

                brushGreen = Brushes.Green;
                brushYellow = Brushes.Yellow;
                brushRed = Brushes.Red;
        }

        public ObservableCollection<Match> Matches {
            get;
            private set;
        } 

        protected override void OnRender(DrawingContext drawingContext) {
            // NOTE the dictionary needs to be built every time because the ListBox.items.CollectionChanged event is inaccessible (protected)
            Dictionary<AudioTrack, WaveView> waveViewMappings = new Dictionary<AudioTrack, WaveView>();
            foreach (AudioTrack audioTrack in multiTrackListBox.Items) {
                ListBoxItem item = (ListBoxItem)multiTrackListBox.ItemContainerGenerator.ContainerFromItem(audioTrack);
                ContentPresenter itemContentPresenter = UIUtil.FindVisualChild<ContentPresenter>(item);
                DataTemplate itemDataTemplate = itemContentPresenter.ContentTemplate;
                WaveView waveView = (WaveView)itemDataTemplate.FindName("waveView", itemContentPresenter);
                waveViewMappings.Add(audioTrack, waveView);
            }

            foreach (Match match in Matches) {
                WaveView waveView1 = waveViewMappings[match.Track1];
                long timestamp1 = waveView1.AudioTrack.Offset.Ticks + match.Track1Time.Ticks;
                Point p1 = waveView1.TranslatePoint(
                    new Point(waveView1.VirtualToPhysicalIntervalOffset(timestamp1), 0), this);
                    
                WaveView waveView2 = waveViewMappings[match.Track2];
                long timestamp2 = waveView2.AudioTrack.Offset.Ticks + match.Track2Time.Ticks;
                Point p2 = waveView2.TranslatePoint(
                    new Point(waveView2.VirtualToPhysicalIntervalOffset(timestamp2), 0), this);

                if (p1.Y < p2.Y) {
                    p1.Y += waveView1.ActualHeight;
                }
                else {
                    p2.Y += waveView2.ActualHeight;
                }

                // make p1 always the left point, p2 the right point
                if (p1.X > p2.X) {
                    Point temp = p1;
                    p1 = p2;
                    p2 = temp;
                }

                // find out if a match is invisible and can be skipped
                double bx1 = 0; // x-coord of left drawing boundary
                double bx2 = ActualWidth; // x-coord of right drawing boundary
                if ((p1.X >= bx1 && p1.X <= bx2)
                    || (p2.X >= bx1 && p2.X <= bx2)
                    || (p1.X < bx1 && p2.X > bx2)) {
                    // calculate bounded line drawing coordinates to avoid that lines with very long lengths need to be rendered
                    // drawing of lines with lengths > 100000 is very very slow or makes the application even stop
                    double k = (p2.Y - p1.Y) / (p2.X - p1.X); // line gradient
                    if (p1.X < bx1) {
                        double delta = Math.Abs(p1.X - bx1);
                        p1.X += delta;
                        p1.Y += k * delta;
                    }
                    if (p2.X > bx2) {
                        double delta = Math.Abs(p2.X - bx2);
                        p2.X -= delta;
                        p2.Y -= k * delta;
                    }
                }
                else {
                    continue; // skip invisible matches
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

        private void Matches_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
            InvalidateVisual();
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
