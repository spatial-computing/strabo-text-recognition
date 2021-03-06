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

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using Strabo.Core.Utility;

namespace Strabo.Core.ImageProcessing
{
    public static class RGBColorThresholding
    {
        private static Bitmap _srcimg;
        private static bool[,] _dstimg;
        private static int _height;
        private static int _width;
        private static BitmapData _srcData;
        private static int _tnum;
        private static RGBThreshold _rgbThreshould;

        //public static string ApplyBradleyLocalThresholding(string inputPath, string outputPath)
        //{
        //    BradleyLocalThresholding bradley = new BradleyLocalThresholding();
        //    Image<Gray, Byte> img = new Image<Gray, Byte>(inputPath);
        //    Bitmap dstimg = bradley.Apply(img.Bitmap);
        //    dstimg.Save(outputPath);
        //    dstimg.Dispose();
        //    dstimg = null;
        //    img.Dispose();
        //    img = null;
        //    return outputPath;
        //}
        public static void RunRGBColorThresholding(object step)
        {
            var t = (int) step;
            var offset1 = _srcData.Stride - _width * 3;
            // do the job
            unsafe
            {
                var src = (byte*) _srcData.Scan0.ToPointer() + t * _srcData.Stride;

                // for each row
                for (var y = t; y < _height; y += _tnum)
                {
                    // for each pixel
                    for (var x = 0; x < _width; x++, src += 3)
                        if (src[RGB.R] <= _rgbThreshould.upperRedColorThd &&
                            src[RGB.R] >= _rgbThreshould.lowerRedColorThd &&
                            src[RGB.G] <= _rgbThreshould.upperGreenColorThd &&
                            src[RGB.G] >= _rgbThreshould.lowerGreenColorThd &&
                            src[RGB.B] <= _rgbThreshould.upperBlueColorThd &&
                            src[RGB.B] >= _rgbThreshould.lowerBlueColorThd)
                            _dstimg[y, x] = true;
                        else
                            _dstimg[y, x] = false;
                    src += offset1 + (_tnum - 1) * _srcData.Stride;
                }
            }
        }

        public static string ApplyRGBColorThresholding(string inputPath, string outputPath, RGBThreshold thd, int tnum)
        {
            _srcimg = null;
            _srcimg = new Bitmap(inputPath);
            _srcimg = ImageUtils.AnyToFormat24bppRgb(_srcimg);
            _width = _srcimg.Width;
            _height = _srcimg.Height;
            _rgbThreshould = thd;
            _tnum = tnum;

            _srcData = _srcimg.LockBits(
                new Rectangle(0, 0, _srcimg.Width, _srcimg.Height),
                ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            _dstimg = new bool[_srcimg.Height, _srcimg.Width];

            try
            {
                var thread_array = new Thread[tnum];
                for (var i = 0; i < tnum; i++)
                {
                    thread_array[i] = new Thread(RunRGBColorThresholding);
                    thread_array[i].Start(i);
                }
                for (var i = 0; i < tnum; i++)
                    thread_array[i].Join();

                _srcimg.UnlockBits(_srcData);
                _srcimg.Dispose();
                _srcimg = null;

                var img = ImageUtils.Array2DToBitmap(_dstimg);
                img.Save(outputPath);
                img.Dispose();
                img = null;
                _dstimg = null;

                return outputPath;
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