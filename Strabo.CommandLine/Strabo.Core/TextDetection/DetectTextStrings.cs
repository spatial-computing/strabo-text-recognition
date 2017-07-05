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

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Strabo.Core.ImageProcessing;
using Strabo.Core.Utility;

namespace Strabo.Core.TextDetection
{
    /// <summary>
    ///     Sima
    ///     Narges: I have changed this class
    /// </summary>
    public class DetectTextStrings
    {
        private readonly int max_height = StraboParameters.bbxMaxHeight;
        private readonly int max_width = StraboParameters.bbxMaxWidth;
        private readonly int min_height = StraboParameters.bbxMinHeight;
        private readonly int min_width = StraboParameters.bbxMinWidth;
        private int width, height;

        public List<TextString> Apply(Bitmap srcimg, Bitmap dilatedimg)
        {
            width = srcimg.Width;
            height = srcimg.Height;

            // Get string labels of the source image which is inverted
            var char_bc = new MyConnectedComponentsAnalysisFGFast.MyBlobCounter();
            var char_blobs = char_bc.GetBlobs(srcimg, 0);
            var char_labels = char_bc.objectLabels;

            // Get string labels of the dilated image
            var string_bc = new MyConnectedComponentsAnalysisFGFast.MyBlobCounter();
            var string_blobs = string_bc.GetBlobs(dilatedimg, 0);
            var string_labels = string_bc.objectLabels;

            var initial_string_list = new List<TextString>();
            var dict = new HashSet<ushort>();

            for (var x = 0; x < width; x++)
            for (var y = 0; y < height; y++)
                dict.Add(char_labels[y * width + x]);
            //Console.WriteLine(char_labels[y * width + x]);

            // Dilated Image
            for (var i = 0; i < string_blobs.Count; i++)
            {
                initial_string_list.Add(new TextString());
                initial_string_list.Last().mass_center = string_blobs[i].bbx.massCenter();
            }

            // Inverted Source Image 
            for (var i = 0; i < char_blobs.Count; i++)
                if (char_blobs[i].bbx.width() > 1 && char_blobs[i].bbx.height() > 1)
                {
                    char_blobs[i].string_id = string_labels[char_blobs[i].sample_y * width + char_blobs[i].sample_x] -
                                              1;
                    if (char_blobs[i].string_id > -1)
                        initial_string_list[char_blobs[i].string_id].AddChar(char_blobs[i]);
                }

            for (var i = 0; i < initial_string_list.Count; i++)
                if (
                    initial_string_list[i].char_list.Count == 0 || initial_string_list[i].bbx.width() < min_width ||
                    initial_string_list[i].bbx.height() < min_height ||
                    initial_string_list[i].bbx.width() > max_width || initial_string_list[i].bbx.height() > max_height
                )
                {
                    initial_string_list.RemoveAt(i);
                    i--;
                }
            foreach (var t in initial_string_list)
                PrintSubStringsSmall(char_labels, t, 0);
            return initial_string_list;
        }

        // We use maximum bounding box to get all the labels for each pixel
        // Even though we use minimum bounding box the labelling process still scans horizontally 
        // row by row
        // TODO: Fix the labelling algorithm
        public void PrintSubStringsSmall(ushort[] char_labels, TextString ts, int margin)
        {
            var stringimg = new bool[ts.maxBbx.Height + margin,
                ts.maxBbx.Width + margin
            ];

            for (var i = 0; i < ts.char_list.Count; i++)
            for (var xx = ts.maxBbx.X; xx < ts.maxBbx.X + ts.maxBbx.Width; xx++)
            for (var yy = ts.maxBbx.Y; yy < ts.maxBbx.Y + ts.maxBbx.Height; yy++)
                if (char_labels[yy * width + xx] == ts.char_list[i].pixel_id)
                    stringimg[yy - ts.maxBbx.Y + margin / 2, xx - ts.maxBbx.X + margin / 2] = true;

            if (ts.char_list.Count > 0)
                ts.srcimg = ImageUtils.ArrayBool2DToBitmap(stringimg);
        }
    }
}