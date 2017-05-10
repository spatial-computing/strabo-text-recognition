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

using AForge.Imaging.Filters;
using Strabo.Core.ImageProcessing;
using Strabo.Core.TextDetection;
using Strabo.Core.Utility;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;

namespace Strabo.Core.Worker
{
    /// <summary>
    /// Sima 
    /// Starting class for text recognition
    /// </summary>
    public class TextDetectionWorker
    {
        public double angleRatio;

        public TextDetectionWorker()
        {
            this.angleRatio = StraboParameters.angleRatio;
        }

        public void Apply(string intermediatePath, double sizeRatio, bool preProcessing, int threadNumber)
        {
            string inputDir = intermediatePath;
            string outputDir = intermediatePath;
            string fileName = StraboParameters.textLayerOutputFileName;
            int charSize = StraboParameters.char_size;

            Apply(
                inputDir, outputDir, fileName, charSize, sizeRatio,
                preProcessing, threadNumber, StraboParameters.language
                );
        }

        public void Apply(
            string inputDir, string outputDir, string fileName, int charSize,
            double sizeRatio, bool preProcessing, int threadNumber, string lang
            )
        {
            MergeTextStrings mts = new MergeTextStrings();

            ImageSlicer imgSlicer = new ImageSlicer();
            // List<TextString> text_string_list = new List<TextString>();
            List<string> imgPathList = imgSlicer.Apply(
                StraboParameters.rowSlice, StraboParameters.colSlice,
                StraboParameters.overlap, inputDir + fileName, outputDir
                );

            Log.WriteLine("Grouping text strings...");
            for (int s = 0; s < imgPathList.Count; s++)
            {
                string cdaFileName = "";
                Bitmap srcimg = null;

                if (lang == "eng")
                {
                    using (Bitmap tileimg = new Bitmap(imgPathList[s]))
                    {
                        RemoveBoarderAndNoiseCC removeBoarderAndNoiseCC =
                            new RemoveBoarderAndNoiseCC();
                        srcimg = removeBoarderAndNoiseCC.Apply(
                            tileimg, charSize, StraboParameters.minPixelAreaSize
                            );
                        Log.WriteBitmap2FolderExactFileName(outputDir, srcimg, "CDAInput_" + s + ".png");
                    }
                }
                else if(lang == "num")
                {
                    using (Bitmap tileimg = new Bitmap(imgPathList[s]))
                    {
                        RemoveBoarderAndNoiseCC removeBoarderAndNoiseCC = 
                            new RemoveBoarderAndNoiseCC();
                        srcimg = removeBoarderAndNoiseCC.Apply(
                            tileimg, charSize, StraboParameters.minPixelAreaSize
                            );
                        Log.WriteBitmap2FolderExactFileName(outputDir, srcimg, "CDAInput_" + s + ".png");
                    }
                }
                else
                {
                    using (Bitmap tileimg = new Bitmap(imgPathList[s]))
                    {
                        RemoveBoarderCC removeBoarderCC = new RemoveBoarderCC();
                        srcimg = removeBoarderCC.Apply(tileimg);
                        cdaFileName = "CDAInput_" + s + ".png";
                        Log.WriteBitmap2FolderExactFileName(outputDir, srcimg, "CDAInput_" + s + ".png");
                    }
                }

                //add the ConnectedComponentClassifyWorker here
                try
                {
                    ConnectedComponentClassifyWorker _connectedComponentClassifyWorker = new ConnectedComponentClassifyWorker();
                    Log.WriteLine("ConnectedComponentClassifyWorker in progress...");
                    //Copy "CDAInput.png" as "CDAInput_original.png"
                    File.Copy(outputDir + "CDAInput_" + s + ".png", outputDir + "CDAInput_original_" + s + ".png");
                    //Use "CDAInput_original.png" as input, classify the CCs and output as "CDAInput.png"
                    _connectedComponentClassifyWorker.Apply(outputDir, "CDAInput_original_" + s + ".png", "CDAInput_" + s + ".png", false);
                    Log.WriteLine("ApplyConnectedComponentClassifyWorker finished");
                }
                catch (Exception e)
                {
                    Log.WriteLine("ApplyConnectedComponentClassifyWorker: " + e.Message);
                    throw;
                }

                //update srcimg
                srcimg = new Bitmap(outputDir + "CDAInput_" + s + ".png");

                ConditionalDilationAutomatic cda = new ConditionalDilationAutomatic();
                cda.angleThreshold = angleRatio;
                string outputImagePath = outputDir + s + ".png";
                cda.Apply(threadNumber, srcimg, sizeRatio, angleRatio, preProcessing, outputImagePath);

                using (Bitmap dilatedimg = new Bitmap(outputImagePath))
                {

                    DetectTextStrings detectTS = new DetectTextStrings();
                    List<TextString> string_list = detectTS.Apply(srcimg, dilatedimg);

                    List<Bitmap> string_img_list = new List<Bitmap>();
                    for (int i = 0; i < string_list.Count; i++)
                        string_img_list.Add(string_list[i].srcimg);
                    int[] offset = imgSlicer.xy_offset_list[s];

                    for (int i = 0; i < string_list.Count; i++)
                    {
                        string_list[i].x_offset = offset[0];
                        string_list[i].y_offset = offset[1];
                        mts.AddTextString(string_list[i]);
                    }
                }

                using (Bitmap CDAInputwithLabel=new Bitmap (outputImagePath))
                {
                    Graphics g = Graphics.FromImage(CDAInputwithLabel);
                    for (int i = 0; i < mts.textStringList.Count; i++)
                    {
                        Font font = new Font("Arial", 20);
                        g.DrawString(i.ToString(), font, Brushes.Red, mts.textStringList[i].bbx.firstVertex().X, mts.textStringList[i].bbx.firstVertex().Y);

                        // Rectangle boundingRect = new Rectangle((int)mts.text_string_list[i].bbx.firstVertex().X,
                        //   (int)mts.text_string_list[i].bbx.firstVertex().Y, (int)mts.text_string_list[i].bbx.width(), (int)mts.text_string_list[i].bbx.height());

                        g.DrawPolygon(new Pen(Color.Green, 4), mts.textStringList[i].bbx.vertices());
                    }
                    Log.WriteBitmap2FolderExactFileName(outputDir, CDAInputwithLabel, "CDAInputwithLabel_" + s + ".png");
                    g.Dispose();
                    g=null;
                }
                srcimg.Dispose();
                srcimg = null;
            }

            //ImageStitcher imgstitcher1 = new ImageStitcher();
            //foreach (TextString ts in mts.textStringList)
            //{
            //    if(ts.char_list.Count > 2)
            //    {
            //        string file_name = "temp_" + ts.bbx.massCenter().X + "_" + ts.bbx.massCenter().Y + ".png";
            //        Console.WriteLine(file_name + " | " + "Orientation: " + ts.bbx.orientation() + " Angle: " + ts.bbx.returnCalculatedAngle());

            //        Log.WriteBitmap(ts.srcimg, file_name);
            //        double angle = ts.bbx.returnCalculatedAngle();
            //        RotateBilinear filter = new RotateBilinear(180 - angle, false);
            //        filter.FillColor = Color.White;
            //        RotateBilinear filter2 = new RotateBilinear(0-angle, false);
            //        filter2.FillColor = Color.White;
            //        // apply the filter
            //        Log.WriteBitmap(filter.Apply(ts.srcimg), "rotated_1_" + file_name);
            //        Log.WriteBitmap(filter2.Apply(ts.srcimg),  "rotated_2_" + file_name);
            //        // imgstitcher1.ExpandCanvas(text_string_list[i].rotated_img_list[s], 20)

            //    }
            //}

            Log.WriteLine("Detecting long string orientation...");
            DetectTextOrientation detectTextOrientation = new DetectTextOrientation();
            detectTextOrientation.Apply(mts, threadNumber);

            //Log.WriteLine("Detecting short string orientation...");

            //for (int i = 0; i < mts.textStringList.Count; i++)
            //{
            //    if (mts.textStringList[i].char_list.Count <= 3)
            //    {
            //        List<int> nearest_string_list = detectTextOrientation.findNearestSrings(i, mts.textStringList);

            //        int initial_orientation_count = mts.textStringList[i].orientation_list.Count;
            //        for (int j = 0; j < nearest_string_list.Count; j++)
            //            mts.textStringList[i].orientation_list.AddRange(mts.textStringList[nearest_string_list[j]].orientation_list);
            //        for (int j = initial_orientation_count; j < mts.textStringList[i].orientation_list.Count; j++)
            //        {
            //            RotateBilinear filter = new RotateBilinear(mts.textStringList[i].orientation_list[j]);
            //            Bitmap rotateimg = ImageUtils.InvertColors(filter.Apply(ImageUtils.InvertColors(mts.textStringList[i].srcimg)));
            //            mts.textStringList[i].rotated_img_list.Add(rotateimg);
            //        }
            //    }
            //}
            Log.WriteLine("Writing string results...");
            mts.WriteBMP(outputDir);
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
