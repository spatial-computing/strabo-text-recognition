using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using Strabo.Core.Utility;
using Accord.Math;
using Emgu.CV;
using System.Drawing.Imaging;
using Strabo.Core.ImageProcessing;

namespace Strabo.Core.TextRecognition
{
    class NonHorizontalTextExtraction
    {
        public Emgu.CV.Structure.MCvBox2D NonHorizontalDownAreaDetecteion(Emgu.CV.Structure.MCvBox2D box, Rectangle boundingbox, string RecognizedText, int numberofArea, int x, int y)
        {
            PotentialTextAreaExtractation Area = new PotentialTextAreaExtractation();
            Emgu.CV.Structure.MCvBox2D NullAddedArea = new Emgu.CV.Structure.MCvBox2D();
            Emgu.CV.Structure.MCvBox2D addedArea = box;
            int numberOfRecognizedLetters = RecognizedText.Length;

            if (box.size.Height > box.size.Width)   ///Non horizental bounding boxes that slope of bounding box is less than zero 
            ///
            {
                double WidthAddition = Area.AddToHeight(RecognizedText, addedArea.size.Width);
                double angleInRadius = ((90 + box.angle) * Math.PI) / 180;


                double addToX = (((box.size.Height) * ((numberofArea - 1) * 2 + numberOfRecognizedLetters + 1)) / (numberOfRecognizedLetters * 2)) * Math.Cos(angleInRadius);  // I have multiplied height to 3/4 because we have four letter in recognized image and I want to find the center of added part for boundingbox
                double addToY = (((box.size.Height) * ((numberofArea - 1) * 2 + numberOfRecognizedLetters + 1)) / (numberOfRecognizedLetters * 2)) * Math.Sin(angleInRadius);


                addedArea.center.X += (x - boundingbox.X) + (float)(addToX) + (float)(0.5 * WidthAddition * Math.Sin(angleInRadius));
                addedArea.center.Y += (y - boundingbox.Y) + (float)(addToY) - (float)(0.5 * WidthAddition * Math.Cos(angleInRadius));

                addedArea.size.Height = (float)((box.size.Height) / numberOfRecognizedLetters);
                addedArea.size.Width += (float)WidthAddition;
            }

            else    ///Non horizental bounding boxes that slope of bounding box is greater than zero 
            {
                double HeightAddition = Area.AddToHeight(RecognizedText, addedArea.size.Height);

                double angleInRadius = (((-1) * box.angle) * Math.PI) / 180;


                double addToX = (((box.size.Width) * ((numberofArea - 1) * 2 + numberOfRecognizedLetters + 1)) / (numberOfRecognizedLetters * 2)) * Math.Cos(angleInRadius);  // I have multiplied height to 3/4 because we have four letter in recognized image and I want to find the center of added part for boundingbox
                double addToY = (((box.size.Width) * ((numberofArea - 1) * 2 + numberOfRecognizedLetters + 1)) / (numberOfRecognizedLetters * 2)) * Math.Sin(angleInRadius);

                if (char.IsUpper(RecognizedText.ToCharArray()[0]) && Area.ContinueFromLeft(RecognizedText))
                {
                    addedArea.center.X += (x - boundingbox.X) - (float)addToX - (float)(0.5 * HeightAddition * Math.Sin(angleInRadius));
                    addedArea.center.Y += (y - boundingbox.Y) + (float)addToY - (float)(0.5 * HeightAddition * Math.Cos(angleInRadius));

                    addedArea.size.Width = (float)((box.size.Width) / numberOfRecognizedLetters);
                    addedArea.size.Height += (float)HeightAddition;
                }
                else if (!char.IsUpper(RecognizedText.ToCharArray()[0]) && Area.ContinueFromLeft(RecognizedText))
                {
                    addedArea.center.X += (x - boundingbox.X) - (float)addToX - (float)((StraboParameters.UpperToLowerCaseWidthRatio * box.size.Width * Math.Cos(angleInRadius) * (2 * (numberofArea - 1) + 1)) / (numberOfRecognizedLetters * 2)) - (float)(0.5 * HeightAddition * Math.Sin(angleInRadius));
                    addedArea.center.Y += (y - boundingbox.Y) + (float)addToY + (float)((StraboParameters.UpperToLowerCaseWidthRatio * box.size.Width * Math.Sin(angleInRadius) * (2 * (numberofArea - 1) + 1)) / (numberOfRecognizedLetters * 2)) - (float)(0.5 * HeightAddition * Math.Cos(angleInRadius));

                    addedArea.size.Width = (float)((box.size.Width * (1 + StraboParameters.UpperToLowerCaseWidthRatio)) / numberOfRecognizedLetters);
                    addedArea.size.Height += (float)HeightAddition;
                }
                else
                    return NullAddedArea;
            }
            return addedArea;
        }

        public Emgu.CV.Structure.MCvBox2D NonHorizontalUpAreaDetecteion(Emgu.CV.Structure.MCvBox2D box, Rectangle boundingbox, string RecognizedText, int numberofArea, int x, int y)
        {
            PotentialTextAreaExtractation Area = new PotentialTextAreaExtractation();
            Emgu.CV.Structure.MCvBox2D addedArea = box;
            Emgu.CV.Structure.MCvBox2D NullAddedArea = new Emgu.CV.Structure.MCvBox2D();
            int numberOfRecognizedLetters = RecognizedText.Length;

            if (box.size.Height > box.size.Width)   ///Non horizental bounding boxes that slope of bounding box is less than zero 
            {
                double WidthAddition = Area.AddToHeight(RecognizedText, addedArea.size.Width);

                double angleInRadius = ((90 + box.angle) * Math.PI) / 180;

                double addToX = (((box.size.Height) * ((numberofArea - 1) * 2 + numberOfRecognizedLetters + 1)) / (numberOfRecognizedLetters * 2)) * Math.Cos(angleInRadius);  // I have multiplied height to 3/4 because we have four letter in recognized image and I want to find the center of added part for boundingbox
                double addToY = (((box.size.Height) * ((numberofArea - 1) * 2 + numberOfRecognizedLetters + 1)) / (numberOfRecognizedLetters * 2)) * Math.Sin(angleInRadius);


                if (char.IsUpper(RecognizedText.ToCharArray()[0]) && Area.ContinueFromLeft(RecognizedText))
                {
                    addedArea.center.X += (x - boundingbox.X) - (float)addToX + (float)(0.5 * WidthAddition * Math.Sin(angleInRadius));
                    addedArea.center.Y += (y - boundingbox.Y) - (float)addToY - (float)(0.5 * WidthAddition * Math.Cos(angleInRadius));

                    addedArea.size.Height = (float)((box.size.Height) / numberOfRecognizedLetters);
                    addedArea.size.Width += (float)WidthAddition;
                }
                else if (!char.IsUpper(RecognizedText.ToCharArray()[0]) && Area.ContinueFromLeft(RecognizedText))
                {
                    addedArea.center.X += (x - boundingbox.X) - (float)addToX - (float)((StraboParameters.UpperToLowerCaseWidthRatio * box.size.Height * Math.Cos(angleInRadius) * (2 * (numberofArea - 1) + 1)) / (numberOfRecognizedLetters * 2)) + (float)(0.5 * WidthAddition * Math.Sin(angleInRadius));
                    addedArea.center.Y += (y - boundingbox.Y) - (float)addToY - (float)((StraboParameters.UpperToLowerCaseWidthRatio * box.size.Height * Math.Sin(angleInRadius) * (2 * (numberofArea - 1) + 1)) / (numberOfRecognizedLetters * 2)) - (float)(0.5 * WidthAddition * Math.Cos(angleInRadius));

                    addedArea.size.Height = (float)((box.size.Height * (1 + StraboParameters.UpperToLowerCaseWidthRatio)) / numberOfRecognizedLetters);
                    addedArea.size.Width += (float)WidthAddition;
                }
                else
                    return NullAddedArea;
            }

            else    ///Non horizental bounding boxes that slope of bounding box is greater than zero 
            {

                double HeightAddition = Area.AddToHeight(RecognizedText, addedArea.size.Height);
                double angleInRadius = (((-1) * box.angle) * Math.PI) / 180;

                double addToX = (((box.size.Width) * ((numberofArea - 1) * 2 + numberOfRecognizedLetters + 1)) / (numberOfRecognizedLetters * 2)) * Math.Cos(angleInRadius);  // I have multiplied height to 3/4 because we have four letter in recognized image and I want to find the center of added part for boundingbox
                double addToY = (((box.size.Width) * ((numberofArea - 1) * 2 + numberOfRecognizedLetters + 1)) / (numberOfRecognizedLetters * 2)) * Math.Sin(angleInRadius);


                addedArea.center.X += (x - boundingbox.X) + (float)addToX - (float)(0.5 * HeightAddition * Math.Sin(angleInRadius));
                addedArea.center.Y += (y - boundingbox.Y) - (float)addToY - (float)(0.5 * HeightAddition * Math.Cos(angleInRadius));

                addedArea.size.Width = (float)((box.size.Width) / numberOfRecognizedLetters);
                addedArea.size.Height += (float)HeightAddition;
            }
            return addedArea;
        }

        public bool[] NonHorizontalAreaExtraction(Emgu.CV.Structure.MCvBox2D addedArea, Bitmap OriginalBinaryImage)
        {

            int[,] AddedAreaPoints;
            int[,] AddedAreaPointsForHole;
            double AddedAreaPixelToSizeRatio = 1;
            bool[] PixelsRatio_Character = new bool[2];  // element (1) shows if pixel to size ratio of area is more than threshold , element (2) shows if the area has been classified as character  


            PointF[] AddedAreaVertices = addedArea.GetVertices();
            PointF ExploreAddedArea_MinPoint = AddedAreaVertices[0];
            PointF ExploreAddedArea_MaxPoint = AddedAreaVertices[0];
            try
            {
                for (int i = 1; i < AddedAreaVertices.Length; i++)
                {
                    if (AddedAreaVertices[i].X < ExploreAddedArea_MinPoint.X)
                        ExploreAddedArea_MinPoint.X = AddedAreaVertices[i].X;

                    if (AddedAreaVertices[i].Y < ExploreAddedArea_MinPoint.Y)
                        ExploreAddedArea_MinPoint.Y = AddedAreaVertices[i].Y;

                    if (AddedAreaVertices[i].X > ExploreAddedArea_MaxPoint.X)
                        ExploreAddedArea_MaxPoint.X = AddedAreaVertices[i].X;

                    if (AddedAreaVertices[i].Y > ExploreAddedArea_MaxPoint.Y)
                        ExploreAddedArea_MaxPoint.Y = AddedAreaVertices[i].Y;
                }

                AddedAreaPoints = new int[Convert.ToInt32(ExploreAddedArea_MaxPoint.Y - ExploreAddedArea_MinPoint.Y) + 1, Convert.ToInt32(ExploreAddedArea_MaxPoint.X - ExploreAddedArea_MinPoint.X) + 1];      //// int[boundingbox.Height, boundingbox.Width];
                AddedAreaPointsForHole = new int[Convert.ToInt32(ExploreAddedArea_MaxPoint.Y - ExploreAddedArea_MinPoint.Y) + 1, Convert.ToInt32(ExploreAddedArea_MaxPoint.X - ExploreAddedArea_MinPoint.X) + 1];      //// int[boundingbox.Height, boundingbox.Width];

                int AddedAreaPixelNumber = 0;

                double[] slope = new double[4];
                double[] b = new double[4];
                for (int i = 0; i < AddedAreaVertices.Length; i++)    //// find four lines that create min area rectangle bounding box
                {
                    if (i < AddedAreaVertices.Length - 1)
                        slope[i] = (AddedAreaVertices[i].Y - AddedAreaVertices[i + 1].Y) / (AddedAreaVertices[i].X - AddedAreaVertices[i + 1].X);

                    else
                        slope[i] = (AddedAreaVertices[i].Y - AddedAreaVertices[0].Y) / (AddedAreaVertices[i].X - AddedAreaVertices[0].X);

                    b[i] = -(AddedAreaVertices[i].Y - slope[i] * AddedAreaVertices[i].X);
                }

                Rectangle boundingbox = new Rectangle();

                boundingbox.X = (int)AddedAreaVertices[1].X;
                boundingbox.Y = (int)AddedAreaVertices[2].Y;

                boundingbox.Width = (int)(AddedAreaVertices[3].X - AddedAreaVertices[1].X);
                boundingbox.Height = (int)(AddedAreaVertices[0].Y - AddedAreaVertices[2].Y);

                bool[,] _dstimg = new bool[boundingbox.Height, boundingbox.Width];
                bool[,] _dstimg_HoleDetection = new bool[boundingbox.Height, boundingbox.Width];
                //int ForegroundPixels = 0;

                BitmapData _srcData;

                if (boundingbox.X + boundingbox.Width > OriginalBinaryImage.Width)
                    boundingbox.Width = OriginalBinaryImage.Width - boundingbox.X;
                if (boundingbox.X < 0)
                {
                    boundingbox.Width += boundingbox.X;
                    boundingbox.X = 0;
                }

                if (boundingbox.Y + boundingbox.Height > OriginalBinaryImage.Height)
                    boundingbox.Height = OriginalBinaryImage.Height - boundingbox.Y;
                if (boundingbox.Y < 0)
                {
                    boundingbox.Height += boundingbox.Y;
                    boundingbox.Y = 0;
                }

                _srcData = OriginalBinaryImage.LockBits(
                          boundingbox,
                          ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

                //unsafe
                //{
                    int offset1 = (_srcData.Stride - boundingbox.Width * 3);
                    // do the job
                    unsafe
                    {
                        byte* src = (byte*)_srcData.Scan0.ToPointer();

                        // for each row
                        for (int y = 0; y < boundingbox.Height; y += 1)
                        {
                            // for each pixel
                            for (int x = 0; x < boundingbox.Width; x++, src += 3)
                            {
                                if ((x + boundingbox.X < OriginalBinaryImage.Width) && (y + boundingbox.Y < OriginalBinaryImage.Height))
                                {

                                    double[] result = new double[4];
                                    for (int l = 0; l < result.Length; l++)
                                        result[l] = (y + boundingbox.Y) - slope[l] * (x + boundingbox.X) + b[l];

                                    if (result[0] < 0 && result[1] > 0 && result[2] > 0 && result[3] < 0)   /// explore if pixel is in bounding box of Min Area Rectangle
                                    {

                                        if ((src[RGB.R] == 0) &&
                                            (src[RGB.G] == 0) &&
                                            (src[RGB.B] == 0))
                                        {
                                            //AddedAreaPoints[y, x] = 1;
                                            AddedAreaPixelNumber++;
                                            _dstimg[y, x] = true;
                                            _dstimg_HoleDetection[y, x] = false;
                                        }
                                        else
                                        {
                                            //AddedAreaPoints[y, x] = 0;
                                            _dstimg[y, x] = false;
                                            _dstimg_HoleDetection[y, x] = true;
                           
                                        }

                                        if ((x + boundingbox.X) == Convert.ToInt32(ExploreAddedArea_MinPoint.X) || (y + boundingbox.Y) == Convert.ToInt32(ExploreAddedArea_MinPoint.Y) || (x + boundingbox.X) == Convert.ToInt32(ExploreAddedArea_MaxPoint.X) || (y + boundingbox.Y) == Convert.ToInt32(ExploreAddedArea_MaxPoint.Y))
                                        {
                                            if (((x + boundingbox.X) - Convert.ToInt32(ExploreAddedArea_MinPoint.X)) > 0 && (y + boundingbox.Y) - Convert.ToInt32(ExploreAddedArea_MinPoint.Y) > 0 && ((x + boundingbox.X) - Convert.ToInt32(ExploreAddedArea_MinPoint.X)) > boundingbox.Width && (y + boundingbox.Y) - Convert.ToInt32(ExploreAddedArea_MinPoint.Y)>boundingbox.Height)
                                            _dstimg_HoleDetection[(y + boundingbox.Y) - Convert.ToInt32(ExploreAddedArea_MinPoint.Y), (x + boundingbox.X) - Convert.ToInt32(ExploreAddedArea_MinPoint.X)] = true;
                                        }
                                    }
                                }
                            }
                            src += offset1;
                        }
                    }

             //   }

                OriginalBinaryImage.UnlockBits(_srcData);

                Bitmap img = ImageUtils.Array2DToBitmap(_dstimg);
                Bitmap img1 = ImageUtils.Array2DToBitmap(_dstimg_HoleDetection);
                
                AddedAreaPixelToSizeRatio = (double)(AddedAreaPixelNumber) / (addedArea.size.Height * addedArea.size.Width);
                if (AddedAreaPixelToSizeRatio > StraboParameters.PixelToSizeRatio)
                {
                    PixelsRatio_Character[0] = true;
                    if (NonHorizontal_Ischaracter(img, img1, addedArea))
                        PixelsRatio_Character[1] = true;
                    else
                        PixelsRatio_Character[1] = false;
                }
                else
                {
                    PixelsRatio_Character[0] = false;
                    PixelsRatio_Character[1] = false;
                }

            }

            catch (Exception exception)
            {
                Log.Write("Nonhorizontal Area Extraction Error "+exception.Message);
            }
            return PixelsRatio_Character;
        }
        public bool NonHorizontal_Ischaracter(Bitmap AddedAreaPoints, Bitmap AddedAreaPointsForHole, Emgu.CV.Structure.MCvBox2D addedArea)
        {
            PotentialTextAreaExtractation Area = new PotentialTextAreaExtractation();
            Bitmap NeighborhoodConnectedComponent;
            Bitmap NeighborhoodHoles;
            MyConnectedComponentsAnalysisFast.MyBlobCounter blobs = new MyConnectedComponentsAnalysisFast.MyBlobCounter();
            List<MyConnectedComponentsAnalysisFast.MyBlob> holes_char_blobs;
            List<MyConnectedComponentsAnalysisFast.MyBlob> char_blobs;

            try
            {
                AddNeighborhood newClass = new AddNeighborhood();
                NeighborhoodConnectedComponent = AddedAreaPoints;
                NeighborhoodHoles = AddedAreaPointsForHole;
                holes_char_blobs = blobs.GetBlobs(NeighborhoodHoles);  //// hole detection
                NeighborhoodConnectedComponent = ImageUtils.InvertColors(NeighborhoodConnectedComponent);
                char_blobs = blobs.GetBlobs(NeighborhoodConnectedComponent);

                if (holes_char_blobs.Count > StraboParameters.HoleThreshold)
                    return false;

                if (char_blobs.Count > StraboParameters.ConnectedComponentThreshold)
                    return false;


                for (int i = 0; i < char_blobs.Count; i++)
                {
                    int MaxSize = 0;
                    if (char_blobs[i].bbx.Width == addedArea.size.Width || char_blobs[i].bbx.Height == addedArea.size.Height)
                    {
                        if (char_blobs[i].bbx.Width == addedArea.size.Width)
                            MaxSize = char_blobs[i].bbx.Width;
                        else
                            MaxSize = char_blobs[i].bbx.Height;
                        double AveragePixel = (double)char_blobs[i].pixel_count / (double)MaxSize;
                        if (AveragePixel < StraboParameters.PixelToSizeRatio)
                            return false;
                    }
                }
            }
            catch (Exception exception)
            {
                Log.Write(exception.Message);
            }

            return true;
        }

        public List<NeighborhoodResult> NonHorizontalTextLabel(List<PointF> points, int x, int y, string RecognizedText, int imageID, Bitmap OriginalBinaryImage, List<NeighborhoodResult> Neighbors)
        {
            PointF[] pts = points.ToArray();
            Boolean Continue = true;
            double AddedAreaPixelToSizeRatio = 1;
            int NumberofNeighbors = 0;
            int numberOfRecognizedLetters = RecognizedText.Length;
            bool[] text = new bool[2];
            bool TextLastExploredArea = true;
            double pixelToSizeRatio;
            string TotalPixelToSizeRatio;

            try
            {
                Emgu.CV.Structure.MCvBox2D box = PointCollection.MinAreaRect(pts);
                Rectangle boundingbox = PointCollection.BoundingRectangle(pts);
                pixelToSizeRatio = (double)(points.Count) / (double)(box.size.Width * box.size.Height);
                TotalPixelToSizeRatio = Convert.ToString(Math.Round((decimal)pixelToSizeRatio, 2));

                Emgu.CV.Structure.MCvBox2D boxinoriginalImage = box;
                boxinoriginalImage.center.X += x - boundingbox.X;   /// 63=originalboundingbox.x(1069)(I used result of intermediate image)-boundingbox.x(21)
                boxinoriginalImage.center.Y += y - boundingbox.Y;
                PointF[] vertices = boxinoriginalImage.GetVertices();


                for (int i = 0; i < 2; i++)   ///// if i=0 : we are exploring the down part of non-horizontal text label. if i=1 : we are are exploring the the top of non-horizontal text label.  
                {
                    NumberofNeighbors = 1;
                    Continue = true;
                    while (Continue)
                    {
                        NeighborhoodResult neighbor;
                        Emgu.CV.Structure.MCvBox2D addedArea;

                        if (i == 0)
                            addedArea = NonHorizontalDownAreaDetecteion(box, boundingbox, RecognizedText, NumberofNeighbors, x, y);
                        else
                            addedArea = NonHorizontalUpAreaDetecteion(box, boundingbox, RecognizedText, NumberofNeighbors, x, y);

                        if (addedArea.angle == 0 && addedArea.center.X == 0 && addedArea.center.Y == 0)
                        {
                            Continue = false;
                            break;
                        }

                        text = NonHorizontalAreaExtraction(addedArea, OriginalBinaryImage);
                        PointF[] AddedAreaVertices = addedArea.GetVertices();   //// find all vertices of potential character area

                        for (int j = 0; j < 4; j++)
                        {
                            if (AddedAreaVertices[j].X < 0 || AddedAreaVertices[j].Y < 0 || AddedAreaVertices[j].X > OriginalBinaryImage.Width || AddedAreaVertices[j].Y > OriginalBinaryImage.Height)
                                Continue = false;  //// out of image
                        }

                        string TextNonText;  /// fix this part

                        if (!text[0] && TextLastExploredArea)   //// if pixel-to-size ratio is less than threshold and the last explored area is text 
                            break;
                        else if (!text[1] && !TextLastExploredArea)  //// if the current potential area is not character and the last explored area is not text
                        {
                            Neighbors.RemoveAt(Neighbors.Count - 1);   //// remove the last element of Neighbors list
                            NumberofNeighbors--;
                            break;
                        }
                        else if (!text[1] && TextLastExploredArea)
                        {
                            TextNonText = "Non-Text";
                            TextLastExploredArea = false;
                        }
                        else
                            TextNonText = "Text";

                        if ((i == 0 && box.size.Height < box.size.Width) || (i == 1 && box.size.Height > box.size.Width))   //// left neighbors
                          neighbor = new NeighborhoodResult(AddedAreaVertices[0].X, AddedAreaVertices[0].Y, AddedAreaVertices[1].X, AddedAreaVertices[1].Y, AddedAreaVertices[2].X,
                                AddedAreaVertices[2].Y, AddedAreaVertices[3].X, AddedAreaVertices[3].Y, Convert.ToString(imageID) + "-left-" + Convert.ToString(NumberofNeighbors), Convert.ToString(imageID), Convert.ToString(Math.Round((decimal)AddedAreaPixelToSizeRatio, 2)), TextNonText);
                        else    ////// right neighbors
                            neighbor = new NeighborhoodResult(AddedAreaVertices[0].X, AddedAreaVertices[0].Y, AddedAreaVertices[1].X, AddedAreaVertices[1].Y, AddedAreaVertices[2].X,
                                  AddedAreaVertices[2].Y, AddedAreaVertices[3].X, AddedAreaVertices[3].Y, Convert.ToString(imageID) + "-right-" + Convert.ToString(NumberofNeighbors), Convert.ToString(imageID), Convert.ToString(Math.Round((decimal)AddedAreaPixelToSizeRatio, 2)), TextNonText);

                        Neighbors.Add(neighbor);

                        NumberofNeighbors++;
                    }
                }

            }
            catch (Exception exception)
            {
                Log.Write(exception.Message);
                throw;
            }

            return Neighbors;
        }

        public Bitmap NonHorizontalReadImage(int[,] AreaPoints, PointF[] AreaVertices, Bitmap OriginalBinaryImage)
        {

            PotentialTextAreaExtractation Area = new PotentialTextAreaExtractation();
            double[] slope = new double[4];
            double[] b = new double[4];
            for (int k = 0; k < AreaVertices.Length; k++)    //// find four lines that create min area rectangle bounding box
            {
                if (k < AreaVertices.Length - 1)
                    slope[k] = (AreaVertices[k].Y - AreaVertices[k + 1].Y) / (AreaVertices[k].X - AreaVertices[k + 1].X);

                else
                    slope[k] = (AreaVertices[k].Y - AreaVertices[0].Y) / (AreaVertices[k].X - AreaVertices[0].X);

                b[k] = -(AreaVertices[k].Y - slope[k] * AreaVertices[k].X);
            }


            Rectangle boundingbox = new Rectangle();

            boundingbox.X = (int)AreaVertices[1].X;
            boundingbox.Y = (int)AreaVertices[2].Y;

            boundingbox.Width = (int)(AreaVertices[3].X - AreaVertices[1].X);
            boundingbox.Height = (int)(AreaVertices[0].Y - AreaVertices[2].Y);


            if (boundingbox.X + boundingbox.Width > OriginalBinaryImage.Width)
                boundingbox.Width = OriginalBinaryImage.Width - boundingbox.X;
            if (boundingbox.X < 0)
            {
                boundingbox.Width += boundingbox.X;
                boundingbox.X = 0;
            }

            if (boundingbox.Y + boundingbox.Height > OriginalBinaryImage.Height)
                boundingbox.Height = OriginalBinaryImage.Height - boundingbox.Y;
            if (boundingbox.Y < 0)
            {
                boundingbox.Height += boundingbox.Y;
                boundingbox.Y = 0;
            }

            bool[,] _dstimg = new bool[boundingbox.Height, boundingbox.Width];
            //int ForegroundPixels = 0;

            BitmapData _srcData;


            _srcData = OriginalBinaryImage.LockBits(
                      boundingbox,
                      ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

            //unsafe
            //{
                int offset1 = (_srcData.Stride - boundingbox.Width * 3);
                // do the job
                unsafe
                {
                    byte* src = (byte*)_srcData.Scan0.ToPointer();

                    // for each row
                    for (int y = 0; y < boundingbox.Height; y += 1)
                    {
                        // for each pixel
                        for (int x = 0; x < boundingbox.Width; x++, src += 3)
                        {
                            if ((x + boundingbox.X < OriginalBinaryImage.Width) && (y + boundingbox.Y < OriginalBinaryImage.Height))
                            {

                                double[] result = new double[4];
                                for (int l = 0; l < result.Length; l++)
                                    result[l] = (y + boundingbox.Y) - slope[l] * (x + boundingbox.X) + b[l];

                                if (result[0] < 0 && result[1] > 0 && result[2] > 0 && result[3] < 0)   /// explore if pixel is in bounding box of Min Area Rectangle
                                {

                                    if ((src[RGB.R] == 0) &&
                                        (src[RGB.G] == 0) &&
                                        (src[RGB.B] == 0))
                                    {
                                        //AddedAreaPoints[y, x] = 1;
                                        _dstimg[y, x] = true;
                                        AreaPoints[y, x] = 1;
                                        // ForegroundPixels++;
                                    }
                                    else
                                    {
                                        //AddedAreaPoints[y, x] = 0;
                                        _dstimg[y, x] = false;
                                        AreaPoints[y, x] = 0;
                                    }
                                }
                            }
                        }
                        src += offset1;                        
                    }
                }

          //  }

            OriginalBinaryImage.UnlockBits(_srcData);

            Bitmap img = ImageUtils.Array2DToBitmap(_dstimg);
           // img.Save(@"C:\Users\narges\Documents\test\test(Non-Horizontal).png");
            //AddedAreaResults Area = new AddedAreaResults(img, ForegroundPixels);


            //for (int k = Convert.ToInt32(AreaVertices[1].X); k < Convert.ToInt32(AreaVertices[3].X) + 1; k++)
            //{
            //    for (int j = Convert.ToInt32(AreaVertices[2].Y); j < Convert.ToInt32(AreaVertices[0].Y) + 1; j++)
            //    {
            //        if ((k < OriginalBinaryImage.Width) && (j < OriginalBinaryImage.Width) && (k > 0) && (j > 0))
            //        {
            //           // Color color = OriginalBinaryImage.GetPixel(k, j);
            //            double[] result = new double[4];
            //            for (int l = 0; l < result.Length; l++)
            //                result[l] = j - slope[l] * k + b[l];

            //            if (result[0] < 0 && result[1] > 0 && result[2] > 0 && result[3] < 0)   /// explore if pixel is in bounding box of Min Area Rectangle
            //            {

            //                if (!OriginalBinaryImage.GetPixel(k, j).Name.Equals("ffffffff"))
            //                    AreaPoints[j - Convert.ToInt32(AreaVertices[2].Y), k - Convert.ToInt32(AreaVertices[1].X)] = 1;

            //                else
            //                    AreaPoints[j - Convert.ToInt32(AreaVertices[2].Y), k - Convert.ToInt32(AreaVertices[1].X)] = 0;
            //            }

            //        }
            //        else
            //            break;

            //    }
            //}

            return img;
            //return Area.FromArrayToImage(AreaPoints);

        }
    }
}
