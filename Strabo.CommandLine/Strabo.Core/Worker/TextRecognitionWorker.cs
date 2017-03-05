
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

using Strabo.Core.TextRecognition;
using Strabo.Core.Utility;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;
using System.IO;



namespace Strabo.Core.Worker
{
    public class TextRecognitionWorker
    {
        public TextRecognitionWorker() { }
        public void Apply(string inputPath, string outputPath,string TesseractResultsJSONFileName, double top, double left, double bottom, double right)
        {
            Apply(inputPath, outputPath, StraboParameters.oTDResultFolder, TesseractResultsJSONFileName, StraboParameters.language, StraboParameters.dictionaryFilePath, StraboParameters.dictionaryExactMatchStringLength,
                Double.Parse(MapServerParameters.BBOXW), Double.Parse(MapServerParameters.BBOXN), MapServerParameters.xscale, MapServerParameters.yscale, MapServerParameters.srid, top, left, bottom, right, StraboParameters.elasticsearch, StraboParameters.otd);
        }
        public void Apply(string inputPath, string outputPath, string OTDPath ,string TesseractResultsJSONFileName, string lng, string dictionaryFilePath, int dictionaryExactMatchStringLength, double bbxW, double bbxN, double xscale, double yscale, string srid, double top, double left, double bottom, double right, bool elasticsearch, bool otd)
        {
            try
            {
                string tessPath = "";
                //Zhiyuan Wang: use Tesseract data of English for numbers.
                string tesslng = (lng == "num") ? "eng" : lng;
                WrapperTesseract wt = new WrapperTesseract(tessPath, tesslng);
                List<TessResult> tessOcrResultList = wt.Apply(inputPath, outputPath);

                int georef = -1;
                if (elasticsearch)
                {
                    CheckType ct = new CheckType();
                    tessOcrResultList = ct.Apply(tessOcrResultList, georef, lng);
                }

                CleanTesseractResult ctr = new CleanTesseractResult();
                if (!otd)
                {
                    tessOcrResultList = ctr.Apply(tessOcrResultList, dictionaryFilePath, dictionaryExactMatchStringLength, lng, top, left, bottom, right, elasticsearch);
                    WriteToJsonFile(tessOcrResultList, outputPath, OTDPath, TesseractResultsJSONFileName, lng, dictionaryFilePath, dictionaryExactMatchStringLength, bbxW, bbxN, xscale, yscale, srid, georef);
                }
                if (otd)
                {
                    ////////   create a new folder for OTD results///////////
                    if (!Directory.Exists(inputPath + OTDPath))
                        Directory.CreateDirectory(inputPath + OTDPath);
                    DirectoryInfo TheFolder = new DirectoryInfo(inputPath + OTDPath);
                    if (TheFolder.GetFiles() != null)
                        foreach (FileInfo NextFile in TheFolder.GetFiles())
                        {
                            try
                            {
                                File.Delete(NextFile.FullName);
                            }
                            catch (Exception e)
                            {
                                Log.WriteLine(e.ToString());
                            }
                        }
                    ////////   create a new folder for OTD results///////////

                    ///// OTD //////////
                    Strabo.Core.TextRecognition.AddNeighborhood Neighbors = new Strabo.Core.TextRecognition.AddNeighborhood();
                    Neighbors.Apply(tessOcrResultList, OTDPath, inputPath);
                    tessOcrResultList = wt.Apply(inputPath + OTDPath, outputPath);

                    if (elasticsearch)
                    {
                        CheckType ct = new CheckType();
                        tessOcrResultList = ct.Apply(tessOcrResultList, georef, lng);
                    }


                    tessOcrResultList = ctr.Apply(tessOcrResultList, dictionaryFilePath, dictionaryExactMatchStringLength, lng, top, left, bottom, right, elasticsearch);

                    WriteToJsonFile(tessOcrResultList, outputPath, OTDPath, TesseractResultsJSONFileName, lng, dictionaryFilePath, dictionaryExactMatchStringLength, bbxW, bbxN, xscale, yscale, srid, georef);
                }
            }
            catch (Exception e)
            {
                Log.WriteLine(e.Message);
                Log.WriteLine(e.Source);
                Log.WriteLine(e.StackTrace);
                throw;
            }

        }

        void WriteToJsonFile(List<TessResult> tessOcrResultList, string outputPath, string OTDPath, string TesseractResultsJSONFileName, string lng, string dictionaryFilePath, int dictionaryExactMatchStringLength, double bbxW, double bbxN, double xscale, double yscale, string srid, int georef)
        {
            Log.WriteLine("Writing results to GeoJSON...");
            QGISJson.path = outputPath;
            QGISJson.Wx = bbxW;
            QGISJson.Ny = bbxN;
            QGISJson.yscale = yscale;
            QGISJson.xscale = xscale;
            QGISJson.srid = srid;
            QGISJson.Start();
            QGISJson.filename = TesseractResultsJSONFileName ;
            for (int i = 0; i < tessOcrResultList.Count; i++)
            {
                List<KeyValuePair<string, string>> items = new List<KeyValuePair<string, string>>();
                if (tessOcrResultList[i].dict_word3 != null && tessOcrResultList[i].dict_word3.Length > 0) tessOcrResultList[i].dict_word3 = Regex.Replace(tessOcrResultList[i].dict_word3, "\n\n", "");
                //if (tessOcrResultList[i].dict_word3 != null && tessOcrResultList[i].dict_word3.Length > 0) tessOcrResultList[i].dict_word3 = Regex.Replace(tessOcrResultList[i].dict_word3, "\n", "");
                items.Add(new KeyValuePair<string, string>("NameAfterDictionary", tessOcrResultList[i].dict_word3));
                if (tessOcrResultList[i].tess_word3.Length > 0) tessOcrResultList[i].tess_word3 = Regex.Replace(tessOcrResultList[i].tess_word3, "\n\n", "");
                if (tessOcrResultList[i].tess_word3.Length > 0) tessOcrResultList[i].tess_word3 = Regex.Replace(tessOcrResultList[i].tess_word3, "\"", "");
                if (tessOcrResultList[i].tess_word3.Length > 0) tessOcrResultList[i].tess_word3 = Regex.Replace(tessOcrResultList[i].tess_word3, "\n", "");
                items.Add(new KeyValuePair<string, string>("NameBeforeDictionary", tessOcrResultList[i].tess_word3));
                items.Add(new KeyValuePair<string, string>("ImageId", tessOcrResultList[i].id));
                items.Add(new KeyValuePair<string, string>("DictionaryWordSimilarity", tessOcrResultList[i].dict_similarity.ToString()));
                items.Add(new KeyValuePair<string, string>("TesseractCost", tessOcrResultList[i].tess_cost3.ToString()));
                items.Add(new KeyValuePair<string, string>("SameMatches", tessOcrResultList[i].sameMatches));
                QGISJson.AddFeature(tessOcrResultList[i].x, tessOcrResultList[i].y, tessOcrResultList[i].h, tessOcrResultList[i].w, georef, items);
            }
            QGISJson.WriteGeojsonFiles();
            Log.WriteLine("GeoJSON generated");
        }
    }
}


