using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace Strabo.Test
{
    public class LargeImageHandler
    {
        public LargeImageHandler() { }

        public string[] splitWholeImage(string folderPath, string fileName, int width_stride, int height_stride)
        {
            Bitmap source_img = new Bitmap(folderPath + fileName);
            /*string pathWithoutSuffix = "";
            if (wholeImagePath.Substring(wholeImagePath.Length - 4) == ".png")
                pathWithoutSuffix = wholeImagePath.Substring(0, wholeImagePath.Length - 4);*/
            int numHorizontal = (int)Math.Ceiling(source_img.Width * 1.0 / width_stride);
            int numVertical = (int)Math.Ceiling(source_img.Height * 1.0 / height_stride);
            string[] result = new string[numHorizontal * numVertical];
            int i = 0;
            for (int x = 1; (x-1) * width_stride < source_img.Width; x++)
            {
                for(int y = 1; (y-1) * height_stride < source_img.Height; y++)
                {
                    int width = Math.Min(width_stride, source_img.Width - (x - 1) * width_stride);
                    int height = Math.Min(height_stride, source_img.Height - (y - 1) * height_stride);
                    Rectangle rect = new Rectangle((x-1)*width_stride, (y-1)*height_stride, width, height);
                    Bitmap dst = new Bitmap(source_img.Clone(rect, source_img.PixelFormat));
                    dst.Save(folderPath + fileName + "-" + x + "-" + y + ".png", ImageFormat.Png);
                    result[i++] = fileName + "-" + x + "-" + y + ".png";
                }
            }
            return result;
        }
    }

}
