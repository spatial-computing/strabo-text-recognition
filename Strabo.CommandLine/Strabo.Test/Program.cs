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
using Strabo.Core.TextRecognition;
using Strabo.Core.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using OpenTK.Graphics.OpenGL;
using Strabo.Core.MachineLearning;

namespace Strabo.Test
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            //**************************************************************************************************************
            // Before you commit and push your code, you need to make sure both tcl.Test() and tcl.TestLocalFiles() run correctly
            //**************************************************************************************************************
            //try
            {
                //string[] filePaths = Directory.GetFiles(Path.GetFullPath(@"C:\Users\yaoyi\Documents\strabo-text-recognition\Strabo.CommandLine\data\"), "*.tiff");
                //for (var i = 0; i < filePaths.Length; i++)
                //{
                //    TestCommandLineWorker tcl = new TestCommandLineWorker();
                //    tcl.TestLocalFile(Path.GetFileName(filePaths[i]), "uscdl-usgs");
                //}
                // ConnectedComponentClassifier ccc = new ConnectedComponentClassifier();
                // ccc.SVMTrainer(5, false);
                TestCommandLineWorker tcl2 = new TestCommandLineWorker();
                tcl2.TestLocalFile("SourceMapImage.png", "cls-os");
                //tcl2.TestLocalFile("D2_CA_ACTON_Authoritative_US_Topos_1959.tif", "wm-usgs");


                //tcl.TestLocalFile("test2.jpg", "uscdl-usgs");
                // tcl.TestLocalFile("USGS-15-CA-brawley-e1957-s1957-p1961_msmc_te.png", "uscdl-usgs");
                //tcl.TestLocalFile("USGS-15-CA-brawley-e1957-s1957-p1961.jpg", "uscdl-usgs");


            }
            //catch (Exception e)
            //{ Log.WriteLine(e.Message); }
        }
        public static void generateDictionary(string srcpathfn, string dstpathfn)
        {
            StreamReader sr = new StreamReader(srcpathfn);
            StreamWriter sw = new StreamWriter(dstpathfn);
            string line = sr.ReadLine();
            HashSet<String> dict = new HashSet<string>();
            while (line != null)
            {
                char[] split = { ' ', '-' };
                line = Regex.Replace(line, @"[^a-zA-Z\s]", " ").Trim();
                string[] tokens = line.Split(split);
                for (int i = 0; i < tokens.Length; i++)
                {
                    string token = tokens[i];
                    if (token.ToUpper() != token && token.ToLower() != token)
                        dict.Add(token.ToLower());
                }
                line = sr.ReadLine();
            }

            foreach (String val in dict)
            {
                sw.WriteLine(val);
            }
            sr.Close();
            sw.Close();
        }
        public static void TestJaccard()
        {
            string fn1 = "stree";
            string fn2 = "street";
            string fn3 = "steere";

            JaccardDistance jd = new JaccardDistance(2);

            double x = jd.GetDistanceFast(fn1, fn2);
            double y = jd.GetDistanceFast(fn1, fn3);

            double z = 1;
        }
        public static void TestQGram()
        {
            string fn1 = "acht";
            string fn2 = "yacht";
            string fn3 = "acha";

            NGramDistance ngd = new NGramDistance();

            double x = ngd.GetDistance(fn1, fn2);
            double y = ngd.GetDistance(fn1, fn3);

            double z = x;
        }
        public static void BuildDictionary()
        {
            Hashtable table = new Hashtable();
            string fn = @"C:\Users\yaoyichi\Desktop\dict_all2.txt";
            string fn2 = @"C:\Users\yaoyichi\Desktop\dict_all3.txt";
            StreamReader sr = new StreamReader(fn);

            StreamWriter sw = new StreamWriter(fn2);

            string line = sr.ReadLine();
            while (line != null)
            {
                char[] split = { ' ', '-', '/', ';', '(', ')', '\'', '_', '#', '\"', '.' };
                string[] token = line.Split(split);
                for (int i = 0; i < token.Length; i++)
                {
                    if (token[i].Length > 1)
                    {
                        if (!table.ContainsKey(token[i].ToLower()))
                        {
                            table.Add(token[i].ToLower(), "");
                            sw.WriteLine(token[i].ToLower());
                        }
                    }
                }
                line = sr.ReadLine();
            }
            sr.Close();
            sw.Close();
        }
        public static void CleanDictionary()
        {
            Hashtable table = new Hashtable();
            string fn = @"C:\Users\yaoyichi\Desktop\dict_all.txt";
            string fn2 = @"C:\Users\yaoyichi\Desktop\dict_all2.txt";

            List<String> dict = new List<string>();
            StreamReader sr = new StreamReader(fn);

            StreamWriter sw = new StreamWriter(fn2);

            string line = sr.ReadLine();
            while (line != null)
            {
                string clean = Regex.Replace(line, @"[^a-zA-Z]", "");
                if (clean.Length == line.Length && clean.Length > 2)
                    dict.Add(clean.ToLower());

                line = sr.ReadLine();
            }
            dict.Sort();
            for (int i = 0; i < dict.Count; i++)
                sw.WriteLine(dict[i]);
            sr.Close();
            sw.Close();
        }
    }
}
