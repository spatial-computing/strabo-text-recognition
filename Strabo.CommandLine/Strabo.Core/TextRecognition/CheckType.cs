using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Strabo.Core.Utility;

namespace Strabo.Core.TextRecognition
{
    class CheckType
    {
        public List<TessResult> Apply(List<TessResult> tessOcrResultList, int georef, string lng)
        {
            double weight = 1.5;
            double left_weight = 2;
            double sweight = 0.5;
            int dictionaryExactMatchStringLength = 2;
            CleanTesseractResult ctr = new CleanTesseractResult();
            tessOcrResultList = ctr.RemoveMergeMultiLineResults(tessOcrResultList, 3);
            for (int i = 0; i < tessOcrResultList.Count; i++)
            {
                if (tessOcrResultList[i].id != "-1" && lng == "eng")
                {
                    tessOcrResultList[i] = ctr.CleanEnglish(tessOcrResultList[i]);
                }
                else if (tessOcrResultList[i].id != "-1" && lng == "chi_sim")
                {
                    tessOcrResultList[i] = ctr.CleanChinese(tessOcrResultList[i]);
                }

            }
            for (int i = 0; i < tessOcrResultList.Count; i++)
            {
                if (tessOcrResultList[i].id == "-1")
                    tessOcrResultList.RemoveAt(i);
            }
            for (int i = 0; i < tessOcrResultList.Count; i++)
            {
                if (tessOcrResultList[i].tess_word3.Length < dictionaryExactMatchStringLength || tessOcrResultList[i].id == "-1")
                    continue;
                for (int j = i + 1; j < tessOcrResultList.Count; j++)
                {
                    if (tessOcrResultList[j].tess_word3.Length < dictionaryExactMatchStringLength || tessOcrResultList[i].id == "-1")
                        continue;
                    int x1 = tessOcrResultList[i].x;
                    int x2 = tessOcrResultList[j].x;
                    int y1 = tessOcrResultList[i].y * georef;
                    int y2 = tessOcrResultList[j].y * georef;
                    int h = tessOcrResultList[i].h;
                    int w = tessOcrResultList[i].w;

                    if ((x2 - x1 <= weight * w && x2 - x1 >= 0 && (Math.Abs(y2 - y1) <= sweight * h)) || (y1 - y2 <= weight * h && y1 - y2 >= 0 && (Math.Abs(x2 - x1) <= sweight * w)))
                    {
                        tessOcrResultList[i].front = true;
                        tessOcrResultList[j].back = true;
                        //Log.WriteLine("1 Front: " + tessOcrResultList[i].tess_word3 + ". Back: " + tessOcrResultList[j].tess_word3 + ". x1: " + x1 + ". x2: " + x2 + ". y1:" + y1 + ". y2: " + y2 + ". w: " + w + ". h: " + h);
                    }

                    if ((x1 - x2 <= left_weight * w && x1 - x2 >= 0 && (Math.Abs(y2 - y1) <= sweight * h)) || (y2 - y1 <= weight * h && y2 - y1 >= 0 && (Math.Abs(x2 - x1) <= sweight * w)))
                    {
                        tessOcrResultList[i].back = true;
                        tessOcrResultList[j].front = true;
                        //Log.WriteLine("2 Front: " + tessOcrResultList[j].tess_word3 + ". Back: " + tessOcrResultList[i].tess_word3 + ". x1: " + x1 + ". x2: " + x2 + ". y1:" + y1 + ". y2: " + y2 + ". w: " + w + ". h: " + h);

                    }
                }
            }

            for (int i = 0; i < tessOcrResultList.Count; i++)
            {
                if (tessOcrResultList[i].front && tessOcrResultList[i].back)
                {
                    tessOcrResultList[i].front = false;
                    tessOcrResultList[i].back = false;
                }
                else if (!tessOcrResultList[i].front && !tessOcrResultList[i].back)
                {
                    tessOcrResultList[i].front = true;
                    tessOcrResultList[i].back = false;
                }
            }
            return tessOcrResultList;
        }
    }
}
