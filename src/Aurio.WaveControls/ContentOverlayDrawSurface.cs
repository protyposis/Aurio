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
using System.Windows;
using System.Windows.Media;

namespace Aurio.WaveControls
{
    /// <summary>
    /// The draw surface that the ContentOverlay uses to draw above its content.
    /// <see cref="ContentOverlay"/>
    /// </summary>
    internal class ContentOverlayDrawSurface : FrameworkElement
    {
        public ContentOverlayDrawSurface()
        {
            Loaded += TrackMarkerDrawOverlay_Loaded;
        }

        private void TrackMarkerDrawOverlay_Loaded(object sender, RoutedEventArgs e)
        {
            // Search the ContentOverlay that owns this DrawSurface to wire them together
            // This must be done in the Loaded event because the visual tree is not initialized at construction time
            DependencyObject element = this;
            while (element != null)
            {
                if (element is ContentOverlay)
                {
                    // We have found our owner
                    var owner = (ContentOverlay)element;

                    // Wire the owner to this instance to pass the render calls from here to the owner
                    Owner = owner;

                    // Wire this instance to the owner to give him the possibility to signal events (e.g. invalidate visual)
                    owner.DrawSurface = this;

                    break;
                }
                element = VisualTreeHelper.GetParent(element);
            }

            if (element == null)
            {
                throw new Exception("the overlay could not find its owner in the visual tree");
            }
        }

        internal ContentOverlay Owner { get; set; }

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (Owner != null)
            {
                Owner.OnRenderOverlay(drawingContext);
            }
        }
    }
}
