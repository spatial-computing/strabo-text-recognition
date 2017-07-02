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

using Emgu.CV;
using Emgu.CV.Structure;
using Strabo.Core.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using Tesseract;

namespace Strabo.Core.TextRecognition
{
    /// <summary>
    /// Wrapper class for Tesseract OSC Engine
    /// </summary>
    public class WrapperTesseract
    {
        private List<TesseractEngine> _engine = new List<TesseractEngine>();
        public int _short_word_length = 2;
        List<string> filepathfn = new List<string>();
        List<TessResult> tessOcrResultList = new List<TessResult>();
        int num_engine = 2;
        public WrapperTesseract()
        {
            
        }

        private TesseractEngine setTessEngine(string lng)
        {
            //Path should be same as TESSDATA folder
            //This path should always end with a "/" or "\", e.g., TESSDATA_PREFIX="/usr/share/tesseract-ocr/"

            ////// Emgu Tesseract(Tesseract3.1)///////
            // _ocr = new Emgu.CV.OCR.Tesseract(path, lng, Emgu.CV.OCR.Tesseract.OcrEngineMode.OEM_TESSERACT_CUBE_COMBINED);
            // _ocr.SetVariable("tessedit_char_whitelist", "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ");
            // _ocr.SetVariable("user_words_suffix", "user-words");
            //  _ocr.SetVariable("chop_enable", "1");
            //  _ocr.SetVariable("tessedit_char_blacklist", "¢§+~»~`!@#$%^&*()_+-={}[]|\\:\";\'<>?,./");
            ////// Emgu Tesseract(Tesseract3.1)///////
            TesseractEngine engine = null;
            Log.WriteLine("Setting Tesseract traindata and language");
            //////  Tesseract 3.2 ////////
            if (lng == "eng")
            {
                engine = new TesseractEngine(@"./tessdata3/", lng, EngineMode.TesseractAndCube);
            }
            else if (lng == "num")
            {
                // NUMBER DETECTION IS DUBIOUS
                // Just detect numbers
                // Will not work great on low resolution images
                // Useful StackOverFlow URL: http://stackoverflow.com/questions/38336601/ocr-tesseractengine
                // Log.WriteLine("Filtering only numbers");

                // _engine = new TesseractEngine(@"./tessdata3", "eng", EngineMode.TesseractAndCube);
                // _engine.SetVariable("tessedit_char_whitelist", "0123456789");

                lng = "eng";
                engine = new TesseractEngine(@"./tessdata3/", lng, EngineMode.Default);
            }
            else
            {
                engine = new TesseractEngine(@"./tessdata3/", lng, EngineMode.Default);
            }
            Log.WriteLine("Tesseract Version: " + engine.Version);
            return engine;
            //_engine.SetVariable("tessedit_char_whitelist", "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ");
            //_engine.SetVariable("tessedit_char_blacklist", "¢§+~»~`!@#$%^&*()_+-={}[]|\\:\";\'<>?,./");
            //////  Tesseract 3.2 ////////
        }
        public List<TessResult> Apply(string inputPath, string outputPath, string lng, int threadNumber)
        {
            string[] filePaths = Directory.GetFiles(inputPath, "*.png");
            if (filePaths.Length == 0)
                return null;
            for (int i = 0; i < filePaths.Length; i++)
            {
                // if (i == 176)
                //Console.WriteLine("debug");
                string filename = Path.GetFileNameWithoutExtension(filePaths[i]);
                String[] splitTokens = filename.Split('_');

                if (splitTokens.Length == 17)
                {
                    filepathfn.Add(filePaths[i]);
                    
                } 
            }
            for(int i=0;i<num_engine;i++)
                _engine.Add(setTessEngine(lng));

            try
            {
                Log.WriteLine("Tessearct in progress...");

                Thread[] thread_array = new Thread[num_engine];
                for (int i = 0; i < num_engine; i++)
                {
                    thread_array[i] = new Thread(new ParameterizedThreadStart(recognize));
                    thread_array[i].Start(i);
                }
                for (int i = 0; i < num_engine; i++)
                    thread_array[i].Join();
                Log.WriteLine("Tessearct finished");
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

        public void recognize(object s)
        {
            int x = (int)s;

            for (int i = 0; i < filepathfn.Count; i++)
            {
                if (i % num_engine != x) continue;
                string filename = Path.GetFileNameWithoutExtension(filepathfn[i]);
                String[] splitTokens = filename.Split('_');

                using (Image<Gray, Byte> image = new Image<Gray, byte>(filepathfn[i]))
                {
                    string text = "";
                    float conf = 0;
                    var img = Pix.LoadFromFile(filepathfn[i]);
                    Page page;
                    page = _engine[i % num_engine].Process(img, PageSegMode.SingleBlock);
                    text = page.GetText();

                    string HOCR = page.GetHOCRText(1);

                    // PageIteratorLevel a = new PageIteratorLevel();
                    //Rect boundingbox;
                    //page.AnalyseLayout().TryGetBoundingBox(a, out boundingbox);
                    string h = page.GetHOCRText(1);
                    conf = page.GetMeanConfidence();
                    page.Dispose();

                    TessResult tr = new TessResult();

                    if (text.Length > 0 && RemoveNoiseText.NotTooManyNoiseCharacters(text))
                    {
                        // Console.WriteLine(text);
                        tr.id = splitTokens[0];

                        tr.tess_word3 = Regex.Replace(text, "\n\n", "");
                        tr.tess_word3 = Regex.Replace(tr.tess_word3, "\nn", "");
                        tr.tess_raw3 = text;
                        tr.tess_cost3 = (-1 * conf) + 1;
                        tr.hocr = HOCR;


                        tr.fileName = Path.GetFileName(filename);

                        tr.mcX = (int)Convert.ToDouble(splitTokens[3]);
                        tr.mcY = (int)Convert.ToDouble(splitTokens[4]);

                        tr.x = (int)Convert.ToDouble(splitTokens[7]);
                        tr.y = (int)Convert.ToDouble(splitTokens[8]);

                        tr.x2 = (int)Convert.ToDouble(splitTokens[9]);
                        tr.y2 = (int)Convert.ToDouble(splitTokens[10]);
                        tr.x3 = (int)Convert.ToDouble(splitTokens[11]);
                        tr.y3 = (int)Convert.ToDouble(splitTokens[12]);
                        tr.x4 = (int)Convert.ToDouble(splitTokens[13]);
                        tr.y4 = (int)Convert.ToDouble(splitTokens[14]);

                        tr.w = (int)Convert.ToDouble(splitTokens[15]);
                        tr.h = (int)Convert.ToDouble(splitTokens[16]);
                        tessOcrResultList.Add(tr);
                    }
                }
            }
        }
    }
}
