using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace Strabo.Core.BoundingBox
{
    interface BoundingBox
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