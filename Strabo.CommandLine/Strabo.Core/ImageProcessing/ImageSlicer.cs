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
using System.IO;
using Strabo.Core.Utility;

namespace Strabo.Core.ImageProcessing
{
    public class ImageSlicer
    {
        public List<int[]> xy_offset_list;

        public List<string> Apply(int row, int col, int overlap, string inputPath, string outDirPath)
        {
            try
            {
                var results = new List<string>();
                xy_offset_list = new List<int[]>();
                using (var srcimg = new Bitmap(inputPath))
                {
                    var num = row * col;
                    var width = srcimg.Width;
                    var height = srcimg.Height;

                    var twidth = width / col + overlap * 2;
                    var theight = height / row + overlap * 2;
                    for (var j = 0; j < row; j++)
                    for (var i = 0; i < col; i++)
                    {
                        var row_step = height / row;
                        var col_step = width / col;

                        var x = col_step * i;
                        var y = row_step * j;
                        var xwidth = twidth;
                        var yheight = theight;
                        if (i == col - 1)
                            xwidth = width - x;
                        if (j == row - 1)
                            yheight = height - y;
                        var xy_offset = new int[2];
                        xy_offset[0] = x;
                        xy_offset[1] = y;
                        xy_offset_list.Add(xy_offset);
                        var rect = new Rectangle(x, y, xwidth, yheight);
                        using (var tile =
                            new Bitmap(xwidth, yheight))
                        {
                            using (var g = Graphics.FromImage(tile))
                            {
                                g.DrawImage(srcimg, new Rectangle(0, 0, tile.Width, tile.Height),
                                    rect,
                                    GraphicsUnit.Pixel);
                            }
                            var imagePath = Path.Combine(outDirPath, "tile_" + j + "_" + i + ".png");
                            tile.Save(imagePath);
                            results.Add(imagePath);
                            tile.Dispose();
                        }
                    }
                    srcimg.Dispose();
                }
                return results;
            }
            catch (Exception e)
            {
                Log.WriteLine(e.Message);
                Log.WriteLine(e.Source);
                Log.WriteLine(e.StackTrace);
                throw;
            }
        }
    }
}