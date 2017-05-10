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

using Strabo.Core.ImageProcessing;
using Strabo.Core.TextDetection;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Strabo.Core.Utility;
using Emgu.CV.Structure;
using Strabo.Core.BoundingBox;
using Emgu.CV;
using System.Linq;

namespace Strabo.Core.Worker
{
    /// <summary>
    /// Sima
    /// </summary>
    public class MergeTextStrings
    {
        private List<TextString> text_string_list;

        public MergeTextStrings()
        {
            this.text_string_list = new List<TextString>();
        }

        public List<TextString> textStringList
        {
            get
            {
                return this.text_string_list;
            }
        }

        public void WriteBMP(string output_path)
        {
            for (int i = 0; i < text_string_list.Count; i++)
            {
                if (text_string_list[i].char_list.Count == 1)
                    continue;

                int massCenterX = (int) text_string_list[i].bbx.massCenter().X;
                int massCenterY = (int) text_string_list[i].bbx.massCenter().Y;

                for (int s = 0; s < text_string_list[i].orientation_list.Count; s++)
                {
                    string slope = Convert.ToDouble(text_string_list[i].orientation_list[s]).ToString();
                    if (slope == "360") // rotated 360 degress is 0 degree...
                        continue;

                    string vertices = "";
                    foreach(PointF vertex in text_string_list[i].bbx.vertices())
                    {
                        vertices += vertex.X + "_" + vertex.Y + "_";
                    }

                    ImageStitcher imgstitcher1 = new ImageStitcher();
                    MinimumBoundingBox bx = text_string_list[i].bbx;
                    using (Bitmap single_img = imgstitcher1.ExpandCanvas(text_string_list[i].rotated_img_list[s], 20))
                    {
                        string fn = i + "_p_" + text_string_list[i].char_list.Count
                            + "_" + massCenterX + "_" + massCenterY + "_s_" + slope
                            + "_" + vertices + bx.width() + "_" + bx.height()
                            + ".png";
                        single_img.Save(Path.Combine(output_path,fn));
                    }
                }

                string vx = "";
                foreach (PointF vertex in text_string_list[i].bbx.vertices())
                {
                    vx += vertex.X + "_" + vertex.Y + "_";
                }

                MinimumBoundingBox mbr = text_string_list[i].bbx;
                //write original orientation
                ImageStitcher imgstitcher2 = new ImageStitcher();
                using (Bitmap srcimg = imgstitcher2.ExpandCanvas(text_string_list[i].srcimg, 20))
                {
                    string fn = i + "_p_" + text_string_list[i].char_list.Count
                        + "_" + massCenterX + "_" + massCenterY + "_s_0" + "_"
                        + vx + mbr.width() + "_" + mbr.height() + ".png";
                    srcimg.Save(Path.Combine(output_path, fn));
                }
            }
        }

        public void AddTextString(TextString dstString)
        {
            Update(ref dstString);

            List<int> insert_idx_list = new List<int>();// insert = -1;
            int[] matched_idx_array = new int[dstString.char_list.Count];
            for(int i=0;i<matched_idx_array.Length;i++)
                matched_idx_array[i] = -1;
            int[] matched_char_blob_count = new int [text_string_list.Count];
            for (int i = 0; i < text_string_list.Count; i++)
            {
                TextString srcString = text_string_list[i];
                for(int x=0; x<srcString.char_list.Count; x++)
                {
                    for(int y=0; y<dstString.char_list.Count; y++)
                    {
                        if(Distance(srcString.char_list[x].bbx.massCenter(), dstString.char_list[y].bbx.massCenter()) < 3)
                        {
                            matched_idx_array[y] = i;
                            matched_char_blob_count[i]++;
                        }
                    }
                      //if(insert!=-1)
                            //break;
                }
            }

            int max_matched = 0;
            int max_matched_idx = -1;
            for (int i = 0; i < matched_char_blob_count.Length; i++)
            {
                if (matched_char_blob_count[i] > max_matched)
                {
                    max_matched = matched_char_blob_count[i];
                    max_matched_idx = i;
                }
            }

            int insert = max_matched_idx;

            if (insert == -1)
                text_string_list.Add(dstString);
            else
            {
                MinimumBoundingBox verticesMbr = text_string_list[insert].bbx;
                MinimumBoundingBox dstMbr = dstString.bbx;

                List<int> remove_string_list = new List<int>();
                for (int i = 0; i < matched_idx_array.Length ; i++)
                {
                    if (matched_idx_array[i] != -1 && matched_idx_array[i] != insert)
                        remove_string_list.Add(i);
                }
                
                PointF p1 = new PointF(verticesMbr.firstVertex().X, verticesMbr.firstVertex().Y);
                PointF p2 = new PointF(dstMbr.firstVertex().X, dstMbr.firstVertex().Y);

                for (int c = 0; c < dstString.char_list.Count; c++)
                    text_string_list[insert].AddChar(dstString.char_list[c]);

                Bitmap newimg1 = new Bitmap((int) verticesMbr.width(), 
                                            (int) verticesMbr.height());
                Bitmap newimg2 = new Bitmap((int) dstMbr.width(),
                                            (int) dstMbr.height());

                Graphics g1 = Graphics.FromImage(newimg1);
                g1.Clear(Color.White);
                g1.DrawImage(text_string_list[insert].srcimg,
                    p1.X - verticesMbr.firstVertex().X,
                    p1.Y - verticesMbr.firstVertex().Y);

                Graphics g2 = Graphics.FromImage(newimg2);
                g2.Clear(Color.White);
                g2.DrawImage(dstString.srcimg,
                            p2.X - verticesMbr.firstVertex().X,
                            p2.Y - verticesMbr.firstVertex().Y);
                g1.Dispose(); g2.Dispose();
                
                //ASHISH
                newimg1 = ImageUtils.GetIntersection(newimg1, newimg2);

                text_string_list[insert].srcimg = newimg1;

                //for (int i = 0; i < remove_string_list.Count; i++)
                //{

                //}
            }
        } 

        public double Distance(PointF a, PointF b)
        {
            return Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
        }
        public void Update(ref TextString srcString)
        {
            MyConnectedComponentsAnalysisFast.MyBlob char_blob0 = srcString.char_list[0];
            PointF[] char_blob0BbxVertex = char_blob0.bbx.Bbx().GetVertices();
            for(int i=0; i< char_blob0BbxVertex.Length; i++)
            {
                char_blob0BbxVertex[i].X += srcString.x_offset;
                char_blob0BbxVertex[i].Y += srcString.y_offset;
            }
            char_blob0.bbx.updateBbx(PointCollection.MinAreaRect(char_blob0BbxVertex));

            PointF[] pts = char_blob0.bbx.vertices();
            MinimumBoundingBox bbx = new MinimumBoundingBox(pts);

            for (int i = 1; i < srcString.char_list.Count; i++)
            {
                MyConnectedComponentsAnalysisFast.MyBlob char_blob = srcString.char_list[i];
                PointF[] char_blobVertex = char_blob.bbx.Bbx().GetVertices();
                for (int j = 0; j < char_blobVertex.Length; j++)
                {
                    char_blobVertex[j].X += srcString.x_offset;
                    char_blobVertex[j].Y += srcString.y_offset;
                }
                char_blob.bbx.updateBbx(PointCollection.MinAreaRect(char_blobVertex));


                PointF[] bbxVertex = bbx.vertices();
                PointF[] char_blob1Vertex = char_blob.bbx.Bbx().GetVertices();
                PointF[] points = bbxVertex.Concat(char_blob1Vertex).ToArray();
                
                bbx.updateBbx(PointCollection.MinAreaRect(points));
            }
            srcString.bbx = bbx;
        }
    }
}
