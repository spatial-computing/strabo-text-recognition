using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Threading;
using Strabo.Core.ImageProcessing;

namespace Strabo.Core.ColorSegmentation
{
    public class MedianCutMultiThreads : IMedianCut
    {
        public Hashtable color_table = new Hashtable();
        public List<Hashtable> color_table_list = new List<Hashtable>();
        public ColorNode[] imageColors; // original (unique) image colors
        public Hashtable ini_color_table = new Hashtable();
        public List<Hashtable> ini_color_table_list = new List<Hashtable>();
        public int[] pixels;
        private int qimg_counter;
        private List<int> qnum_list = new List<int>();
        public ColorNode[] quantColors = null; // quantized colors

        public List<ColorNode[]> quantColors_list = new List<ColorNode[]>();
        private unsafe byte* src;

        private Bitmap srcimg;
        private int srcOffset;
        private int tnum;
        public int width, height;

        public bool Process(string srcpathfn, string dstpathfn, int color_number)
        {
            var qnum_list = new List<int>();
            qnum_list.Add(color_number);
            GenerateColorPalette(srcpathfn, qnum_list);
            var mcqImagePaths = quantizeImageMT(8, dstpathfn);
            return true;
        }

        ~MedianCutMultiThreads()
        {
            Console.WriteLine("MedianCut Disposed");
        }

        private void GenerateColorPalette(string fn, List<int> qnum_list)
        {
            this.qnum_list = qnum_list;
            srcimg = new Bitmap(fn);
            width = srcimg.Width;
            height = srcimg.Height;
            pixels = ImageUtils.BitmapToArray1DIntRGB(srcimg);
            for (var i = 0; i < qnum_list.Count; i++)
            {
                color_table_list.Add(new Hashtable());
                ini_color_table_list.Add(new Hashtable());
            }
            var colorHist = new ColorHistogram(pixels, false);
            var K = colorHist.getNumberOfColors();
            findRepresentativeColors(K, colorHist);
        }

        private void findRepresentativeColors(int K, ColorHistogram colorHist)
        {
            imageColors = new ColorNode[K];
            for (var i = 0; i < K; i++)
            {
                var rgb = colorHist.getColor(i);
                var cnt = colorHist.getCount(i);
                imageColors[i] = new ColorNode(rgb, cnt);
            }

            //if (K <= qnum_list[0]) // image has fewer colors than Kmax
            //    rCols = imageColors;
            //else
            {
                var initialBox = new ColorBox(0, K - 1, 0, imageColors);
                var colorSet = new List<ColorBox>();
                colorSet.Add(initialBox);
                var k = 1;
                for (var i = 0; i < qnum_list.Count; i++)
                {
                    var Kmax = qnum_list[i];
                    var done = false;
                    while (k < Kmax && !done)
                    {
                        var nextBox = findBoxToSplit(colorSet);
                        if (nextBox != null)
                        {
                            var newBox = nextBox.splitBox();
                            colorSet.Add(newBox);
                            k = k + 1;
                        }
                        else
                        {
                            done = true;
                        }
                    }
                    quantColors_list.Add(averageColors(colorSet, i));
                }
            }
            colorHist = null;
            GC.Collect();
        }

        public void kernel(object step)
        {
            for (var y = (int) step; y < height; y += tnum)
            for (var x = 0; x < width; x++)
            {
                var pos = y * width + x;
                unsafe
                {
                    var cn = quantColors_list[qimg_counter][(int) ini_color_table_list[qimg_counter][pixels[pos]]];
                    src[pos * 3 + y * srcOffset + RGB.R] = (byte) cn.red;
                    src[pos * 3 + y * srcOffset + RGB.G] = (byte) cn.grn;
                    src[pos * 3 + y * srcOffset + RGB.B] = (byte) cn.blu;
                }
            }
        }

        public string[] quantizeImageMT(int tnum, string dstpathfn)
        {
            pixels = null;
            pixels = ImageUtils.BitmapToArray1DIntRGB(srcimg);
            this.tnum = tnum;

            var outImgPaths = new string[qnum_list.Count];
            for (var j = 0; j < qnum_list.Count; j++)
            {
                qimg_counter = j;
                var srcData = srcimg.LockBits(
                    new Rectangle(0, 0, width, height),
                    ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
                unsafe
                {
                    src = (byte*) srcData.Scan0.ToPointer();
                }
                srcOffset = srcData.Stride - width * 3;


                var thread_array = new Thread[tnum];
                for (var i = 0; i < tnum; i++)
                {
                    thread_array[i] = new Thread(kernel);
                    thread_array[i].Start(i);
                }
                for (var i = 0; i < tnum; i++)
                    thread_array[i].Join();

                srcimg.UnlockBits(srcData);
                //outImgPaths[j] = output_dir + fn + qnum_list[j] + ".png";
                srcimg.Save(dstpathfn, ImageFormat.Png);
            }

            return outImgPaths;
        }

        public void quantizeImageMT(string dir, string fn)
        {
            for (var j = 0; j < qnum_list.Count; j++)
                srcimg.Save(dir + fn + qnum_list[j] + ".png", ImageFormat.Png);
        }

        private ColorBox findBoxToSplit(List<ColorBox> colorBoxes)
        {
            ColorBox boxToSplit = null;
            colorBoxes.Sort();
            for (var i = 0; i < colorBoxes.Count; i++)
            {
                var box = colorBoxes[i];
                if (box.colorCount() >= 2)
                {
                    boxToSplit = box;
                    break;
                }
            }
            return boxToSplit;
        }

        private ColorNode[] averageColors(List<ColorBox> colorBoxes, int c)
        {
            var n = colorBoxes.Count();
            var avgColors = new ColorNode[n];
            for (var i = 0; i < colorBoxes.Count; i++)
            {
                var box = colorBoxes[i];
                avgColors[i] = box.getAverageColor(ini_color_table_list[c], i);
                //i = i + 1;
            }
            return avgColors;
        }

        public class RGB
        {
            public const short B = 0;
            public const short G = 1;
            public const short R = 2;
        }

        // -------------- class ColorNode -------------------------------------------

        public class ColorNode : IComparable
        {
            public int cnt;
            public int red, grn, blu;
            public int rgb;

            public ColorNode(int rgb, int cnt)
            {
                this.rgb = rgb & 0xFFFFFF;
                red = (rgb & 0xFF0000) >> 16;
                grn = (rgb & 0xFF00) >> 8;
                blu = rgb & 0xFF;
                this.cnt = cnt;
            }

            public ColorNode(int red, int grn, int blu, int cnt)
            {
                rgb = ((red & 0xff) << 16) | ((grn & 0xff) << 8) | (blu & 0xff);
                this.red = red;
                this.grn = grn;
                this.blu = blu;
                this.cnt = cnt;
            }

            // Implement IComparable CompareTo to provide default sort order.
            int IComparable.CompareTo(object obj)
            {
                var c = (ColorNode) obj;
                if (cnt > c.cnt) return 1;
                if (cnt < c.cnt) return -1;
                return 0;
            }

            public static IComparer sortRed()
            {
                return new sortRedHelper();
            }

            public static IComparer sortGreen()
            {
                return new sortGreenHelper();
            }

            public static IComparer sortBlue()
            {
                return new sortBlueHelper();
            }

            public int distance2(int red, int grn, int blu)
            {
                // returns the squared distance between (red, grn, blu)
                // and this color
                var dr = this.red - red;
                var dg = this.grn - grn;
                var db = this.blu - blu;
                return dr * dr + dg * dg + db * db;
            }

            private class sortRedHelper : IComparer
            {
                int IComparer.Compare(object a, object b)
                {
                    var cn1 = (ColorNode) a;
                    var cn2 = (ColorNode) b;
                    if (cn1.red > cn2.red) return 1;
                    if (cn1.red < cn2.red) return -1;
                    return 0;
                }
            }

            private class sortGreenHelper : IComparer
            {
                int IComparer.Compare(object a, object b)
                {
                    var cn1 = (ColorNode) a;
                    var cn2 = (ColorNode) b;
                    if (cn1.grn > cn2.grn) return 1;
                    if (cn1.grn < cn2.grn) return -1;
                    return 0;
                }
            }

            private class sortBlueHelper : IComparer
            {
                int IComparer.Compare(object a, object b)
                {
                    var cn1 = (ColorNode) a;
                    var cn2 = (ColorNode) b;
                    if (cn1.blu > cn2.blu) return 1;
                    if (cn1.blu < cn2.blu) return -1;
                    return 0;
                }
            }
        }

        // -------------- class ColorBox -------------------------------------------

        public class ColorBox : IComparable
        {
            public int bmin, bmax; // range of contained colors in blue dimension
            public int count; // number of pixels represented by thos color box
            public int gmin, gmax; // range of contained colors in green dimension
            private readonly ColorNode[] imageColors;
            public int level; // split level o this color box
            public int lower; // lower index into 'imageColors'
            public int rmin, rmax; // range of contained colors in red dimension
            public int upper = -1; // upper index into 'imageColors'

            public ColorBox(int lower, int upper, int level, ColorNode[] imageColors)
            {
                this.lower = lower;
                this.upper = upper;
                this.level = level;
                this.imageColors = imageColors;
                trim();
            }

            int IComparable.CompareTo(object obj)
            {
                var cb = (ColorBox) obj;
                if (level > cb.level) return 1;
                if (level < cb.level) return -1;
                return 0;
            }

            public int colorCount()
            {
                return upper - lower;
            }

            public void trim()
            {
                // recompute the boundaries of this color box
                rmin = 255;
                rmax = 0;
                gmin = 255;
                gmax = 0;
                bmin = 255;
                bmax = 0;
                count = 0;
                for (var i = lower; i <= upper; i++)
                {
                    var color = imageColors[i];
                    count = count + color.cnt;
                    var r = color.red;
                    var g = color.grn;
                    var b = color.blu;
                    if (r > rmax) rmax = r;
                    if (r < rmin) rmin = r;
                    if (g > gmax) gmax = g;
                    if (g < gmin) gmin = g;
                    if (b > bmax) bmax = b;
                    if (b < bmin) bmin = b;
                }
            }

            // Split this color box at the median point along its 
            // longest color dimension
            public ColorBox splitBox()
            {
                if (colorCount() < 2) // this box cannot be split
                    return null;
                // find longest dimension of this box:
                var dim = getLongestColorDimension();

                // find median along dim
                var med = findMedian(dim);

                // now split this box at the median return the resulting new
                // box.
                var nextLevel = level + 1;
                var newBox = new ColorBox(med + 1, upper, nextLevel, imageColors);
                upper = med;
                level = nextLevel;
                trim();
                return newBox;
            }

            // Find longest dimension of this color box (RED, GREEN, or BLUE)
            public int getLongestColorDimension()
            {
                var rLength = rmax - rmin;
                var gLength = gmax - gmin;
                var bLength = bmax - bmin;
                if (bLength >= rLength && bLength >= gLength)
                    return 3; //B
                if (gLength >= rLength && gLength >= bLength)
                    return 2; //G
                return 1; //R
            }

            // Find the position of the median in RGB space along
            // the red, green or blue dimension, respectively.
            //public int CDCompare(ColorNode cn1, ColorNode cn2)
            //{
            //    if (cn1.red == cn2.red) return 0;
            //    else if (cn1.red < cn2.red) return -1;
            //    else return 1;
            //}
            public int findMedian(int dim) //ColorDimension dim)
            {
                // sort color in this box along dimension dim:
                var length = upper + 1 - lower;
                if (dim == 1)
                    Array.Sort(imageColors, lower, length, ColorNode.sortRed());
                if (dim == 2)
                    Array.Sort(imageColors, lower, length, ColorNode.sortGreen());
                if (dim == 3)
                    Array.Sort(imageColors, lower, length, ColorNode.sortBlue());
                // find the median point:
                var half = count / 2;
                int nPixels, median;
                for (median = lower, nPixels = 0; median < upper; median++)
                {
                    nPixels = nPixels + imageColors[median].cnt;
                    if (nPixels >= half)
                        break;
                }
                return median;
            }

            public ColorNode getAverageColor(Hashtable ini_color_table, int idx)
            {
                var rSum = 0;
                var gSum = 0;
                var bSum = 0;
                var n = 0;
                for (var i = lower; i <= upper; i++)
                {
                    var ci = imageColors[i];
                    var cnt = ci.cnt;
                    rSum = rSum + cnt * ci.red;
                    gSum = gSum + cnt * ci.grn;
                    bSum = bSum + cnt * ci.blu;
                    n = n + cnt;
                    if (!ini_color_table.ContainsKey(ci.rgb))
                        ini_color_table.Add(ci.rgb, idx);
                }
                double nd = n;
                var avgRed = (int) (0.5 + rSum / nd);
                var avgGrn = (int) (0.5 + gSum / nd);
                var avgBlu = (int) (0.5 + bSum / nd);

                return new ColorNode(avgRed, avgGrn, avgBlu, n);
            }
        }
    } //class MedianCut
}