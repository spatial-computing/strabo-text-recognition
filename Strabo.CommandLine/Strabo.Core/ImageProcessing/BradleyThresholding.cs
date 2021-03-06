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

using AForge.Imaging.Filters;
using Emgu.CV;
using Emgu.CV.Structure;

namespace Strabo.Core.ImageProcessing
{
    public class BradleyThresholding : IAutomaticBinarization
    {
        public bool Process(string srcpathfn, string dstpathfn)
        {
            var bradley = new BradleyLocalThresholding();
            var img = new Image<Gray, byte>(srcpathfn);
            var dstimg = bradley.Apply(img.Bitmap);
            dstimg.Save(dstpathfn);
            dstimg.Dispose();
            dstimg = null;
            img.Dispose();
            img = null;
            return true;
        }
    }
}