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
using System.Drawing;
using System.IO;
using System.Linq;
using Npgsql;
using Nest;
//using Math;



namespace Strabo.Core.TextRecognition
{
    //public class DictResult
    //{
    //    public string text;
    //    public double similarity;
    //    public double word_count=1;
    //}
    public class CheckDictionaryElasticSearch
    {
        static int _maxNumCharacterInAWord = 30;
        static int _maxWordLength = 6;
        static int _minWordLength = 2;
        static double _minWordSimilarity = 0.33;
        static int _minWordLengthForDictionaryComparison = 3;
        static List<string>[] _IndexDictionary = new List<string>[_maxNumCharacterInAWord];
        static bool dictProcessed = false;
        static int _dictionaryExactMatchStringLength;
        static string database = "dictionary";
        static string user = "postgres";
        static string password = "Dornsife123";
        static string server = "localhost";
        static string port = "5432";
        static string connstring = String.Format("Server={0};Port={1};" +
                "User Id={2};Password={3};Database={4};CommandTimeout=0",
                server, port, user,
                password, database);
        static NpgsqlConnection conn = new NpgsqlConnection(connstring);
        static NpgsqlCommand command;
        static NpgsqlDataReader dr;
        static ElasticClient client = new ElasticClient();
        public static HashSet<string> geo_dictionary = new HashSet<string>();

        public class Word
        {
            public int word_id { get; set; }
            public int word_length { get; set; }
            public string word_name { get; set; }
            public string entity_id { get; set; }
            public long word_count { get; set; }

        }

        public class Frequency
        {
            public int front { get; set; }
            public int back { get; set; }
            public int word_id { get; set; }
            public string entity_id { get; set; }
            public long word_count { get; set; }
            public string word_name { get; set; }
        }

        private static string reverseWord(string word)
        {
            char[] charArray = word.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }
        public static double findSimilarDictionaryWord(string word, double maxSimilarity, int index, List<string> equalMinDistanceDictWordList, bool exact)
        {
            index = index - _minWordLength;
            int WordLength = word.Length;
            int index2 = index;
            if (index < 0 || (WordLength>=2 && char.IsUpper(word[0])&&!char.IsUpper(word[1])) )
                index2 = 0;
            word = word.ToLower();
            bool noSpace = false;
            if (word.CompareTo(word.Trim()) == 0)
                noSpace = true;
            else
                word = word.Trim();

            

            double NewSimilarity = 0;
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
                string temp = _IndexDictionary[WordLength + index][j];
                if(noSpace&&temp.CompareTo(word)==0)
                {
                    equalMinDistanceDictWordList.Clear();
                    equalMinDistanceDictWordList.Add(temp);
                    return 10;
                }
                else if (temp.Contains(word))
                {
                    equalMinDistanceDictWordList.Add(/*item);*/temp);
                    maxSimilarity = 1;
                }
                else if(index <= 2)
                {
                    for (int i = 0; i <= index2; i++)
                    {
                        string s = temp.Substring(i);
                        string s2 = temp.Substring(0, temp.Length - index2);
                        //Console.WriteLine(item);
                        if (!exact)
                            NewSimilarity = Math.Max(jd.GetDistance(word, s), jd.GetDistance(word, s2));
                        else
                        {
                            NewSimilarity = jd.GetDistance(word, temp);
                            if (NewSimilarity == 1)
                            {
                                equalMinDistanceDictWordList.Clear();
                                equalMinDistanceDictWordList.Add(s);
                                maxSimilarity = NewSimilarity;
                            }
                            return maxSimilarity;
                        }

                        if (NewSimilarity > .33)
                        {
                            //equalMinDistanceDictWordList.Clear();
                            equalMinDistanceDictWordList.Add(/*item);*/temp);
                            maxSimilarity = NewSimilarity;
                            break;
                        }
                        
                    }
                }
                           }
            return maxSimilarity;
        }

        public static void readDictionary(double top, double left, double bottom, double right)
        {
            if (!dictProcessed)
                conn.Open();
            geo_dictionary.Clear();
            HashSet<string> id = new HashSet<string>();
            string sql = "SELECT id FROM location WHERE location.geom && ST_MakeEnvelope(" + left + ", " + top + ", " + right + ", " + bottom + ", 4326)";
            command = new NpgsqlCommand(sql, conn);
            dr = command.ExecuteReader();

            while (dr.Read())
            {
                id.Add(dr[0].ToString());
            }

            foreach (var item in id)
            {
                sql = "SELECT name FROM entity WHERE id = " + item;
                command = new NpgsqlCommand(sql, conn);
                dr = command.ExecuteReader();
                while (dr.Read())
                {
                    foreach (var item2 in dr[0].ToString().Trim().Split(new Char[] { ',', ' ', '&', '-', ':', '[', ']', '/', '(', ')', '.' }))
                    {
                        geo_dictionary.Add(item2.ToLower());
                    }
                }
            }


            //foreach (var item in geo_dictionary)
                //Log.WriteLine(item);
            if (dictProcessed) return;
            dictProcessed = true;
            //StreamReader file = new StreamReader(DictionaryPath);   /// relative path
            //List<string> Dictionary = new List<string>();
            //string line;

            //// read dictionary
            //while ((line = file.ReadLine()) != null)
            //{
            //    if (_IndexDictionary[line.Length] == null)
            //        _IndexDictionary[line.Length] = new List<string>();
            //    _IndexDictionary[line.Length].Add(line);
            //}

            
        }

        private static string NeedlemanWunschTiebreaker(IEnumerable<Frequency> candidates, string text, bool front, bool back)
        {
            double maxscore = -1;
            string maxstr = "";
            long maxfreq = 0;
            List<string> geo = new List<string>();
            foreach (var candidate in candidates)
            {
                string item = candidate.word_name;
                float score = NeedlemanWunsch.findSimScore(item, text);

                //Log.WriteLine("Needleman Original text is: " + text + ". Compare to: " + item + ". Score: " + score.ToString() +  ". In BB: " + geo_dictionary.Contains(item) + ". Front: " + front + ". Back: " + back);

                if (score >= maxscore-3)
                {
                    
                    long freq = -1;
                    if (front)
                        freq = candidate.front;
                    else if (back)
                        freq = candidate.back;
                    else
                        freq = candidate.word_count - candidate.front - candidate.back;

                    var checkExact = client.Search<Frequency>(q => q
                        .From(0)
                        .Size(100)
                        .Index("general_terms")
                        .Type("general_terms")
                        .Query(fq => fq
                          .Filtered(fqq => fqq
                            .Query(qq => qq.MatchAll())
                            .Filter(ff => ff
                              .Bool(b => b
                                .Must(m1 => m1.Term("name", item.ToLower()))
                                )
                              )
                            )
                          )
                        );
                    bool general = checkExact.Documents.Count() > 0;
                    bool bb = geo_dictionary.Contains(item);
                    if (bb)
                        score += 3;
                    if (general)
                        score += 3;

                    if (score > maxscore)
                    {
                        maxfreq = Convert.ToInt64(freq);
                        maxscore = score;
                        maxstr = item;
                    }
                    else if (score == maxscore && freq>maxfreq)
                    {
                        maxfreq = freq;
                        maxscore = score;
                        maxstr = item;
                    }
                    //Log.WriteLine("Needleman Original text is: " + text + ". Compare to: " + item + ". Score: " + score.ToString() + ". Frequency: " + freq.ToString() + ". In BB: " + geo_dictionary.Contains(item) + ". Front: "+front+". Back: "+back);
                }
            }
            //Log.WriteLine("Original: "+text+".  Winner: " + maxstr + ".  Score: " + maxscore+".  Freq: "+maxfreq);
            return maxstr;
        }

        private static DictResult checkOneWord(string text, bool front, bool back)
        {
            double maxSimilarity = 0;
            DictResult dictR = new DictResult();
            List<string> equalMinDistanceDictWordList = new List<string>();
  
            var checkExact = client.Search<Frequency>(q => q
                .From(0)
                 .Size(100)
                .Index("frequency")
                .Type("frequency")
				.Query(fq => fq
                .Filtered(fqq => fqq
                    .Query(qq => qq.MatchAll())
                    .Filter(ff => ff
                        .Bool(b => b
                            .Must(m1 => m1.Term("word_name", text.ToLower()))
                        )
                    )
                )
				)
			);
            if (text.Length < _dictionaryExactMatchStringLength)
            {
                dictR.similarity = 0;
                dictR.text = "";
            }
            else if (text.Length == _dictionaryExactMatchStringLength)//short strings are looking for the exact match
            {
                //maxSimilarity = findSimilarDictionaryWord(text, maxSimilarity, 0, equalMinDistanceDictWordList, true);
                if (checkExact.Documents.Count()==0)
                {
                    dictR.similarity = 0;
                    dictR.text = "";
                }
                else
                {
                    dictR.similarity = maxSimilarity;
                    dictR.text = text;//NeedlemanWunschTiebreaker(equalMinDistanceDictWordList, text);
                }
            }
            else if (checkExact.Documents.Count() > 0)
                dictR.text = text;
            else
            {
                string temp = text.ToLower();
                int fuzziness = 1;
                temp = temp + "  ";
                fuzziness += 2;
                //fuzziness+=20;
                if(!(char.IsLower(text[1])&&char.IsUpper(text[0])))
                {
                    temp = "  " + temp;
                    fuzziness += 2;
                }
                if (text.Length > 3)
                    fuzziness++;
                if (text.Length > 4)
                    fuzziness++;
                //if (text.Length > 5)
                //    fuzziness++;
                double minsim = 1- (double) fuzziness/(double) temp.Length-.01;
                var searchResults = client.Search<Frequency>(s => s
                .From(0)
                 .Size(50)
                .TrackScores(true)
                .Index("frequency")
                 .Type("frequency")
                 .Query(q => q
               .FuzzyLikeThis(fz => fz.OnFields(w=>w.word_name).LikeText(text.ToLower()).MaxQueryTerms(20000).MinimumSimilarity(minsim))
               )
              // .Sort(sort => sort.OnField("_score").Descending())
                );
                //Log.WriteLine("EDIT DISTANCE:  " + fuzziness+".  MINSIM:" + minsim);
                    //dictR.similarity = maxSimilarity;
                    //Log.WriteLine("--------------------------------------------------------------------------------------"+text);
                    //equalMinDistanceDictWordList = searchResults.Documents.ToList();
                    var wordList = searchResults.Documents;
                    if (wordList.Count() == 1)
                    {
                        dictR.text = wordList.ToList()[0].word_name;// NeedlemanWunschTiebreaker(equalMinDistanceDictWordList, text);
                        //Log.WriteLine("%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%" + dictR.text);
                    }
                    else
                        dictR.text = NeedlemanWunschTiebreaker(wordList, text, front, back);
                    //Log.WriteLine("--------------------------------------------------------------------------------------");
            }
            dictR.text=dictR.text.Trim('\n','\r',' ');
            return dictR;
        }
        private static DictResult checkOneLine(string text, bool f, bool b)
        {
            DictResult dictRLine = new DictResult();
            dictRLine.similarity = 0;
            dictRLine.text = "";

            List<DictResult> dictionaryResultList = new List<DictResult>();

            string[] InputFragments = text.Split(' ');
            for (int k = 0; k < InputFragments.Length; k++)
            {
                //Zhiyuan Wang: filter the invalid words
                if (InputFragments[k].Length < _dictionaryExactMatchStringLength) continue;
                bool front = false, back = false;
                if (k == 0 && f)
                    front = true;
                else if (k == InputFragments.Length - 1 && b)
                    back = true;
                DictResult dictRWord = checkOneWord(InputFragments[k], front, back);
                dictionaryResultList.Add(dictRWord);
            }
            for (int i = 0; i < dictionaryResultList.Count; i++)
            {
                //if (dictionaryResultList[i].similarity > _minWordSimilarity)
                {
                    if (i == dictionaryResultList.Count - 1)
                        dictRLine.text += dictionaryResultList[i].text;
                    else
                        dictRLine.text += dictionaryResultList[i].text + " ";
                    dictRLine.similarity += dictionaryResultList[i].similarity;
                    dictRLine.word_count++;
                }
            }
            dictRLine.similarity /= (double)(dictRLine.word_count);
            dictRLine.text.Trim();
            return dictRLine;
        }
        private static DictResult checkMultiLines(string text, bool front, bool back)
        {
            DictResult dictRPage = new DictResult();
            dictRPage.similarity = 0;
            dictRPage.text = "";

            List<DictResult> dictionaryResultList = new List<DictResult>();

            string[] InputFragments = text.Split('\n');
            for (int k = 0; k < InputFragments.Length; k++)
            {
                bool tfront = false, tback = false;
                if(k==0&&front)
                    tfront = true;
                else if (k == InputFragments.Length-1 && back)
                    tback= true;

                DictResult dictRWord = checkOneLine(InputFragments[k], tfront, tback);
                dictionaryResultList.Add(dictRWord);
            }
            for (int i = 0; i < dictionaryResultList.Count; i++)
            {
                if (i == dictionaryResultList.Count - 1)
                    dictRPage.text += dictionaryResultList[i].text;
                else
                    dictRPage.text += dictionaryResultList[i].text + "\n";

                dictRPage.similarity += dictionaryResultList[i].similarity * (double)dictionaryResultList[i].word_count;
                dictRPage.word_count = +dictionaryResultList[i].word_count;
            }
            dictRPage.similarity /= (double)(dictRPage.word_count);
            dictRPage.text.Trim();
            return dictRPage; ;
        }
        public static TessResult getDictionaryWord(TessResult tr, int dictionaryExactMatchStringLength)
        {
            _dictionaryExactMatchStringLength = dictionaryExactMatchStringLength;

            if (tr.tess_word3.Contains("ouse"))
                Console.WriteLine("debug");
            try
            {
                if (tr.tess_word3 == null || tr.tess_word3.Length < _dictionaryExactMatchStringLength) //input is invalid
                {
                    tr.id = "-1";
                }
                else
                {
                    DictResult dictR;

                    if (!tr.tess_word3.Contains(" ") && !tr.tess_word3.Contains("\n")) // input is a single word
                        dictR = checkOneWord(tr.tess_word3, tr.front, tr.back);
                    else if (!tr.tess_word3.Contains("\n")) // input is a single line
                        dictR = checkOneLine(tr.tess_word3, tr.front, tr.back);
                    else // input is multi-lines
                        dictR = checkMultiLines(tr.tess_word3, tr.front, tr.back);
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
