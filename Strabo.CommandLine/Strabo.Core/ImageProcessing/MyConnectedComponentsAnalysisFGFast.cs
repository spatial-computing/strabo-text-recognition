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
using System.Drawing.Imaging;
using Strabo.Core.BoundingBox;

namespace Strabo.Core.ImageProcessing
{
    /// <summary>
    ///     Narges
    ///     MyConnectedComponentsAnalysisFast class is used to retrieve all blobs in an image
    ///     In this class we have two other classes "MyBlob" and "MyBlobCounter". In MyBlob class we have information that we
    ///     want for a blob.
    ///     In MyBlobCounter we will investigate pixel by pixel of image to find different lables and blobs
    ///     ///
    /// </summary>
    public class MyConnectedComponentsAnalysisFGFast
    {
        public class MyBlob
        {
            public double b;
            public MinimumBoundingBox bbx;

            /// It has been used in MyConnectedComponentsAnalysisFast but MyBlobCounter
            public bool included = false;

            public double m;
            public Rectangle maxBbx;
            public int maxBbxArea;
            public Point maxBbxMassCenter;
            public int neighbor_count = 0;
            public List<int> neighbors = new List<int>();
            public int pixel_count;

            /// It has been used in MyConnectedComponentsAnalysisFast but MyBlobCounter
            public int pixel_id;

            public int sample_x;
            public int sample_y;
            public bool sizefilter_included = true;
            public bool split_here = false;
            public int split_visited = 0;
            public int string_id;
            public bool visited = false;

            public MyBlob(PointF[] points)
            {
                // MinumumBoundingBox provides methods for
                // area, orientation, dimensions and many other
                bbx = new MinimumBoundingBox(points);
            }

            public MyBlob()
            {
            }
        }

        public class MyBlobCounter
        {
            public ushort[] objectLabels;
            private ushort objectsCount;

            private int[] objectSize;

            // To any new user modifying this code
            // Do not write comments like this <- who are you??
            ////I don’t know if I should have another function with a similar name that accepts path as input?
            private void Process(Bitmap srcImg, int fg_color)
            {
                // Steps:
                // Get source Image dimensions
                // Allocate labels Array
                // Initialize the label count as 0
                // Initialize the maximum number of labels that can be found
                // Map all labels to themselves

                srcImg = ImageUtils.toGray(srcImg);
                var width = srcImg.Width;
                var height = srcImg.Height;
                objectLabels = new ushort[width * height];

                ushort labelsCount = 0;
                var maxObjects = (width / 2 + 1) * (height / 2 + 1) + 1;
                var map = new int[maxObjects];
                for (var i = 0; i < maxObjects; i++)
                    map[i] = i;

                // lock source bitmap data
                var srcData = srcImg.LockBits(
                    new Rectangle(0, 0, width, height),
                    ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);

                var srcStride = srcData.Stride;
                var srcOffset = srcStride - width;
                // do the job
                unsafe
                {
                    var src = (byte*) srcData.Scan0.ToPointer();
                    var p = 0;
                    // label the first pixel
                    // 1 - for pixels of the first row
                    if (*src == fg_color)
                        objectLabels[p] = ++labelsCount; // label start from 1
                    ++src;
                    ++p;
                    // label the first row
                    for (var x = 1; x < width; x++, src++, p++)
                        // check if we need to label current pixel
                        if (*src == fg_color)
                            if (src[-1] == fg_color)
                                // label current pixel, as the previous
                                objectLabels[p] = objectLabels[p - 1];
                            else
                                // create new label
                                objectLabels[p] = ++labelsCount;
                    src += srcOffset;
                    // 2 - for other rows
                    // for each row
                    for (var y = 1; y < height; y++)
                    {
                        // for the first pixel of the row, we need to check
                        // only upper and upper-right pixels
                        if (*src == fg_color)
                            if (src[-srcStride] == fg_color)
                                // label current pixel, as the above
                                objectLabels[p] = objectLabels[p - width];
                            else if (src[1 - srcStride] == fg_color)
                                // label current pixel, as the above right
                                objectLabels[p] = objectLabels[p + 1 - width];
                            else
                                // create new label
                                objectLabels[p] = ++labelsCount;
                        ++src;
                        ++p;
                        // check left pixel and three upper pixels
                        for (var x = 1; x < width - 1; x++, src++, p++)
                            if (*src == fg_color)
                            {
                                // check surrounding pixels
                                if (src[-1] == fg_color)
                                    // label current pixel, as the left
                                    objectLabels[p] = objectLabels[p - 1];
                                else if (src[-1 - srcStride] == fg_color)
                                    // label current pixel, as the above left
                                    objectLabels[p] = objectLabels[p - 1 - width];
                                else if (src[-srcStride] == fg_color)
                                    // label current pixel, as the above
                                    objectLabels[p] = objectLabels[p - width];
                                if (src[1 - srcStride] == fg_color)
                                    if (objectLabels[p] == 0)
                                        // label current pixel, as the above right
                                    {
                                        objectLabels[p] = objectLabels[p + 1 - width];
                                    }
                                    else
                                    {
                                        int l1 = objectLabels[p];
                                        int l2 = objectLabels[p + 1 - width];
                                        if (l1 != l2 && map[l1] != map[l2])
                                        {
                                            // merge
                                            if (map[l1] == l1)
                                                // map left value to the right
                                            {
                                                map[l1] = map[l2];
                                            }
                                            else if (map[l2] == l2)
                                                // map right value to the left
                                            {
                                                map[l2] = map[l1];
                                            }
                                            else
                                            {
                                                // both values already mapped
                                                map[map[l1]] = map[l2];
                                                map[l1] = map[l2];
                                            }
                                            // reindex
                                            for (var i = 1; i <= labelsCount; i++)
                                                if (map[i] != i)
                                                {
                                                    // reindex
                                                    var j = map[i];
                                                    while (j != map[j])
                                                        j = map[j];
                                                    map[i] = j;
                                                }
                                        }
                                    }
                                if (objectLabels[p] == 0)
                                    // create new label
                                    objectLabels[p] = ++labelsCount;
                            }
                        // for the last pixel of the row, we need to check
                        // only upper and upper-left pixels
                        if (*src == fg_color)
                            if (src[-1] == fg_color)
                                objectLabels[p] = objectLabels[p - 1];
                            else if (src[-1 - srcStride] == fg_color)
                                objectLabels[p] = objectLabels[p - 1 - width];
                            else if (src[-srcStride] == fg_color)
                                objectLabels[p] = objectLabels[p - width];
                            else
                                objectLabels[p] = ++labelsCount;
                        ++src;
                        ++p;
                        src += srcOffset;
                    }
                }
                // unlock source images
                srcImg.UnlockBits(srcData);
                // allocate remapping array
                var reMap = new ushort[map.Length];
                // count objects and prepare remapping array
                objectsCount = 0;
                for (var i = 1; i <= labelsCount; i++)
                    if (map[i] == i)
                        reMap[i] = ++objectsCount;
                // second pass to compete remapping
                for (var i = 1; i <= labelsCount; i++)
                    if (map[i] != i)
                        reMap[i] = reMap[map[i]];
                // repair object labels
                for (int i = 0, n = objectLabels.Length; i < n; i++)
                    objectLabels[i] = reMap[objectLabels[i]];
            }

            // Get array of objects rectangles
            public List<MyBlob> GetBlobs(Bitmap srcImg, int fg)
            {
                Process(srcImg, fg);

                var labels = objectLabels;
                int count = objectsCount;
                objectSize = new int[count + 1];
                var center_x = new double[count + 1];
                var center_y = new double[count + 1];
                var width = srcImg.Width;
                var height = srcImg.Height;
                int i = 0, label;
                // create object coordinates arrays
                var x1 = new int[count + 1];
                var y1 = new int[count + 1];
                var x2 = new int[count + 1];
                var y2 = new int[count + 1];
                var sample_x = new int[count + 1];
                var sample_y = new int[count + 1];
                for (var j = 1; j <= count; j++)
                {
                    x1[j] = width;
                    y1[j] = height;
                }

                // walk through labels array, skip one row and one col
                for (var y = 0; y < height; y++)
                for (var x = 0; x < width; x++, i++)
                {
                    // get current label
                    label = labels[i];
                    // skip unlabeled pixels
                    if (label == 0)
                        continue;

                    objectSize[label]++;

                    // check and update all coordinates
                    center_x[label] += x;
                    center_y[label] += y;
                    sample_x[label] = x; // record the last point as a sample
                    sample_y[label] = y;
                    if (x < x1[label])
                        x1[label] = x;
                    if (x > x2[label])
                        x2[label] = x;
                    if (y < y1[label])
                        y1[label] = y;
                    if (y > y2[label])
                        y2[label] = y;
                }

                //Get the list of pixel locations to calculate minimum bounding rectangles
                var positions = new List<PointF[]>();
                //Store the next bias to insert in each list in positions
                var nextPos = new int[count + 1];

                //It seems that the index of label has been out of range of positions, 
                //so use a dictionary to project labels to their position in nextPos array
                //Dictionary<int, int> labelToNumInOrder = new Dictionary<int, int>();
                for (var j = 1; j <= count; j++)
                    positions.Add(new PointF[objectSize[j]]);
                i = 0;
                for (var y = 0; y < height; y++)
                for (var x = 0; x < width; x++, i++)
                {
                    // because label starts from 1
                    label = labels[i] - 1;
                    if (label == -1)
                        continue;

                    // not sure if the label number is always less than count ????
                    positions[label][nextPos[label]] = new PointF(x, y);
                    nextPos[label] += 1;
                }

                var blobs = new List<MyBlob>();
                for (var j = 1; j <= count; j++)
                {
                    var b = new MyBlob(positions[j - 1]);
                    b.maxBbx = new Rectangle(x1[j], y1[j], x2[j] - x1[j] + 1, y2[j] - y1[j] + 1);
                    b.pixel_count = objectSize[j];
                    b.maxBbxMassCenter = new Point(Convert.ToInt32(center_x[j] / b.pixel_count),
                        Convert.ToInt32(center_y[j] / b.pixel_count));
                    b.maxBbxArea = (x2[j] - x1[j] + 1) * (y2[j] - y1[j] + 1);

                    b.pixel_id = j;
                    b.sample_x = sample_x[j];
                    b.sample_y = sample_y[j];
                    blobs.Add(b);
                }

                return blobs;
            }
        }
    }
}