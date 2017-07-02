using System;
using System.Collections;
using System.Drawing.Imaging;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Strabo.Core.ImageProcessing;

namespace Strabo.Core.ColorSegmentation
{
    public class MedianCutMultiThreads:IMedianCut
    {
        public ColorNode[] imageColors = null;	// original (unique) image colors
        public ColorNode[] quantColors = null;	// quantized colors
        public int[] pixels;
        public int width, height;
        public Hashtable color_table = new Hashtable();
        public Hashtable ini_color_table = new Hashtable();

        public List<ColorNode[]> quantColors_list = new List<ColorNode[]>();
        public List<Hashtable> color_table_list = new List<Hashtable>();
        public List<Hashtable> ini_color_table_list = new List<Hashtable>();

        Bitmap srcimg;
        int tnum = 0;
        List<int> qnum_list = new List<int>();
        int qimg_counter = 0;
        unsafe byte* src;
        int srcOffset;

        public class RGB
        {
            public const short B = 0;
            public const short G = 1;
            public const short R = 2;
        }

        public MedianCutMultiThreads() { }
        ~MedianCutMultiThreads() { Console.WriteLine("MedianCut Disposed"); }

        private void GenerateColorPalette(string fn, List<int> qnum_list)
        {
            this.qnum_list = qnum_list;
            srcimg = new Bitmap(fn);
            width = srcimg.Width;
            height = srcimg.Height;
            pixels = ImageUtils.BitmapToArray1DIntRGB(srcimg);
            for (int i = 0; i < qnum_list.Count; i++)
            {
                color_table_list.Add(new Hashtable());
                ini_color_table_list.Add(new Hashtable());
            }
            ColorHistogram colorHist = new ColorHistogram(pixels, false);
            int K = colorHist.getNumberOfColors();
            findRepresentativeColors(K, colorHist);
        }

        void findRepresentativeColors(int K, ColorHistogram colorHist)
        {
            imageColors = new ColorNode[K];
            for (int i = 0; i < K; i++)
            {
                int rgb = colorHist.getColor(i);
                int cnt = colorHist.getCount(i);
                imageColors[i] = new ColorNode(rgb, cnt);
            }

            //if (K <= qnum_list[0]) // image has fewer colors than Kmax
            //    rCols = imageColors;
            //else
            {
                ColorBox initialBox = new ColorBox(0, K - 1, 0, imageColors);
                List<ColorBox> colorSet = new List<ColorBox>();
                colorSet.Add(initialBox);
                int k = 1;
                for (int i = 0; i < qnum_list.Count; i++)
                {
                    int Kmax = qnum_list[i];
                    bool done = false;
                    while (k < Kmax && !done)
                    {
                        ColorBox nextBox = findBoxToSplit(colorSet);
                        if (nextBox != null)
                        {
                            ColorBox newBox = nextBox.splitBox();
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
            for (int y = (int)step; y < height; y += tnum)
                for (int x = 0; x < width; x++)
                {
                    int pos = (y * width + x);
                    unsafe
                    {
                        ColorNode cn = quantColors_list[qimg_counter][(int)ini_color_table_list[qimg_counter][pixels[pos]]];
                        src[pos * 3 + y * srcOffset + RGB.R] = (byte)cn.red;
                        src[pos * 3 + y * srcOffset + RGB.G] = (byte)cn.grn;
                        src[pos * 3 + y * srcOffset + RGB.B] = (byte)cn.blu;
                    }
                }
        }

        public bool Process(String srcpathfn, String dstpathfn, int color_number)
        {
            List<int> qnum_list = new List<int>();
            qnum_list.Add(color_number);
            GenerateColorPalette(srcpathfn, qnum_list);
            string[] mcqImagePaths = quantizeImageMT(8, dstpathfn);
            return true;
        }
        public string[] quantizeImageMT(int tnum, string dstpathfn)
        {
            pixels = null;
            pixels = ImageUtils.BitmapToArray1DIntRGB(srcimg);
            this.tnum = tnum;

            string[] outImgPaths = new string[qnum_list.Count];
            for (int j = 0; j < qnum_list.Count; j++)
            {
                qimg_counter = j;
                BitmapData srcData = srcimg.LockBits(
                            new Rectangle(0, 0, width, height),
                            ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
                unsafe
                {
                    src = (byte*)srcData.Scan0.ToPointer();
                }
                srcOffset = srcData.Stride - width * 3;


                Thread[] thread_array = new Thread[tnum];
                for (int i = 0; i < tnum; i++)
                {
                    thread_array[i] = new Thread(new ParameterizedThreadStart(kernel));
                    thread_array[i].Start(i);
                }
                for (int i = 0; i < tnum; i++)
                    thread_array[i].Join();

                srcimg.UnlockBits(srcData);
                //outImgPaths[j] = output_dir + fn + qnum_list[j] + ".png";
                srcimg.Save(dstpathfn, ImageFormat.Png);
            }

            return outImgPaths;
        }
        public void quantizeImageMT(string dir, string fn)
        {
            for (int j = 0; j < qnum_list.Count; j++)
                srcimg.Save((dir + fn + qnum_list[j] + ".png"), ImageFormat.Png);
        }
        private ColorBox findBoxToSplit(List<ColorBox> colorBoxes)
        {
            ColorBox boxToSplit = null;
            colorBoxes.Sort();
            for (int i = 0; i < colorBoxes.Count; i++)
            {
                ColorBox box = colorBoxes[i];
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
            int n = colorBoxes.Count();
            ColorNode[] avgColors = new ColorNode[n];
            for (int i = 0; i < colorBoxes.Count; i++)
            {
                ColorBox box = colorBoxes[i];
                avgColors[i] = box.getAverageColor(ini_color_table_list[c], i);
                //i = i + 1;
            }
            return avgColors;
        }

        // -------------- class ColorNode -------------------------------------------

        public class ColorNode : IComparable
        {
            // Implement IComparable CompareTo to provide default sort order.
            int IComparable.CompareTo(object obj)
            {
                ColorNode c = (ColorNode)obj;
                if (this.cnt > c.cnt) return 1;
                if (this.cnt < c.cnt) return -1;
                return 0;
            }

            public static IComparer sortRed()
            {
                return (IComparer)new sortRedHelper();
            }
            public static IComparer sortGreen()
            {
                return (IComparer)new sortGreenHelper();
            }
            public static IComparer sortBlue()
            {
                return (IComparer)new sortBlueHelper();
            }

            private class sortRedHelper : IComparer
            {
                int IComparer.Compare(object a, object b)
                {
                    ColorNode cn1 = (ColorNode)a;
                    ColorNode cn2 = (ColorNode)b;
                    if (cn1.red > cn2.red) return 1;
                    if (cn1.red < cn2.red) return -1;
                    else return 0;
                }
            }
            private class sortGreenHelper : IComparer
            {
                int IComparer.Compare(object a, object b)
                {
                    ColorNode cn1 = (ColorNode)a;
                    ColorNode cn2 = (ColorNode)b;
                    if (cn1.grn > cn2.grn) return 1;
                    if (cn1.grn < cn2.grn) return -1;
                    else return 0;
                }
            }
            private class sortBlueHelper : IComparer
            {
                int IComparer.Compare(object a, object b)
                {
                    ColorNode cn1 = (ColorNode)a;
                    ColorNode cn2 = (ColorNode)b;
                    if (cn1.blu > cn2.blu) return 1;
                    if (cn1.blu < cn2.blu) return -1;
                    else return 0;
                }
            }
            public int rgb;
            public int red, grn, blu;
            public int cnt;

            public ColorNode(int rgb, int cnt)
            {
                this.rgb = (rgb & 0xFFFFFF);
                this.red = (rgb & 0xFF0000) >> 16;
                this.grn = (rgb & 0xFF00) >> 8;
                this.blu = (rgb & 0xFF);
                this.cnt = cnt;
            }
            public ColorNode(int red, int grn, int blu, int cnt)
            {
                this.rgb = ((red & 0xff) << 16) | ((grn & 0xff) << 8) | blu & 0xff;
                this.red = red;
                this.grn = grn;
                this.blu = blu;
                this.cnt = cnt;
            }
            public int distance2(int red, int grn, int blu)
            {
                // returns the squared distance between (red, grn, blu)
                // and this color
                int dr = this.red - red;
                int dg = this.grn - grn;
                int db = this.blu - blu;
                return dr * dr + dg * dg + db * db;
            }
        }

        // -------------- class ColorBox -------------------------------------------

        public class ColorBox : IComparable
        {
            public int lower = 0; 	// lower index into 'imageColors'
            public int upper = -1; // upper index into 'imageColors'
            public int level; 		// split level o this color box
            public int count = 0; 	// number of pixels represented by thos color box
            public int rmin, rmax;	// range of contained colors in red dimension
            public int gmin, gmax;	// range of contained colors in green dimension
            public int bmin, bmax;	// range of contained colors in blue dimension
            ColorNode[] imageColors = null;
            public ColorBox(int lower, int upper, int level, ColorNode[] imageColors)
            {
                this.lower = lower;
                this.upper = upper;
                this.level = level;
                this.imageColors = imageColors;
                this.trim();
            }
            int IComparable.CompareTo(object obj)
            {
                ColorBox cb = (ColorBox)obj;
                if (this.level > cb.level) return 1;
                if (this.level < cb.level) return -1;
                return 0;
            }
            public int colorCount()
            {
                return upper - lower;
            }
            public void trim()
            {
                // recompute the boundaries of this color box
                rmin = 255; rmax = 0;
                gmin = 255; gmax = 0;
                bmin = 255; bmax = 0;
                count = 0;
                for (int i = lower; i <= upper; i++)
                {
                    ColorNode color = imageColors[i];
                    count = count + color.cnt;
                    int r = color.red;
                    int g = color.grn;
                    int b = color.blu;
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
                if (this.colorCount() < 2)	// this box cannot be split
                    return null;
                else
                {
                    // find longest dimension of this box:
                    int dim = getLongestColorDimension();

                    // find median along dim
                    int med = findMedian(dim);

                    // now split this box at the median return the resulting new
                    // box.
                    int nextLevel = level + 1;
                    ColorBox newBox = new ColorBox(med + 1, upper, nextLevel, imageColors);
                    this.upper = med;
                    this.level = nextLevel;
                    this.trim();
                    return newBox;
                }
            }
            // Find longest dimension of this color box (RED, GREEN, or BLUE)
            public int getLongestColorDimension()
            {
                int rLength = rmax - rmin;
                int gLength = gmax - gmin;
                int bLength = bmax - bmin;
                if (bLength >= rLength && bLength >= gLength)
                    return 3; //B
                else if (gLength >= rLength && gLength >= bLength)
                    return 2; //G
                else return 1; //R
            }

            // Find the position of the median in RGB space along
            // the red, green or blue dimension, respectively.
            //public int CDCompare(ColorNode cn1, ColorNode cn2)
            //{
            //    if (cn1.red == cn2.red) return 0;
            //    else if (cn1.red < cn2.red) return -1;
            //    else return 1;
            //}
            public int findMedian(int dim)//ColorDimension dim)
            {
                // sort color in this box along dimension dim:
                int length = upper + 1 - lower;
                if (dim == 1)
                    Array.Sort(imageColors, lower, length, ColorNode.sortRed());
                if (dim == 2)
                    Array.Sort(imageColors, lower, length, ColorNode.sortGreen());
                if (dim == 3)
                    Array.Sort(imageColors, lower, length, ColorNode.sortBlue());
                // find the median point:
                int half = count / 2;
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
                int rSum = 0;
                int gSum = 0;
                int bSum = 0;
                int n = 0;
                for (int i = lower; i <= upper; i++)
                {
                    ColorNode ci = imageColors[i];
                    int cnt = ci.cnt;
                    rSum = rSum + cnt * ci.red;
                    gSum = gSum + cnt * ci.grn;
                    bSum = bSum + cnt * ci.blu;
                    n = n + cnt;
                    if (!ini_color_table.ContainsKey(ci.rgb))
                        ini_color_table.Add(ci.rgb, idx);
                }
                double nd = n;
                int avgRed = (int)(0.5 + rSum / nd);
                int avgGrn = (int)(0.5 + gSum / nd);
                int avgBlu = (int)(0.5 + bSum / nd);

                return new ColorNode(avgRed, avgGrn, avgBlu, n);
            }
        }
    } //class MedianCut
}