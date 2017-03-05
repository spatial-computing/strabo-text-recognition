using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Strabo.Core.ImageProcessing;
using System.Drawing;
using Accord.Math;

namespace Strabo.Test
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

    class NeighborhoodExploring
    {
        public void Neighborhoods(Bitmap Neighborhood)
        {
            Neighborhood = ImageUtils.InvertColors(Neighborhood); 
            MyConnectedComponentsAnalysisFast.MyBlobCounter blobs = new MyConnectedComponentsAnalysisFast.MyBlobCounter();
            List<MyConnectedComponentsAnalysisFast.MyBlob> char_blobs = blobs.GetBlobs(Neighborhood);
            Bitmap OriginalBinaryImage = new Bitmap(@"C:\Users\nhonarva\Documents\strabo-command-line-master\strabo-command-line-master\Strabo.CommandLine\data\intermediate\BinaryOutput.png");
            //bool[] touch=new bool[4];    /// 0=left, 2=right , 1=up , 3=down

            Neighborhood = ImageUtils.InvertColors(Neighborhood);

            Rectangle boundingbox = new Rectangle(0,0,Neighborhood.Width,Neighborhood.Height);


            int[,] AddedAreaPoints = FromImageToArray(Neighborhood,boundingbox);
            List<MeetingAreas>[] Meeting = Intersections(AddedAreaPoints);
            int[] NumberOfIntersections = new int[4];





            if (Meeting[1]!=null)
            {
                //// neighborhood cordinates X= 958 Y=401

                Point right=new Point();
                right.X = 958;
                right.Y = 401;
                int add = 8;
                Bitmap addedAreabitmap = new Bitmap(Neighborhood.Width, Neighborhood.Height+add);

                Rectangle NewBoundingbox = new Rectangle(right.X, right.Y - add, Neighborhood.Width, Neighborhood.Height + add);

                int[,] NewAddedArea = FromImageToArray(OriginalBinaryImage, NewBoundingbox);
                addedAreabitmap = FromArrayToImage(NewAddedArea);

                addedAreabitmap.Save(@"C:\Users\nhonarva\Documents\ResultsOfScaling\copy.png");
            }
             

            //// touch left ////
        //    if (char_blobs[0].bbx.X == 0)
       //     {
               // touch[0] = true;

                for (int i=1;i<Neighborhood.Height;i++)
                {
                    if ((Neighborhood.GetPixel(0, i-1).Name.Equals("ffffffff")) && (!Neighborhood.GetPixel(0, i).Name.Equals("ffffffff")))
                        NumberOfIntersections[0]++;
                }
                
        //    }
            //// touch left ////

            //// touch right ////
      //      if ((char_blobs[0].bbx.X + char_blobs[0].bbx.Width) == Neighborhood.Size.Width)
      //      {

                for (int i = 1; i < Neighborhood.Height; i++)
                {
                    if ((Neighborhood.GetPixel(Neighborhood.Width-1, i-1).Name.Equals("ffffffff")) && (!Neighborhood.GetPixel(Neighborhood.Width-1, i ).Name.Equals("ffffffff")))
                        NumberOfIntersections[2]++;
                }
                
      //      }
            //// touch right ////

            //// touch up ////
       //     if (char_blobs[0].bbx.Y == 0)
       //     {
                for (int i = 1; i < Neighborhood.Width; i++)
                {
                    if ((Neighborhood.GetPixel(i-1 , 0).Name.Equals("ffffffff")) && (!Neighborhood.GetPixel(i , 0).Name.Equals("ffffffff")))
                        NumberOfIntersections[1]++;
                }
       //     }
                
            //// touch up ////

            //// touch down ////
      //      if ((char_blobs[0].bbx.Y + char_blobs[0].bbx.Height) == Neighborhood.Size.Height)
     //       {
                for (int i = 1; i < Neighborhood.Width; i++)
                {
                    if ((Neighborhood.GetPixel(i-1, Neighborhood.Height-1).Name.Equals("ffffffff")) && (!Neighborhood.GetPixel(i , Neighborhood.Height-1).Name.Equals("ffffffff")))
                        NumberOfIntersections[3]++;
                }
      //      }
                
            //// touch down ////




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
        public Bitmap FromArrayToImage(int[,] AddeedAreaPoints)
        {
            Bitmap AddedArea = new Bitmap(AddeedAreaPoints.Columns(),AddeedAreaPoints.Rows());

            for (int i = 0; i < AddeedAreaPoints.Rows(); i++)
            {
                    for (int j = 0; j < AddeedAreaPoints.Columns(); j++)
                {
                        if (AddeedAreaPoints[i, j] == 1)
                        AddedArea.SetPixel(j, i, Color.Black);
                    else
                        AddedArea.SetPixel(j, i, Color.White);
                }
            }

            return AddedArea;
        }
        public int[,] FromImageToArray(Bitmap Neighborhood, Rectangle boundingbox)
        {
            int[,] AddedAreaPoints = new int[boundingbox.Height, boundingbox.Width];

            for (int i = boundingbox.X ; i < boundingbox.X+ boundingbox.Width ; i++)
            {
                for (int j = boundingbox.Y ; j < boundingbox.Y + boundingbox.Height ; j++)
                {
                    if ((!Neighborhood.GetPixel(i, j).Name.Equals("ffffffff")))
                    {

                        AddedAreaPoints[j - boundingbox.Y, i - boundingbox.X] = 1;  
                    }
                    else
                        AddedAreaPoints[j - boundingbox.Y, i - boundingbox.X] = 0;  
                }
            }
            return AddedAreaPoints;
        }
    }
}
