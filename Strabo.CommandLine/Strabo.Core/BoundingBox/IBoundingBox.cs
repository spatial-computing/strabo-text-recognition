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

using System.Drawing;

namespace Strabo.Core.BoundingBox
{
    interface IBoundingBox
    {
        // A bounding box will have masscenter, vertices, height, width, startx, starty

        PointF massCenter();
        PointF[] vertices();

        // The (x, y) co-ordinate of the first intersecting edge
        PointF firstVertex();

        // PointF lastVertex();

        float width();
        float height();
        float orientation();
        float area();
        float returnCalculatedAngle();

        PointF returnExtremeStart();
        PointF returnExtremeEnd();
        float returnExtremeStartX();
        float returnExtremeStartY();
        float returnExtremeEndX();
        float returnExtremeEndY();
        PointF returnDiagonalVertex();

        // http://stackoverflow.com/questions/15956124/minarearect-angles-unsure-about-the-angle-returned
        // OpenCv returns orientation ranging from [-90, 0]
        // -90/0 means perfectly horizontal/vertical
        // We will want to have some method that takes care of this transformation
        // and find the angles in range [0, 180]

    }
}