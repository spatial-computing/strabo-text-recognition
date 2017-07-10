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
using System.Text.RegularExpressions;
using Strabo.Core.TextRecognition;
using Strabo.Core.Utility;

namespace Strabo.Core.Worker
{
    public class TextRecognitionWorker : IStraboWorker
    {
        public string Apply(string inputDir, string interDir, string outputDir, string srcfileName, int threadNumber)
        {
            return Apply(inputDir, outputDir, StraboParameters.oTDResultFolder,
                Path.GetFileNameWithoutExtension(srcfileName), StraboParameters.language,
                StraboParameters.dictionaryFilePath,
                StraboParameters.dictionaryExactMatchStringLength,
                StraboParameters.elasticsearch, StraboParameters.otd, threadNumber);
        }

        public string Apply(string inputPath, string outputPath, string OTDPath,
            string TesseractResultsJSONFileName, string lng,
            string dictionaryFilePath, int dictionaryExactMatchStringLength,
            bool elasticsearch, bool otd, int threadNumber)
        {
            try
            {
                var wt = new WrapperTesseract();
                var tessOcrResultList = wt.Apply(inputPath, outputPath, lng, threadNumber);

                //foreach (TessResult tr in tessOcrResultList)
                //{
                //    Log.WriteLine("Raw Text: " + tr.tess_raw3 + " Stripped Text:" + tr.tess_word3);
                //}

                //int georef = -1;
                //if (elasticsearch)
                //{
                //    CheckType ct = new CheckType();
                //    tessOcrResultList = ct.Apply(tessOcrResultList, georef, lng);
                //}

                var ctr = new CleanTesseractResult();
                tessOcrResultList = ctr.Apply(tessOcrResultList, dictionaryFilePath, dictionaryExactMatchStringLength,
                    lng, false, 1);

                //foreach (TessResult tr in tessOcrResultList)
                ////{


                //    Log.WriteLine("Raw Text before GeoJSON: " + tr.tess_raw3 + " Text before GeoJSON: " + tr.tess_word3);
                //    // Log.WriteLine("HOCR: " + tr.hocr);
                //}

                //NumberCorrection nc = new NumberCorrection(tessOcrResultList);
                //tessOcrResultList = nc.mergeFeatures();

                WriteToJsonFile(tessOcrResultList, outputPath, OTDPath, TesseractResultsJSONFileName, lng,
                    dictionaryFilePath, dictionaryExactMatchStringLength, -1);
            }
            catch (Exception e)
            {
                Log.WriteLine(e.Message);
                Log.WriteLine(e.Source);
                Log.WriteLine(e.StackTrace);
                throw;
            }
            return TesseractResultsJSONFileName;
        }

        private void WriteToJsonFile(List<TessResult> tessOcrResultList, string outputPath, string OTDPath,
            string TesseractResultsJSONFileName, string lng, string dictionaryFilePath,
            int dictionaryExactMatchStringLength, int georef)
        {
            Log.WriteLine("Writing results to GeoJSON...");
            QGISJson.path = outputPath;
            QGISJson.Start();
            QGISJson.filename = TesseractResultsJSONFileName;
            for (var i = 0; i < tessOcrResultList.Count; i++)
            {
                var items = new List<KeyValuePair<string, string>>();
                if (tessOcrResultList[i].dict_word3 != null && tessOcrResultList[i].dict_word3.Length > 0)
                    tessOcrResultList[i].dict_word3 = Regex.Replace(tessOcrResultList[i].dict_word3, "\n\n", "");

                //if (tessOcrResultList[i].dict_word3 != null && tessOcrResultList[i].dict_word3.Length > 0) tessOcrResultList[i].dict_word3 = Regex.Replace(tessOcrResultList[i].dict_word3, "\n", "");
                items.Add(new KeyValuePair<string, string>("NameAfterDictionary", tessOcrResultList[i].dict_word3));

                if (tessOcrResultList[i].tess_word3.Length > 0)
                    tessOcrResultList[i].tess_word3 = Regex.Replace(tessOcrResultList[i].tess_word3, "\n\n", "");
                if (tessOcrResultList[i].tess_word3.Length > 0)
                    tessOcrResultList[i].tess_word3 = Regex.Replace(tessOcrResultList[i].tess_word3, "\"", "");
                if (tessOcrResultList[i].tess_word3.Length > 0)
                    tessOcrResultList[i].tess_word3 = Regex.Replace(tessOcrResultList[i].tess_word3, "\n", "");

                items.Add(new KeyValuePair<string, string>("NameBeforeDictionary", tessOcrResultList[i].tess_word3));
                items.Add(new KeyValuePair<string, string>("ImageId", tessOcrResultList[i].id));
                items.Add(new KeyValuePair<string, string>("DictionaryWordSimilarity",
                    tessOcrResultList[i].dict_similarity.ToString()));
                items.Add(new KeyValuePair<string, string>("TesseractCost",
                    tessOcrResultList[i].tess_cost3.ToString()));
                items.Add(new KeyValuePair<string, string>("SameMatches", tessOcrResultList[i].sameMatches));
                QGISJson.AddFeature(tessOcrResultList[i], georef, items);
            }
            QGISJson.WriteGeojsonFiles();
            Log.WriteLine("GeoJSON generated");
        }
    }
}