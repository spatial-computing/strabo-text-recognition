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

using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Drawing;
using System.Linq;

namespace Strabo.Core.BoundingBox
{
    public class MinimumBoundingBox : IBoundingBox
    {
        private MCvBox2D bbx;

        public MinimumBoundingBox(PointF[] points)
        {
            //from this webpage: http://www.emgu.com/wiki/index.php/Minimum_Area_Rectangle_in_CSharp
            this.bbx = PointCollection.MinAreaRect(points);
        }

        public MinimumBoundingBox()
        {
        }

        public MCvBox2D Bbx()
        {
            return this.bbx;
        }

        public void updateBbx(MCvBox2D bbx)
        {
            this.bbx = bbx;
        }

        public float area()
        {
            return this.bbx.size.Height * this.bbx.size.Width;
        }

        public float returnCalculatedAngle()
        {
            // Return the angle with the longer side
            if (this.bbx.size.Height >= this.bbx.size.Width)
            {
                return Math.Abs(this.bbx.angle) + 90;
            }
            else
            {
                return Math.Abs(this.bbx.angle);
            }
        }

        public PointF firstVertex()
        {
            return this.bbx.GetVertices()[0];
        }

        public float height()
        {
            return this.bbx.size.Height;
        }

        public PointF massCenter()
        {
            return this.bbx.center;
        }

        public float orientation()
        {
            return this.bbx.angle;
        }

        public PointF[] vertices()
        {
            return this.bbx.GetVertices();
        }

        public float width()
        {
            return this.bbx.size.Width;
        }

        public PointF returnExtremeStart()
        {
            PointF[] rawPoints = vertices();
            rawPoints.OrderBy(p => p.X).ThenBy(p => p.Y);
            return rawPoints[0];
        }

        public PointF returnExtremeEnd()
        {
            PointF[] rawPoints = vertices();
            rawPoints.OrderBy(p => p.X).ThenBy(p => p.Y);
            return rawPoints[rawPoints.Length - 1];
        }

        public float returnExtremeStartX()
        {
            PointF[] rawPoints = vertices();
            rawPoints.OrderBy(p => p.X);
            return rawPoints[0].X;
        }
        public float returnExtremeStartY()
        {
            PointF[] rawPoints = vertices();
            rawPoints.OrderBy(p => p.Y);
            return rawPoints[0].Y;
        }
        public float returnExtremeEndX()
        {
            PointF[] rawPoints = vertices();
            rawPoints.OrderBy(p => p.X);
            return rawPoints[rawPoints.Length - 1].X;
        }
        public float returnExtremeEndY()
        {
            PointF[] rawPoints = vertices();
            rawPoints.OrderBy(p => p.Y);
            return rawPoints[rawPoints.Length - 1].Y;
        }

        public PointF returnDiagonalVertex()
        {
            return this.bbx.GetVertices()[2];
        }
    }
}
