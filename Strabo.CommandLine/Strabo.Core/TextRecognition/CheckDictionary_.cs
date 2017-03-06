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

using SpellChecker.Net.Search.Spell;
using Strabo.Core.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Strabo.Core.TextRecognition
{
    class CheckDictionary_
    {
        static int _maxNumCharacterInAWord = 30;
        static int _maxWordLength = 3;
        static int _minWordLength = 0;
        static int _minWordLengthForDictionaryComparison = 3;
        static List<string>[] _IndexDictionary = new List<string>[_maxNumCharacterInAWord];
        static bool dictProcessed = false;
        static string tempReplacement = "";
        private static string reverseWord(string word)
        {
            char[] charArray = word.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }
        public static double findSimilarDictionaryWord(string word, double maxSimilarity, int index, Dictionary<string, double> equalMinDistanceDictWordList)
        {
            try
            {

                double distancethreshold = 0.3;
                index = index - _minWordLength;
                double NewDistance = 0;
                int WordLenght = word.Length;
                if ((WordLenght + index) < 0)
                    return maxSimilarity;

                if ((WordLenght + index) >= _IndexDictionary.Length)
                    return maxSimilarity;
                if (_IndexDictionary[WordLenght - 1 + index] == null)
                    return 0;
                for (int j = 0; j < _IndexDictionary[WordLenght - 1 + index].Count; j++)
                {

                    JaroWinklerDistance JaroDist = new JaroWinklerDistance();
                    NGramDistance ng = new NGramDistance();
                    JaccardDistance jd = new JaccardDistance();
                    string temp =  _IndexDictionary[WordLenght - 1 + index][j];
                    NewDistance = jd.GetDistance(word, temp);
                    double NewDistance2 = -1;

                    if (NewDistance < NewDistance2)
                        NewDistance = NewDistance2;

                    if (NewDistance > maxSimilarity)
                    {

                        foreach (var item in equalMinDistanceDictWordList.ToList())
                        {
                            if (item.Value <= NewDistance - distancethreshold)
                                equalMinDistanceDictWordList.Remove(item.Key);
                        }

                        tempReplacement = temp;
                        if (!equalMinDistanceDictWordList.ContainsKey(temp))
                            equalMinDistanceDictWordList.Add(temp, NewDistance);
                        //else
                        //    equalMinDistanceDictWordList[tempReplacement] = NewDistance;
                        maxSimilarity = NewDistance;
                    }
                    else if (NewDistance <= maxSimilarity + distancethreshold && NewDistance >= maxSimilarity - distancethreshold && NewDistance > 0)
                        if (!equalMinDistanceDictWordList.ContainsKey(temp))
                            equalMinDistanceDictWordList.Add(temp, NewDistance);


                }
            }
            catch (Exception e)
            {
                throw e;
            }
            return maxSimilarity;
        }
        public static double findSimilarDictionaryWord(string word, double maxSimilarity, int index, List<string> equalMinDistanceDictWordList)
        {
            index = index - _minWordLength;
            word = word.ToLower();
            double NewSimilarity = 0;
            int WordLength = word.Length;
            if ((WordLength + index) < 0)
                return maxSimilarity;
            if ((WordLength + index) >= _IndexDictionary.Length)
                return maxSimilarity;
            if (_IndexDictionary[WordLength + index] == null)
                return maxSimilarity;

            for (int j = 0; j < _IndexDictionary[WordLength + index].Count; j++)
            {
                JaroWinklerDistance JaroDist = new JaroWinklerDistance();
                NGramDistance ng = new NGramDistance();
                JaccardDistance jd = new JaccardDistance();

                NewSimilarity = jd.GetDistance(word, _IndexDictionary[WordLength + index][j]);//(double)JaroDist.GetDistance(word, _IndexDictionary[WordLenght - 1 + index][j]);

                if (NewSimilarity > maxSimilarity)
                {
                    equalMinDistanceDictWordList.Clear();
                    equalMinDistanceDictWordList.Add(_IndexDictionary[WordLength + index][j]);
                    maxSimilarity = NewSimilarity;
                }
                else if (NewSimilarity == maxSimilarity)
                    equalMinDistanceDictWordList.Add(_IndexDictionary[WordLength + index][j]);
            }
            return maxSimilarity;
        }
        public static void readDictionary(string DictionaryPath)
        {
            if (dictProcessed) return;
            dictProcessed = true;
            StreamReader file = new StreamReader(DictionaryPath);   /// relative path
            List<string> Dictionary = new List<string>();
            string line;

            // read dictionary
            while ((line = file.ReadLine()) != null)
            {
                if (_IndexDictionary[line.Length] == null)
                    _IndexDictionary[line.Length] = new List<string>();
                _IndexDictionary[line.Length].Add(line);
            }
        }
        public static TessResult getDictionaryWord(TessResult tr, int dictionaryExactMatchStringLength)
        {
            if (tr.tess_word3 == null || tr.tess_word3.Length < _minWordLengthForDictionaryComparison || tr.tess_word3.Length < dictionaryExactMatchStringLength)
            {
                tr.id ="-1";
                return tr;
            }
            List<string> dictionaryResultList = new List<string>();
            List<string> tempInputFragments = new List<string>(tr.tess_word3.Split(' '));
            if (tr.tess_word3.Split(' ').Length > 1)
                tempInputFragments.Add(tr.tess_word3);
            string[] InputFragments = new string[tempInputFragments.Count];
            int len = 0;
            foreach (string substr in tempInputFragments)
            {
                InputFragments[len++] = substr;
            }

            string FinalReplacement = "";

            double maxSimilarity = 0;
            double avg_similarity = 0;
            int word_count = 0;
            Dictionary<string, double> equalMaxSimilarDictWordList = new Dictionary<string, double>();
            List<string> equalMinDistanceDictWordList = new List<string>();
            string Replacement = "";
            string combinedMatch = "";
            string individualMatch = "";
            List<string> sameMatch = new List<string>();

            try
            {
                for (int k = 0; k < InputFragments.Length; k++)
                {
                    equalMaxSimilarDictWordList.Clear();
                    maxSimilarity = 0;
                    if (InputFragments[k].Length < 2)
                        continue;
                    word_count++;

                    InputFragments[k] = InputFragments[k].ToLower();


                    int WordLength = InputFragments[k].Length;
                   	if(WordLength==dictionaryExactMatchStringLength)
		            {
                        maxSimilarity = findSimilarDictionaryWord(InputFragments[k], maxSimilarity, 0, equalMinDistanceDictWordList);
			            if (maxSimilarity != 1)
			            {
				            maxSimilarity = 0;
			            }
						else
							combinedMatch+=equalMinDistanceDictWordList[0];
		            }
		            else
			            for (int m = 0; m < _maxWordLength; m++)
			            {
                            maxSimilarity = findSimilarDictionaryWord(InputFragments[k], maxSimilarity, m, equalMinDistanceDictWordList);
			            }
                    avg_similarity += maxSimilarity;
                    if (maxSimilarity > 0.33)
                        combinedMatch += equalMinDistanceDictWordList[0];
                    //if (maxSimilarity < 0.33) //dictionary word not found (most similar is 1) hill vs hall = 0.333333
                    //   Replacement = InputFragments[k];
                    //else
                    /*{
                        if (k < InputFragments.Length - 1)
                        {
                            combinedMatch += tempReplacement;
                            combinedMatch += " ";
                        }
                    }
             
                    if (k < InputFragments.Length - 1)
                    {
                        Replacement = "";
                        foreach (var item in equalMaxSimilarDictWordList)
                        {
                            if (item.Value == maxSimilarity)
                            {
                                Replacement = item.Key;
                                break;
                            }
                        }
                        combinedMatch += Replacement + " ";
                    }*/
                    

                }
                equalMaxSimilarDictWordList.Add(combinedMatch, 1.1);

                double maxscore = -1;
                string maxstr = "";
                Dictionary<string, int> matchVal = new Dictionary<string, int>();

                foreach (var item in equalMaxSimilarDictWordList)
                {

                    int score = NeedlemanWunsch.findSimScore(item.Key, tr.tess_word3);
                    matchVal.Add(item.Key, score);

                    if (score > maxscore)
                    {
                        maxscore = score;
                        maxstr = item.Key;
                    }
                }

                foreach (var sameitem in matchVal)
                {
                    if (maxscore == sameitem.Value)
                        sameMatch.Add(sameitem.Key);
                }
				
				foreach (var item in equalMinDistanceDictWordList)
                { 

                    int score = NeedlemanWunsch.findSimScore(item, tr.tess_word3);
                    matchVal.Add(item, score);

                    if (score > maxscore)
                    {
                        maxscore = score;
                        maxstr = item;
                    }
                }

                foreach (var sameitem in matchVal)
                {
                    if (maxscore == sameitem.Value)
                        sameMatch.Add(sameitem.Key);
                }

                // Replacement += equalMaxSimilarDictWordList[0]; // get the first dictionary word
                Replacement = maxstr;
                FinalReplacement = Replacement;
                maxSimilarity = maxscore;


            }


            catch (Exception e)
            {
                Log.WriteLine("Check Dictionary: " + e.Message);
                throw e;
            }
            tr.dict_word3 = FinalReplacement.Trim();

            sameMatch = sameMatch.Distinct().ToList();
            string sameMatches = "";
           // sameMatches = String.Join(" , ", sameMatch);
            foreach (string str in sameMatch)
                sameMatches += str + "  , ";
            if (sameMatches.Length > 0)
                sameMatches = sameMatches.Remove(sameMatches.Length - 1);
            tr.sameMatches = sameMatches;

            if (word_count == 0)
                tr.dict_similarity = 0;
            else
                tr.dict_similarity = maxSimilarity;
            // tr.dict_similarity = avg_similarity / Convert.ToDouble(word_count);
            return tr;
        }
    }
}