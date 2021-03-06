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

using System.Text.RegularExpressions;
using System.Threading;

namespace Strabo.Core.TextRecognition
{
    public class RemoveNoiseText
    {
        public static bool NotUpperCaseInTheMiddle(string word)
        {
            var testword = Regex.Replace(word, @"\n", " ");
            var token = word.Split(' ');
            //Check case
            //Get the culture property of the thread.
            var cultureInfo = Thread.CurrentThread.CurrentCulture;
            //Create TextInfo object.
            var textInfo = cultureInfo.TextInfo;
            for (var i = 0; i < token.Length; i++)
                // remove anything with an upper-case letter in the middle of the string e.g., xxXxx 
                if (textInfo.ToTitleCase(token[i]) == token[i] || textInfo.ToUpper(token[i]) == token[i] ||
                    textInfo.ToLower(token[i]) == token[i])
                    return true;
            return false;
        }

        public static bool NotTooManyNoiseCharacters(string word)
        {
            if (word == null) return false;

            //Check alphabet ratio
            var clean = Regex.Replace(word, @"[^a-zA-Z0-9 -]", ""); //Regex.Replace(word, @"[^a-zA-Z0-9]", "");

            // @ialok

            if (clean.Length / (double) (word.Length - 2) <= 0.5) ////// I removed the equal sign from if statement    
                return false; // too many noise characters
            //if (clean.Length < 3)
            //    return false;


            //Q and digits
            //    if (word.Contains("Q") && Regex.IsMatch(word, @"\d"))
            //        return false;
            //Q and some other lower case characters
            //    if (Regex.IsMatch(word, @"[a-z]") && word.Contains("Q"))
            //       return false;

            //Check specific special characters
            //if (word.Contains("/") || word.Contains("\\") || word.Contains("%") || word.Contains("?") || word.Contains("!") || word.Contains("|") || word.Contains("*") || word.Equals("Q") || 
            //    word.Contains("_") || word.Contains("$") || word.Contains("¢") || word.Contains("§") || word.Contains("+") || word.Contains("~") || word.Contains("»") || word.Contains("<") || word.Contains(">") || word.Contains(@"\"))
            //    return false;

            // These settings map be map specific
            var count = word.Split('Q').Length - 1;
            if (count >= 2) return false;
            count = word.Split('1').Length - 1;
            if (count >= 4) return false;
            count = word.Split('I').Length - 1;
            if (count >= 5) return false;

            // Trailing 1's can be recognized as backslash
            var backSlashCount = word.Split('\\').Length - 1;
            if (backSlashCount >= 2)
                return false;

            // Quote count
            var quoteCount = word.Split('\"').Length - 1;
            if (quoteCount >= 2)
                return false;

            // int backSlashCount = word.Split('\"').Length - 1;

            // if ((word.Contains(";")))
            //     return false;

            //if (!Regex.IsMatch(word, @"[A-Za-z0-9.? )*]"))
            //    return false;
            //if (Regex.IsMatch(word, @"\n\n"))
            //{
            //    if (word.Length == 3)
            //        return false;
            //    if (word.Length == 4)
            //    {
            //       string tempword = word.Substring(0, 2);
            //     //  foreach (char inputchar in tempword)
            //     //  {
            //       if (char.IsLower(tempword[0]) && char.IsLower(tempword[1]))
            //               return false;
            //       if (char.IsLower(tempword[0]) && char.IsUpper(tempword[1]))
            //           return false;
            //     //  }
            //       //if (!CheckDictionaryTwoLetters.checkTwoWordDictionary(tempword))
            //       //    return false;
            //    }
            //    else if (word.Length > 4)
            //    {
            //        word = EditWord(word);
            //        if (word.Equals(""))
            //            return false;
            //    }
            //}
            //else
            //{
            //    if (word.Length == 1)
            //        return false;

            //    //if (word.Length == 2)
            //    //{
            //    //    if (!CheckDictionaryTwoLetters.checkTwoWordDictionary(word))
            //    //        return false;
            //    //}
            //    else if (word.Length > 2)
            //    {
            //        word = EditWord(word);
            //        if (word.Equals(""))
            //            return false;
            //    }
            //}

            return true;
        }

        public static string EditWord(string word)
        {
            var finalResult = "";
            //string newword = Regex.Replace(word, "\n\n", "");
            char[] linedelimiterChars = {'\n'};
            char[] spacedelimiter = {' '};
            var splitline = word.Split(linedelimiterChars);
            if (splitline.Length > 6)
                return "";
            for (var i = 0; i < splitline.Length - 2; i++)
            {
                if (Regex.IsMatch(splitline[i], " "))
                {
                    var spliteachline = splitline[i].Split(spacedelimiter);
                    for (var j = 0; j < spliteachline.Length; j++)
                        if (spliteachline[j].Length > 0)
                            if (j < spliteachline.Length - 1)
                                finalResult += spliteachline[j] + " ";
                            else
                                finalResult += spliteachline[j];
                }
                else
                {
                    if (splitline[i].Length > 1)
                        finalResult += splitline[i];
                }

                if (!finalResult.Equals("") && splitline[i].Length != 1)
                    if (i < splitline.Length - 3)
                        finalResult += "\n";
                    else
                        finalResult += "\n\n";

                if (!finalResult.Equals("") && splitline[i].Length == 1 && i == splitline.Length - 3)
                    finalResult += "\n";
            }
            return finalResult;
        }
    }
}