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

using Strabo.Core.ColorSegmentation;
using Strabo.Core.Utility;
using System;
using System.IO;

namespace Strabo.Core.Worker
{
    /// <summary>
    /// Starting class for color segmentation
    /// </summary>
    public class ColorSegmentationWorker : IStraboWorker
    {
        public ColorSegmentationWorker() { }
        public string Apply(string inputDir, string interDir, string outputDir, string srcfileName, int threadNumber)
        {
            string srcpath = Path.Combine(inputDir, srcfileName);
            string dstpath = Path.Combine(interDir, Path.GetFileNameWithoutExtension(srcfileName) + "_");
            return Apply(srcpath, dstpath, threadNumber);
        }
        /// <summary>
        /// Applies <c>meanshift</c> and <c>median cut</c> algorithms
        /// for color quantization
        /// </summary>
        /// <param name="srcpathfn">Input image location</param>
        /// <param name="dstpathfn">Output image location</param>
        /// <param name="threadNumber">Number of threads</param>
        /// <param name="mc">Median Cut implimentation</param>
        /// <returns>Path of the Quantized image</returns>
        public string Apply(string srcpathfn, string dstpathfn, int threadNumber)
        {
            try
            {
                //MeanShift
                Log.WriteLine("Meanshift in progress...");
                dstpathfn = Path.Combine(Path.GetDirectoryName(dstpathfn), Path.GetFileNameWithoutExtension(dstpathfn) + "ms.png");
                MeanShiftMultiThreads mt = new MeanShiftMultiThreads();
                mt.ApplyYIQMT(srcpathfn, threadNumber,
                    StraboParameters.spatialDistance, StraboParameters.colorDistance, dstpathfn);
                mt = null;
                GC.Collect();
                Log.WriteLine("Meanshift finished");
               
                // Median Cut
                Log.WriteLine("Median-Cut in progress...");
                srcpathfn = dstpathfn;
                dstpathfn = Path.Combine(Path.GetDirectoryName(dstpathfn), Path.GetFileNameWithoutExtension(dstpathfn) + "mc.png");
                IMedianCut mc = new MedianCutMultiThreads(); //AForgeMedianCut();
                mc.Process(srcpathfn, dstpathfn, StraboParameters.medianCutColors);
                Log.WriteLine("Median-Cut finished");
                return dstpathfn;
            }
            catch (Exception e)
            {
                Log.WriteLine("ColorSegmentationWorker.Apply: " + e.Message);
                Log.WriteLine(e.Source);
                Log.WriteLine(e.StackTrace);
                throw;
            }
        }
    }
}
