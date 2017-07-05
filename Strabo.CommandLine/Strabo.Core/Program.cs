using System;
using Strabo.Core.Utility;

namespace Strabo.Core.Worker
{
    internal static class Program
    {
        /// <summary>
        ///     The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main(string[] args)
        {
            if (args.Length != 5)
            {
                Console.WriteLine(
                    "Input parameter is invalid. Please use \"Strabo.core.exe [input file path & name] [intermediate_folder] [output_folder] [layer] [thread_number]\"");
                return;
            }
            try
            {
                //BoundingBox bbx = new BoundingBox();
                //bbx.BBW = args[0];
                //bbx.BBN = args[1];
                var inputArgs = new InputArgs();
                inputArgs.outputFileName = "geojson";
                //inputArgs.bbx = bbx;
                inputArgs.intermediatePath = args[1];
                inputArgs.outputPath = args[2];
                inputArgs.mapLayerName = args[3];
                inputArgs.threadNumber = int.Parse(args[4]);

                var cmdWorker = new CommandLineWorker();
                cmdWorker.Apply(inputArgs, args[0]);
                Log.WriteLine("Strabo process finised.");
            }
            catch (Exception e)
            {
                Log.WriteLine(e.Message);
                Log.WriteLine(e.Source);
                Log.WriteLine(e.StackTrace);
            }
        }
    }
}