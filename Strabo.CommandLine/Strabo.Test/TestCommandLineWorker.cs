using Strabo.Core.Utility;
using Strabo.Core.Worker;
using System;
using System.IO;

namespace Strabo.Test
{
    public class TestCommandLineWorker
    {
        public TestCommandLineWorker() { }
        public void Test()//do not change this function, write your own test function instead
        {
            //try
            //{
            //    Test("nls-sixinch");
            //}
            //catch (Exception e) { };
            //try
            //{
            //    Test("nls-cs-1920");
            //}
            //catch (Exception e) { };
            //try
            //{
            //    Test("Tianditu_cva");
            //}
            //catch (Exception e) { };
        }
        public void Test(string layer)//do not change this function, write your own test function instead
        {
            BoundingBox bbx = new BoundingBox();
            InputArgs inputArgs = new InputArgs();
            inputArgs.outputFileName = layer;

            if (layer == "nls-sixinch")
            {
                inputArgs.mapLayerName = layer;
                bbx.BBW = "597528";
                bbx.BBN = "210822";
                //417600 274400
                //482400 262400
                //412000 202400
                //488000 221600


            }
            if (layer == "nls-cs-1920")
            {
                inputArgs.mapLayerName = layer;
                bbx.BBW = "314100";
                bbx.BBN = "543800";
            }
            if (layer == "Tianditu_cva")
            {
                inputArgs.mapLayerName = layer;
                bbx.BBW = "13006520";
                bbx.BBN = "4853838";
            }
            if (layer == "Tianditu_eva")
            {
                inputArgs.mapLayerName = layer;
                bbx.BBW = "12957312";
                bbx.BBN = "4852401";
            }
            string appPath = AppDomain.CurrentDomain.BaseDirectory;
            string dataPath = Directory.GetParent(appPath).Parent.Parent.FullName + "\\data\\";
            inputArgs.bbx = bbx;



            inputArgs.intermediatePath = dataPath + "intermediate\\";
            inputArgs.outputPath = dataPath + "output\\";

            inputArgs.threadNumber = 8;

            CommandLineWorker cmdWorker = new CommandLineWorker();
            //cmdWorker.Apply(inputArgs, false, 0,0,0,0);
            //cmdWorker.Apply(inputArgs, true);
            File.Copy(inputArgs.outputPath + "SourceMapImage.png", dataPath + layer + ".png", true);
            File.Copy(inputArgs.outputPath + layer + "ByPixels.txt", dataPath + layer + "geojsonByPixels.txt", true);
            Log.WriteLine("Process finished");
        }
        public void TestLocalFiles()//do not change this function, write your own test function instead
        {
            BoundingBox bbx = new BoundingBox();
            bbx.BBW = "2977681";
            bbx.BBN = "5248978";

            InputArgs inputArgs = new InputArgs();
            inputArgs.outputFileName = "Geojson";

            inputArgs.bbx = bbx;
            inputArgs.mapLayerName = "nls-cs-1920";
            inputArgs.threadNumber = 8;

            string appPath = AppDomain.CurrentDomain.BaseDirectory;
            string dataPath = Directory.GetParent(appPath).Parent.Parent.FullName + "\\data\\";

            inputArgs.bbx = bbx;

            inputArgs.intermediatePath = dataPath + "intermediate\\";
            inputArgs.outputPath = dataPath + "output\\";
            try
            {

                //double[,] coordinates = new double[,] {{ 51.612961808346, 0.7341333483182767, 51.62160570111173, 0.7491015465216221 }, 
                //{ 51.6269443576099, 0.8062426299015226, 51.63557875288053, 0.8212288917069887 }, 
                //{ 51.65805109006676, 0.8217285148953754, 51.666683266211315, 0.8367281449327424 }, 
                //{ 51.675516021954614, 0.7646911667890038, 51.68415554029906, 0.779686123859217 }, 
                //{ 51.725724823274724, 0.6620249144045003, 51.73437743111727, 0.6770177479248397 }, 
                //{ 51.740352303690166, 0.7500629858262108, 51.74899333124705, 0.7650771692544902 }, 
                //{ 51.754711721550926, 0.8237386829167501, 51.7633430256784, 0.8387714425072738 }, 
                //{ 51.755968540487856, 0.8432213588349019, 51.76459728647148, 0.858258165853294 }, 
                //{ 51.76956959182837, 0.9015968337365646, 51.778190603233256, 0.9166491091661833 }, 
                //{ 51.84555647676825, 1.2579248616697, 51.85413007413581, 1.2730691065924755 }};
                Log.SetLogDir(inputArgs.intermediatePath);
                CommandLineWorker cmdWorker = new CommandLineWorker();
                for (int i = 1; i < 10; i++)
                {
                    //if (i <5) continue;
                    //system.io.streamreader coordinatereader = new system.io.streamreader(datapath + "1920-" + i + "-coordinates.txt");
                    //string coordtext = coordinatereader.readline();
                    //double[] coordinates = new double[4];
                    //string[] coordtextsplit = coordtext.split(' ');
                    //for (int j = 0; j < 4; j++)
                    //    coordinates[j] = double.parse(coordtextsplit[j]);
                    File.Copy(dataPath + "1920-" + i + ".png", inputArgs.outputPath + "SourceMapImage.png", true);
                    inputArgs.outputFileName = "1920-" + i + ".png";
                    cmdWorker.Apply(inputArgs, dataPath + "1920-" + i + ".png");
                    //cmdWorker.Apply(inputArgs, true, 51, 0.7, 51, 0.7);
                    File.Copy(inputArgs.outputPath + "1920-" + i + ".pngByPixels.txt", dataPath + "1920-" + i + ".pngByPixels.txt", true);

                }
            }
            catch (Exception e) { Log.WriteLine(e.Message); Log.WriteLine(e.StackTrace); };
            Log.WriteLine("Process finished");
        }
        public void TestLocalFile(string filename, string mapLayerName)//do not change this function, write your own test function instead
        {


            InputArgs inputArgs = new InputArgs();
            inputArgs.outputFileName = Path.GetFileNameWithoutExtension(filename);

            inputArgs.mapLayerName = mapLayerName;
            inputArgs.threadNumber = 8;

            string appPath = AppDomain.CurrentDomain.BaseDirectory;
            string dataPath = Path.Combine(Directory.GetParent(appPath).Parent.Parent.FullName , "data");

            inputArgs.intermediatePath = Path.Combine(dataPath, "intermediate\\");
            inputArgs.outputPath = Path.Combine(dataPath, "output\\");
            try
            {
                Log.SetLogDir(inputArgs.intermediatePath);
                CommandLineWorker cmdWorker = new CommandLineWorker();
                cmdWorker.Apply(inputArgs, Path.Combine(dataPath , filename));
                File.Copy(Path.Combine(inputArgs.outputPath, inputArgs.outputFileName + "ByPixels.txt"), Path.Combine(dataPath , inputArgs.outputFileName + ".pngByPixels.txt"), true);
            }
            catch (Exception e) { Log.WriteLine(e.Message); Log.WriteLine(e.StackTrace); };
            Log.WriteLine("Process finished");
        }
    }
}
