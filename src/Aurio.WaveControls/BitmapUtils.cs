using System.Runtime.CompilerServices;

namespace Aurio.WaveControls;

public static class BitmapUtils
{
    /// <summary>
    /// Fast Bresenham line drawing algorithm
    /// Taken and adapted from http://www.cs.unc.edu/~mcmillan/comp136/Lecture6/Lines.html (lineFast)
    /// </summary>
    public static unsafe void DrawLine(
        int x0,
        int y0,
        int x1,
        int y1,
        int* pixels,
        int width,
        int height,
        int color
    )
    {
        int dy = y1 - y0;
        int dx = x1 - x0;
        int stepx,
            stepy;
        int numPixels = width * height;

        if (dy < 0)
        {
            dy = -dy;
            stepy = -width;
        }
        else
        {
            stepy = width;
        }

        if (dx < 0)
        {
            dx = -dx;
            stepx = -1;
        }
        else
        {
            stepx = 1;
        }

        dy <<= 1;
        dx <<= 1;

        y0 *= width;
        y1 *= width;
        DrawPixel(pixels, numPixels, x0, y0, color);
        if (dx > dy)
        {
            int fraction = dy - (dx >> 1);
            while (x0 != x1)
            {
                if (fraction >= 0)
                {
                    y0 += stepy;
                    fraction -= dx;
                }

                x0 += stepx;
                fraction += dy;
                DrawPixel(pixels, numPixels, x0, y0, color);
            }
        }
        else
        {
            int fraction = dx - (dy >> 1);
            while (y0 != y1)
            {
                if (fraction >= 0)
                {
                    x0 += stepx;
                    fraction -= dy;
                }

                y0 += stepy;
                fraction += dx;
                DrawPixel(pixels, numPixels, x0, y0, color);
            }
        }
    }

    /// <summary>
    /// Fast Bresenham line drawing algorithm
    /// Taken and adapted from http://www.cs.unc.edu/~mcmillan/comp136/Lecture6/Lines.html (lineFast)
    /// </summary>
    public static unsafe void DrawLine(
        int x0,
        int y0,
        int x1,
        int y1,
        int[] pixels,
        int width,
        int height,
        int color
    )
    {
        fixed (int* arrayPointer = &pixels[0])
        {
            DrawLine(x0, y0, x1, y1, arrayPointer, width, height, color);
        }
    }

    /// <summary>
    /// Draw a 3x3 block at a given point (similar to SoundForge).
    /// </summary>
    public static void DrawPointMarker(int x, int y, int[] pixels, int width, int height, int color)
    {
        for (int i = x - 1; i <= x + 1; i++)
        {
            for (int j = y - 1; j <= y + 1; j++)
            {
                if (i < 0 || j < 0 || i >= width || j >= height)
                {
                    continue; // skip pixels that don't fit the bitmap
                }

                int pixelOffset = (j * width + i);
                pixels[pixelOffset] = color;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void DrawPixel(int* pixels, int numPixels, int x, int y, int color)
    {
        int pixelIndex = x + y;
        if (pixelIndex >= 0 && pixelIndex < numPixels)
        {
            pixels[pixelIndex] = color;
        }
    }
}
