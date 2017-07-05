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
using System.Collections;
using System.IO;

namespace Strabo.Core.Utility
{
    /// <summary>
    ///     Sima
    /// </summary>
    public static class ReadConfigFile
    {
        private static readonly Hashtable config = new Hashtable();

        public static void init()
        {
            try
            {
                var sr = new StreamReader(@".\config.txt");
                var line = sr.ReadLine();
                while (line != null)
                {
                    char[] s = {':'};
                    var tokens = line.Split(s);
                    if (!config.ContainsKey(tokens[0] + ":" + tokens[1]))
                        config.Add(tokens[0] + ":" + tokens[1], tokens[2]);
                    line = sr.ReadLine();
                }
                sr.Close();
            }
            catch (Exception e)
            {
                Log.WriteLine(e.Message);
                Log.WriteLine(e.Source);
                Log.WriteLine(e.StackTrace);
                throw;
            }
        }

        public static string ReadModelConfiguration(string key)
        {
            try
            {
                if (config.ContainsKey(key))
                    return config[key].ToString();
                return "";
            }
            catch (Exception e)
            {
                Log.WriteLine(e.Message);
                Log.WriteLine(e.Source);
                Log.WriteLine(e.StackTrace);
                throw;
            }
        }
    }
}