/*******************************************************************************
 * Copyright 2010 University of Southern California
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 * 	http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 * 
 * This code was developed as part of the Strabo map processing project 
 * by the Spatial Sciences Institute and by the Information Integration Group 
 * at the Information Sciences Institute of the University of Southern 
 * California. For more information, publications, and related projects, 
 * please see: http://spatial-computing.github.io/
 ******************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Threading;
using Strabo.Core.ImageProcessing;
using Strabo.Core.Utility;

namespace Strabo.Core.TextDetection
{
    public class ConditionalDilationAutomatic
    {
        public double angleThreshold;
        private readonly Hashtable char_blob_idx_max_size_table;

        private List<MyConnectedComponentsAnalysisFGFast.MyBlob> char_blobs;
        private bool[] char_labels_confirmation;
        private short[] char_labels_tmp;
        private short[] charLabels;
        private readonly List<HashSet<int>> connected_char_blob_idx_set_list;
        private Hashtable connecting_angle_table;
        private readonly HashSet<int> expendable_char_blob_idx_set;
        private int iteration_counter;
        private Hashtable iteration_for_first_connection_idx_table;
        public int iterationThreshold;

        private int minimum_distance_between_CCs_in_string;
        private ushort[] originalCharLabels;
        private readonly HashSet<int> short_char_blob_idx_set;
        public double sizeRatio;
        private readonly HashSet<string> weak_link_set;

        private int width, height, tnum;

        public ConditionalDilationAutomatic()
        {
            sizeRatio = StraboParameters.cdaSizeRatio;
            angleThreshold = StraboParameters.cdaAngleThreshold;
            iterationThreshold = StraboParameters.cdaIterationThreshold;
            minimum_distance_between_CCs_in_string = StraboParameters.minimumDistBetweenCC;
            char_blob_idx_max_size_table = new Hashtable();
            short_char_blob_idx_set = new HashSet<int>();
            expendable_char_blob_idx_set = new HashSet<int>();
            connected_char_blob_idx_set_list = new List<HashSet<int>>();
            weak_link_set = new HashSet<string>();
            connecting_angle_table = new Hashtable();
            iteration_for_first_connection_idx_table = new Hashtable();
            iteration_counter = 0;
        }


        public void kernel(object step) // expand CC areas
        {
            // multi-threading setup
            var delta_y = height / tnum;
            var start_y = delta_y * (int) step;
            var stop_y = start_y + delta_y;
            if ((int) step == tnum - 1) stop_y = height;

            for (var j = start_y; j < stop_y; j++)
            for (var i = 0; i < width; i++)
                if (charLabels[j * width + i] == 0) //bg
                {
                    var connected_char_blob_idx_set = new HashSet<short>();
                    for (var x = i - 1; x <= i + 1; x++) // check 8 neighboors
                    for (var y = j - 1; y <= j + 1; y++)
                    {
                        // outside image boundaries
                        if (x < 0 || x >= width || y < 0 || y >= height)
                            continue;
                        var idx = y * width + x;
                        if (charLabels[idx] != 0 && expendable_char_blob_idx_set.Contains(charLabels[idx] - 1)) //narges
                            connected_char_blob_idx_set.Add((short) (charLabels[idx] - 1));
                    }
                    // 0 connecting pixels, do not expand
                    if (connected_char_blob_idx_set.Count == 0 || connected_char_blob_idx_set.Count > 2)
                        char_labels_tmp[j * width + i] = 0;
                    // 1 connecting pixel, check if short-string-only expansion applied
                    else if (connected_char_blob_idx_set.Count == 1)
                        char_labels_tmp[j * width + i]
                            = (short) (connected_char_blob_idx_set.ElementAt(0) + 1);
                    else //(connected_char_blob_idx_set.Count == 2)
                        char_labels_tmp[j * width + i] =
                            (short) FindLabelForTheBiggestCharBlob(connected_char_blob_idx_set);
                }
                else
                {
                    char_labels_tmp[j * width + i] = charLabels[j * width + i];
                }
        }

        public void kernel_confirmation(object step)
        {
            var delta_y = height / tnum;
            var start_y = delta_y * (int) step;
            var stop_y = start_y + delta_y;
            if ((int) step == tnum - 1) stop_y = height;
            for (var j = start_y; j < stop_y; j++)
            for (var i = 0; i < width; i++)
                // expended areas and new expension
                if (originalCharLabels[j * width + i] == 0 &&
                    char_labels_tmp[j * width + i] != 0)
                {
                    var connected_char_blob_idx_set = new HashSet<short>();
                    byte connecting_counter = 0;
                    for (var x = i - 1; x <= i + 1; x++)
                    for (var y = j - 1; y <= j + 1; y++)
                    {
                        if (x < 0 || x >= width || y < 0 || y >= height) continue;
                        var idx = y * width + x;

                        if (idx >= 0 && idx < width * height)
                            if (char_labels_tmp[idx] != 0)
                            {
                                connecting_counter++;
                                connected_char_blob_idx_set.Add((short) (char_labels_tmp[idx] - 1));
                            }
                    }
                    if (connecting_counter == 1 || connected_char_blob_idx_set.Count == 0 ||
                        connected_char_blob_idx_set.Count > 2)
                        char_labels_confirmation[j * width + i] = false;
                    else if (connected_char_blob_idx_set.Count == 1)
                        char_labels_confirmation[j * width + i] = true;
                    else
                        char_labels_confirmation[j * width + i] = ValidateConnection(connected_char_blob_idx_set);
                }
                else
                {
                    char_labels_confirmation[j * width + i] = true;
                }
        }

        public bool BreakingAngle(int idx1, int idx2)
        {
            if (connected_char_blob_idx_set_list[idx1].Count == 0 && // idx1 and idx2 do not connect to anyone else
                connected_char_blob_idx_set_list[idx2].Count == 0)
                return false;
            if (connected_char_blob_idx_set_list[idx1].Contains(idx2) ||
                connected_char_blob_idx_set_list[idx2].Contains(idx1))
                return false;

            var idx11 = -1;
            if (connected_char_blob_idx_set_list[idx1].Count == 1)
                idx11 = connected_char_blob_idx_set_list[idx1].First(); // the list has exactly one element

            var idx22 = -1;
            if (connected_char_blob_idx_set_list[idx2].Count == 1)
                idx22 = connected_char_blob_idx_set_list[idx2].First(); // the list has exactly one element

            double angle1 = 0;
            if (idx11 != -1)
                angle1 = CosAngel(idx11, idx2, idx1);
            double angle2 = 0;
            if (idx22 != -1)
                angle2 = CosAngel(idx1, idx22, idx2);
            if (angle1 > angleThreshold || angle2 > angleThreshold)
                return true;
            return false;
        }

        public int FindLabelForTheBiggestCharBlob(HashSet<short> char_blob_idx_set)
        {
            int idx1 = char_blob_idx_set.First(); // char_blob_idx_set has exactly two elements
            int idx2 = char_blob_idx_set.Last();
            // check if any of the connecting CC has two neighbors already
            if (connected_char_blob_idx_set_list[idx1].Count == 2 &&
                !connected_char_blob_idx_set_list[idx1].Contains(idx2)
                || connected_char_blob_idx_set_list[idx2].Count == 2 &&
                !connected_char_blob_idx_set_list[idx2].Contains(idx1))
                return 0;
            if (BreakingAngle(idx1, idx2))
                return 0;
            // check for size ratio
            var size2 = (int) char_blob_idx_max_size_table[idx2];
            var size1 = (int) char_blob_idx_max_size_table[idx1];

            if (size1 / (double) size2 > sizeRatio
                || size2 / (double) size1 > sizeRatio)
                return 0;
            if (size1 > size2)
                return idx1 + 1;
            return idx2 + 1;
        }

        public bool ValidateConnection(HashSet<short> char_blob_idx_set)
        {
            // check if it has a weak link
            int idx1 = char_blob_idx_set.First(); // the set has exactly two elements
            int idx2 = char_blob_idx_set.Last();

            // check regular stuff
            if (FindLabelForTheBiggestCharBlob(char_blob_idx_set) != 0)
            {
                //if (weak_link_set.Count > 0) // now I know the real connecting nodes
                //{
                //    // the second confirmation check
                //    if (weak_link_set.Contains(idx1 + ":" + idx2)
                //        && weak_link_set.Contains(idx2 + ":" + idx1))
                //    {
                //        connected_char_blob_idx_set_list[idx1].Remove(idx2);
                //        connected_char_blob_idx_set_list[idx2].Remove(idx1);
                //        return false;
                //    }
                //}
                connected_char_blob_idx_set_list[idx1].Add(idx2);
                connected_char_blob_idx_set_list[idx2].Add(idx1);
                return true;
            }
            return false;
        }

        public void k1()
        {
            char_labels_tmp = new short[width * height];
            var thread_array = new Thread[tnum];
            for (var i = 0; i < tnum; i++)
            {
                thread_array[i] = new Thread(kernel);
                thread_array[i].Start(i);
            }
            for (var i = 0; i < tnum; i++)
                thread_array[i].Join();
            ConfirmationStep();
            IdentifyExpendableCharBlobs();
        }

        public void ConfirmationStep()
        {
            // record connectivity
            weak_link_set.Clear(); // we don't check weak links
            k_confirmation();
            //clear weak links
            for (var j = 0; j < height; j++)
            for (var i = 0; i < width; i++)
                if (char_labels_tmp[j * width + i] != 0 &&
                    char_labels_confirmation[j * width + i])
                    charLabels[j * width + i] = char_labels_tmp[j * width + i];
                else if (originalCharLabels[j * width + i] == 0)
                    charLabels[j * width + i] = 0;
        }

        public void k_confirmation()
        {
            char_labels_confirmation = new bool[width * height];
            var thread_array2 = new Thread[tnum];
            for (var i = 0; i < tnum; i++)
            {
                thread_array2[i] = new Thread(kernel_confirmation);
                thread_array2[i].Start(i);
            }
            for (var i = 0; i < tnum; i++)
                thread_array2[i].Join();
        }

        public string Apply(int tnum, Bitmap srcimg, double size_ratio, double ang_threshold, bool preprocessing,
            string outImagePath)
        {
            this.tnum = tnum;
            width = srcimg.Width;
            height = srcimg.Height;
            var iteration = StraboParameters.minimumDistBetweenCC; // default: 2
            sizeRatio = size_ratio;
            angleThreshold = ang_threshold;
            //input image: white as forground
            srcimg = ImageUtils.ConvertGrayScaleToBinary(srcimg, 128);

            if (preprocessing)
            {
                var cg = new ConnectLinesWithOnePixelGap();
                srcimg = cg.Apply(srcimg);
            }

            //srcimg = ImageUtils.InvertColors(srcimg);
            //Log.WriteBitmap(srcimg, "InvertedImage.png");
            minimum_distance_between_CCs_in_string = iteration;

            var char_bc = new MyConnectedComponentsAnalysisFGFast.MyBlobCounter();

            char_blobs = char_bc.GetBlobs(srcimg, 0);
            charLabels = new short[width * height];

            for (var i = 0; i < width * height; i++)
                charLabels[i] = (short) char_bc.objectLabels[i];

            for (var i = 0; i < char_blobs.Count; i++)
            {
                char_blob_idx_max_size_table.Add(i,
                    (int) Math.Max(char_blobs[i].bbx.width(),
                        char_blobs[i].bbx.height())); // original size of the character //narges
                expendable_char_blob_idx_set.Add(i);
                connected_char_blob_idx_set_list.Add(new HashSet<int>());
            }
            originalCharLabels = char_bc.objectLabels;
            for (var i = 0; i < width * height; i++)
                charLabels[i] = (short) char_bc.objectLabels[i];

            k1();

            iteration_counter = 1;
            while (iteration_counter < iterationThreshold)
            {
                k1();
                iteration_counter++;
            }
            Print(outImagePath);
            return outImagePath;
        }

        public void IdentifyExpendableCharBlobs()
        {
            short_char_blob_idx_set.Clear();
            var resultimg = Print();
            //resultimg = ImageUtils.InvertColors(resultimg);

            var string_bc = new MyConnectedComponentsAnalysisFGFast.MyBlobCounter();
            var string_blobs = string_bc.GetBlobs(resultimg, 0);
            var string_labels = string_bc.objectLabels;
            var string_count = string_blobs.Count;

            var string_list = new List<List<MyConnectedComponentsAnalysisFGFast.MyBlob>>();
            for (var i = 0; i < string_count; i++)
                string_list.Add(new List<MyConnectedComponentsAnalysisFGFast.MyBlob>());

            for (var i = 0; i < char_blobs.Count; i++)
            {
                // sample_x and sample_y are concerning
                char_blobs[i].string_id =
                    string_labels[char_blobs[i].sample_y * width + char_blobs[i].sample_x] - 1;

                string_list[char_blobs[i].string_id].Add(char_blobs[i]);
            }
            for (var i = 0; i < string_list.Count; i++)
                if (string_list[i].Count > 0)
                {
                    var avg_size = 0;
                    for (var j = 0; j < string_list[i].Count; j++)
                        avg_size += (int) Math.Max(string_list[i][j].bbx.width(), string_list[i][j].bbx.height());
                    avg_size /= string_list[i].Count;
                    for (var j = 0; j < string_list[i].Count; j++)
                        char_blob_idx_max_size_table[string_list[i][j].pixel_id - 1] = avg_size;
                }
            // recomputing expendable char blob idx
            for (var i = 0; i < char_blobs.Count; i++)
                if (connected_char_blob_idx_set_list[i].Count == 2)
                {
                    expendable_char_blob_idx_set.Remove(i);
                }
                else // 0 or 1 connecting CC
                {
                    var max_dist = (int) ((double) (int) char_blob_idx_max_size_table[i] / 3);
                    // int max_dist = (int)((double)((int)char_blob_idx_max_size_table[i])/2 );
                    if (max_dist < minimum_distance_between_CCs_in_string * 2)
                        max_dist = minimum_distance_between_CCs_in_string * 2;

                    if (iteration_counter > max_dist)
                        expendable_char_blob_idx_set.Remove(i);
                }
        }

        public Bitmap Printnumb()
        {
            var dstimg = new Bitmap(width, height);
            var g = Graphics.FromImage(dstimg);
            for (var i = 0; i < char_blobs.Count; i++)
            {
                var font2 = new Font("Arial", 8);
                g.DrawString(i.ToString(), font2, Brushes.Red, char_blobs[i].bbx.massCenter());
            }
            g.Dispose();
            return dstimg;
        }

        public Bitmap PrintRGB()
        {
            var img = new int[height * width];
            for (var i = 0; i < width; i++)
            for (var j = 0; j < height; j++)
                if (charLabels[j * width + i] == 0)
                    img[j * width + i] = 255 * 256 * 256 + 255 * 256 + 255;
                else if (charLabels[j * width + i] == 3)
                    img[j * width + i] = 255 * 256 * 256;
                else if (charLabels[j * width + i] == 6)
                    img[j * width + i] = 255 * 256;
                else
                    img[j * width + i] = 0;
            return ImageUtils.Array1DToBitmapRGB(img, width, height);
        }

        public Bitmap PrintTMPRGB()
        {
            var img = new int[height * width];
            for (var i = 0; i < width; i++)
            for (var j = 0; j < height; j++)
                if (char_labels_tmp[j * width + i] == 0)
                    img[j * width + i] = 255 * 256 * 256 + 255 * 256 + 255;
                else if (char_labels_tmp[j * width + i] == 3)
                    img[j * width + i] = 255 * 256 * 256;
                else if (char_labels_tmp[j * width + i] == 6)
                    img[j * width + i] = 255 * 256;
                else
                    img[j * width + i] = 0;
            return ImageUtils.Array1DToBitmapRGB(img, width, height);
        }

        public void Print(string outImagePath)
        {
            var img = new bool[height, width];
            for (var i = 0; i < width; i++)
            for (var j = 0; j < height; j++)
                if (charLabels[j * width + i] != 0)
                    img[j, i] = true;
                else
                    img[j, i] = false;
            ImageUtils.ArrayBool2DToBitmap(img).Save(outImagePath, ImageFormat.Png);
        }

        public Bitmap Print()
        {
            var img = new bool[height, width];
            for (var i = 0; i < width; i++)
            for (var j = 0; j < height; j++)
                if (charLabels[j * width + i] != 0)
                    img[j, i] = true;
                else
                    img[j, i] = false;
            return ImageUtils.ArrayBool2DToBitmap(img);
        }

        public double CosAngel(int idx1, int idx2, int i)
        {
            var angle1 = CosAngel(new PointF(char_blobs[idx1].bbx.massCenter().X, char_blobs[idx1].bbx.massCenter().Y),
                new PointF(char_blobs[idx2].bbx.massCenter().X, char_blobs[idx2].bbx.massCenter().Y),
                new PointF(char_blobs[i].bbx.massCenter().X, char_blobs[i].bbx.massCenter().Y));

            var angle2 = NormAngel(idx1, idx2, i);

            return (angle2 - angle1) / angle1;
        }

        public double NormAngel(int idx1, int idx2, int i)
        {
            var h1 = Math.Max(char_blobs[idx1].bbx.width(), char_blobs[idx1].bbx.height());
            var w1 = Math.Min(char_blobs[idx1].bbx.width(), char_blobs[idx1].bbx.height());

            var h2 = Math.Max(char_blobs[idx2].bbx.width(), char_blobs[idx2].bbx.height());
            var w2 = Math.Min(char_blobs[idx2].bbx.width(), char_blobs[idx2].bbx.height());

            var hi = Math.Max(char_blobs[i].bbx.width(), char_blobs[i].bbx.height());
            var wi = Math.Min(char_blobs[i].bbx.width(), char_blobs[i].bbx.height());

            var p1 = new PointF(w1 / 2, h1 / 2);
            var p2 = new PointF(w2 / 2 + w1 + wi, h2 / 2);
            var p3 = new PointF(wi / 2 + w1, hi / 2);


            return CosAngel(p1, p2, p3);
        }

        public double CosAngel(PointF p1, PointF p2, PointF p3)
        {
            var error = 0.00001;
            double x1 = p1.X;
            double x2 = p2.X;
            double x3 = p3.X;
            double y1 = p1.Y;
            double y2 = p2.Y;
            double y3 = p3.Y;
            if (x1 == x3 && x2 == x3)
                return 180;
            if (y1 == y3 && y2 == y3)
                return 180;
            var adotb = (x2 - x3) * (x1 - x3) + (y2 - y3) * (y1 - y3);
            var tmp1 = Math.Sqrt((x2 - x3) * (x2 - x3) + (y2 - y3) * (y2 - y3)) *
                       Math.Sqrt((x1 - x3) * (x1 - x3) + (y1 - y3) * (y1 - y3));
            var tmp2 = adotb / tmp1;
            double angel = 0;
            if (Math.Abs(Math.Abs(tmp2) - 1) < error)
            {
                angel = 180;
            }
            else
            {
                angel = Math.Acos(adotb / tmp1);
                angel = angel * 180 / Math.PI;
            }
            return angel;
        }

        //////   I want to add a function that remove small shapes that we are not interested in having them //////

        private Bitmap RemoveNoiseFromImage(Bitmap srcImg, int MyX, int MyY, int MyWidth, int MyHeight)
        {
            var Mybitmap = new Bitmap(srcImg);

            for (var i = MyX; i < MyX + MyWidth; i++)
            for (var j = MyHeight; j < MyY + MyHeight; j++)
                Mybitmap.SetPixel(i, j, Color.Black);
            /*
            BitmapData srcData = srcImg.LockBits(
                  new Rectangle(MyX, MyY, MyWidth, MyHeight),
                  ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);
            int srcStride = srcData.Stride;
            int srcOffset = srcStride - width;
            int BlobSize = MyWidth * MyHeight;

            unsafe
            {
                byte* src = (byte*)srcData.Scan0.ToPointer();
                for (int i = 0; i < BlobSize; i++)
                {
                    if (*src == 255)
                    {
                        *src = 0;
                        src++;
                    }
                }
            }
            srcImg.UnlockBits(srcData);
             **/
            return Mybitmap;
        }
    }
}