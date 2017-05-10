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
using System.Linq;
using System.Text;

namespace Strabo.Core.TextRecognition
{
    public class TessResult
    {
        public TessResult()
        {

        }
        public TessResult(string id, string tess_word3, string tess_raw3,
                            double tess_cost3, string hocr, string fileName,
                            int x, int y, int w, int h)
        {
            this.id = id;
            this.tess_word3 = tess_word3;
            this.tess_raw3 = tess_raw3;
            this.tess_cost3 = tess_cost3;
            this.hocr = hocr;
            this.fileName = fileName;
            this.x = x;
            this.y = y;
            this.h = h;
            this.w = w;
        }

        public string id;
        public double dict_similarity;

        public string tess_raw3;
        public string tess_word3;
        public string dict_word3;
        public double tess_cost3;
        public string hocr;

        public string tess_word;
        public string dict_word;
        public double tess_cost;

        public string fileName;
        public int x;
        public int y;
        public int x2;
        public int y2;
        public int x3;
        public int y3;
        public int x4;
        public int y4;

        public int w;
        public int h;

        public int mcX;
        public int mcY;
        public string sameMatches;

        public bool front = false;
        public bool back = false;
    }
}

