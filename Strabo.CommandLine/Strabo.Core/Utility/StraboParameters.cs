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
using System;
using System.IO;
using System.Linq;

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

        public bool useAutomaticThresholding;
    }

    public static class StraboParameters
    {
        private static RGBThreshold _rgbThreshold;

       

        public static RGBThreshold rgbThreshold
        {
            get => _rgbThreshold;
            set => _rgbThreshold = value;
        }

        public static int NumberOfSegmentationColor { get; set; }

        public static string straboReleaseVersion { get; set; }

        public static string sourceMapFileName { get; private set; }

        public static string dictionaryFilePath { get; private set; }

        public static int dictionaryExactMatchStringLength { get; private set; }

        public static int char_size { get; private set; }

        public static string language { get; private set; }

        public static bool elasticsearch { get; private set; }

        public static int spatialDistance { get; private set; }

        public static int colorDistance { get; private set; }

        public static int medianCutColors { get; private set; }

        public static double cdaAngleThreshold { get; private set; }

        public static int cdaIterationThreshold { get; private set; }

        public static int minimumDistBetweenCC { get; private set; }

        public static double cdaAngleRatio { get; private set; }

        public static int rowSlice { get; private set; }

        public static int colSlice { get; private set; }

        public static int overlap { get; private set; }

        public static double minPixelAreaSize { get; private set; }

        public static int bbxMinHeight { get; private set; }

        public static int bbxMinWidth { get; private set; }

        public static int bbxMaxHeight { get; private set; }

        public static int bbxMaxWidth { get; private set; }

        public static bool cdaPreProcessing { get; private set; }

        public static double cdaSizeRatio { get; private set; }

        public static int bbxMultiplier { get; private set; }

        public static string CleanWithSVM { get; private set; }


        /*
         * Generate the HSV histogram of the map to decide the threshold 
         */
        public static int findRGBThreshold(string inputPath)
        {

            Image<Bgr, Byte> image = new Image<Bgr, byte>(inputPath);

            Image<Hsv, byte> hsvImage = image.Convert<Hsv, byte>();

            Image<Gray, byte>[] channels = hsvImage.Copy().Split();
            Image<Gray, byte> hue = channels[0];
            Image<Gray, byte> sat = channels[1];

            DenseHistogram dh = new DenseHistogram(20, new RangeF(0, 255));

            dh.Calculate<byte>(new Image<Gray, byte>[] { sat }, true, null);

            float[] array = dh.GetBinValues();

            float max = array.Max();

            int index = array.ToList().IndexOf(max);

            return index;
        }

            public static void readConfigFile(string layer, string path)
        {
            try
            {
                Log.WriteLine("Reading application settings...");
                ReadConfigFile.init();
                if (ReadString(layer + ":Name", "") != "")
                    layer += ":"; //setting found
                else
                    layer = ""; //use the default setting

                int index;
                index = findRGBThreshold(path);

                _rgbThreshold = new RGBThreshold();

                if (index <= 3)
                {
                    _rgbThreshold.upperBlueColorThd = ReadInt(layer + "UpperBlueColorThreshold", 0);
                    _rgbThreshold.upperRedColorThd = ReadInt(layer + "UpperRedColorThreshold", 0);
                    _rgbThreshold.upperGreenColorThd = ReadInt(layer + "UpperGreenColorThreshold", 0);
                    _rgbThreshold.lowerBlueColorThd = ReadInt(layer + "LowerBlueColorThreshold", 0);
                    _rgbThreshold.lowerRedColorThd = ReadInt(layer + "LowerRedColorThreshold", 0);
                    _rgbThreshold.lowerGreenColorThd = ReadInt(layer + "LowerGreenColorThreshold", 0);
                }
                else
                {
                    _rgbThreshold.upperBlueColorThd = ReadInt(layer + "UpperBlueColorThreshold2", 0);
                    _rgbThreshold.upperRedColorThd = ReadInt(layer + "UpperRedColorThreshold2", 0);
                    _rgbThreshold.upperGreenColorThd = ReadInt(layer + "UpperGreenColorThreshold2", 0);
                    _rgbThreshold.lowerBlueColorThd = ReadInt(layer + "LowerBlueColorThreshold", 0);
                    _rgbThreshold.lowerRedColorThd = ReadInt(layer + "LowerRedColorThreshold", 0);
                    _rgbThreshold.lowerGreenColorThd = ReadInt(layer + "LowerGreenColorThreshold", 0);
                }

                //Console.WriteLine(_rgbThreshold.upperRedColorThd);

                if (_rgbThreshold.upperBlueColorThd == _rgbThreshold.lowerBlueColorThd
                    || _rgbThreshold.upperGreenColorThd == _rgbThreshold.lowerGreenColorThd
                    || _rgbThreshold.upperRedColorThd == _rgbThreshold.lowerRedColorThd)
                    _rgbThreshold.useAutomaticThresholding = true;
                else
                    _rgbThreshold.useAutomaticThresholding = false;

                NumberOfSegmentationColor = ReadInt(layer + "NumberOfSegmentationColor", 0);
                char_size = ReadInt(layer + "CharSize", 12);
                language = ReadString(layer + "Language", "en");
                elasticsearch = ReadBool(layer + "Elasticsearch", false);
                dictionaryFilePath = ReadString(layer + "DictionaryFilePath", "");
                dictionaryExactMatchStringLength = ReadInt(layer + "DictionaryTwoLetterFilePath", 2);
                sourceMapFileName = ReadString("SourceMapFileName", "SourceMapImage.png");
                straboReleaseVersion = ReadString("Version", "");
                spatialDistance = ReadInt(layer + "SpatialDistance", 3);
                colorDistance = ReadInt(layer + "ColorDistance", 3);
                medianCutColors = ReadInt(layer + "MedianCutColors", 256);


                minimumDistBetweenCC = ReadInt(layer + "MinimumDistanceBetweenCC", 2);

                cdaAngleRatio = ReadDouble(layer + "AngleRatio", 0.3);
                rowSlice = ReadInt(layer + "RowSlice", 1);
                colSlice = ReadInt(layer + "ColSlice", 1);
                overlap = ReadInt(layer + "Overlap", 100);
                minPixelAreaSize = ReadDouble(layer + "MinPixelAreaSize", 0.18);

                bbxMinHeight = ReadInt(layer + "BbxMinHeight", 10);
                bbxMinWidth = ReadInt(layer + "BbxMinWidth", 10);
                bbxMaxHeight = ReadInt(layer + "BbxMaxHeight", 500);
                bbxMaxWidth = ReadInt(layer + "BbxMaxWidth", 500);

                cdaAngleThreshold = ReadDouble(layer + "cdaAngleThreshold", 0.3);
                cdaPreProcessing = ReadBool(layer + "cdaPreProcessing", false);
                cdaSizeRatio = ReadDouble(layer + "cdaSizeRatio", 2.5);
                cdaIterationThreshold = ReadInt(layer + "cdaIterationThreshold", 15);

                bbxMultiplier = ReadInt(layer + "bbxMultiplier", 3);

                CleanWithSVM = ReadString(layer + "CleanWithSVM", "");
            }
            catch (Exception e)
            {
                Log.WriteLine(e.Message);
                Log.WriteLine(e.Source);
                Log.WriteLine(e.StackTrace);
                throw;
            }
        }

        private static string ReadString(string key, string default_value)
        {
            return ReadConfigFile.ReadModelConfiguration(key) != ""
                ? ReadConfigFile.ReadModelConfiguration(key)
                : default_value;
        }

        private static int ReadInt(string key, int default_value)
        {
            return ReadConfigFile.ReadModelConfiguration(key) != ""
                ? Convert.ToInt16(ReadConfigFile.ReadModelConfiguration(key))
                : default_value;
        }

        private static double ReadDouble(string key, double default_value)
        {
            return ReadConfigFile.ReadModelConfiguration(key) != ""
                ? Convert.ToDouble(ReadConfigFile.ReadModelConfiguration(key))
                : default_value;
        }

        private static bool ReadBool(string key, bool default_value)
        {
            return ReadConfigFile.ReadModelConfiguration(key) != ""
                ? Convert.ToBoolean(ReadConfigFile.ReadModelConfiguration(key))
                : default_value;
        }
    }
}