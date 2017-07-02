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

using Strabo.Core.Utility;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Strabo.Core.Worker
{
    /// <summary>
    /// Read App.Config-> GetMapFromWMS/WMTS/Local -> ColorSegmentation -> ExtractTexLayerFromMapWorker -> TextDetectionWorker -> TextRecognitionWorker
    /// </summary>

    public struct BoundingBox
    {
        public string BBW;
        public string BBN;
    }

    public struct InputArgs
    {
        public BoundingBox bbx;
        public string intermediatePath;
        public string outputPath;
        public string outputFileName;
        public string mapLayerName;
        public int threadNumber;
    }

    public class CommandLineWorker
    {
        public CommandLineWorker() { }
        private void SetFolder(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            DirectoryInfo TheFolder = new DirectoryInfo(path);
            if (TheFolder.GetDirectories() != null)
                foreach (DirectoryInfo NextDir in TheFolder.GetDirectories())
                {
                    try
                    {
                        Directory.Delete(NextDir.FullName);
                    }
                    catch (Exception e)
                    {
                        //Log.WriteLine(e.Message);
                        //var errorCode = Marshal.GetHRForException(e) & ((1 << 16) - 1);
                        //if (errorCode == 32 || errorCode == 33)
                        //    Log.WriteLine("Don't worry. Everything is fine!");
                    }
                }
            if (TheFolder.GetFiles() != null)
                foreach (FileInfo NextFile in TheFolder.GetFiles())
                {
                    try
                    {
                        if (!(NextFile.FullName.EndsWith(StraboParameters.sourceMapFileName) || NextFile.FullName.EndsWith("gitkeep") || NextFile.FullName.EndsWith("log.txt") || NextFile.FullName.EndsWith(StraboParameters.sourceMapFileName)))
                        {
                            File.Delete(NextFile.FullName);
                        }
                    }
                    catch (Exception e)
                    {
                        Log.WriteLine(e.Message);
                        var errorCode = Marshal.GetHRForException(e) & ((1 << 16) - 1);
                        if (errorCode == 32 || errorCode == 33)
                            Log.WriteLine("Don't worry. Everything is fine!");
                    }
                }
        }
        public void Apply(InputArgs inputArgs, string path)
        {
            ColorSegmentationWorker _colorSegmentationWorker = new ColorSegmentationWorker();
            TextExtractionWorker _textExtractionWorker = new TextExtractionWorker();
            TextCleaningWorker _textCleaningWorker = new TextCleaningWorker();
            TextDetectionWorker _textDetectionWorker = new TextDetectionWorker();
            TextRecognitionWorker _textRecognitionWorker = new TextRecognitionWorker();

            //set log folders
            Log.SetLogDir(inputArgs.intermediatePath);
            Log.SetOutputDir(inputArgs.intermediatePath);
            Log.WriteLine("************Strabo Starts************");
            Log.WriteLine("CommandLineWorker in progress...");
            Log.SetStartTime();

            //read settings
            try
            {
                StraboParameters.readConfigFile(inputArgs.mapLayerName);
            }
            catch (Exception e)
            {
                Log.WriteLine("Error reading app.config: " + e.Message);
                throw;
            }

            Log.WriteLine("Strabo release version:" + StraboParameters.straboReleaseVersion);
            Log.WriteLine("Process map: " + inputArgs.mapLayerName);

            //set and clear folders
            SetFolder(inputArgs.intermediatePath);
            SetFolder(inputArgs.outputPath);

            if (!inputArgs.intermediatePath.EndsWith("\\"))
                inputArgs.intermediatePath = inputArgs.intermediatePath + "\\";
            if (!inputArgs.outputPath.EndsWith("\\"))
                inputArgs.outputPath = inputArgs.outputPath + "\\";

            Log.WriteLine("Initialization finished. Folders checked and cleaned.");

            string intermediate_result_pathfn=path;
            if (StraboParameters.numberOfSegmentationColor > 0)
            {
                try
                {
                    Log.WriteLine("ColorSegmentationWorker in progress...");
                    intermediate_result_pathfn = _colorSegmentationWorker.Apply(Path.GetDirectoryName(path), inputArgs.intermediatePath, inputArgs.outputPath, Path.GetFileName(path), inputArgs.threadNumber);
                    Log.WriteLine("ColorSegmentationWorker finished");
                }
                catch (Exception e)
                {
                    Log.WriteLine("ColorSegmentationWorker: " + e.Message);
                    throw;
                }
            }

            try
            {
                Log.WriteLine("TextExtractionWorker in progress...");
                intermediate_result_pathfn = _textExtractionWorker.Apply(Path.GetDirectoryName(intermediate_result_pathfn), inputArgs.intermediatePath, inputArgs.outputPath, Path.GetFileName(intermediate_result_pathfn), inputArgs.threadNumber);
                Log.WriteLine("TextExtractionWorker finished");
            }
            catch (Exception e)
            {
                Log.WriteLine("ApplyTextExtractionWorker: " + e.Message);
                throw;
            }

            try
            {
                Log.WriteLine("TextDetectionWorker in progress...");
                intermediate_result_pathfn = _textDetectionWorker.Apply(Path.GetDirectoryName(intermediate_result_pathfn), inputArgs.intermediatePath, inputArgs.outputPath, Path.GetFileName(intermediate_result_pathfn), inputArgs.threadNumber);
                Log.WriteLine("ApplyTextDetection finished");

            }
            catch (Exception e)
            {
                Log.WriteLine("ApplyTextDetectionWorker: " + e.Message);
                Log.WriteLine(e.StackTrace);
                throw;
            }

            try
            {
                Log.WriteLine("TextRecognition in progress...");
                intermediate_result_pathfn = _textRecognitionWorker.Apply(Path.GetDirectoryName(intermediate_result_pathfn) + "\\", inputArgs.intermediatePath, inputArgs.outputPath, Path.GetFileName(path), inputArgs.threadNumber);
                Log.WriteLine("TextRecognitionWorker finished");

            }
            catch (Exception e)
            {
                Log.WriteLine("ApplyTextRecognition: " + e.Message);
                throw;
            }

            Log.WriteLine("Execution time: " + Log.GetDurationInSeconds().ToString());
        }
    }
}
