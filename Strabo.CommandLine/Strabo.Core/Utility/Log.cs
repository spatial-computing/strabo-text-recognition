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
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace Strabo.Core.Utility
{
    public class Log
    {
        public static bool write_console_only = false;
        public static bool write_log_and_console = true;
        public static int image_counter;
        public static DateTime start;
        public static DateTime[] start_timer = new DateTime[10];
        private static string _output_dir = "";
        private static string _debug_dir = "";
        private static string _log_dir = "";

        public static void SetOutputDir(string dir)
        {
            _output_dir = dir;
        }

        public static void SetDebugDir(string dir)
        {
            _debug_dir = dir;
        }

        public static void SetLogDir(string dir)
        {
            _log_dir = dir;
        }

        public static void SetStartTime()
        {
            start = DateTime.Now;
        }

        public static void SetStartTime(int i)
        {
            start_timer[i] = DateTime.Now;
        }

        public static double GetDurationInSeconds()
        {
            var stop = DateTime.Now;
            var elapsedTime = stop - start;
            return elapsedTime.TotalSeconds;
        }

        public static double GetDurationInSeconds(int i)
        {
            var stop = DateTime.Now;
            var elapsedTime = stop - start_timer[i];
            return elapsedTime.TotalSeconds;
        }

        public static void ArchiveLog()
        {
            var now = DateTime.Now;
            var date = now.ToString();
            date = date.Replace('/', '-');
            date = date.Replace(':', '-');
            if (File.Exists(_output_dir + "log.txt"))
                File.Move(_output_dir + "log.txt", _log_dir + "log" + date + ".txt");
        }

        public static void DeleteAll()
        {
            var TheFolder = new DirectoryInfo(_log_dir);
            if (TheFolder.GetFiles() != null)
                foreach (var NextFile in TheFolder.GetFiles())
                    File.Delete(NextFile.FullName);
            TheFolder = new DirectoryInfo(_debug_dir);
            if (TheFolder.GetFiles() != null)
                foreach (var NextFile in TheFolder.GetFiles())
                    File.Delete(NextFile.FullName);
            TheFolder = new DirectoryInfo(_output_dir);
            if (TheFolder.GetFiles() != null)
                foreach (var NextFile in TheFolder.GetFiles())
                    File.Delete(NextFile.FullName);
        }

        public static void DeleteAll(string dir)
        {
            try
            {
                var TheFolder = new DirectoryInfo(dir);
                if (TheFolder.GetFiles() != null)
                    foreach (var NextFile in TheFolder.GetFiles())
                        File.Delete(NextFile.FullName);
            }
            catch (Exception e)
            {
                WriteLine(e.Message);
                WriteLine(e.ToString());
            }
        }

        public static void DeleteAllDebug()
        {
            try
            {
                var TheFolder = new DirectoryInfo(_debug_dir);
                if (TheFolder.GetFiles() != null)
                    foreach (var NextFile in TheFolder.GetFiles())
                        File.Delete(NextFile.FullName);
            }
            catch (Exception e)
            {
                WriteLine(e.Message);
                WriteLine(e.ToString());
            }
        }

        public static void DeleteAllOutput()
        {
            try
            {
                var TheFolder = new DirectoryInfo(_output_dir);
                if (TheFolder.GetFiles() != null)
                    foreach (var NextFile in TheFolder.GetFiles())
                        File.Delete(NextFile.FullName);
            }
            catch (Exception e)
            {
                WriteLine(e.Message);
                WriteLine(e.ToString());
            }
        }

        public static void WriteBitmap(Bitmap srcimg, string fn)
        {
            try
            {
                srcimg.Save(_log_dir + "\\" + fn + ".png", ImageFormat.Png);
                image_counter++;
            }
            catch (Exception e)
            {
                WriteLine(e.Message);
                WriteLine(e.ToString());
            }
        }

        public static void Write(string input)
        {
            if (write_console_only)
            {
                Console.Write(input);
            }
            else
            {
                var sw = new StreamWriter(_log_dir + "\\" + "log.txt", true);
                sw.Write(" " + DateTime.Now + " " + input);
                if (write_log_and_console) Console.Write(" " + DateTime.Now + " " + input);
                sw.Close();
            }
        }

        public static void WriteLine(string input)
        {
            if (write_console_only)
            {
                Console.WriteLine(input);
            }
            else
            {
                var sw = new StreamWriter(_log_dir + "\\" + "log.txt", true);

                sw.WriteLine(DateTime.Now + " " + input);
                if (write_log_and_console) Console.WriteLine(DateTime.Now + " " + input);
                sw.Close();
            }
        }

        public static void WriteBitmap2Debug(Bitmap srcimg, string fn)
        {
            try
            {
                //string test = "skip write";
                srcimg.Save(_debug_dir + image_counter.ToString("0000") + Path.GetFileNameWithoutExtension(fn) + ".png",
                    ImageFormat.Png);
                image_counter++;
            }
            catch (Exception e)
            {
                WriteLine(e.Message);
                WriteLine(e.ToString());
            }
        }

        public static void WriteBitmap2Debug(string path, string fn)
        {
            var srcimg = new Bitmap(path);
            try
            {
                //string test = "skip write";
                srcimg.Save(_debug_dir + image_counter.ToString("0000") + Path.GetFileNameWithoutExtension(fn) + ".png",
                    ImageFormat.Png);
                image_counter++;
            }
            catch (Exception e)
            {
                WriteLine(e.Message);
                WriteLine(e.ToString());
            }
        }

        public static void WriteBitmap2DebugExactFileName(Bitmap srcimg, string fn)
        {
            try
            {
                //string test = "skip write";
                srcimg.Save(_debug_dir + Path.GetFileNameWithoutExtension(fn) + ".png", ImageFormat.Png);
                image_counter++;
            }
            catch (Exception e)
            {
                WriteLine(e.Message);
                WriteLine(e.ToString());
            }
        }

        public static void WriteBitmap2DebugExactFileName(string path, string fn)
        {
            var srcimg = new Bitmap(path);
            try
            {
                //string test = "skip write";
                srcimg.Save(_debug_dir + Path.GetFileNameWithoutExtension(fn) + ".png", ImageFormat.Png);
                image_counter++;
            }
            catch (Exception e)
            {
                WriteLine(e.Message);
                WriteLine(e.ToString());
            }
        }

        public static void WriteBitmap2FolderExactFileName(string folder, Bitmap srcimg, string fn)
        {
            try
            {
                //string test = "skip write";
                //Console.WriteLine(fn);
                srcimg.Save(folder + Path.GetFileNameWithoutExtension(fn) + ".png", ImageFormat.Png);
                image_counter++;
            }
            catch (Exception e)
            {
                WriteLine(e.Message);
                WriteLine(e.ToString());
            }
        }

        public static void WriteBitmap2FolderExactFileName(string folder, string path, string fn)
        {
            var srcimg = new Bitmap(path);
            try
            {
                //string test = "skip write";
                //Console.WriteLine(fn);
                srcimg.Save(folder + Path.GetFileNameWithoutExtension(fn) + ".png", ImageFormat.Png);
                image_counter++;
            }
            catch (Exception e)
            {
                WriteLine(e.Message);
                WriteLine(e.ToString());
            }
        }

        //static public void WriteBitmap(Bitmap srcimg, string fn)
        //{
        //    WriteBitmap2Debug(srcimg, fn);
        //}
        public static void WriteBitmap2Output(Bitmap srcimg, string fn)
        {
            try
            {
                srcimg.Save(_output_dir + fn + ".png", ImageFormat.Png);
                image_counter++;
            }
            catch (Exception e)
            {
                WriteLine(e.Message);
                WriteLine(e.ToString());
            }
        }

        public static void WriteBitmap2Output(string path, string fn)
        {
            var srcimg = new Bitmap(path);
            try
            {
                srcimg.Save(_output_dir + fn + ".png", ImageFormat.Png);
                image_counter++;
            }
            catch (Exception e)
            {
                WriteLine(e.Message);
                WriteLine(e.ToString());
            }
        }
    }
}