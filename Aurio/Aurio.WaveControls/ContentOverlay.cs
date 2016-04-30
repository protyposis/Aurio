// 
// Aurio: Audio Processing, Analysis and Retrieval Library
// Copyright (C) 2010-2016  Mario Guggenberger <mg@protyposis.net>
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
using System.Windows;
using System.Windows.Media;

namespace Aurio.WaveControls {
    /// <summary>
    /// Wraps an element and adds an overlay to draw above the wrapped element. 
    /// 
    /// Directly drawing in a ContentControl does not work because the content is rendered afterwards and
    /// overpaints what was drawn, i.e. the content drawing is one layer above the ContentControl drawing.
    /// 
    /// This control adds a layer above the content and makes it possible to draw over the content. This
    /// is similar to an adorner, except the adorner cannot be configures in XAML and must be added in
    /// code behind. This control can just be added IN XAML as a wrapper of a content element to draw above.
    /// </summary>
    public abstract class ContentOverlay : VirtualContentViewBase {

        public static readonly DependencyProperty OverlayOpacityProperty;

        static ContentOverlay() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ContentOverlay),
                new FrameworkPropertyMetadata(typeof(ContentOverlay)));

            OverlayOpacityProperty = DependencyProperty.Register("OverlayOpacity",
                typeof(double), typeof(ContentOverlay));
        }

        internal ContentOverlayDrawSurface DrawSurface { get; set; }

        public double OverlayOpacity {
            get { return (double)GetValue(OverlayOpacityProperty); }
            set { SetValue(OverlayOpacityProperty, value); }
        }

        internal abstract void OnRenderOverlay(DrawingContext drawingContext);

        protected override void OnViewportOffsetChanged(long oldValue, long newValue) {
            DrawSurface?.InvalidateVisual();
        }

        protected override void OnViewportWidthChanged(long oldValue, long newValue) {
            DrawSurface?.InvalidateVisual();
        }
    }
}
