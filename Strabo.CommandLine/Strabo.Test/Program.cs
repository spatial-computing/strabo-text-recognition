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
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Configuration;

using Strabo.Core.TextRecognition;
using Strabo.Core.Utility;

using SpellChecker.Net.Search.Spell;
using System.Drawing;

using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.UI;

namespace Strabo.Test
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            //TestHoughCircle thc = new TestHoughCircle();
            //thc.ApplyEmguCV();
            //**************************************************************************************************************
            // Use TestGetMapFromServiceWorker to get maps from WMS
            //**************************************************************************************************************
            //TestGetMapFromServiceWorker testGMFSWorker = new TestGetMapFromServiceWorker();
            //testGMFSWorker.Apply();
            //**************************************************************************************************************
            // Before you commit and push your code, you need to make sure both tcl.Test() and tcl.TestLocalFiles() run correctly
            //**************************************************************************************************************
            try
            {
                TestCommandLineWorker tcl = new TestCommandLineWorker();
                tcl.testBlockMaps();

                //PointF[] points = new PointF[]
                //{
                //    new PointF(561.629f, 168.07f),
                //    new PointF(317.392f, 129.095f),
                //    new PointF(323.532f, 88.8367f),
                //    new PointF(567.769f, 136.812f)
                //};

                //Rectangle rect = new Rectangle(250, 150, 71, 141);

                //MCvBox2D bbx = PointCollection.MinAreaRect(points);
                //Console.WriteLine("Angle: " + bbx.angle);
                //string vertex = "";
                //foreach(PointF pts in bbx.GetVertices())
                //{
                //    vertex += "(" + pts.X + ", " + pts.Y + ") ";
                //}
                //Console.WriteLine("vertices: " + vertex);
                //Console.WriteLine("Width: " + bbx.size.Width + " Height: " + bbx.size.Height);

                //Image<Bgr, byte> img = new Image<Bgr, byte>(600, 600, new Bgr(Color.White));
                //img.Draw(bbx, new Bgr(Color.Red), 1);

                //foreach (PointF p in points)
                //    img.Draw(new CircleF(p, 2), new Bgr(Color.Green), 1);

                //img.Draw(rect, new Bgr(Color.Black), 1);

                //ImageViewer.Show(img, String.Format("Time used: {0} milliseconds", 200000));

                //tcl.TestLocalTianditu_evaFiles();
                // tcl.TestLocalFiles();
                // tcl.testLocalWMWholeFile();
                // tcl.Test();

                // TestWorkers test_object = new TestWorkers();
                // TestUSGSMaps obj = new TestUSGSMaps();
                // obj.testMaps();
                // test_object.testCommandLineWorker();
                // test_object.testUSGSMaps();
                // test_object.testColorSegmentationWorker();
                //  test_object.testTextLayerExtractionWorker();
            }
            catch (Exception e)
            { Log.WriteLine(e.Message); }
            //**************************************************************************************************************

            //TestTesseract.Test();
            //CleanDictionary();
            //BuildDictionary();
            //TestJaccard();
            //TestQGram();

            //SampleImageSVM SVM = new SampleImageSVM();
            //SVM.cropImage();

            //BoundingBoxDetection bbx = new BoundingBoxDetection();
            //bbx.Rotation();

            //ScalingImage scaling = new ScalingImage(@"C:\Users\nhonarva\Documents\MachineLearning\sampleTraintData\");
            //scaling.Scaling();


            //TestSymbolRecognition test = new TestSymbolRecognition();
            //Console.WriteLine("Hello World!");
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
