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
using System.IO;
using Strabo.Core.ImageProcessing;
using Strabo.Core.Utility;

namespace Strabo.Core.Worker
{
    public class TextExtractionWorker : IStraboWorker
    {
        public string Apply(string inputDir, string interDir, string outputDir, string srcfileName, int threadNumber)
        {
            //Get user parameters
            var threshold = StraboParameters.rgbThreshold;
            return Apply(Path.Combine(inputDir, srcfileName),
                Path.Combine(interDir, Path.GetFileNameWithoutExtension(srcfileName) + "_"), threshold, threadNumber);
        }

        /// <param name="srcpathfn">Source Image path</param>
        /// <param name="dstpathfn">Output Image path</param>
        /// <param name="intermediatePath">Location containing different versions of the image</param>
        /// <param name="threshold">RGB threshold value</param>
        /// <param name="threadNumber">Number of threads</param>
        public string Apply(string srcpathfn, string dstpathfn, RGBThreshold threshold, int threadNumber)
        {
            try
            {
                dstpathfn = Path.Combine(Path.GetDirectoryName(dstpathfn),
                    Path.GetFileNameWithoutExtension(dstpathfn) + "te.png");
                if (threshold.useAutomaticThresholding)
                {
                    Log.WriteLine("Bradley local thresholding in progress");
                    var bt = new BradleyThresholding();
                    bt.Process(srcpathfn, dstpathfn);
                    Log.WriteLine("Bradley local thresholding finished");
                }
                else
                {
                    Log.WriteLine("RGB thresholding in progress");
                    RGBColorThresholding.ApplyRGBColorThresholding(
                        srcpathfn, dstpathfn,
                        threshold, threadNumber
                    );
                    Log.WriteLine("RGB thresholding finished");
                }
                return dstpathfn;
            }
            catch (Exception e)
            {
                Log.WriteLine("TextExtractionWorker:" + e.Message);
                Log.WriteLine(e.Source);
                Log.WriteLine(e.StackTrace);
                throw;
            }
        }
    }
}