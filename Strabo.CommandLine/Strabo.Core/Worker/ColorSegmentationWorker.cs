﻿/*******************************************************************************
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

using nQuant;
using Strabo.Core.ColorSegmentation;
using Strabo.Core.Utility;
using Strabo.Core.ImageProcessing;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace Strabo.Core.Worker
{
    /// <summary>
    /// Starting class for color segmentation
    /// </summary>
    public class ColorSegmentationWorker
    {
        public ColorSegmentationWorker() { }
        public string Apply(string intermediatePath, int threadNumber)
        {
            string inputDir = intermediatePath;
            string outputDir = intermediatePath;
            string fileName = StraboParameters.sourceMapFileName;
            // 'k' value in the k-means algorithm
            int k = StraboParameters.numberOfSegmentationColor;
            return Apply(inputDir, outputDir, fileName, k, threadNumber);
        }

        /// <summary>
        /// Applies <c>meanshift</c> and <c>median cut</c> algorithms
        /// for color quantization
        /// </summary>
        /// <param name="inputDir">Input image location</param>
        /// <param name="outputDir">Output image location</param>
        /// <param name="fileName">Input filename</param>
        /// <param name="k">k in the k-keans algorithm</param>
        /// <param name="threadNumber">Number of threads</param>
        /// <returns>Path of the Quantized image</returns>
        public string Apply(string inputDir, string outputDir, string fileName,
            int k, int threadNumber)
        {
            try
            {
                string fileNameWithoutPath = Path.GetFileNameWithoutExtension(fileName);

                //MeanShift
                Log.WriteLine("Meanshift in progress...");
                MeanShiftMultiThreads mt = new MeanShiftMultiThreads();
                string meanShiftPath = outputDir + fileNameWithoutPath + "_ms.png";

                mt.ApplyYIQMT(
                    inputDir + fileName, threadNumber,
                    StraboParameters.spatialDistance, StraboParameters.colorDistance,
                    meanShiftPath);
                mt = null;
                GC.Collect();
                Log.WriteLine("Meanshift finished...");

                // Median Cut
                Log.WriteLine("Median Cut in progress...");
                int medianCutColors = StraboParameters.medianCutColors;
                string medianCutPath = outputDir + fileNameWithoutPath + "_mc" +
                    medianCutColors.ToString() + ".png";
                Bitmap msimg = new Bitmap(
                    outputDir + fileNameWithoutPath + "_ms.png"
                    );
                msimg = ImageUtils.AnyToFormat32bppRgb(msimg);
                using (msimg)
                {
                    WuQuantizer wq = new WuQuantizer();
                    wq.QuantizeImage(msimg).Save(medianCutPath, ImageFormat.Png);
                }
                Log.WriteLine("Median Cut finished");

                // KMeans
                Log.WriteLine("KMeans in progress...");
                MyKMeans kmeans = new MyKMeans();
                string kmeans_path = outputDir + fileNameWithoutPath + "_mc" +
                    medianCutColors + "_k" + k + ".png";
                kmeans.Apply(k, medianCutPath, kmeans_path);
                kmeans = null;
                GC.Collect();
                Log.WriteLine("KMeans finished...");
                return kmeans_path;
            }
            catch (Exception e)
            {
                Log.WriteLine("ColorSegmentationWorker: " + e.Message);
                Log.WriteLine(e.Source);
                Log.WriteLine(e.StackTrace);
                throw;
            }
        }
    }
}
