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

using System;
using System.Drawing;
using System.Linq;
using Emgu.CV;
using Emgu.CV.Structure;

namespace Strabo.Core.BoundingBox
{
    public class MinimumBoundingBox : IBoundingBox
    {
        private RotatedRect bbx;

        public MinimumBoundingBox(PointF[] points)
        {
            //from this webpage: http://www.emgu.com/wiki/index.php/Minimum_Area_Rectangle_in_CSharp
            bbx = CvInvoke.MinAreaRect(points);
        }

        public MinimumBoundingBox()
        {
        }

        public float area()
        {
            return bbx.Size.Height * bbx.Size.Width;
        }

        public float returnCalculatedAngle()
        {
            // Return the angle with the longer side
            if (bbx.Size.Height >= bbx.Size.Width)
                return Math.Abs(bbx.Angle) + 90;
            return Math.Abs(bbx.Angle);
        }

        public PointF firstVertex()
        {
            return bbx.GetVertices()[0];
        }

        public float height()
        {
            return bbx.Size.Height;
        }

        public PointF massCenter()
        {
            return bbx.Center;
        }

        public float orientation()
        {
            return bbx.Angle;
        }

        public PointF[] vertices()
        {
            return bbx.GetVertices();
        }

        public float width()
        {
            return bbx.Size.Width;
        }

        public PointF returnExtremeStart()
        {
            var rawPoints = vertices();
            rawPoints.OrderBy(p => p.X).ThenBy(p => p.Y);
            return rawPoints[0];
        }

        public PointF returnExtremeEnd()
        {
            var rawPoints = vertices();
            rawPoints.OrderBy(p => p.X).ThenBy(p => p.Y);
            return rawPoints[rawPoints.Length - 1];
        }

        public float returnExtremeStartX()
        {
            var rawPoints = vertices();
            rawPoints.OrderBy(p => p.X);
            return rawPoints[0].X;
        }

        public float returnExtremeStartY()
        {
            var rawPoints = vertices();
            rawPoints.OrderBy(p => p.Y);
            return rawPoints[0].Y;
        }

        public float returnExtremeEndX()
        {
            var rawPoints = vertices();
            rawPoints.OrderBy(p => p.X);
            return rawPoints[rawPoints.Length - 1].X;
        }

        public float returnExtremeEndY()
        {
            var rawPoints = vertices();
            rawPoints.OrderBy(p => p.Y);
            return rawPoints[rawPoints.Length - 1].Y;
        }

        public PointF returnDiagonalVertex()
        {
            return bbx.GetVertices()[2];
        }

        public RotatedRect Bbx()
        {
            return bbx;
        }

        public void updateBbx(RotatedRect bbx)
        {
            this.bbx = bbx;
        }
    }
}