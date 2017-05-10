using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Strabo.Core.Utility;
using Priority_Queue;

namespace Strabo.Core.TextRecognition
{
    public class NumberCorrection
    {
        private List<TessResult> mergedFeatures { get; set; }
        private List<TessResult> allFeatures { get; set; }
        private Dictionary<TessResult, SimplePriorityQueue<Tuple<TessResult, double>>> nearestNeighbors { get; set; }

        public NumberCorrection(List<TessResult> ocrResults)
        {

            this.mergedFeatures = new List<TessResult>();
            this.allFeatures = ocrResults;
            this.nearestNeighbors = new Dictionary<TessResult, SimplePriorityQueue<Tuple<TessResult, double>>>();
            findNearestNeighbors(ocrResults);

        }

        private double getL2Distance(TessResult tr1, TessResult tr2)
        {
            return Math.Sqrt(
                    (int)Math.Pow(tr1.mcX - tr2.mcX, 2) + (int)Math.Pow(tr1.mcY - tr2.mcY, 2)
                );
        }

        /// <summary>
        /// Finds the 5 nearest neighbors for each element
        /// It finds all the neaerest neighbors for each element 
        /// then selects the nearest 5
        /// </summary>
        /// <param name="ocrResults"></param>
        private void findNearestNeighbors(List<TessResult> ocrResults)
        {
            for (int i = 0; i < ocrResults.Count; i++)
            {
                SimplePriorityQueue<Tuple<TessResult, double>> neighbors =
                        new SimplePriorityQueue<Tuple<TessResult, double>>();
                SimplePriorityQueue<Tuple<TessResult, double>> nearest5Neighbors =
                        new SimplePriorityQueue<Tuple<TessResult, double>>();

                for (int j = 0; j < ocrResults.Count; j++)
                {
                    if (i == j)
                        continue;
                    double dist = getL2Distance(ocrResults[i], ocrResults[j]);
                    neighbors.Enqueue(
                         new Tuple<TessResult, double>(ocrResults[j], dist),
                         (float)dist
                        );
                }
                // Get the 5 nearest neighbors
                int count = 0;
                while (count < 5 && neighbors.Count != 0)
                {
                    Tuple<TessResult, double> node = neighbors.Dequeue();
                    // Log.WriteLine("Node: " + ocrResults[i].fileName + " Neighbor: " + node.Item1.fileName);
                    nearest5Neighbors.Enqueue(node, (float)node.Item2);
                    count++;
                }
                this.nearestNeighbors[ocrResults[i]] = nearest5Neighbors;
            }
        }

        private Boolean checkHorizontalOverlap(TessResult tr1, TessResult tr2)
        {
            // Check for horizontal overlap assuming similar y Axis
            // For tr1 to overlap tr2
            // the xOverlap should be more than the start of tr2.x
            // but the xOverlap shoulbe be less than the end of tr2
            int numChar = tr1.tess_word3.ToCharArray().Length;
            if (numChar == 0)
                return false;
            int xOverlap = tr1.x + tr1.w + tr1.w / numChar;
            int yStart = tr1.y - tr1.h / 2;
            int yEnd = tr1.y + tr1.h + tr1.h / 2;
            if ((xOverlap > tr2.x) && (xOverlap < tr2.x + tr2.w) &&
                 (tr2.y >= yStart && tr2.y <= yEnd))
            {
                return true;
            }
            return false;
        }

        private Boolean checkVerticalOverlap(TessResult tr1, TessResult tr2)
        {
            // Check for horizontal overlap assuming similar y Axis
            // For tr1 to overlap tr2
            // the xOverlap should be more than the start of tr2.x
            // but the xOverlap shoulbe be less than the end of tr2
            int numChar = tr1.tess_word3.ToCharArray().Length;
            if (numChar == 0)
                return false;
            int yOverlap = tr1.y + tr1.h + tr1.h / numChar;
            int xStart = tr1.x - tr1.w / 2;
            int xEnd = tr1.x + tr1.w + tr1.w / 2;
            if ((yOverlap > tr2.y) && (yOverlap < tr2.y + tr2.h) &&
                 (tr2.x >= xStart && tr2.x <= xEnd))
            {
                return true;
            }
            return false;
        }

        private Boolean _checkFeatureOverlaps(TessResult tr, Tuple<TessResult, double> nearestFeature)
        {

            if (checkHorizontalOverlap(tr, nearestFeature.Item1))
            {
                int y = tr.y > nearestFeature.Item1.y ? tr.y : nearestFeature.Item1.y;
                int h = tr.h > nearestFeature.Item1.h ? tr.h : nearestFeature.Item1.h;

                mergedFeatures.Add(new TessResult(tr.id + "_" + nearestFeature.Item1.id,
                    tr.tess_word3 + nearestFeature.Item1.tess_word3,
                    tr.tess_raw3 + nearestFeature.Item1.tess_raw3,
                    tr.tess_cost3,
                    tr.hocr + nearestFeature.Item1.hocr,
                    tr.fileName + "_" + nearestFeature.Item1.fileName,
                    tr.x, y, tr.w + nearestFeature.Item1.w,
                    h));
                allFeatures.Remove(tr);
                allFeatures.Remove(nearestFeature.Item1);
                return true;
            }
            else if (checkVerticalOverlap(tr, nearestFeature.Item1))
            {
                int x = tr.x > nearestFeature.Item1.x ? tr.x : nearestFeature.Item1.x;
                int w = tr.w > nearestFeature.Item1.w ? tr.w : nearestFeature.Item1.w;

                mergedFeatures.Add(new TessResult(tr.id + "_" + nearestFeature.Item1.id,
                    tr.tess_word3 + nearestFeature.Item1.tess_word3,
                    tr.tess_raw3 + nearestFeature.Item1.tess_raw3,
                    tr.tess_cost3,
                    tr.hocr + nearestFeature.Item1.hocr,
                    tr.fileName + "_" + nearestFeature.Item1.fileName,
                    x, tr.y, w,
                    tr.h + nearestFeature.Item1.h));
                allFeatures.Remove(tr);
                allFeatures.Remove(nearestFeature.Item1);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        ///  Merges features with character count = 2
        /// </summary>
        private void mergeSmallFeatures()
        {
            foreach (TessResult tr in this.nearestNeighbors.Keys)
            {
                // Only merge features with charCount = 2
                if (int.Parse(tr.fileName.Split('_')[2]) == 2)
                {
                    Tuple<TessResult, double> nearestFeature = this.nearestNeighbors[tr].First();

                    if (int.Parse(nearestFeature.Item1.fileName.Split('_')[2]) == 2 &&
                        _checkFeatureOverlaps(tr, nearestFeature))
                        Log.WriteLine("MERGING !!! " + tr.fileName + " || " + nearestFeature.Item1.fileName);
                }
            }
        }

        /// <summary>
        /// Tries to merge features of size 3 with 1
        /// </summary>
        private void mregeMediumFeature()
        {
            // We assume that if a feature with charCount = 3
            // and a feature with a feature with charCount = 1
            // has to be merged then feature with charCount = 1 will be the nearest neighbor
            foreach (TessResult tr in this.nearestNeighbors.Keys)
            {
                // 3 + 1
                if (int.Parse(tr.fileName.Split('_')[2]) == 3)
                {
                    Tuple<TessResult, double> nearestFeature = this.nearestNeighbors[tr].First();
                    if (int.Parse(nearestFeature.Item1.fileName.Split('_')[2]) == 1 &&
                        _checkFeatureOverlaps(tr, nearestFeature))
                        Log.WriteLine("MERGING !!! " + tr.fileName + " || " + nearestFeature.Item1.fileName);
                }
            }
        }

        /// <summary>
        /// Tries to merge features of size 1 with 3
        /// </summary>
        private void mergeSmallestFeature()
        {
            // We assume that if a feature with charCount = 1
            // and a feature with a feature with charCount = 3
            // has to be merged then feature with charCount = 3 will be the nearest neighbor
            foreach (TessResult tr in this.nearestNeighbors.Keys)
            {
                // 1 + 3
                if (int.Parse(tr.fileName.Split('_')[2]) == 1)
                {
                    Tuple<TessResult, double> nearestFeature = this.nearestNeighbors[tr].First();
                    if (int.Parse(nearestFeature.Item1.fileName.Split('_')[2]) == 3 &&
                        _checkFeatureOverlaps(tr, nearestFeature))
                        Log.WriteLine("MERGING !!! " + tr.fileName + " || " + nearestFeature.Item1.fileName);
                }
            }
        }

        private void estimateBestPrefixSuffix()
        {
            List<TessResult> toChange = new List<TessResult>();
            foreach (TessResult tr in this.allFeatures)
            {
                // If features with charCount = 3 still exists
                // This means merging has failed
                // We need to find best estimate
                char[] digits = tr.tess_word3.ToCharArray();
                if (digits.Length == 3)
                {
                    while (this.nearestNeighbors[tr].Count > 0)
                    {
                        Tuple<TessResult, double> nearestFeature = this.nearestNeighbors[tr].Dequeue();
                        char[] individualDigits = nearestFeature.Item1.tess_word3.ToCharArray();

                        if (individualDigits.Length == 4 &&
                            int.Parse(digits[0].ToString()) == int.Parse(individualDigits[0].ToString()))
                        {
                            tr.tess_word3 += individualDigits[3].ToString();
                            toChange.Add(tr);
                            break;

                        }
                        else if (individualDigits.Length == 4 &&
                                int.Parse(digits[0].ToString()) != int.Parse(individualDigits[0].ToString()))
                        {
                            tr.tess_word3 = individualDigits[0].ToString() + tr.tess_word3;
                            break;
                        }
                    }
                }
            }
            foreach (TessResult tr in toChange)
            {
                this.mergedFeatures.Add(tr);
                this.allFeatures.Remove(tr);
            }
        }

        /// <summary>
        /// It has been seen that the charCount given in filename need
        /// not be the true representation of the number of characters
        /// Use this to eliminate noise
        /// </summary>
        public void removeNoise()
        {
            List<TessResult> toChange = new List<TessResult>();
            foreach (TessResult tr in this.allFeatures)
            {
                int numDigits = tr.tess_word3.ToCharArray().Length;
                int expectedDigits = int.Parse(tr.fileName.Split('_')[2]);

                Log.WriteLine(tr.fileName + ":" + numDigits + ":" + expectedDigits + ":" + tr.tess_word3);

                if (numDigits == expectedDigits)
                    continue;
                else if (numDigits + 1 == expectedDigits)
                {
                    continue;

                }
                else if (expectedDigits + 1 == numDigits)
                {
                    continue;
                }
                else
                {
                    // we assume that maximum difference between the expected digits and the actual digits
                    // should be 1
                    toChange.Add(tr);
                }

            }
            foreach (TessResult tr in toChange)
            {
                this.allFeatures.Remove(tr);
            }

        }
        public List<TessResult> mergeFeatures()
        {
            mergeSmallFeatures();
            mregeMediumFeature();
            mergeSmallestFeature();
            estimateBestPrefixSuffix();
            removeNoise();

            List<TessResult> result = new List<TessResult>();
            result.AddRange(allFeatures);
            result.AddRange(mergedFeatures);
            return result;
        }
        ///*
        // * Finds the possible candidates that can be merged with charCount = 2
        // */
        //private List<TessResult> mergeFeatureWithCount2()
        //{

        //    HashSet<int> uniqueFeaturesIdx = new HashSet<int>();
        //    List<TessResult> features = new List<TessResult>();

        //    for (int i=0; i < this.smallFeatures.Count; i++)
        //        uniqueFeaturesIdx.Add(i);

        //    for(int i = 0; i < this.smallFeatures.Count; i++)
        //    {
        //        for (int j=0; j < this.smallFeatures.Count; j++)
        //        {
        //            if (i == j)
        //                continue;

        //            if (checkHorizontalOverlap(this.smallFeatures[i], this.smallFeatures[j]))
        //            {
        //                uniqueFeaturesIdx.Remove(i);
        //                uniqueFeaturesIdx.Remove(j);

        //                int y = this.smallFeatures[i].y > this.smallFeatures[j].y ? this.smallFeatures[i].y : this.smallFeatures[j].y;
        //                int h = this.smallFeatures[i].h > this.smallFeatures[j].h ? this.smallFeatures[i].h : this.smallFeatures[j].h;
        //                features.Add( new TessResult(this.smallFeatures[i].id,
        //                    this.smallFeatures[i].tess_word3 + this.smallFeatures[j].tess_word3,
        //                    this.smallFeatures[i].tess_raw3 + this.smallFeatures[j].tess_raw3,
        //                    this.smallFeatures[i].tess_cost3,
        //                    this.smallFeatures[i].hocr + this.smallFeatures[j].hocr,
        //                    this.smallFeatures[i].fileName + "_" + this.smallFeatures[j].fileName,
        //                    this.smallFeatures[i].x, y, this.smallFeatures[i].w + this.smallFeatures[j].w,
        //                    h));
        //            }
        //        }
        //    }
        //    foreach(int i in uniqueFeaturesIdx)
        //    {
        //        features.Add(this.smallFeatures[i]);
        //    }
        //    return features;
        //}

        ///// <summary>
        ///// Checks for features with charCount = 1 
        ///// which may be near to the features with charCount = 3
        ///// Tries to merge them
        ///// </summary>
        ///// <returns></returns>
        //private List<TessResult> mergeFeaturesWithCount3()
        //{
        //    for(int i=0; i< this.mediumFeatures.Count; i++)
        //    {
        //        for(int j=0; j<this.smallestFeatures.Count; j++)
        //        {

        //        }
        //    }
        //    return null;
        //}

        //private void findBestPrefixSuffix()
        //{

        //}
        //public List<TessResult> mergeFeatures()
        //{

        //    List<TessResult> finalResult = new List<TessResult>();
        //    finalResult.AddRange(mergeFeatureWithCount2());
        //    finalResult.AddRange(this.regularFeatures);

        //    return finalResult;
        //}
    }
}
