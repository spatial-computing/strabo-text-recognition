using Strabo.Core.Worker;
using System;

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using Emgu.CV.Structure;
using Emgu.CV;
using Strabo.Core.Utility;
using AForge.Imaging.Filters;

namespace Strabo.Core.TextDetection
{
    public class DetectTextOrientation
    {
        private MergeTextStrings _mts = null;
        private int _tnum;
        public DetectTextOrientation() { }

        public void Apply(MergeTextStrings mts, int tnum)
        {
            _tnum = tnum;
            _mts = mts;
            Thread[] thread_array = new Thread[_tnum];
            for (int i = 0; i < tnum; i++)
            {
                thread_array[i] = new Thread(new ParameterizedThreadStart(returnOrientationThread));
                thread_array[i].Start(i);
            }
            for (int i = 0; i < tnum; i++)
                thread_array[i].Join();
        }

        public void returnOrientationThread(object s)
        {
            int start = (int)s;
            for(int i=start; i<_mts.textStringList.Count; i+=_tnum)
            {
                if(_mts.textStringList[i].char_list.Count > 2)
                {
                    double angle = _mts.textStringList[i].bbx.returnCalculatedAngle();
                    // 360 - angle == -angle (counterclockwise equivalent of negative clockwise angle)
                    RotateBilinear clockwiseRotation = new RotateBilinear(0 - angle, false);
                    RotateBilinear counterClockwiseRotation = new RotateBilinear(180 - angle, false);
                    counterClockwiseRotation.FillColor = Color.White;
                    clockwiseRotation.FillColor = Color.White;

                    _mts.textStringList[i].orientation_list.Add(360 - angle);
                    _mts.textStringList[i].orientation_list.Add(180 - angle);

                    _mts.textStringList[i].rotated_img_list.Add(
                        clockwiseRotation.Apply(_mts.textStringList[i].srcimg));
                    _mts.textStringList[i].rotated_img_list.Add(
                        counterClockwiseRotation.Apply(_mts.textStringList[i].srcimg));
                }
            }
        }

        public void DetectOrientationThread(object s)
        {
            int counter = 0;
            int start = (int)s;
            for (int i = start; i < _mts.textStringList.Count; i += _tnum)
            {
                if (_mts.textStringList[i].char_list.Count > 2)
                {
                    double avg_size = 0;
                    for (int j = 0; j < _mts.textStringList[i].char_list.Count; j++)
                    {
                        avg_size += _mts.textStringList[i].char_list[j].bbx.width() +
                                    _mts.textStringList[i].char_list[j].bbx.height();
                    }
                    counter++;
                    avg_size /= (double)(_mts.textStringList[i].char_list.Count * 2);
                    MultiThreadsSkewnessDetection mtsd = new MultiThreadsSkewnessDetection();
                    int[] idx = mtsd.Apply(1, _mts.textStringList[i].srcimg, (int)avg_size, 0, 180, 3);

                    if (idx[0] <= 90)
                    {
                        _mts.textStringList[i].orientation_list.Add(idx[0]);
                        _mts.textStringList[i].rotated_img_list.Add((Bitmap)mtsd.rotatedimg_table[idx[0]]);
                        if (GeometryUtils.DiffSlope(idx[0], 90) < 5)
                        {
                            _mts.textStringList[i].orientation_list.Add(idx[1]);
                            _mts.textStringList[i].rotated_img_list.Add((Bitmap)mtsd.rotatedimg_table[idx[1]]);
                        }
                    }
                    else
                    {
                        _mts.textStringList[i].orientation_list.Add(idx[1]);
                        _mts.textStringList[i].rotated_img_list.Add((Bitmap)mtsd.rotatedimg_table[idx[1]]);
                        if (GeometryUtils.DiffSlope(idx[1], 270) < 5)
                        {
                            _mts.textStringList[i].orientation_list.Add(idx[0]);
                            _mts.textStringList[i].rotated_img_list.Add((Bitmap)mtsd.rotatedimg_table[idx[0]]);
                        }
                    }
                }
            }
        }

        public List<int> findNearestSrings(int num, List<TextString> text_string_list)
        {

            MCvBox2D rec = text_string_list[num].bbx.Bbx();

            // Scale the bbx 4times
            PointF[] points = rec.GetVertices();
            for(int i=0; i<points.Length; i++)
            {
                points[i].X *= 4;
            }
            MCvBox2D bbx = PointCollection.MinAreaRect(points);
            List<int> nearest_string_list = new List<int>();
            string line = "";

            for (int i = 0; i < text_string_list.Count; i++)
            {
                if (text_string_list[i].char_list.Count <= 3)
                    continue;

                if (bbx.MinAreaRect().IntersectsWith(text_string_list[i].bbx.Bbx().MinAreaRect()))
                {
                    nearest_string_list.Add(i);
                    line += (i + 1) + "@" + text_string_list[i].rotated_img_list.Count + "@";
                }
            }
            return nearest_string_list;
        }

    }
}
