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
using Strabo.Core.Utility;

namespace Strabo.Core.ImageProcessing
{
    public class RemoveBoarderAndNoiseCC
    {
        public Bitmap Apply(Bitmap srcimg, int char_size, double min_pixel_area_size)
        {
            var char_bc = new MyConnectedComponentsAnalysisFGFast.MyBlobCounter();
            var char_blobs = char_bc.GetBlobs(srcimg, 0);
            var char_labels = char_bc.objectLabels;

            var boarder_char_idx_set = new HashSet<int>();
            var noise_char_idx_set = new HashSet<int>();

            for (var i = 0; i < char_blobs.Count; i++)
            {
                if (char_blobs[i].bbx.returnExtremeStartX() == 0 ||
                    char_blobs[i].bbx.returnExtremeEndX() == srcimg.Width ||
                    char_blobs[i].bbx.returnExtremeStartY() == 0 ||
                    char_blobs[i].bbx.returnExtremeEndY() == srcimg.Height)
                    boarder_char_idx_set.Add(i);

               // Line
                if ((double)char_blobs[i].pixel_count / char_blobs[i].bbx.area() < min_pixel_area_size)
                    noise_char_idx_set.Add(i);
                //Small CC
                if (char_blobs[i].bbx.width() < char_size &&
                    char_blobs[i].bbx.height() < char_size)
                    noise_char_idx_set.Add(i);

                if (char_blobs[i].bbx.width() > char_size * 10 ||
                    char_blobs[i].bbx.height() > char_size * 10)
                    noise_char_idx_set.Add(i);

                if (char_blobs[i].bbx.width() < char_size &&
                    char_blobs[i].bbx.height() > char_size * StraboParameters.bbxMultiplier)
                    noise_char_idx_set.Add(i);
                if (char_blobs[i].bbx.height() < char_size &&
                    char_blobs[i].bbx.width() > char_size * StraboParameters.bbxMultiplier)
                    noise_char_idx_set.Add(i);
            }

            for (var i = 0; i < srcimg.Width * srcimg.Height; i++)
                if (char_labels[i] != 0)
                {
                    var idx = char_labels[i] - 1;
                    if (boarder_char_idx_set.Contains(idx)) char_labels[i] = 0;
                    if (noise_char_idx_set.Contains(idx)) char_labels[i] = 0;
                }
            var img = new bool[srcimg.Height, srcimg.Width];
            for (var i = 0; i < srcimg.Width; i++)
            for (var j = 0; j < srcimg.Height; j++)
                if (char_labels[j * srcimg.Width + i] == 0)
                    img[j, i] = false;
                else
                    img[j, i] = true;
            return ImageUtils.ArrayBool2DToBitmap(img);
        }
    }
}