using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Strabo.Core.Utility;
using Accord.Math;
using System.Drawing;
using System.Drawing.Imaging;
using Strabo.Core.ImageProcessing;


namespace Strabo.Core.TextRecognition
{

    class AddedAreaResults
    {
        public Bitmap Area;
        public int AreaForeGoundPixels;
        public AddedAreaResults(Bitmap area, int areaForeGoundPixels)
        {
            Area=area;
            AreaForeGoundPixels = areaForeGoundPixels;
        }
    }
    class PotentialTextAreaExtractation
    {
        public Bitmap FromArrayToImage(int[,] AddeedAreaPoints)
        {
            Bitmap AddedArea = new Bitmap(AddeedAreaPoints.Columns(), AddeedAreaPoints.Rows());

            BitmapData _srcData;

            _srcData = AddedArea.LockBits(
                      new Rectangle(0, 0, AddedArea.Width, AddedArea.Height),
                      ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

            bool[,] _dstimg = new bool[AddedArea.Height, AddedArea.Width]; 
            unsafe
            {


                int offset1 = (_srcData.Stride - AddedArea.Width * 3);
                // do the job
                unsafe
                {
                    byte* src = (byte*)_srcData.Scan0.ToPointer() ;

                    // for each row
                    for (int y = 0; y < AddedArea.Height; y += 1)
                    {
                        // for each pixel
                        for (int x = 0; x < AddedArea.Width; x++, src += 3)
                        {
                            if (AddeedAreaPoints[y, x] == 1)
                                 _dstimg[y,x] = true;
                            else
                                _dstimg[y,x] = false;
                        }
                        src += offset1 ;
                    }
                }

            }

            AddedArea.UnlockBits(_srcData);
            AddedArea.Dispose();
            AddedArea = null;

            Bitmap img = ImageUtils.Array2DToBitmap(_dstimg);


            return img;
        }

        ///public int[,] FromImageToArray(Bitmap Neighborhood, Rectangle boundingbox)
        ///
        public AddedAreaResults FromImageToArray(Bitmap Neighborhood, Rectangle boundingbox)
        {
            //int[,] AddedAreaPoints = new int[boundingbox.Height, boundingbox.Width];

            bool[,] _dstimg = new bool[boundingbox.Height, boundingbox.Width];
            int ForegroundPixels = 0;

            BitmapData _srcData;

            if (boundingbox.X + boundingbox.Width > Neighborhood.Width)
                boundingbox.Width = Neighborhood.Width - boundingbox.X;
            if (boundingbox.X < 0)
            {
                boundingbox.Width += boundingbox.X;
                boundingbox.X = 0;
            }

            if (boundingbox.Y + boundingbox.Height > Neighborhood.Height)
                boundingbox.Height = Neighborhood.Height - boundingbox.Y;
            if (boundingbox.Y < 0)
            {
                boundingbox.Height += boundingbox.Y;
                boundingbox.Y = 0;
            }

            _srcData = Neighborhood.LockBits(
                      boundingbox,
                      ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

            unsafe
            {
                int offset1 = (_srcData.Stride - boundingbox.Width * 3);
                // do the job
                unsafe
                {
                    byte* src = (byte*)_srcData.Scan0.ToPointer();

                    // for each row
                    for (int y = 0; y < boundingbox.Height ; y += 1)
                    {
                        // for each pixel
                        for (int x = 0; x < boundingbox.Width ; x++, src += 3)
                        {
                            if ((src[RGB.R] == 0) &&
                                (src[RGB.G] == 0) &&
                                (src[RGB.B] == 0))
                            {
                                //AddedAreaPoints[y, x] = 1;
                                //if (x == 0 && y == 0)
                                //    Console.WriteLine("debug");
                                _dstimg[y, x] = true;
                                ForegroundPixels++;
                            }
                            else
                            {
                                //AddedAreaPoints[y, x] = 0;
                                _dstimg[y, x] = false;
                            }
                        }
                        src += offset1;
                    }
                }

            }

            Neighborhood.UnlockBits(_srcData);

            Bitmap img = ImageUtils.Array2DToBitmap(_dstimg);
            img.Save(@"C:\Users\narges\Documents\test\test-new.png");
            AddedAreaResults Area = new AddedAreaResults(img, ForegroundPixels);

            return Area;
        }

        public int CountForegroundPixels(int[,] AddedArea)
        {
            int NumberOfForegroundPixels = 0;
            for (int i = 0; i < AddedArea.Rows(); i++)
            {
                for (int j = 0; j < AddedArea.Columns(); j++)
                {
                    if (AddedArea[i, j] == 1)
                        NumberOfForegroundPixels++;
                }
            }
            return NumberOfForegroundPixels;
        }

        public double AddToHeight(string RecongnizedText, double height)
        {
            double AddToHeight = 0;
            if (!Regex.IsMatch(RecongnizedText, @"[A-Zbdfhklt0-9]") && !Regex.IsMatch(RecongnizedText, @"[gjpq]"))
                AddToHeight = Convert.ToInt32(StraboParameters.FirstToThirdGroupHeightRatio * height);

            else if ((!Regex.IsMatch(RecongnizedText, @"[A-Zbdfhklt0-9]") && Regex.IsMatch(RecongnizedText, @"[gjpq]")))
                AddToHeight = Convert.ToInt32(StraboParameters.FirstToSecondGroupHeightRatio * height);
            //else if (!Regex.IsMatch(RecongnizedText, @"[A-Zbdfhklt]") && Regex.IsMatch(RecongnizedText, @"[0-9]"))
            //    AddToHeight = Convert.ToInt32(0.35 * height);
            return AddToHeight;
        }

        public bool ContinueFromLeft(string RecognizedWord)
        {
            if (RecognizedWord.Length > 0)
            {
                if (char.IsUpper(RecognizedWord.ToCharArray()[0]))
                {
                    for (int i = 1; i < RecognizedWord.Length; i++)
                    {
                        if (char.IsLower(RecognizedWord.ToCharArray()[i]))
                            return false;
                    }
                }
                return true;
            }
            if (RecognizedWord.Length == 0)
                return false;

            return true;
        }
    }
}
