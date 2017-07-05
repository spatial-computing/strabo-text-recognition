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

namespace Strabo.Core.ImageProcessing
{
    /// <summary>
    ///     Narges
    /// </summary>
    public class ImageStitcher
    {
        public int cell_width = -1, cell_height = -1;
        public int cols, rows;

        public Bitmap ExpandCanvas(Bitmap srcimg, int size)
        {
            var string_img = new Bitmap(srcimg.Width + size * 2, srcimg.Height + size * 2);
            var g = Graphics.FromImage(string_img);
            g.Clear(Color.White);
            g.DrawImage(srcimg, new Point(size, size));
            g.Dispose();
            return string_img;
        }

        public Bitmap mergeContiguousImageBlocks(List<Bitmap> srcImgs,
            int row, int col)
        {
            // Assume all the sub-images will have similar dimensions
            // Width = (width of 1 image) * (number of columns)
            // Height = (height of 1 image) * (number of rows)
            var imgWidth = srcImgs[0].Width * col;
            var imgHeight = srcImgs[0].Height * row;
            var stichedImage = new Bitmap(imgWidth, imgHeight);

            try
            {
                using (var g = Graphics.FromImage(stichedImage))
                {
                    g.Clear(Color.White);
                    var row_offset = 0;
                    for (var r = 0; r < row; r++)
                    {
                        var col_offset = 0;
                        for (var c = 0; c < col; c++)
                        {
                            var idx = r * col + c;
                            if (idx >= srcImgs.Count)
                                continue;
                            g.DrawImage(srcImgs[idx],
                                new Rectangle(col_offset, row_offset, srcImgs[idx].Width, srcImgs[idx].Height));
                            // g.DrawImage(srcImgs[idx], col_offset, row_offset);
                            col_offset += srcImgs[c].Width;
                        }
                        row_offset += srcImgs[r].Height;
                    }
                    return stichedImage;
                }
            }
            catch (Exception)
            {
                if (stichedImage != null)
                    stichedImage.Dispose();
                throw;
            }
            finally
            {
                // Cleanup memory
                foreach (var img in srcImgs)
                    img.Dispose();
            }
        }

        public Bitmap ApplyWithoutGridLines(List<Bitmap> srcimg_list, int max_width)
        {
            for (var i = 0; i < srcimg_list.Count; i++)
            {
                if (cell_height < srcimg_list[i].Height)
                    cell_height = srcimg_list[i].Height;
                if (cell_width < srcimg_list[i].Width)
                    cell_width = srcimg_list[i].Width;
            }

            cell_width += 10;
            cell_height += 10;

            cols = max_width / cell_width;
            rows = srcimg_list.Count / cols + 1;
            var image_width = cols * cell_width;
            var image_height = rows * cell_height;
            var string_img = new Bitmap(image_width, image_height);
            var g = Graphics.FromImage(string_img);
            g.Clear(Color.White);

            for (var r = 0; r < rows; r++)
            for (var c = 0; c < cols; c++)
            {
                var idx = r * cols + c;
                if (idx >= srcimg_list.Count) continue;
                var margin = 4;
                var w = cell_width - srcimg_list[idx].Width - margin;
                w /= 2;
                var h = cell_height - srcimg_list[idx].Height - margin;
                h /= 2;

                g.DrawImage(srcimg_list[idx],
                    new Point(c * cell_width + w, r * cell_height + h));
            }
            g.Dispose();
            return string_img;
        }

        public Bitmap Apply(List<Bitmap> srcimg_list, int max_width)
        {
            for (var i = 0; i < srcimg_list.Count; i++)
            {
                if (cell_height < srcimg_list[i].Height)
                    cell_height = srcimg_list[i].Height;
                if (cell_width < srcimg_list[i].Width)
                    cell_width = srcimg_list[i].Width;
            }

            cell_width += 10;
            cell_height += 10;

            cols = max_width / cell_width;
            rows = srcimg_list.Count / cols + 1;
            var image_width = cols * cell_width;
            var image_height = rows * cell_height;
            var string_img = new Bitmap(image_width, image_height);
            var g = Graphics.FromImage(string_img);
            g.Clear(Color.White);
            for (var r = 0; r < rows; r++)
                g.DrawLine(new Pen(Color.Black, 3), new Point(0, r * cell_height),
                    new Point(image_width, r * cell_height));
            for (var c = 0; c < cols; c++)
                g.DrawLine(new Pen(Color.Black), new Point(c * cell_width, 0), new Point(c * cell_width, image_height));

            for (var r = 0; r < rows; r++)
            for (var c = 0; c < cols; c++)
            {
                var idx = r * cols + c;
                if (idx >= srcimg_list.Count) continue;
                var margin = 4;
                var w = cell_width - srcimg_list[idx].Width - margin;
                w /= 2;
                var h = cell_height - srcimg_list[idx].Height - margin;
                h /= 2;

                g.DrawImage(srcimg_list[idx],
                    new Point(c * cell_width + w, r * cell_height + h));
            }
            g.Dispose();
            return string_img;
        }

        public Bitmap ApplyAtLeastTwoRows(List<Bitmap> srcimg_list, int max_width)
        {
            for (var i = 0; i < srcimg_list.Count; i++)
            {
                if (cell_height < srcimg_list[i].Height)
                    cell_height = srcimg_list[i].Height;
                if (cell_width < srcimg_list[i].Width)
                    cell_width = srcimg_list[i].Width;
            }

            cell_width += 50;
            cell_height += 50;

            cols = srcimg_list.Count + 1; // max_width / cell_width;
            rows = 2; // srcimg_list.Count / cols + 1;
            var image_width = cols * cell_width;
            var image_height = rows * cell_height;
            var string_img = new Bitmap(image_width, image_height);
            var g = Graphics.FromImage(string_img);
            g.Clear(Color.White);
            for (var r = 0; r < rows; r++)
                g.DrawLine(new Pen(Color.Black, 3), new Point(0, r * cell_height),
                    new Point(image_width, r * cell_height));
            for (var c = 0; c < cols; c++)
                g.DrawLine(new Pen(Color.Black), new Point(c * cell_width, 0), new Point(c * cell_width, image_height));
            g.DrawRectangle(new Pen(Color.Black, 3), new Rectangle(0, 0, image_width, image_height));
            for (var r = 0; r < rows; r++)
            for (var c = 0; c < cols; c++)
            {
                var idx = r * cols + c;
                if (idx >= srcimg_list.Count) continue;
                var margin = 30;
                var w = cell_width - srcimg_list[idx].Width - margin;
                w /= 2;
                var h = cell_height - srcimg_list[idx].Height - margin;
                h /= 2;

                g.DrawImage(srcimg_list[idx],
                    new Point(c * cell_width + w, r * cell_height + h));
            }
            g.Dispose();
            return string_img;
        }
    }
}