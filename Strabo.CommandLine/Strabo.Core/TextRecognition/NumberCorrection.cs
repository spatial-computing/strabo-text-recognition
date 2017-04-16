using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Strabo.Core.Utility;

namespace Strabo.Core.TextRecognition
{
    public class NumberCorrection
    {

        // Find nearest neighbor only when the character size is less than 4
        private List<TessResult> smallFeatures { get; set; }
        private List<TessResult> regularFeatures { get; set; }

        public NumberCorrection(List<TessResult> ocrResults)
        {

            smallFeatures = new List<TessResult>();
            regularFeatures = new List<TessResult>();

            // Separate the features into regular and small
            foreach(TessResult tr in ocrResults)
            {
                string[] fileInfo = tr.fileName.Split('_');
                int charCount = int.Parse(fileInfo[2]);

                if (charCount == 2)
                {
                    smallFeatures.Add(tr);
                }
                else
                {
                    regularFeatures.Add(tr);
                }
            }
        }
        
        private Boolean checkHorizontalOverlap(TessResult tr1, TessResult tr2)
        {
            // Check for horizontal overlap assuming similar y Axis
            // For tr1 to overlap tr2
            // the xOverlap should be more than the start of tr2.x
            // but the xOverlap shoulbe be less than the end of tr2
            int xOverlap = tr1.x + tr1.w + tr1.w / 2;

            int yStart = tr1.y - tr1.h/2;
            int yEnd = tr1.y + tr1.h + tr1.h / 2;
            if ( ( xOverlap > tr2.x) && (xOverlap < tr2.x + tr2.w) &&
                 ( tr2.y >= yStart && tr2.y <= yEnd))
            {
                return true;
            }
            return false;
        }
        /*
         * Finds the possible candidates that can be merged with charCount = 2
         */
        private List<TessResult> mergeFeatureWithCount2()
        {

            HashSet<int> uniqueFeaturesIdx = new HashSet<int>();
            List<TessResult> features = new List<TessResult>();

            for (int i=0; i < this.smallFeatures.Count; i++)
                uniqueFeaturesIdx.Add(i);

            for(int i = 0; i < this.smallFeatures.Count; i++)
            {
                for (int j=0; j < this.smallFeatures.Count; j++)
                {
                    if (i == j)
                        continue;

                    if (checkHorizontalOverlap(this.smallFeatures[i], this.smallFeatures[j]))
                    {
                        uniqueFeaturesIdx.Remove(i);
                        uniqueFeaturesIdx.Remove(j);

                        int y = this.smallFeatures[i].y > this.smallFeatures[j].y ? this.smallFeatures[i].y : this.smallFeatures[j].y;
                        int h = this.smallFeatures[i].h > this.smallFeatures[j].h ? this.smallFeatures[i].h : this.smallFeatures[j].h;
                        features.Add( new TessResult(this.smallFeatures[i].id,
                            this.smallFeatures[i].tess_word3 + this.smallFeatures[j].tess_word3,
                            this.smallFeatures[i].tess_raw3 + this.smallFeatures[j].tess_raw3,
                            this.smallFeatures[i].tess_cost3,
                            this.smallFeatures[i].hocr + this.smallFeatures[j].hocr,
                            this.smallFeatures[i].fileName + "_" + this.smallFeatures[j].fileName,
                            this.smallFeatures[i].x, y, this.smallFeatures[i].w + this.smallFeatures[j].w,
                            h));
                    }
                }
            }
            foreach(int i in uniqueFeaturesIdx)
            {
                features.Add(this.smallFeatures[i]);
            }
            return features;
        }

        public List<TessResult> mergeFeatures()
        {

            List<TessResult> finalResult = new List<TessResult>();
            finalResult.AddRange(mergeFeatureWithCount2());
            finalResult.AddRange(this.regularFeatures);

            return finalResult;
        }
    }
}
