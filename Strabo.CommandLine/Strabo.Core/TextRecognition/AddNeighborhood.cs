using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using Strabo.Core.Utility;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Accord.Math;
using Strabo.Core.ImageProcessing;
using AForge.Imaging;
using System.Collections;
using System.Drawing.Imaging;
using Strabo.Core.Utility;
using Strabo.Core.ImageProcessing;





namespace Strabo.Core.TextRecognition
{

    public class MeetingAreas
    {
        public int MinX;
        public int MaxX;
        public int MinY;
        public int MaxY;

        public int Width;  ///// use if horizental
        public int Height;  ///// use if vertical

        public float centerX;
        public float centerY;

    }
    public class NeighborhoodResult
    {
        public double LeftUp_X;
        public double LeftUp_Y;

        public double RightUp_X;
        public double RightUp_Y;

        public double RightDown_X;
        public double RightDown_Y;

        public double LeftDown_X;
        public double LeftDown_Y;

       public PointF MinX;
       public PointF MinY;

       public PointF MaxX;
       public PointF MaxY;

       public string ImageID;
       public string OriginalImageID;
       public string PixelToSizeRatio;
       public string Text;

       public PointF[] Vertices;

       public NeighborhoodResult(double x1, double y1, double x2, double y2, double x3, double y3, double x4, double y4, string imageID, string originalImageID, string pixelToSizeRatio, string textNonText)
        {
            Vertices = new PointF[4];

            Vertices[0].X = (float)x1;
            Vertices[0].Y = (float)y1;


            Vertices[1].X = (float)x2;
            Vertices[1].Y = (float)y2;

            Vertices[2].X = (float)x3;
            Vertices[2].Y = (float)y3;


            Vertices[3].X = (float)x4;
            Vertices[3].Y = (float)y4;


            MinX = Vertices[0];
            MaxX = Vertices[0];

            MinY = Vertices[0];
            MaxY = Vertices[0];

            for (int i = 1; i < Vertices.Length; i++)
            {
                if (Vertices[i].X < MinX.X)
                    MinX = Vertices[i];

                if (Vertices[i].Y < MinY.Y)
                    MinY = Vertices[i];

                if (Vertices[i].X > MaxX.X)
                    MaxX = Vertices[i];

                if (Vertices[i].Y > MaxY.Y)
                    MaxY = Vertices[i];
            }


            ImageID = imageID;
            OriginalImageID = originalImageID;
            PixelToSizeRatio = pixelToSizeRatio;
            Text = textNonText;
        }
    }

    public class ArrayImageResult
    {
        public int[,] ArrayOfImage;
        string PixelToSizeRaio;

        public ArrayImageResult() { }

    }

    class AddNeighborhood
    {
        List<NeighborhoodResult> Neighbors = new List<NeighborhoodResult>();
        Bitmap OriginalBinaryImage;

        public void Apply(List<TessResult> tessOcrResultList, string OTDPath, string inputpath)
        {
            int x, y;
            int width;
            int height;
            int angle;
            String[] splitTokens;
            string TotalPixelToSizeRatio;
            string file_name = "";
            List<PointF> points = new List<PointF>();
            tessOcrResultList = EditTesseractResults(tessOcrResultList, inputpath);

            try
            {
                Log.WriteLine("OTD in progress...");
                for (int k = 0; k < tessOcrResultList.Count; k++)  /// add neighbors of each bounding box
                {
                    points = new List<PointF>();
                    TotalPixelToSizeRatio = "";
                    file_name = tessOcrResultList[k].fileName;
                    points = ReadImage(inputpath, file_name);

                    //if (k == 78)
                    //    Console.WriteLine("debug");

                    OriginalBinaryImage = new Bitmap(inputpath + StraboParameters.textLayerOutputFileName);
                    splitTokens = file_name.Split('_');
                    angle = Convert.ToInt32(splitTokens[6]);
                    //if (k == 14)
                    //    Console.WriteLine();

                    x = Convert.ToInt32(tessOcrResultList[k].x);
                    y = Convert.ToInt32(tessOcrResultList[k].y);
                    width = Convert.ToInt32(tessOcrResultList[k].w);
                    height = Convert.ToInt32(tessOcrResultList[k].h);
                    PotentialTextAreaExtractation Area = new PotentialTextAreaExtractation();

                    if (tessOcrResultList[k].tess_word3 == "")
                        continue;


                    if (angle < 10 || (360 - angle) < 10)     /// horizontal bounding box
                    {
                        Rectangle boundingbox;

                        boundingbox = new Rectangle(x, y, width, height);
                        AddedAreaResults AddedArea = Area.FromImageToArray(OriginalBinaryImage, boundingbox);
                       // int[,] TextLabelAreaPoints = Area.FromImageToArray(OriginalBinaryImage, boundingbox);

                        double PixelToSizeRatio = (double)(AddedArea.AreaForeGoundPixels) / (double)(width * (height));

                        NeighborhoodResult Neighbor = new NeighborhoodResult(x, y, x + width, y, x + width, y + height, x, y + height, tessOcrResultList[k].id, tessOcrResultList[k].fileName, Convert.ToString(PixelToSizeRatio), tessOcrResultList[k].tess_word3);
                        Neighbors.Add(Neighbor);   //////// in this part I add the result of strabo (not neighborhoods) to Neighbors list

                        HorizontalTextExtraction AreaDetection = new HorizontalTextExtraction();
                        Neighbors = AreaDetection.Apply(width, height, x, y, tessOcrResultList[k].tess_word3.Substring(0, tessOcrResultList[k].tess_word3.Length ), tessOcrResultList[k].id, OriginalBinaryImage, Neighbors);

                    }
                    else
                    {
                        PointF[] pts;
                        pts = points.ToArray();
                        Emgu.CV.Structure.MCvBox2D box = PointCollection.MinAreaRect(pts);
                        Rectangle boundingbox = PointCollection.BoundingRectangle(pts);

                        Emgu.CV.Structure.MCvBox2D boxinoriginalImage = box;
                        boxinoriginalImage.center.X += x - boundingbox.X;
                        boxinoriginalImage.center.Y += y - boundingbox.Y;
                        PointF[] vertices = boxinoriginalImage.GetVertices();
                        double pixelToSizeRatio = (double)(points.Count) / (double)(box.size.Width * box.size.Height);
                        TotalPixelToSizeRatio = Convert.ToString(Math.Round((decimal)pixelToSizeRatio, 2));

                        NeighborhoodResult Neighbor = new NeighborhoodResult(vertices[0].X, vertices[0].Y, vertices[1].X, vertices[1].Y, vertices[2].X, vertices[2].Y, vertices[3].X, vertices[3].Y, tessOcrResultList[k].id, tessOcrResultList[k].fileName, TotalPixelToSizeRatio, tessOcrResultList[k].tess_word3);
                        Neighbors.Add(Neighbor);  //////// in this part I add the result of strabo (not neighborhoods) to Neighbors list

                        NonHorizontalTextExtraction AreaDetection = new NonHorizontalTextExtraction();
                        AreaDetection.NonHorizontalTextLabel(points, x, y, tessOcrResultList[k].tess_word3.Substring(0, tessOcrResultList[k].tess_word3.Length), Convert.ToInt16(tessOcrResultList[k].id), OriginalBinaryImage, Neighbors);
                    }
                }

                CropNewTextLabels(inputpath, OTDPath);

                OriginalBinaryImage.Dispose();

                for (int i = 0; i < Neighbors.Count; i++)
                    QGISJsonNeighbothood.AddFeature(Neighbors[i].LeftUp_X, Neighbors[i].LeftUp_Y, Neighbors[i].RightUp_X, Neighbors[i].RightUp_Y, Neighbors[i].RightDown_X, Neighbors[i].RightDown_Y, Neighbors[i].LeftDown_X, Neighbors[i].LeftDown_Y, Neighbors[i].ImageID, Neighbors[i].PixelToSizeRatio, Neighbors[i].Text, -1);

                QGISJsonNeighbothood.WriteGeojsonFileByPixels();
                Log.WriteLine("OTD Finished");

            }
            catch (Exception exception)
            {
                Log.Write(" Add Potential Text Area(Neighborhood) Failed" + exception.Message);

            }
        }

       


        public List<TessResult> EditTesseractResults(List<TessResult> tessResults, string inputpath)
        {
            PotentialTextAreaExtractation Area = new PotentialTextAreaExtractation();

            try
            {
                for (int i = 0; i < tessResults.Count; i++)
                {
                    if (tessResults[i].hocr == null)
                        continue;
                    string[] HOCRsplit = tessResults[i].hocr.Split(new string[] { "line" }, StringSplitOptions.None);
                    if (HOCRsplit != null && HOCRsplit.Length > 3)   // && tessResults[i].fileName.Split('_')[6]=="0")
                    {

                        int NumberOfLines = (HOCRsplit.Length - 1) / 2;
                        for (int j = 1; j < NumberOfLines + 1; j++)
                        {
                            string BoundingBox = HOCRsplit[2 * j].Split(new string[] { "bbox " }, StringSplitOptions.None)[1];
                            BoundingBox = BoundingBox.Split('\"')[0];

                            int bbx_X = Convert.ToInt32(BoundingBox.Split(' ')[0]) - 20 + tessResults[i].x;
                            int bbx_Y = Convert.ToInt32(BoundingBox.Split(' ')[1]) - 20 + tessResults[i].y;
                            int bbx_Width = Convert.ToInt32(BoundingBox.Split(' ')[2]) - Convert.ToInt32(BoundingBox.Split(' ')[0]);
                            int bbx_Height = Convert.ToInt32(BoundingBox.Split(' ')[3]) - Convert.ToInt32(BoundingBox.Split(' ')[1]);

                            Bitmap OriginalBinaryImage = new Bitmap(inputpath + StraboParameters.textLayerOutputFileName);  //TextLayerOutputFileName

                            if (bbx_X > OriginalBinaryImage.Width || bbx_Y > OriginalBinaryImage.Height)
                            {
                                //tessResults.RemoveAt(i);
                                //i--;
                                continue;
                            }

                            Rectangle boundingbox = new Rectangle(bbx_X, bbx_Y, bbx_Width, bbx_Height);

                            

                                
                            //int[,] AddedAreaPoints = Area.FromImageToArray(OriginalBinaryImage, boundingbox);
                            //Bitmap NewBBx = Area.FromArrayToImage(AddedAreaPoints);
                            Bitmap NewBBx = Area.FromImageToArray(OriginalBinaryImage, boundingbox).Area;
                            string[] ParentfileName = tessResults[i].fileName.Split('_');

                            ParentfileName[0] += "(" + j + ")";
                            ParentfileName[6] = "0";

                            ParentfileName[7] = Convert.ToString(bbx_X);
                            ParentfileName[8] = Convert.ToString(bbx_Y);
                            ParentfileName[9] = Convert.ToString(bbx_Width);
                            ParentfileName[10] = Convert.ToString(bbx_Height);

                            string fileName = String.Join("_", ParentfileName);

                            TessResult tr = new TessResult();
                            if (tessResults[i].tess_raw3.Contains("\n\n"))
                            {
                                string temp = "";
                                temp=tessResults[i].tess_raw3.Replace("\n\n", "\n");
                                tessResults[i].tess_raw3 = temp;
                            }

                            tr.tess_word3 = tessResults[i].tess_raw3.Split('\n')[j - 1];
                            tr.tess_cost3 = tessResults[i].tess_cost3;
                            tr.id = fileName.Split('_')[0];
                            tr.fileName = fileName;

                            tr.x = bbx_X;
                            tr.y = bbx_Y;
                            tr.w = bbx_Width;
                            tr.h = bbx_Height;



                            tessResults.Add(tr);

                            ImageStitcher imgstitcher1 = new ImageStitcher();
                            NewBBx = imgstitcher1.ExpandCanvas(NewBBx, 20);
                            NewBBx.Save(inputpath + fileName + ".png");
                        }

                        tessResults.RemoveAt(i);
                        i--;

                    }
                }
            }
            catch (Exception exception)
            {
                Log.Write("Edit Text Lable Problem " + exception.Message);
            }

            return tessResults;
        }

        public List<PointF> ReadImage(string inputpath, string file_name)
        {
            List<PointF> points = new List<PointF>();
            Bitmap ResultOfTextDetection;

            string[] split;
            int angle;

            try
            {
                split = file_name.Split('_');
                angle = Convert.ToInt32(split[6]);

                if (angle < 10 || (360 - angle) < 10)
                    ResultOfTextDetection = new Bitmap(inputpath + file_name + ".png");
                else
                {
                    string newFileName = "";
                    split[6] = "0";
                    newFileName = string.Join("_", split);
                    ResultOfTextDetection = new Bitmap(inputpath + newFileName + ".png");
                }

                BitmapData _srcData;
                _srcData = ResultOfTextDetection.LockBits(
                          new Rectangle(0, 0, ResultOfTextDetection.Width, ResultOfTextDetection.Height),
                          ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

                unsafe
                {
                    int offset1 = (_srcData.Stride - ResultOfTextDetection.Width * 3);
                    // do the job
                    unsafe
                    {
                        byte* src = (byte*)_srcData.Scan0.ToPointer();

                        // for each row
                        for (int y = 0; y < ResultOfTextDetection.Height; y += 1)
                        {
                            // for each pixel
                            for (int x = 0; x < ResultOfTextDetection.Width; x++, src += 3)
                            {
                                if ((src[Strabo.Core.ImageProcessing.RGB.R] == 0) &&
                                    (src[Strabo.Core.ImageProcessing.RGB.G] == 0) &&
                                    (src[Strabo.Core.ImageProcessing.RGB.B] == 0))
                                {
                                    PointF point = new PointF(x, y);
                                    points.Add(point);
                                }
                            }
                            src += offset1;
                        }
                    }

                }


                ResultOfTextDetection.Dispose();
            }
            catch (Exception exception)
            {
                Log.Write("Read Image Error " + exception.Message);
            }

            return points;
        }

        public void CropNewTextLabels(string inputPath, string OTDPath)
        {

            string[] NeighborhoodAreas=new string[0];
            PointF MinX, MaxX, MinY, MaxY;
            int NumberOfNeighbors;
            Rectangle boundingbox;
            Bitmap CropedFromOriginalImage;
            string FileName = "";
            string[] FileNameSplit = new string[11];
            PotentialTextAreaExtractation Area = new PotentialTextAreaExtractation();
            int angle;
            int NumberOfLeftNeighbors = 0;
            int NumberOfRightNeighbors = 0;

            try
            {

                for (int i = 0; i < Neighbors.Count; i++)
                {
                    NumberOfNeighbors = 0;
                    NumberOfLeftNeighbors = 0;
                    NumberOfRightNeighbors = 0;
                    angle = Convert.ToInt32(Neighbors[i].OriginalImageID.Split('_')[6]);

                    MinX = Neighbors[i].MinX;
                    MinY = Neighbors[i].MinY;
                    MaxX = Neighbors[i].MaxX;
                    MaxY = Neighbors[i].MaxY;

                    if (i + 1 < Neighbors.Count)
                        NeighborhoodAreas = Neighbors[i + 1].ImageID.Split('-');
                    ///  NeighborhoodAreas = Neighbors[i + NumberOfNeighbors + 1].ImageID.Split('-');
                    else if (i >= Neighbors.Count)
                        break;

                    while (NeighborhoodAreas.Length > 1 && NeighborhoodAreas[0] == Neighbors[i].ImageID && (i + NumberOfNeighbors + 1) < Neighbors.Count)
                    {
                        if (Neighbors[i + NumberOfNeighbors + 1].MinX.X <= MinX.X)
                            MinX = Neighbors[i + NumberOfNeighbors + 1].MinX;

                        if (Neighbors[i + NumberOfNeighbors + 1].MinY.Y <= MinY.Y)
                            MinY = Neighbors[i + NumberOfNeighbors + 1].MinY;

                        if (Neighbors[i + NumberOfNeighbors + 1].MaxX.X >= MaxX.X)
                            MaxX = Neighbors[i + NumberOfNeighbors + 1].MaxX;

                        if (Neighbors[i + NumberOfNeighbors + 1].MaxY.Y >= MaxY.Y)
                            MaxY = Neighbors[i + NumberOfNeighbors + 1].MaxY;

                        NumberOfNeighbors++;

                        if (NeighborhoodAreas[1] == "left")
                            NumberOfLeftNeighbors++;
                        else if (NeighborhoodAreas[1] == "right")
                            NumberOfRightNeighbors++;

                        if (i + NumberOfNeighbors + 1 < Neighbors.Count)
                            NeighborhoodAreas = Neighbors[i + NumberOfNeighbors + 1].ImageID.Split('-');
                        else
                            break;
                    }

                    i += NumberOfNeighbors;

                    FileNameSplit = Neighbors[i - NumberOfNeighbors].OriginalImageID.Split('_');
                    FileNameSplit[7] = Convert.ToString(Convert.ToInt32(MinX.X));
                    FileNameSplit[8] = Convert.ToString(Convert.ToInt32(MinY.Y));
                    FileNameSplit[9] = Convert.ToString(Convert.ToInt32(MaxX.X - MinX.X));
                    FileNameSplit[10] = Convert.ToString(Convert.ToInt32(MaxY.Y - MinY.Y));
                    FileName = string.Join("_", FileNameSplit);

                    if (angle < 10 || (360 - angle) < 10)   //// in this case the Min Area Bounding box is horizontal
                    {
                        boundingbox = new Rectangle(Convert.ToInt32(MinX.X), Convert.ToInt32(MinY.Y), Convert.ToInt32(MaxX.X - MinX.X), Convert.ToInt32(MaxY.Y - MinY.Y));
                        //TextLabelAreaPoints = Area.FromImageToArray(OriginalBinaryImage, boundingbox);
                        //CropedFromOriginalImage = Area.FromArrayToImage(TextLabelAreaPoints);
                        CropedFromOriginalImage = Area.FromImageToArray(OriginalBinaryImage, boundingbox).Area;
                    }
                    else              //// in this case the Min Area Bounding box is horizontal
                    {

                        PointF[] AreaVertices = new PointF[4];
                        AreaVertices[0] = MaxY;
                        AreaVertices[1] = MinX;
                        AreaVertices[2] = MinY;
                        AreaVertices[3] = MaxX;

                        int[,] AreaPoints = new int[Convert.ToInt32(MaxY.Y - MinY.Y) + 1, Convert.ToInt32(MaxX.X - MinX.X) + 1];      //// int[boundingbox.Height, boundingbox.Width];


                        NonHorizontalTextExtraction AreaExtraction = new NonHorizontalTextExtraction();
                        CropedFromOriginalImage = AreaExtraction.NonHorizontalReadImage(AreaPoints, AreaVertices, OriginalBinaryImage);

                        Emgu.CV.Structure.MCvBox2D box = PointCollection.MinAreaRect(AreaVertices);

                        RotateImage Rotation = new RotateImage();

                        // CropedFromOriginalImage.Save(inputPath + Neighbors[i].ImageID + "(BeforeRotation).png");
                        double MaxY_MinX_distance = Math.Pow(MaxY.X - MinX.X, 2) + Math.Pow(MaxY.Y - MinX.Y, 2);
                        double MaxY_MaxX_distance = Math.Pow(MaxY.X - MaxX.X, 2) + Math.Pow(MaxY.Y - MaxX.Y, 2);
                        if (MaxY_MinX_distance < MaxY_MaxX_distance)
                            CropedFromOriginalImage = Rotation.Apply(CropedFromOriginalImage, -Convert.ToInt32(box.angle));
                        else
                            CropedFromOriginalImage = Rotation.Apply(CropedFromOriginalImage, -(90 + Convert.ToInt32(box.angle)));


                    }
                    ImageStitcher imgstitcher = new ImageStitcher();                   
                    CropedFromOriginalImage = imgstitcher.ExpandCanvas(CropedFromOriginalImage, 20);
                    CropedFromOriginalImage.Save(inputPath + OTDPath + "\\" + FileName + ".png");
                }
            }
            catch (Exception exception)
            {
                Log.Write("Crop Image Error " + exception.Message);
            }

        }

        public List<MeetingAreas>[] Intersections(int[,] AddedAreaPoints)
        {
            List<MeetingAreas>[] Meeting = new List<MeetingAreas>[4];
            int[] NumberOfIntersections = new int[4];

            //// touch left ////
            for (int i = 0; i < AddedAreaPoints.Rows(); i++)
            {
                if (((i > 0) && (AddedAreaPoints[i - 1, 0] == 0) && (AddedAreaPoints[i, 0] == 1)) || ((i == 0) && ((AddedAreaPoints[i, 0] == 1))))  /// when we meet a white point which has a black point exactly after it
                {
                    if (Meeting[0] == null)
                        Meeting[0] = new List<MeetingAreas>();

                    MeetingAreas NewArea = new MeetingAreas();
                    NewArea.MinX = 0;
                    NewArea.MinY = i;

                    while ((i + 1 < AddedAreaPoints.Rows()) && (AddedAreaPoints[i + 1, 0] == 1))
                        i++;

                    NewArea.MaxX = 0;
                    NewArea.MaxY = i;
                    NewArea.Height = NewArea.MaxY - NewArea.MinY + 1;
                    NewArea.centerX = 0;
                    NewArea.centerY = (float)(NewArea.MinY + NewArea.MaxY) / 2;
                    Meeting[0].Add(NewArea);
                    NumberOfIntersections[0]++;
                }
            }

            //// touch right ////
            for (int i = 0; i < AddedAreaPoints.Rows(); i++)
            {
                if (((i > 0) && (AddedAreaPoints[i - 1, AddedAreaPoints.Columns() - 1] == 0) && (AddedAreaPoints[i, AddedAreaPoints.Columns() - 1] == 1)) || ((i == 0) && (AddedAreaPoints[i, AddedAreaPoints.Columns() - 1] == 1)))
                {
                    if (Meeting[2] == null)
                        Meeting[2] = new List<MeetingAreas>();

                    MeetingAreas NewArea = new MeetingAreas();
                    NewArea.MinX = AddedAreaPoints.Columns() - 1;
                    NewArea.MinY = i;

                    while ((i + 1 < AddedAreaPoints.Rows()) && (AddedAreaPoints[i + 1, AddedAreaPoints.Columns() - 1] == 1))
                        i++;

                    NewArea.MaxX = AddedAreaPoints.Columns() - 1;
                    NewArea.MaxY = i;
                    NewArea.Height = NewArea.MaxY - NewArea.MinY + 1;
                    NewArea.centerX = AddedAreaPoints.Columns() - 1;
                    NewArea.centerY = (float)(NewArea.MinY + NewArea.MaxY) / 2;
                    Meeting[2].Add(NewArea);
                    NumberOfIntersections[2]++;
                }
            }

            //// touch up ////
            for (int i = 0; i < AddedAreaPoints.Columns(); i++)
            {
                if (((i > 0) && (AddedAreaPoints[0, i - 1] == 0) && (AddedAreaPoints[0, i] == 1)) || ((i == 0) && (AddedAreaPoints[0, i + 1] == 1)))
                {
                    if (Meeting[1] == null)
                        Meeting[1] = new List<MeetingAreas>();

                    MeetingAreas NewArea = new MeetingAreas();
                    NewArea.MinX = i;
                    NewArea.MinY = 0;

                    while ((i + 1 < AddedAreaPoints.Columns()) && (AddedAreaPoints[0, i + 1] == 1))
                        i++;

                    NewArea.MaxX = i;
                    NewArea.MaxY = 0;
                    NewArea.Width = NewArea.MaxX - NewArea.MinX + 1;
                    NewArea.centerX = (float)(NewArea.MinX + NewArea.MaxX) / 2;
                    NewArea.centerY = 0;
                    Meeting[1].Add(NewArea);
                    NumberOfIntersections[1]++;
                }
            }

            //// touch down ////
            for (int i = 0; i < AddedAreaPoints.Columns(); i++)
            {
                if (((i > 0) && (AddedAreaPoints[AddedAreaPoints.Rows() - 1, i - 1] == 0) && (AddedAreaPoints[AddedAreaPoints.Rows() - 1, i] == 1)) || ((i == 0) && (AddedAreaPoints[AddedAreaPoints.Rows() - 1, i] == 1)))
                {
                    if (Meeting[3] == null)
                        Meeting[3] = new List<MeetingAreas>();

                    MeetingAreas NewArea = new MeetingAreas();
                    NewArea.MinX = i;
                    NewArea.MinY = AddedAreaPoints.Rows() - 1;

                    while ((i + 1 < AddedAreaPoints.Columns()) && (AddedAreaPoints[AddedAreaPoints.Rows() - 1, i + 1] == 1))
                        i++;

                    NewArea.MaxX = i;
                    NewArea.MaxY = AddedAreaPoints.Rows() - 1;
                    NewArea.Width = NewArea.MaxX - NewArea.MinX + 1;
                    NewArea.centerX = (float)(NewArea.MinX + NewArea.MaxX) / 2;
                    NewArea.centerY = AddedAreaPoints.Rows() - 1;
                    Meeting[3].Add(NewArea);
                    NumberOfIntersections[3]++;
                }

            }
            return Meeting;
        }

       
        public bool UpDownPixels(int[,] AddedArea)
        {
            bool EnoughPixels = true;
            int UpNumberOfPixels = 0;
            int DownNumberOfPixels = 0;

            for (int i = 0 ; i < Convert.ToInt32(AddedArea.Rows()/2) ; i++)
            {
                for (int j = 0; j < AddedArea.Columns(); j++)
                {
                    if (AddedArea[i, j] == 1)
                        UpNumberOfPixels++;
                }
            }

            for (int i = (Convert.ToInt32(AddedArea.Rows()/2)+1) ; i < Convert.ToUInt32(AddedArea.Rows()); i++)
            {
                for (int j = 0; j < AddedArea.Columns(); j++)
                {
                    if (AddedArea[i, j] == 1)
                        DownNumberOfPixels++;
                }
            }

            double PixelsRatio=0;

            if (UpNumberOfPixels > DownNumberOfPixels)
                PixelsRatio = (double)(DownNumberOfPixels)/(double)(UpNumberOfPixels);
            else
                PixelsRatio = (double)(UpNumberOfPixels)/(double)(DownNumberOfPixels);

            if (PixelsRatio < 0.42)
                EnoughPixels = false;

            return EnoughPixels;
        }

       
        public void ScanImage(Bitmap srcImg)
        {
            srcImg = ImageUtils.toGray(srcImg);
            // get source image size
            int width = srcImg.Width;
            int height = srcImg.Height;


            // create map
            int maxObjects = ((width / 2) + 1) * ((height / 2) + 1) + 1;
            int[] map = new int[maxObjects];
            // initially map all labels to themself
            for (int i = 0; i < maxObjects; i++)
                map[i] = i;
            // lock source bitmap data
            BitmapData srcData = srcImg.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);
            int srcStride = srcData.Stride;
            int srcOffset = srcStride - width;
            // do the job
            unsafe
            {
                byte* src = (byte*)srcData.Scan0.ToPointer();

                // label the first pixel
                // 1 - for pixels of the first row
                if (*src != 0)

                    ++src;

                // label the first row
                for (int x = 1; x < width; x++, src++)
                {
                    // check if we need to label current pixel
                    if (*src != 0)
                    {
                        // check if the previous pixel already labeled
                        if (src[-1] != 0)
                        {

                        }
                        // label current pixel, as the previous


                        // create new label

                    }
                }
                src += srcOffset;
                // 2 - for other rows
                // for each row

            }
            // unlock source images
            srcImg.UnlockBits(srcData);
        }
   


    }
}



