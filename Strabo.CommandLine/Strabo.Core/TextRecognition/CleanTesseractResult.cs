using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using Strabo.Core.Utility;
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

namespace Strabo.Core.TextRecognition
{
    public class CleanTesseractResult
    {
        private int dictionaryExactMatchStringLength;
        private string dictionaryPath;
        private bool elasticsearch;
        private string lng;
        private List<TessResult> tessOcrResultList;
        private int threadNumber = 1;

        public List<TessResult> RemoveMergeMultiLineResults(List<TessResult> tessOcrResultList, int maxLineLimit)
        {
            // compare cost
            char[] delimiter = {'\n'};

            for (var i = 0; i < tessOcrResultList.Count; i++)
            {
                var firsttempword = tessOcrResultList[i].tess_word3;
                var firstsplit = firsttempword.Split(delimiter);
                if (firstsplit.Length > maxLineLimit)
                {
                    tessOcrResultList[i].id = "-1";
                    continue;
                }

                for (var j = i + 1; j < tessOcrResultList.Count; j++)
                    if (tessOcrResultList[i].id == tessOcrResultList[j].id)
                    {
                        var secondtempword = tessOcrResultList[j].tess_word3;
                        var secondsplit = secondtempword.Split(delimiter);
                        if (firstsplit.Length < secondsplit.Length) //j has more lines
                        {
                            tessOcrResultList[j].id = "-1";
                            continue;
                        }
                        if (firstsplit.Length > secondsplit.Length)
                        {
                            tessOcrResultList[i].id = "-1";
                            break;
                        }
                        if (tessOcrResultList[i].tess_cost3+ (1-tessOcrResultList[i].dict_similarity) < tessOcrResultList[j].tess_cost3+ (1 - tessOcrResultList[j].dict_similarity))
                        {
                            tessOcrResultList[j].id = "-1";
                        }
                        else
                        {
                            tessOcrResultList[i].id = "-1";
                            break;
                        }
                    }
            }
            return tessOcrResultList;
        }

        public TessResult CleanEnglish(TessResult tessOcrResult)
        {
            //if (!RemoveNoiseText.NotTooManyNoiseCharacters(tessOcrResult.tess_word3))
            //    tessOcrResult.id = "-1";
            //else
            tessOcrResult.tess_word3 = Regex.Replace(tessOcrResult.tess_word3, @"[^a-zA-Z0-9\s]", "");
            return tessOcrResult;
        }

        public TessResult CleanChinese(TessResult tessOcrResult)
        {
            tessOcrResult.tess_word3 = Regex.Replace(tessOcrResult.tess_word3,
                @"[^\u4E00-\u9FFF\u6300-\u77FF\u7800-\u8CFF\u8D00-\u9FFF]", "");
            return tessOcrResult;
        }

        public TessResult CleanNumber(TessResult tessOcrResult)
        {
            // °
            var raw_result = tessOcrResult.tess_word3;

            Log.WriteLine("Raw CleanNumber: " + raw_result);

            raw_result = checkForDegrees(raw_result);
            raw_result = checkFor1s(raw_result);

            tessOcrResult.tess_word3 = Regex.Replace(raw_result, @"[^0-9]", "");
            Log.WriteLine("Raw CleanNumber after final Regex: " + tessOcrResult.tess_word3);

            return tessOcrResult;
        }

        private string checkForDegrees(string raw_result)
        {
            // Two types of degrees exists
            // ASCII 0176, 0186
            const char degree_1 = (char) 176;
            var degreestr = degree_1.ToString();
            raw_result = raw_result.Replace(degreestr, "0");

            const char degree_2 = (char) 186;
            degreestr = degree_2.ToString();
            raw_result = raw_result.Replace(degreestr, "0");

            return raw_result;
        }

        private string checkFor1s(string raw_result)
        {
            // 1 can be mistaken for \, /, |, l

            // Replacing | with 1
            raw_result = raw_result.Replace('|', '1');

            // Replacing l with 1
            raw_result = raw_result.Replace('l', '1');

            // Replacing \ with 1
            raw_result = raw_result.Replace('\\', '1');

            // Replacing / with 1
            raw_result = raw_result.Replace('/', '1');

            return raw_result;
        }

        public List<TessResult> Apply(List<TessResult> tessOcrResultList, string dictionaryPath,
            int dictionaryExactMatchStringLength, string lng, bool elasticsearch, int threadNumber)
        {
            try
            {
                this.tessOcrResultList = tessOcrResultList;
                this.dictionaryPath = dictionaryPath;
                this.dictionaryExactMatchStringLength = dictionaryExactMatchStringLength;
                this.lng = lng;
                this.elasticsearch = elasticsearch;
                this.threadNumber = threadNumber;

               

                if (dictionaryPath != "")
                    if (!elasticsearch)
                    {
                        Log.WriteLine("Reading Dictionary");
                        CheckDictionary.readDictionary(dictionaryPath);
                    }

                var thread_array = new Thread[threadNumber];
                for (var i = 0; i < threadNumber; i++)
                {
                    thread_array[i] = new Thread(check);
                    thread_array[i].Start(i);
                }
                for (var i = 0; i < threadNumber; i++)
                    thread_array[i].Join();

                tessOcrResultList = RemoveMergeMultiLineResults(tessOcrResultList, 3);
                //else ElasticSearch needs a geo bounding box
                //CheckDictionaryElasticSearch.readDictionary(top, left, bottom, right);


                for (var i = 0; i < tessOcrResultList.Count; i++)
                    if (tessOcrResultList[i].id == "-1")
                    {
                        //Log.WriteLine("Removed Word: " + tessOcrResultList[i].tess_word3);
                        tessOcrResultList.RemoveAt(i);
                        i--;
                    }
                return tessOcrResultList;
            }
            catch (Exception e)
            {
                Log.WriteLine(e.Message);
                Log.WriteLine(e.Source);
                Log.WriteLine(e.StackTrace);
                throw;
            }
        }

        private void check(object s)
        {
            var x = (int) s;

            for (var i = 0; i < tessOcrResultList.Count; i++)
            {
                //if (tessOcrResultList[i].id == "16" || tessOcrResultList[i].id == "25" || tessOcrResultList[i].id == "48")
                //    Console.WriteLine("debug");
                if (i % threadNumber != x) continue;
                //if (!elasticsearch)
                {
                    if (tessOcrResultList[i].id != "-1" && lng == "eng")
                        tessOcrResultList[i] = CleanEnglish(tessOcrResultList[i]);
                    if (tessOcrResultList[i].id != "-1" && lng == "chi_sim")
                        tessOcrResultList[i] = CleanChinese(tessOcrResultList[i]);
                    if (tessOcrResultList[i].id != "-1" && lng == "num")
                        tessOcrResultList[i] = CleanNumber(tessOcrResultList[i]);
                }
                if (tessOcrResultList[i].id != "-1")
                {
                    //if (tessOcrResultList[i].tess_word3.Length < dictionaryExactMatchStringLength)
                    //    tessOcrResultList[i].id = "-1";
                    if (tessOcrResultList[i].id =="6")
                        Console.WriteLine("debug");
                        if (dictionaryPath != "" && tessOcrResultList[i].tess_word3.Length >=
                        dictionaryExactMatchStringLength)
                        tessOcrResultList[i] =
                            CheckDictionary.getDictionaryWord(tessOcrResultList[i], dictionaryExactMatchStringLength);
                }
                else
                {
                    tessOcrResultList[i].id = "-1";
                }
            }
        }
    }
}