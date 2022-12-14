using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;

namespace TerrainEditor
{
    public static class Helpers
    {
        public static double Clamp(double val, double min, double max)
        {
            return (val < min) ? min : (val > max) ? max : val;
        }

        public static void SaveProject(TerrainDataModel model, string path)
        {
            using (var strm = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                var serializer = new XmlSerializer(model.GetType());
                serializer.Serialize(strm, model);
            }
            var imgPath = Path.Combine(Path.GetDirectoryName(path), "image" + Path.GetExtension(model.ImagePath));

            if (!Path.Equals(imgPath, model.ImagePath))
            {
                if (File.Exists(imgPath))
                {
                    File.Delete(imgPath);
                }
                File.Copy(model.ImagePath, imgPath);
                model.ImagePath = Path.GetFileName(imgPath);
            }
        }

        public static TerrainDataModel LoadProject(string path)
        {
            using (var strm = File.OpenRead(path))
            {
                var serializer = new XmlSerializer(typeof(TerrainDataModel));
                var retval = serializer.Deserialize(strm) as TerrainDataModel;
                if (retval != null)
                {
                    retval.ImagePath = Path.Combine(Path.GetDirectoryName(path), retval.ImagePath);
                }
                return retval;
            }
        }

        public static byte[] GetImageBytes(BitmapImage bmpImg)
        {
            int width = bmpImg.PixelWidth;
            int height = bmpImg.PixelHeight;
            int stride = width * ((bmpImg.Format.BitsPerPixel + 7) / 8);

            var bytes = new byte[height * stride];
            bmpImg.CopyPixels(bytes, stride, 0);

            return bytes;
        }

        public static Color? GetPixelColor(BitmapImage bmpImg, byte[] bytes, int x, int y)
        {
            int width = bmpImg.PixelWidth;
            int height = bmpImg.PixelHeight;
            int stride = width * ((bmpImg.Format.BitsPerPixel + 7) / 8);

            int idx = y * stride + 4 * x;

            if (idx + 4 <= bytes.Length)
                return new Color { A = bytes[idx], R = bytes[idx + 1], G = bytes[idx + 2], B = bytes[idx + 3] };
            
            return null;
        }

        public static Point GetDirLengthPoint(Point point, int dir, int len)
        {
            return new Point
            {
                X = point.X + (len * Math.Cos(dir)),
                Y = point.Y + (len * Math.Sin(dir))
            };
        }

        public static PixelColor GetColor(BitmapSource source, PixelColor[,] pixelColors,Point mousePos)
        {
            var pX = (mousePos.X * pixelColors.GetLength(0)) / source.Width;
            var pY = (mousePos.Y * pixelColors.GetLength(1)) / source.Height;
            return pixelColors[(int)pX, (int)pY]; 
        }

        public static Point? GetCircleNonTransparentFirstPixel(BitmapSource source, PixelColor[,] pixelColors, Point pos, int radius = 20)
        {
            var firstColor = GetColor(source, pixelColors, pos);
            for (var r = 0; r <= radius; r++)
            {
                for (var d = 0; d < 360; d++)
                {
                    var nPoint = GetDirLengthPoint(pos, d, r);
                    var pColor = GetColor(source, pixelColors, nPoint);

                    if ((firstColor.Alpha <= 0 && pColor.Alpha > 0) || (pColor.Alpha <= 0 && firstColor.Alpha > 0))
                    {
                        var diff = firstColor.Alpha > 0 ? -1 : 1;
                        return r > 0 ? GetDirLengthPoint(pos, d, r+diff) : nPoint; // the best point is at GetDirLengthPoint(pos, d, r-1)!!!
                    }
                }
            }
            return null;
        }

        public static string GenerateGMCode(TerrainDataModel terrain)
        {
            var retval = string.Empty;
            for (var i = 0; i < terrain.Segments.Count(); i++)
            {
                var seg = terrain.Segments[i];

                retval += string.Format("//\tSegement #{0}\n", i + 1);
                for (var j = 0; j < seg.SegmentPoints.Count()-1; j++)
                {
                    var p1 = seg.SegmentPoints[j];
                    var p2 = seg.SegmentPoints[j+1];

                    retval += string.Format("scr_create_line({0:0.00}, {1:0.00}, {2:0.00}, {3:0.00});\n", p1.X, p1.Y, p2.X, p2.Y);
                }
                if (seg.EndPoint != null)
                {
                    retval += string.Format("scr_create_line({0:0.00}, {1:0.00}, {2:0.00}, {3:0.00});\n", seg.EndPoint.X, seg.EndPoint.Y, seg.StartPoint.X, seg.StartPoint.Y);
                }
            }

            return retval;
        }

        public static PixelColor[,] GetPixels(BitmapSource source)
        {
            if (source.Format != PixelFormats.Bgra32)
                source = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);

            int width = source.PixelWidth;
            int height = source.PixelHeight;
            PixelColor[,] result = new PixelColor[width, height];
            
            source.CopyPixels(result, width * 4, 0, false);
            return result;
        }
    }
}
