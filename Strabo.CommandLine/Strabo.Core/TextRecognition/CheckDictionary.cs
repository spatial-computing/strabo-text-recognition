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
using System.Collections.Generic;
using System.IO;
using SpellChecker.Net.Search.Spell;
using Strabo.Core.Utility;

namespace Strabo.Core.TextRecognition
{
    public class DictResult
    {
        public double similarity;
        public string text;
        public double word_count = 1;
    }

    public class CheckDictionary
    {
        private static readonly int _maxNumCharacterInAWord = 30;
        private static readonly int _maxWordLength = 4;
        private static readonly int _minWordLength = 2;
        private static readonly double _minWordSimilarity = 0.33;
        private static readonly List<string>[] _IndexDictionary = new List<string>[_maxNumCharacterInAWord];
        private static bool dictProcessed;
        private static int _dictionaryExactMatchStringLength;

        private static string reverseWord(string word)
        {
            var charArray = word.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }

        public static double findSimilarDictionaryWord(string word, double maxSimilarity, int index,
            List<string> equalMinDistanceDictWordList)
        {
            index = index - _minWordLength;
            word = word.ToLower();
            //if(word == "main")
            //    Console.WriteLine("debug");
            double NewSimilarity = 0;
            var WordLength = word.Length;
            if (WordLength + index < 0)
                return maxSimilarity;
            if (WordLength + index >= _IndexDictionary.Length)
                return maxSimilarity;
            if (_IndexDictionary[WordLength + index] == null)
                return maxSimilarity;

            for (var j = 0; j < _IndexDictionary[WordLength + index].Count; j++)
            {
                var JaroDist = new JaroWinklerDistance();
                var ng = new NGramDistance();
                var jd = new JaccardDistance();
                //if(_IndexDictionary[
                //    WordLength +
                //    index][j] == "main")
                //    Console.WriteLine(("debug"));
                NewSimilarity =
                    jd.GetDistance(word,
                        _IndexDictionary[
                            WordLength +
                            index][j]); //(double)JaroDist.GetDistance(word, _IndexDictionary[WordLenght - 1 + index][j]);

                if (NewSimilarity > maxSimilarity)
                {
                    equalMinDistanceDictWordList.Clear();
                    equalMinDistanceDictWordList.Add(_IndexDictionary[WordLength + index][j]);
                    maxSimilarity = NewSimilarity;
                }
                else if (NewSimilarity == maxSimilarity)
                {
                    equalMinDistanceDictWordList.Add(_IndexDictionary[WordLength + index][j]);
                }
            }
            return maxSimilarity;
        }

        public static void readDictionary(string DictionaryPath)
        {
            if (dictProcessed) return;
            dictProcessed = true;
            var file = new StreamReader(DictionaryPath); /// relative path
            var Dictionary = new List<string>();
            string line;

            // read dictionary
            while ((line = file.ReadLine()) != null)
            {
                if (_IndexDictionary[line.Length] == null)
                    _IndexDictionary[line.Length] = new List<string>();
                _IndexDictionary[line.Length].Add(line.ToLower());
            }
        }

        private static DictResult checkOneWord(string text)
        {
            double maxSimilarity = 0;
            var dictR = new DictResult();
            var equalMinDistanceDictWordList = new List<string>();

            if (text.Length == _dictionaryExactMatchStringLength) //short strings are looking for the exact match
            {
                maxSimilarity = findSimilarDictionaryWord(text, maxSimilarity, 0, equalMinDistanceDictWordList);
                if (maxSimilarity != 1)
                {
                    dictR.similarity = 0;
                    dictR.text = "";
                }
                else
                {
                    dictR.similarity = maxSimilarity;
                    dictR.text = equalMinDistanceDictWordList[0];
                }
            }
            else
            {
                for (var m = 0; m < _maxWordLength; m++)
                    maxSimilarity = findSimilarDictionaryWord(text, maxSimilarity, m, equalMinDistanceDictWordList);
                if (maxSimilarity < _minWordSimilarity
                ) //dictionary word not found (most similar is 1) hill vs hall = 0.333333
                {
                    dictR.similarity = 0;
                    dictR.text = "";
                }
                else
                {
                    dictR.similarity = maxSimilarity;
                    dictR.text = equalMinDistanceDictWordList[0];
                }
            }
            return dictR;
        }

        private static DictResult checkOneLine(string text)
        {
            var dictRLine = new DictResult();
            dictRLine.similarity = 0;
            dictRLine.text = "";

            var dictionaryResultList = new List<DictResult>();

            var InputFragments = text.Split(' ');
            for (var k = 0; k < InputFragments.Length; k++)
            {
                var dictRWord = checkOneWord(InputFragments[k]);
                dictionaryResultList.Add(dictRWord);
            }
            for (var i = 0; i < dictionaryResultList.Count; i++)
                if (dictionaryResultList[i].similarity > _minWordSimilarity)
                {
                    if (i == dictionaryResultList.Count - 1)
                        dictRLine.text += dictionaryResultList[i].text;
                    else
                        dictRLine.text += dictionaryResultList[i].text + " ";
                    dictRLine.similarity += dictionaryResultList[i].similarity;
                    dictRLine.word_count++;
                }
            dictRLine.similarity /= dictRLine.word_count;
            return dictRLine;
        }

        private static DictResult checkMultiLines(string text)
        {
            var dictRPage = new DictResult();
            dictRPage.similarity = 0;
            dictRPage.text = "";

            var dictionaryResultList = new List<DictResult>();

            var InputFragments = text.Split('\n');
            for (var k = 0; k < InputFragments.Length; k++)
            {
                var dictRWord = checkOneLine(InputFragments[k]);
                dictionaryResultList.Add(dictRWord);
            }
            for (var i = 0; i < dictionaryResultList.Count; i++)
            {
                if (i == dictionaryResultList.Count - 1)
                    dictRPage.text += dictionaryResultList[i].text;
                else
                    dictRPage.text += dictionaryResultList[i].text + "\n";

                dictRPage.similarity += dictionaryResultList[i].similarity * dictionaryResultList[i].word_count;
                dictRPage.word_count = +dictionaryResultList[i].word_count;
            }
            dictRPage.similarity /= dictRPage.word_count;
            return dictRPage;
            ;
        }

        public static TessResult getDictionaryWord(TessResult tr, int dictionaryExactMatchStringLength)
        {
            _dictionaryExactMatchStringLength = dictionaryExactMatchStringLength;

            //if (tr.tess_word3.Contains("ouse"))
            //    Console.WriteLine("debug");
            try
            {
                // Also removes any single digit that may be valid
                if (tr.tess_word3 == null ||
                    tr.tess_word3.Length < _dictionaryExactMatchStringLength) //input is invalid
                {
                    tr.id = "-1";
                }
                else
                {
                    DictResult dictR;

                    if (!tr.tess_word3.Contains(" ") && !tr.tess_word3.Contains("\n"))
                        dictR = checkOneWord(tr.tess_word3);
                    else if (!tr.tess_word3.Contains("\n"))
                        dictR = checkOneLine(tr.tess_word3);
                    else
                        dictR = checkMultiLines(tr.tess_word3);
                    tr.dict_similarity = dictR.similarity;
                    tr.dict_word3 = dictR.text;
                }
                return tr;
            }
            catch (Exception e)
            {
                Log.WriteLine("Check Dictionary: " + e.Message);
                throw e;
            }
        }
    }
}