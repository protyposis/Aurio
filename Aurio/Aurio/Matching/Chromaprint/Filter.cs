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

namespace Aurio.Matching.Chromaprint
{
    class Filter
    {
        private delegate double FilterFunctionDelegate(IntegralImage i, int x, int y, int w, int h);

        private FilterFunctionDelegate FilterFunction;
        private int y;
        private int width;
        private int height;

        public Filter(int type, int y, int width, int height)
        {
            switch (type)
            {
                case 0:
                    FilterFunction = Filter0;
                    break;
                case 1:
                    FilterFunction = Filter1;
                    break;
                case 2:
                    FilterFunction = Filter2;
                    break;
                case 3:
                    FilterFunction = Filter3;
                    break;
                case 4:
                    FilterFunction = Filter4;
                    break;
                case 5:
                    FilterFunction = Filter5;
                    break;
                default:
                    throw new ArgumentException("invalid filter type " + type);
            }

            this.y = y;
            this.width = width;
            this.height = height;
        }

        public int Width
        {
            get { return width; }
        }

        public int Height
        {
            get { return height; }
        }

        public double Apply(IntegralImage image, int x)
        {
            return FilterFunction(image, x, y, width, height);
        }

        private double Subtract(double a, double b)
        {
            return Math.Log(1.0 + a) - Math.Log(1.0 + b);
        }

        /// <summary>
        /// xxxxxxxx
        /// xxxxxxxx
        /// xxxxxxxx
        /// xxxxxxxx
        /// </summary>
        private double Filter0(IntegralImage i, int x, int y, int w, int h)
        {
            double a = i.CalculateArea(x, y, x + w - 1, y + h - 1);
            double b = 0;
            return Subtract(a, b);
        }

        /// <summary>
        /// ........
        /// ........
        /// xxxxxxxx
        /// xxxxxxxx
        /// </summary>
        private double Filter1(IntegralImage i, int x, int y, int w, int h)
        {
            int halfH = h / 2;
            double a = i.CalculateArea(x, y + halfH, x + w - 1, y + h - 1);
            double b = i.CalculateArea(x, y, x + w - 1, y + halfH - 1);
            return Subtract(a, b);
        }

        /// <summary>
        /// ....xxxx
        /// ....xxxx
        /// ....xxxx
        /// ....xxxx
        /// </summary>
        private double Filter2(IntegralImage i, int x, int y, int w, int h)
        {
            int halfW = w / 2;
            double a = i.CalculateArea(x + halfW, y, x + w - 1, y + h - 1);
            double b = i.CalculateArea(x, y, x + halfW - 1, y + h - 1);
            return Subtract(a, b);
        }

        /// <summary>
        /// ....xxxx
        /// ....xxxx
        /// xxxx....
        /// xxxx....
        /// </summary>
        private double Filter3(IntegralImage i, int x, int y, int w, int h)
        {
            int halfW = w / 2;
            int halfH = h / 2;
            double a =
                i.CalculateArea(x, y + halfH, x + halfW - 1, y + h - 1)
                + i.CalculateArea(x + halfW, y, x + w - 1, y + halfH - 1);
            double b =
                i.CalculateArea(x, y, x + halfW - 1, y + halfH - 1)
                + i.CalculateArea(x + halfW, y + halfH, x + w - 1, y + h - 1);
            return Subtract(a, b);
        }

        /// <summary>
        /// ........
        /// xxxxxxxx
        /// ........
        /// </summary>
        private double Filter4(IntegralImage i, int x, int y, int w, int h)
        {
            int thirdH = h / 3;
            double a = i.CalculateArea(x, y + thirdH, x + w - 1, y + 2 * thirdH - 1);
            double b =
                i.CalculateArea(x, y, x + w - 1, y + thirdH - 1)
                + i.CalculateArea(x, y + 2 * thirdH, x + w - 1, y + h - 1);
            return Subtract(a, b);
        }

        /// <summary>
        /// ...xxx...
        /// ...xxx...
        /// ...xxx...
        /// <summary>
        private double Filter5(IntegralImage i, int x, int y, int w, int h)
        {
            int thirdW = w / 3;
            double a = i.CalculateArea(x + thirdW, y, x + 2 * thirdW - 1, y + h - 1);
            double b =
                i.CalculateArea(x, y, x + thirdW - 1, y + h - 1)
                + i.CalculateArea(x + 2 * thirdW, y, x + w - 1, y + h - 1);
            return Subtract(a, b);
        }
    }
}
