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
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Threading;
using AForge.Imaging.Filters;
using Strabo.Core.BoundingBox;
using Strabo.Core.ImageProcessing;
using Strabo.Core.MachineLearning;
using Strabo.Core.TextDetection;
using Strabo.Core.Utility;

namespace Strabo.Core.Worker
{
    public class TextDetectionWorker : IStraboWorker
    {
        private readonly List<TextString> all_string_list = new List<TextString>();
        private List<string> imgPathfnList;
        private readonly ImageSlicer imgSlicer = new ImageSlicer();
        private string interDir;
        private int threadNumber;

        public string Apply(string inputDir, string interDir, string outputDir, string srcfileName, int threadNumber)
        {
            this.interDir = interDir;
            this.threadNumber = threadNumber;
            imgPathfnList = imgSlicer.Apply(
                StraboParameters.rowSlice, StraboParameters.colSlice,
                StraboParameters.overlap, Path.Combine(inputDir, srcfileName), interDir
            );
            Log.write_console_only = true;
            var thread_array = new Thread[imgPathfnList.Count];
            for (var i = 0; i < imgPathfnList.Count; i++)
            {
                thread_array[i] = new Thread(detect);
                thread_array[i].Start(i);
            }
            for (var i = 0; i < imgPathfnList.Count; i++)
                thread_array[i].Join();
            Log.write_console_only = false;
            for (var i = 0; i < all_string_list.Count; i++)
            {
                var bbx = all_string_list[i].bbx;
                var points = bbx.vertices();
                for (var p = 0; p < points.Length; p++)
                {
                    points[p].X = points[p].X + all_string_list[i].x_offset;
                    points[p].Y = points[p].Y + all_string_list[i].y_offset;
                }

                all_string_list[i].bbx = new MinimumBoundingBox(points);
            }

            // prepare images for string detection on the entire image
            // draw minimum bounding boxes
            var srcimg2 = new Bitmap(Path.Combine(inputDir, srcfileName));
            var polygon = new Bitmap(srcimg2.Width, srcimg2.Height);
            var g = Graphics.FromImage(polygon);
            g.FillRectangle(Brushes.White, 0, 0, polygon.Width, polygon.Height);
            foreach (var t in all_string_list)
                g.FillPolygon(new SolidBrush(Color.Black), t.bbx.vertices(), FillMode.Winding);
            g.Dispose();
            g = null;
            //Log.WriteBitmap2FolderExactFileName(@"C:\Users\yaoyi\Documents\strabo-text-recognition\Strabo.CommandLine\data\intermediate\", polygon, "srcimg0.png");
            polygon = ImageUtils.ConvertToGrayScale(polygon);
            //Log.WriteBitmap2FolderExactFileName(@"C:\Users\yaoyi\Documents\strabo-text-recognition\Strabo.CommandLine\data\intermediate\", polygon, "srcimg1.png");
            // create filter
            var filter = new Erosion();
            // apply the filter
            polygon = filter.Apply(polygon);
            polygon = ImageUtils.ConvertGrayScaleToBinary(polygon, 128);

            // remove noise from srcimg for fast cc detection
            srcimg2 = ImageUtils.ConvertGrayScaleToBinary(srcimg2, 128);
            var filter2 = new Add(srcimg2);
            srcimg2 = filter2.Apply(polygon);

            //Log.WriteBitmap2FolderExactFileName(@"C:\Users\yaoyi\Documents\strabo-text-recognition\Strabo.CommandLine\data\intermediate\", srcimg2, "srcimg_polygon.png");
            //Log.WriteBitmap2FolderExactFileName(@"C:\Users\yaoyi\Documents\strabo-text-recognition\Strabo.CommandLine\data\intermediate\", polygon, "srcimg.png");

            var detectTS = new DetectTextStrings();
            var string_list2 = detectTS.Apply(srcimg2, polygon);

            //debug
            //using (Bitmap CDAInputwithLabel = ImageUtils.AnyToFormat24bppRgb(srcimg2))
            //{
            //    Graphics g2 = Graphics.FromImage(CDAInputwithLabel);
            //    for (int i = 0; i < string_list2.Count; i++)
            //    {
            //        Font font = new Font("Arial", 20);
            //        g2.DrawString(i.ToString(), font, Brushes.Red, string_list2[i].bbx.firstVertex().X, string_list2[i].bbx.firstVertex().Y);
            //        g2.DrawPolygon(new Pen(Color.Green, 4), string_list2[i].bbx.vertices());
            //    }
            //    Log.WriteBitmap2FolderExactFileName(interDir, CDAInputwithLabel, "CDAInputwithLabel.png");
            //    g2.Dispose();
            //    g2 = null;
            //}

            srcimg2.Dispose();
            polygon.Dispose();
            srcimg2 = null;
            polygon = null;

            // draw minimum bounding box results for debuggin
            //using (Bitmap CDAInputwithLabel = ImageUtils.AnyToFormat24bppRgb(new Bitmap(Path.Combine(inputDir, srcfileName))))
            //{
            //    Graphics g = Graphics.FromImage(CDAInputwithLabel);
            //    for (int i = 0; i < mts.textStringList.Count; i++)
            //    {
            //        Font font = new Font("Arial", 20);
            //        g.DrawString(i.ToString(), font, Brushes.Red, mts.textStringList[i].bbx.firstVertex().X, mts.textStringList[i].bbx.firstVertex().Y);
            //        g.DrawPolygon(new Pen(Color.Green, 4), mts.textStringList[i].bbx.vertices());
            //    }
            //    Log.WriteBitmap2FolderExactFileName(interDir, CDAInputwithLabel, "CDAInputwithLabel.png");
            //    g.Dispose();
            //    g = null;
            //}
            //mts.textStringList.AddRange(string_list2);
            Log.WriteLine("Detecting long string orientation...");
            var detectTextOrientation = new DetectTextOrientation();
            detectTextOrientation.Apply(string_list2, threadNumber);

            Log.WriteLine("Writing string results...");
            WriteBMP(string_list2, interDir);
            return interDir;
        }

        public void WriteBMP(List<TextString> text_string_list, string output_path)
        {
            for (var i = 0; i < text_string_list.Count; i++)
            {
                if (text_string_list[i].char_list.Count == 1)
                    continue;

                var massCenterX = (int) text_string_list[i].bbx.massCenter().X;
                var massCenterY = (int) text_string_list[i].bbx.massCenter().Y;

                for (var s = 0; s < text_string_list[i].orientation_list.Count; s++)
                {
                    var slope = Convert.ToDouble(text_string_list[i].orientation_list[s]).ToString();
                    if (slope == "360") // rotated 360 degress is 0 degree...
                        continue;

                    var vertices = "";
                    foreach (var vertex in text_string_list[i].bbx.vertices())
                        vertices += vertex.X + "_" + vertex.Y + "_";

                    var imgstitcher1 = new ImageStitcher();
                    var bx = text_string_list[i].bbx;
                    using (var single_img = imgstitcher1.ExpandCanvas(text_string_list[i].rotated_img_list[s], 20))
                    {
                        var fn = i + "_p_" + text_string_list[i].char_list.Count
                                 + "_" + massCenterX + "_" + massCenterY + "_s_" + slope
                                 + "_" + vertices + bx.width() + "_" + bx.height()
                                 + ".png";
                        single_img.Save(Path.Combine(output_path, fn));
                    }
                }

                var vx = "";
                foreach (var vertex in text_string_list[i].bbx.vertices())
                    vx += vertex.X + "_" + vertex.Y + "_";

                var mbr = text_string_list[i].bbx;
                //write original orientation
                var imgstitcher2 = new ImageStitcher();
                using (var srcimg = imgstitcher2.ExpandCanvas(text_string_list[i].srcimg, 20))
                {
                    var fn = i + "_p_" + text_string_list[i].char_list.Count
                             + "_" + massCenterX + "_" + massCenterY + "_s_0" + "_"
                             + vx + mbr.width() + "_" + mbr.height() + ".png";
                    srcimg.Save(Path.Combine(output_path, fn));
                }
            }
        }

        private void detect(object k)
        {
            var s = (int) k;
            //for (int s = 0; s < imgPathfnList.Count; s++)
            {
                Bitmap srcimg = null;
                var srcpathfn = "";
                var intermediate_result_pathfn =
                    Path.Combine(interDir, "CDAInput_RemoveBoarderAndNoiseCC_" + s + ".png");

                using (var tileimg = new Bitmap(imgPathfnList[s]))
                {
                    Log.WriteLine("Noise cleaning in progress..." + s);
                    var removeBoarderAndNoiseCC =
                        new RemoveBoarderAndNoiseCC();
                    srcimg = removeBoarderAndNoiseCC.Apply(
                        tileimg, StraboParameters.char_size, StraboParameters.minPixelAreaSize
                    );
                    srcimg.Save(intermediate_result_pathfn);
                    Log.WriteLine("Noise cleaning finished " + s);
                }

                if (StraboParameters.CleanWithSVM != "")
                {
                    try
                    {

                        Log.WriteLine("ConnectedComponentClassifier in progress..." + s);
                        //Copy "CDAInput.png" as "CDAInput_original.png"
                        srcpathfn = intermediate_result_pathfn;
                        intermediate_result_pathfn = Path.Combine(interDir,
                            Path.GetFileNameWithoutExtension(intermediate_result_pathfn) + "_CCSVM.png");

                        ConnectedComponentClassifier _connectedComponentClassifyWorker =
                            new ConnectedComponentClassifier();
                        _connectedComponentClassifyWorker.Apply(interDir, Path.GetFileName(srcpathfn),
                            Path.GetFileName(intermediate_result_pathfn), false);
                        Log.WriteLine("ApplyConnectedComponentClassifyWorker finished " + s);
                    }
                    catch (Exception e)
                    {
                        Log.WriteLine("ApplyConnectedComponentClassifyWorker: " + e.Message);
                        throw;
                    }
                }
                Log.WriteLine("Noise cleaning finished");

                try
                {
                    Log.WriteLine("CDA in progress...");
                    srcimg = new Bitmap(intermediate_result_pathfn); // prepare CDA input 
                    intermediate_result_pathfn = Path.Combine(interDir,
                        Path.GetFileNameWithoutExtension(intermediate_result_pathfn) + "_CDA.png");
                    var cda = new ConditionalDilationAutomatic();
                    cda.Apply(threadNumber, srcimg, StraboParameters.cdaSizeRatio, StraboParameters.cdaAngleRatio,
                        StraboParameters.cdaPreProcessing, intermediate_result_pathfn);
                    Log.WriteLine("CDA finished");
                }
                catch (Exception e)
                {
                    Log.WriteLine("CDA: " + e.Message);
                    throw;
                }

                // detect string pixels using CDA output
                using (var dilatedimg = new Bitmap(intermediate_result_pathfn))
                {
                    Log.WriteLine("Minimum bounding box detection in progress...");
                    var preSVMimg = new Bitmap(imgPathfnList[s]);
                    var detectMS = new DetectMinimumStrings();
                    var string_list = detectMS.Apply(preSVMimg, srcimg, dilatedimg); // src, cleaned, cda

                    for (var i = 0; i < string_list.Count; i++)
                    {
                        string_list[i].x_offset = imgSlicer.xy_offset_list[s][0];
                        string_list[i].y_offset = imgSlicer.xy_offset_list[s][1];
                    }

                    all_string_list.AddRange(string_list);
                    Log.WriteLine("Minimum bounding box detection finished");
                }
            }
        }

        #region debuging_code

        //public Bitmap PrintSubStringBBXonMap(Bitmap srcimg)
        //{
        //    //changed here - ASHISH

        //    srcimg = ImageUtils.InvertColors(srcimg);

        //    /*
        //    Invert ivt = new Invert();
        //    srcimg = ivt.Apply(srcimg);
        //    */

        //    srcimg = ImageUtils.toRGB(srcimg);
        //    Graphics g = Graphics.FromImage(srcimg);
        //    for (int i = 1; i < initial_string_list.Count; i++)
        //    {
        //        g.DrawRectangle(new Pen(Color.Red, 6), initial_string_list[i].bbx);
        //        //Font font = new Font("Arial", 20);
        //        //g.DrawString(i.ToString(), font, Brushes.Blue, initial_string_list[i].bbx.X, initial_string_list[i].bbx.Y);
        //        for (int j = 0; j < initial_string_list[i].final_string_list.Count; j++)
        //            g.DrawRectangle(new Pen(Color.Green, 4), initial_string_list[i].final_string_list[j].bbx);

        //        //for (int j = 0; j < initial_string_list[i].char_list.Count; j++)
        //        //{
        //        //    Font font2 = new Font("Arial", 20);
        //        //    g.DrawString(j.ToString(), font2, Brushes.Red, initial_string_list[i].char_list[j].bbx.X, initial_string_list[i].char_list[j].bbx.Y);
        //        //   // g.DrawRectangle(new Pen(Color.Yellow, 2), initial_string_list[i].char_list[j].bbx);
        //        //}

        //    }
        //    g.Dispose();
        //    return srcimg;
        //}
        //public Bitmap PrintSubStringNumonMap(Bitmap srcimg)
        //{
        //    //ASHISH
        //    srcimg = ImageUtils.InvertColors(srcimg);
        //    /*
        //    Invert ivt = new Invert();
        //    srcimg = ivt.Apply(srcimg);
        //    */
        //    srcimg = ImageUtils.toRGB(srcimg);
        //    Graphics g = Graphics.FromImage(srcimg);
        //    for (int i = 1; i < initial_string_list.Count; i++)
        //    {
        //        for (int j = 0; j < initial_string_list[i].char_list.Count; j++)
        //        {
        //            Font font2 = new Font("Arial", 20);
        //            g.DrawString(j.ToString(), font2, Brushes.Red, initial_string_list[i].char_list[j].bbx.X, initial_string_list[i].char_list[j].bbx.Y);
        //            // g.DrawRectangle(new Pen(Color.Yellow, 2), initial_string_list[i].char_list[j].bbx);
        //        }
        //    }
        //    g.Dispose();
        //    return srcimg;
        //}
        //public Bitmap PrintSubStringsSmall(TextString ts)
        //{
        //    bool[,] stringimg = new bool[ts.bbx.Height + 100, ts.bbx.Width + 100];
        //    for (int i = 0; i < ts.char_list.Count; i++)
        //    {
        //        for (int xx = ts.bbx.X; xx < ts.bbx.X + ts.bbx.Width; xx++)
        //            for (int yy = ts.bbx.Y; yy < ts.bbx.Y + ts.bbx.Height; yy++)
        //            {
        //                if (char_labels[yy * mw + xx] == ts.char_list[i].pixel_id)
        //                    stringimg[yy - ts.bbx.Y + 50, xx - ts.bbx.X + 50] = true;
        //                //else
        //                //   stringimg[yy - ts.bbx.Y, xx - ts.bbx.X] = false;
        //            }

        //    }
        //    ts.srcimg = ImageUtils.ArrayBool2DToBitmap(stringimg); ;
        //    return ts.srcimg;
        //}
        //public Bitmap PrintSubStringsSmall(TextString ts, int margin)
        //{
        //    bool[,] stringimg = new bool[ts.bbx.Height + margin, ts.bbx.Width + margin];
        //    for (int i = 0; i < ts.char_list.Count; i++)
        //    {
        //        for (int xx = ts.bbx.X; xx < ts.bbx.X + ts.bbx.Width; xx++)
        //            for (int yy = ts.bbx.Y; yy < ts.bbx.Y + ts.bbx.Height; yy++)
        //            {
        //                if (char_labels[yy * mw + xx] == ts.char_list[i].pixel_id)
        //                    stringimg[yy - ts.bbx.Y + margin / 2, xx - ts.bbx.X + margin / 2] = true;
        //                //else
        //                //   stringimg[yy - ts.bbx.Y, xx - ts.bbx.X] = false;
        //            }

        //    }
        //    ts.srcimg = ImageUtils.ArrayBool2DToBitmap(stringimg); ;
        //    return ts.srcimg;
        //}

        #endregion debuging_code
    }
}