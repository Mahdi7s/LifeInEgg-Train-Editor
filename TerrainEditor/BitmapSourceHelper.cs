﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace TerrainEditor
{
    public static class BitmapSourceHelper
    {
#if UNSAFE
  public unsafe static void CopyPixels(this BitmapSource source, PixelColor[,] pixels, int stride, int offset)
  {
    fixed(PixelColor* buffer = &pixels[0, 0])
      source.CopyPixels(
        new Int32Rect(0, 0, source.PixelWidth, source.PixelHeight),
        (IntPtr)(buffer + offset),
        pixels.GetLength(0) * pixels.GetLength(1) * sizeof(PixelColor),
        stride);
  }
#else
        public static void CopyPixels(this BitmapSource source, PixelColor[,] pixels, int stride, int offset, bool fake)
        {
            var height = source.PixelHeight;
            var width = source.PixelWidth;
            var pixelBytes = new byte[height * width * 4];
            source.CopyPixels(pixelBytes, stride, 0);
            int y0 = offset / width;
            int x0 = offset - width * y0;
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    pixels[x + x0, y + y0] = new PixelColor
                    {
                        Blue = pixelBytes[(y * width + x) * 4 + 0],
                        Green = pixelBytes[(y * width + x) * 4 + 1],
                        Red = pixelBytes[(y * width + x) * 4 + 2],
                        Alpha = pixelBytes[(y * width + x) * 4 + 3],
                    };
        }
#endif
    }
}
