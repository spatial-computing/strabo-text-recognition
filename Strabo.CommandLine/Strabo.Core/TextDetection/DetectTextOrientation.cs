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
using System.Threading;
using AForge.Imaging.Filters;

namespace Strabo.Core.TextDetection
{
    public class DetectTextOrientation
    {
        private int _tnum;
        private List<TextString> textStringList;

        public void Apply(List<TextString> textStringList, int tnum)
        {
            _tnum = tnum;
            this.textStringList = textStringList;
            var thread_array = new Thread[_tnum];
            for (var i = 0; i < tnum; i++)
            {
                thread_array[i] = new Thread(returnOrientationThread);
                thread_array[i].Start(i);
            }
            for (var i = 0; i < tnum; i++)
                thread_array[i].Join();
        }

        public void returnOrientationThread(object s)
        {
            var start = (int) s;
            for (var i = start; i < textStringList.Count; i += _tnum)
                if (textStringList[i].char_list.Count > 2)
                {
                    double angle = textStringList[i].bbx.returnCalculatedAngle();
                    // 360 - angle == -angle (counterclockwise equivalent of negative clockwise angle)
                    var clockwiseRotation = new RotateBilinear(0 - angle, false);
                    var counterClockwiseRotation = new RotateBilinear(180 - angle, false);
                    counterClockwiseRotation.FillColor = Color.White;
                    clockwiseRotation.FillColor = Color.White;

                    textStringList[i].orientation_list.Add(360 - angle);
                    textStringList[i].orientation_list.Add(180 - angle);

                    textStringList[i].rotated_img_list.Add(
                        clockwiseRotation.Apply(textStringList[i].srcimg));
                    textStringList[i].rotated_img_list.Add(
                        counterClockwiseRotation.Apply(textStringList[i].srcimg));
                }
        }
    }
}