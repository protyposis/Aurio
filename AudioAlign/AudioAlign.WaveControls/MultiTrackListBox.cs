using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using System.Diagnostics;

namespace AudioAlign.WaveControls {
    class MultiTrackListBox : ListBox {
        
        public static readonly DependencyProperty VirtualViewportWidthProperty;
        public static readonly DependencyProperty TrackHeadersVisibilityProperty;

        static MultiTrackListBox() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(MultiTrackListBox), new FrameworkPropertyMetadata(typeof(MultiTrackListBox)));

            VirtualViewportWidthProperty = VirtualViewBase.VirtualViewportWidthProperty.AddOwner(typeof(MultiTrackListBox), new FrameworkPropertyMetadata() { Inherits = true });
            
            TrackHeadersVisibilityProperty = DependencyProperty.Register(
                "TrackHeadersVisibility", typeof(Visibility), typeof(MultiTrackListBox),
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
    }
}
