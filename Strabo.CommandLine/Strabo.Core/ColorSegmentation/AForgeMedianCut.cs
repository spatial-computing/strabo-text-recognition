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

using System.Drawing;
using System.IO;
using AForge.Imaging.ColorReduction;

namespace Strabo.Core.ColorSegmentation
{
    public class AForgeMedianCut : IMedianCut
    {
        public bool Process(string srcpathfn, string dstpathfn, int color_number)
        {
            using (var msimg = new Bitmap(srcpathfn))
            {
                var ciq = new ColorImageQuantizer(new MedianCutQuantizer());
                ciq.ReduceColors(msimg, color_number).Save(dstpathfn);
            }
            return File.Exists(dstpathfn);
        }
    }
}