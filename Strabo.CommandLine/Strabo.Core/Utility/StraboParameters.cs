﻿/*******************************************************************************
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
using System.Configuration;

namespace Strabo.Core.Utility
{
    public struct RGBThreshold
    {
        public int upperRedColorThd;
        public int upperGreenColorThd;
        public int upperBlueColorThd;


        public int lowerRedColorThd;
        public int lowerGreenColorThd;
        public int lowerBlueColorThd;
    }
    public static class StraboParameters
    {
        private static RGBThreshold _rgbThreshold;
        private static string _sourceMapFileName;
        private static string _dictionaryFilePath;
        private static string _textLayerOutputFileName;
        private static int _dictionaryExactMatchStringLength;
        private static string _dictionaryWeightsPath;
        private static string _straboReleaseVersion;
        private static int _numberOfSegmentationColor;
        private static int _char_size;
        private static string _language;
        private static bool _elasticsearch;
        private static bool _otd;
        private static string _oTDResultFolder;
        private static int _connectedComponentThreshold;
        private static int _holeThreshold;
        private static double _averagePixel;
        private static double _pixelToSizeRatio;
        private static double _upperToLowerCaseWidthRatio;
        private static double _firstToThirdGroupHeightRatio;
        private static double _firstToSecondGroupHeightRatio;


        public static void readConfigFile(string layer)
        {
            try
            {
                Log.WriteLine("Reading application settings...");

                if (ReadString(layer + ":URL", "") != "")
                    layer += ":";   //setting found
                else
                    layer = "";     //use the default setting
                /*the above setting is false when it's processing local files, 
                where "layer:URL" is usually ""(empty) */


                _rgbThreshold = new RGBThreshold();
                _rgbThreshold.upperBlueColorThd = ReadInt(layer + "UpperBlueColorThreshold", 0);
                _rgbThreshold.upperRedColorThd = ReadInt(layer + "UpperRedColorThreshold", 0);
                _rgbThreshold.upperGreenColorThd = ReadInt(layer + "UpperGreenColorThreshold", 0);
                _rgbThreshold.lowerBlueColorThd = ReadInt(layer + "LowerBlueColorThreshold", 0);
                _rgbThreshold.lowerRedColorThd = ReadInt(layer + "LowerRedColorThreshold", 0);
                _rgbThreshold.lowerGreenColorThd = ReadInt(layer + "LowerGreenColorThreshold", 0);
                _numberOfSegmentationColor = ReadInt(layer + "NumberOfSegmentationColor", 0);
                _char_size = ReadInt(layer + "CharSize", 12);
                _language = ReadString(layer + "Language", "en");
                _elasticsearch = ReadBool(layer + "Elasticsearch", false);
                _otd = ReadBool(layer + "OTD", false);
                _dictionaryFilePath = ReadString(layer + "DictionaryFilePath", "");
                _dictionaryExactMatchStringLength = ReadInt(layer + "DictionaryTwoLetterFilePath", 2);
                _dictionaryWeightsPath = ReadString(layer + "DictionaryWeightsPath", "");

                _oTDResultFolder = ReadString(layer + "OTDResultFolder", "");
                _sourceMapFileName = ReadString("SourceMapFileName", "SourceMapImage.png");
                _textLayerOutputFileName = ReadString("TextLayerOutputFileName", "BinaryOutput.png");
                _straboReleaseVersion = ReadString("Version", "");
                _connectedComponentThreshold = ReadInt(layer + "ConnectedComponentThreshold", 0);
                _holeThreshold = ReadInt(layer + "HoleThreshold", 0);
                _averagePixel = ReadDouble(layer + "AveragePixel", 0);
                _pixelToSizeRatio = ReadDouble(layer + "PixelToSizeRatio", 0);
                _upperToLowerCaseWidthRatio = ReadDouble(layer + "UpperToLowerCaseWidthRatio", 0);
                _firstToThirdGroupHeightRatio = ReadDouble(layer + "FirstToThirdGroupHeightRatio", 0);
                _firstToSecondGroupHeightRatio = ReadDouble(layer + "FirstToSecondGroupHeightRatio", 0);
            }
            catch(Exception e)
            {
                Log.WriteLine(e.Message);
                Log.WriteLine(e.Source);
                Log.WriteLine(e.StackTrace);
                throw;
            }

        }
        private static string ReadString(string key, string default_value)
        {
            return ConfigurationManager.AppSettings[key] != null ? ConfigurationManager.AppSettings[key].ToString() : default_value;
        }
        private static int ReadInt(string key, int default_value)
        {
            return ConfigurationManager.AppSettings[key] != null ? Convert.ToInt16(ConfigurationManager.AppSettings[key].ToString()) : default_value;
        }
        private static double ReadDouble(string key, double default_value)
        {
            return ConfigurationManager.AppSettings[key] != null ? Convert.ToDouble(ConfigurationManager.AppSettings[key].ToString()) : default_value;
        }
        private static bool ReadBool(string key, bool default_value)
        {
            return ConfigurationManager.AppSettings[key] != null ? Convert.ToBoolean(ConfigurationManager.AppSettings[key].ToString()) : default_value;
        }
        public static int numberOfSegmentationColor
        {
            get { return StraboParameters._numberOfSegmentationColor; }
            set { StraboParameters._numberOfSegmentationColor = value; }
        }
        public static string straboReleaseVersion
        {
            get { return _straboReleaseVersion; }
            set { _straboReleaseVersion = value; }
        }
        public static RGBThreshold rgbThreshold
        {
            get { return _rgbThreshold; }
            set { _rgbThreshold = value; }
        }
        public static string textLayerOutputFileName
        {
            get
            {
                return _textLayerOutputFileName;
            }
        }
        public static string sourceMapFileName
        {
            get
            {
                return _sourceMapFileName;
            }
        }
        public static string dictionaryFilePath
        {
            get
            {
                return _dictionaryFilePath;
            }
        }
        public static int dictionaryExactMatchStringLength
        {
            get
            {
                return _dictionaryExactMatchStringLength;
            }
        }

        public static string oTDResultFolder
        {
            get
            {
                return _oTDResultFolder;
            }
        }

        public static int ConnectedComponentThreshold
        {
            get
            {
                return _connectedComponentThreshold;
            }
        }

        public static double AveragePixel
        {
            get
            {
                return _averagePixel;
            }
        }
        public static double PixelToSizeRatio
        {
            get
            {
                return _pixelToSizeRatio;
            }
        }
        public static int HoleThreshold
        {
            get
            {
                return _holeThreshold;
            }
        }
        public static double UpperToLowerCaseWidthRatio
        {
            get
            {
                return _upperToLowerCaseWidthRatio;
            }
        }
        public static double FirstToThirdGroupHeightRatio
        {
            get
            {
                return _firstToThirdGroupHeightRatio;
            }
        }
       public static double FirstToSecondGroupHeightRatio
        {
            get
            {
                return _firstToSecondGroupHeightRatio;
            }
        }
        public static string dictionaryWeightsPath
        {
            get
            {
                return _dictionaryWeightsPath;
            }
        }
        public static int char_size
        {
            get
            {
                return _char_size;
            }
        }
        public static string language
        {
            get
            {
                return _language;
            }
        }
        public static bool elasticsearch
        {
            get
            {
                return _elasticsearch;
            }
        }
        public static bool otd
        {
            get
            {
                return _otd;
            }
        }
    }
}