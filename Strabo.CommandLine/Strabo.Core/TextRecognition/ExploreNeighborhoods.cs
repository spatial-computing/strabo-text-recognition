using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Strabo.Core.ImageProcessing;
using System.Drawing;

namespace Strabo.Core.TextRecognition
{
    class ExploreNeighborhoods
    {
        public void Neighborhoods(Bitmap Neighborhood)
        {
            MyConnectedComponentsAnalysisFast.MyBlobCounter blobs = new MyConnectedComponentsAnalysisFast.MyBlobCounter();
            List<MyConnectedComponentsAnalysisFast.MyBlob> char_blobs=blobs.GetBlobs(Neighborhood);
        }
    }
}
