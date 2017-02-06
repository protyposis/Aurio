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
using System.Windows.Controls;
using System.Windows;
using Aurio;

namespace Aurio.WaveControls {
    public class VirtualContentViewBase : ContentControl, VirtualView {

        public static readonly DependencyProperty VirtualViewportOffsetProperty =
            VirtualViewBase.VirtualViewportOffsetProperty.AddOwner(typeof(VirtualContentViewBase),
            new FrameworkPropertyMetadata() { Inherits = true, PropertyChangedCallback = OnViewportOffsetChanged });

        public static readonly DependencyProperty VirtualViewportWidthProperty =
            VirtualViewBase.VirtualViewportWidthProperty.AddOwner(typeof(VirtualContentViewBase),
            new FrameworkPropertyMetadata() { Inherits = true, PropertyChangedCallback = OnViewportWidthChanged });

        public static readonly DependencyProperty VirtualViewportMinWidthProperty =
            VirtualViewBase.VirtualViewportMinWidthProperty.AddOwner(typeof(VirtualContentViewBase),
            new FrameworkPropertyMetadata() { Inherits = true, PropertyChangedCallback = OnViewportMinWidthChanged });

        public static readonly DependencyProperty VirtualViewportMaxWidthProperty =
            VirtualViewBase.VirtualViewportMaxWidthProperty.AddOwner(typeof(VirtualContentViewBase),
            new FrameworkPropertyMetadata() { Inherits = true, PropertyChangedCallback = OnViewportMaxWidthChanged });

        public static readonly DependencyProperty DebugOutputProperty =
            VirtualViewBase.DebugOutputProperty.AddOwner(typeof(VirtualContentViewBase),
            new FrameworkPropertyMetadata() { Inherits = true });

        private static void OnViewportOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            VirtualContentViewBase ctrl = (VirtualContentViewBase)d;
            ctrl.OnViewportOffsetChanged((long)e.OldValue, (long)e.NewValue);
        }

        private static void OnViewportWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            VirtualContentViewBase ctrl = (VirtualContentViewBase)d;
            ctrl.OnViewportWidthChanged((long)e.OldValue, (long)e.NewValue);
        }

        private static void OnViewportMinWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        }

        private static void OnViewportMaxWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        }

        public long VirtualViewportOffset {
            get { return (long)GetValue(VirtualViewportOffsetProperty); }
            set { SetValue(VirtualViewportOffsetProperty, value); }
        }

        public long VirtualViewportWidth {
            get { return (long)GetValue(VirtualViewportWidthProperty); }
            set { SetValue(VirtualViewportWidthProperty, value); }
        }

        public long VirtualViewportMinWidth {
            get { return (long)GetValue(VirtualViewportMinWidthProperty); }
            set { SetValue(VirtualViewportMinWidthProperty, value); }
        }

        public long VirtualViewportMaxWidth {
            get { return (long)GetValue(VirtualViewportMaxWidthProperty); }
            set { SetValue(VirtualViewportMaxWidthProperty, value); }
        }

        public bool DebugOutput {
            get { return (bool)GetValue(DebugOutputProperty); }
            set { SetValue(DebugOutputProperty, value); }
        }

        public Interval VirtualViewportInterval {
            get { return new Interval(VirtualViewportOffset, VirtualViewportOffset + VirtualViewportWidth); }
        }

        public long PhysicalToVirtualOffset(double physicalOffset) {
            return VirtualViewBase.PhysicalToVirtualOffset(VirtualViewportWidth, ActualWidth, physicalOffset);
        }

        public double VirtualToPhysicalOffset(long virtualOffset) {
            return VirtualViewBase.VirtualToPhysicalOffset(VirtualViewportWidth, ActualWidth, virtualOffset);
        }

        public long PhysicalToVirtualIntervalOffset(double physicalOffset) {
            return VirtualViewBase.PhysicalToVirtualIntervalOffset(VirtualViewportInterval, ActualWidth, physicalOffset);
        }

        public double VirtualToPhysicalIntervalOffset(long virtualOffset) {
            return VirtualViewBase.VirtualToPhysicalIntervalOffset(VirtualViewportInterval, ActualWidth, virtualOffset);
        }

        protected virtual void OnViewportOffsetChanged(long oldValue, long newValue) { }
        protected virtual void OnViewportWidthChanged(long oldValue, long newValue) { }
    }
}
