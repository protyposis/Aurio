using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using System.Diagnostics;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using AudioAlign.Audio.Project;

namespace AudioAlign.WaveControls {
    [TemplatePart(Name = "PART_VerticalScrollBar", Type = typeof(ScrollBar))]
    public class MultiTrackListBox : ListBox {
        
        public static readonly DependencyProperty VirtualViewportWidthProperty;
        public static readonly DependencyProperty TrackHeadersVisibilityProperty;
        public static readonly DependencyProperty ControlsVisibilityProperty;

        static MultiTrackListBox() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(MultiTrackListBox), new FrameworkPropertyMetadata(typeof(MultiTrackListBox)));

            VirtualViewportWidthProperty = VirtualViewBase.VirtualViewportWidthProperty.AddOwner(typeof(MultiTrackListBox), new FrameworkPropertyMetadata() { Inherits = true });
            
            TrackHeadersVisibilityProperty = DependencyProperty.Register(
                "TrackHeadersVisibility", typeof(Visibility), typeof(MultiTrackListBox),
                    new FrameworkPropertyMetadata { AffectsRender = true, DefaultValue = Visibility.Visible });

            ControlsVisibilityProperty = DependencyProperty.Register(
                "ControlsVisibility", typeof(Visibility), typeof(MultiTrackListBox),
                    new FrameworkPropertyMetadata { AffectsRender = true, DefaultValue = Visibility.Visible });
        }

        public long VirtualViewportWidth {
            get { return (long)GetValue(VirtualViewportWidthProperty); }
            set { SetValue(VirtualViewportWidthProperty, value); }
        }

        public Visibility TrackHeadersVisibility {
            get { return (Visibility)GetValue(TrackHeadersVisibilityProperty); }
            set { SetValue(TrackHeadersVisibilityProperty, value); }
        }

        public Visibility ControlsVisibility {
            get { return (Visibility)GetValue(ControlsVisibilityProperty); }
            set { SetValue(ControlsVisibilityProperty, value); }
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e) {
            base.OnKeyUp(e);
            if (SelectedItem != null && Keyboard.Modifiers == ModifierKeys.Shift) {
                TrackList<AudioTrack> itemsSource = (TrackList<AudioTrack>) ItemsSource;

                int oldIndex = itemsSource.IndexOf((AudioTrack)SelectedItem);
                int newIndex = oldIndex;

                if (e.Key == Key.Up) {
                    newIndex = Math.Max(0, newIndex - 1);
                }
                else if (e.Key == Key.Down) {
                    newIndex = Math.Min(itemsSource.Count - 1, oldIndex + 1);
                }

                if (newIndex != oldIndex) {
                    itemsSource.Move(oldIndex, newIndex);
                    SelectedIndex = newIndex;

                    // http://stackoverflow.com/a/10463162
                    ListBoxItem listBoxItem = (ListBoxItem)ItemContainerGenerator.ContainerFromItem(SelectedItem);
                    listBoxItem.Focus();
                }

                e.Handled = true;
            }
        }
    }
}
