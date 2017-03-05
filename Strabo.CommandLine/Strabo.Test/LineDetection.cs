using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AForge.Imaging;
using System.Drawing;

namespace Strabo.Test
{
   class LineDetection
    {
        public void test()
        {
            Bitmap OriginalBinaryImage = new Bitmap(@"C:\Users\nhonarva\Documents\strabo-command-line-master\strabo-command-line-master\Strabo.CommandLine\data\intermediate\SourceMapImage.png");
            HoughLineTransformation lineTransform = new HoughLineTransformation();
            //Bitmap grayScaleBP = new System.Drawing.Bitmap(OriginalBinaryImage.Width, OriginalBinaryImage.Height, System.Drawing.Imaging.PixelFormat.Format16bppGrayScale);

            //for (int i = 0; i < OriginalBinaryImage.Width; i++)
            //{
            //    for (int j = 0; j < OriginalBinaryImage.Height ; j++)
            //    {
            //        if ((!OriginalBinaryImage.GetPixel(i, j).Name.Equals("ffffffff")))
            //            grayScaleBP.SetPixel(i, j, Color.Black);
            //        else
            //            grayScaleBP.SetPixel(i, j, Color.White);
             
            //    }
            //}

            lineTransform.ProcessImage(OriginalBinaryImage);
            Bitmap houghLineImage = lineTransform.ToBitmap();
            // get lines using relative intensity
            houghLineImage.Save(@"C:\Users\nhonarva\Documents\HoughLine.png");

        //    Bitmap grayScaleBP = new System.Drawing.Bitmap(2, 2, System.Drawing.Imaging.PixelFormat.Format16bppGrayScale);
           
            HoughLine[] lines = lineTransform.GetLinesByRelativeIntensity(0.5);


            //for (int i=0;i<lines.Length;i++)
            //{
            //    float rho = lines[i].Radius;
            //    double theta = lines[i].Theta;
            //    Point pt1, pt2;
            //    double a = Math.Cos(theta), b = Math.Sin(theta);
            //    double x0 = a * rho, y0 = b * rho;
            //    pt1.x = cvRound(x0 + 1000 * (-b)); //??
            //    pt1.y = cvRound(y0 + 1000 * (a)); //??
            //    pt2.x = cvRound(x0 - 1000 * (-b)); //??
            //    pt2.y = cvRound(y0 - 1000 * (a)); //??
            //    line(cdst, pt1, pt2, Scalar(0, 0, 255), 3, CV_AA);
            //}
            //lines[];
            //if (lines.Length == 1)
            //{

            //}
        }

      

    }
}
