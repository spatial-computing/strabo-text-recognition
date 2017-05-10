using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using Strabo.Core.Utility;
using Accord.Math;
using Emgu.CV;
using Strabo.Core.ImageProcessing;

namespace Strabo.Core.TextRecognition
{
    class HorizontalTextExtraction
    {
        public bool IsCharacter(Bitmap PotentialAreaPoints)
        {
            // PotentialTextAreaExtractation Area = new PotentialTextAreaExtractation();
            // Bitmap Neighborhood = Area.FromArrayToImage(PotentialAreaPoints);
            Bitmap Neighborhood = PotentialAreaPoints;
            MyConnectedComponentsAnalysisFast.MyBlobCounter blobs = new MyConnectedComponentsAnalysisFast.MyBlobCounter();

            //if (Neighborhood.Width < 2 && Neighborhood.Height < 2)
            //    return false;

            try
            {
                if (Neighborhood.Width == 1 || Neighborhood.Height == 1)
                    return false;
                List<MyConnectedComponentsAnalysisFast.MyBlob> holes_char_blobs = blobs.GetBlobs(Neighborhood);  //// hole detection

                /// Neighborhood.Save(@"C:\Users\narges\Documents\test\FALSE-HOLE.PNG");

                if (holes_char_blobs.Count > StraboParameters.HoleThreshold)
                    return false;

                Neighborhood = ImageUtils.InvertColors(Neighborhood);
                List<MyConnectedComponentsAnalysisFast.MyBlob> char_blobs = blobs.GetBlobs(Neighborhood);

                if (char_blobs.Count > StraboParameters.ConnectedComponentThreshold)
                    return false;

                for (int i = 0; i < char_blobs.Count; i++)
                {
                    float MaxSize = 0;
                    if (char_blobs[i].bbx.width() == Neighborhood.Width || char_blobs[i].bbx.height() == Neighborhood.Height)
                    {
                        if (char_blobs[i].bbx.width() == Neighborhood.Width)
                            MaxSize = char_blobs[i].bbx.width();
                        else
                            MaxSize = char_blobs[i].bbx.height();
                        double AveragePixel = (double)char_blobs[i].pixel_count / (double)MaxSize;
                        if (AveragePixel < StraboParameters.AveragePixel)
                        {
                            return false;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.WriteLine("IsCharacter function (Horizontal Text Label) error" + e.Message);
            }
            return true;
        }


        public List<NeighborhoodResult> Apply(int width, int height, int x, int y, string RecognizedText, string imageID, Bitmap OriginalBinaryImage, List<NeighborhoodResult> Neighbors)
        {
            Boolean Continue = true;
            Boolean TextLastExploredArea = true;
            int NumberOfNeighbors = 0;
            //int NumberOfRightNeighbors = 0;
            //int NumberOfLeftNeighbors = 0;
            Point point = new Point();
            double PixelToSizeRatio = 1;
            int AddedAreaWidth;
            int numberOfRecognizedLetters;
            int estimatedWidthOfOneLetter;
            double HeightAddition;
            PotentialTextAreaExtractation Area = new PotentialTextAreaExtractation();

            try
            {
                numberOfRecognizedLetters = RecognizedText.Length;
                estimatedWidthOfOneLetter = width / numberOfRecognizedLetters;
                HeightAddition = Area.AddToHeight(RecognizedText, Convert.ToDouble(height));
                if ((y - HeightAddition) < 0)
                    HeightAddition = y;

                for (int i = 0; i < 2; i++)           ///// i=0 is for exploring the left neighborhoods and i=1 is for the right neighborhood
                {
                    Continue = true;
                    TextLastExploredArea = true;
                    NumberOfNeighbors = 0;


                    if (!char.IsUpper(RecognizedText.ToCharArray()[0]) && i == 0)
                        AddedAreaWidth = Convert.ToInt32(estimatedWidthOfOneLetter * 1.3);
                    else
                        AddedAreaWidth = Convert.ToInt32(estimatedWidthOfOneLetter);

                    if (i == 0)
                        point.X = x - AddedAreaWidth;
                    else
                        point.X = x + width;


                    point.Y = y - Convert.ToInt32(HeightAddition);


                    if ((Area.ContinueFromLeft(RecognizedText) && i == 0) || (i == 1))   //// i=0 for exploring the left neighbors and i=1 for exploring the right neighbors 
                    {
                        while (Continue)
                        {

                            if (point.X < 0 || point.X + estimatedWidthOfOneLetter > OriginalBinaryImage.Width)   ///// if some part of neighborhood results is outside of boundry of image
                            {
                                if (i == 0)   /// left neighbors
                                {
                                    if (char.IsUpper(RecognizedText.ToCharArray()[0]))

                                        AddedAreaWidth = point.X + Convert.ToInt32(estimatedWidthOfOneLetter);
                                    else
                                        AddedAreaWidth = point.X + Convert.ToInt32(estimatedWidthOfOneLetter * 1.3);

                                    point.X = 0;
                                }
                                else
                                    AddedAreaWidth = OriginalBinaryImage.Width - point.X;

                                Continue = false;
                                if (AddedAreaWidth <= 0)
                                    break;
                            }


                            Rectangle boundingbox = new Rectangle(point.X, point.Y, AddedAreaWidth, height + Convert.ToInt32(HeightAddition));

                            AddedAreaResults AddedArea = Area.FromImageToArray(OriginalBinaryImage, boundingbox);
                            //int[,] AddedAreaPoints = Area.FromImageToArray(OriginalBinaryImage, boundingbox);
                            ///AddedArea.Area.Save(@"C:\Users\narges\Documents\test\false.png");

                            PixelToSizeRatio = (double)(AddedArea.AreaForeGoundPixels) / (double)(AddedAreaWidth * (height + HeightAddition));
                            string TextNonText = "";

                            if (PixelToSizeRatio < StraboParameters.PixelToSizeRatio && TextLastExploredArea)
                                break;
                            else if (PixelToSizeRatio < StraboParameters.PixelToSizeRatio && !TextLastExploredArea)
                            {
                                Neighbors.RemoveAt(Neighbors.Count - 1);
                                NumberOfNeighbors--;
                                break;
                            }

                            else if (!IsCharacter(AddedArea.Area) && !TextLastExploredArea)
                            {
                                Neighbors.RemoveAt(Neighbors.Count - 1);
                                NumberOfNeighbors--;
                                break;
                            }
                            else if (!IsCharacter(AddedArea.Area) && TextLastExploredArea)
                            {
                                TextNonText = "Non-Text";
                                TextLastExploredArea = false;
                            }
                            else
                            {
                                TextNonText = "Text";
                                TextLastExploredArea = true;
                            }
                            NeighborhoodResult neighbor;

                            if (i == 0)
                                neighbor = new NeighborhoodResult(point.X, point.Y, point.X + AddedAreaWidth, point.Y, point.X + AddedAreaWidth, point.Y + height + HeightAddition, point.X, point.Y + height + HeightAddition, imageID + "-left-" + Convert.ToString(NumberOfNeighbors), imageID, Convert.ToString(Math.Round((decimal)PixelToSizeRatio, 2)), TextNonText);
                            else
                                neighbor = new NeighborhoodResult(point.X, point.Y, point.X + AddedAreaWidth, point.Y, point.X + AddedAreaWidth, point.Y + height + HeightAddition, point.X, point.Y + height + HeightAddition, imageID + "-right-" + Convert.ToString(NumberOfNeighbors), imageID, Convert.ToString(Math.Round((decimal)PixelToSizeRatio, 2)), TextNonText);

                            Neighbors.Add(neighbor);

                            if (i == 0)
                                point.X -= AddedAreaWidth;

                            if (i == 1)
                                point.X += estimatedWidthOfOneLetter;

                            NumberOfNeighbors++;
                        }
                    }

                }


                if (NumberOfNeighbors == 0 && !RemoveNoiseText.NotTooManyNoiseCharacters(RecognizedText))
                    Neighbors.RemoveAt(Neighbors.Count - 1);


            }
            catch (Exception exception)
            {
                Log.Write("Horizontal Text Detection Error" + exception.Message);
            }
            return Neighbors;
        }
    }
}
