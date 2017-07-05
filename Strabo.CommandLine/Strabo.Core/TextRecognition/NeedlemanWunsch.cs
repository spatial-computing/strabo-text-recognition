using System;
using System.Configuration;
using System.IO;

namespace Strabo.Core.TextRecognition
{
    internal class NeedlemanWunsch
    {
        private static int refSeqCnt;
        private static int alineSeqCnt;
        private static int[,] scoringMatrix;
        private static object[,] excelarray;
        private static int excelrowNo;
        private static int matchcharcount;
        private static readonly int initialWeights = -2;
        private static readonly int deleteWeights = -3;
        private static readonly int editWeights = -1;
        private static readonly int addWeights = -2;
        private static readonly int matchWeights = 3;
        private static readonly int matchdefinedWeights = 1;

        public static void initSimMatrix(string refSeq, string alineSeq)
        {
            refSeqCnt = refSeq.Length + 1;
            alineSeqCnt = alineSeq.Length + 1;
            scoringMatrix = new int[alineSeqCnt, refSeqCnt];

            for (var i = 0; i < alineSeqCnt; i++)
                scoringMatrix[i, 0] = initialWeights;

            for (var j = 0; j < refSeqCnt; j++)
                if (alineSeq.Length >= 2 && char.IsUpper(alineSeq[0]) && char.IsLower(alineSeq[1]))
                {
                    if (j <= refSeqCnt - alineSeqCnt)
                        scoringMatrix[0, j] = initialWeights - 2;
                    else
                        scoringMatrix[0, j] = initialWeights;
                }
                else
                {
                    if (j <= refSeqCnt - alineSeqCnt)
                        scoringMatrix[0, j] = 0;
                    else
                        scoringMatrix[0, j] = initialWeights;
                }

            //for (int i = 0, j = 0; i < alineSeqCnt && j < refSeqCnt; i++, j++)
            //{

            //    scoringMatrix[i, j] = 0;
            //}
            scoringMatrix[0, 0] = 0;
        }

        public static int findSimScore(string refSeq, string alineSeq) //dict, OCR
        {
            try
            {
                var weightsPath = ConfigurationSettings.AppSettings["DictionaryWeightsPath"] != ""
                    ? ConfigurationSettings.AppSettings["DictionaryWeightsPath"]
                    : "";
                var lines = File.ReadAllLines(weightsPath);

                var temp = 0;
                initSimMatrix(refSeq, alineSeq);
                var quit = true;

                refSeq = refSeq.ToLower();
                alineSeq = alineSeq.ToLower();

                for (var i = 1; i < alineSeqCnt; i++)
                {
                    var t = 0;
                    for (var j = 1; j < refSeqCnt; j++)
                    {
                        /*if (j - i >= refSeqCnt / 3 || i-j>refSeqCnt/3)
                        {
                            scoringMatrix[i, j] = -2;
                            continue;
                        }*/
                        var scroeDiag = 0;
                        if (alineSeq[i - 1] == ' ' || refSeq.Substring(j - 1, 1) == alineSeq.Substring(i - 1, 1))
                        {
                            scroeDiag = scoringMatrix[i - 1, j - 1] + matchWeights;
                        }
                        else
                        {
                            int scoreval;
                            string prevchar1, prevchar2;

                            if (j > 1)
                                prevchar1 = refSeq.Substring(j - 2, 1);
                            else
                                prevchar1 = string.Empty;

                            if (i > 1)
                                prevchar2 = alineSeq.Substring(i - 2, 1);
                            else
                                prevchar2 = string.Empty;

                            scoreval = GetScore(lines, refSeq.Substring(j - 1, 1), prevchar1,
                                alineSeq.Substring(i - 1, 1), prevchar2);

                            scroeDiag = scoringMatrix[i - 1, j - 1] + scoreval;
                        }

                        var tempAW = addWeights;
                        //if (j > alineSeqCnt)
                        //    tempAW = 0;
                        //else
                        //    tempAW = addWeights;
                        var scoreLeft = scoringMatrix[i, j - 1] + tempAW; // addWeights; //insert
                        var scoreUp = scoringMatrix[i - 1, j] + deleteWeights; // deleteWeights; //delete
                        //if(scoreUp)
                        var maxScore = Math.Max(Math.Max(scroeDiag, scoreLeft), scoreUp);

                        /*if(i==3&&j==3&&maxScore<=0&&scoringMatrix[3,1]<=0&&scoringMatrix[2,1]<=0)
                        {
                            return -1;
                        }*/
                        scoringMatrix[i, j] = maxScore;
                    }
                }
                //    findAlignment(refSeq,alineSeq);
                return scoringMatrix[alineSeqCnt - 1, refSeqCnt - 1];
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        private static void findAlignment(string refSeq, string alineSeq) //not used
        {
            //Traceback Step
            var alineSeqArray = alineSeq.ToCharArray();
            var refSeqArray = refSeq.ToCharArray();

            var AlignmentA = string.Empty;
            var AlignmentB = string.Empty;
            var m = alineSeqCnt - 1;
            var n = refSeqCnt - 1;

            while (m > 0 || n > 0)
            {
                var scroeDiag = 0;

                if (m == 0 && n > 0)
                {
                    AlignmentA = refSeqArray[n - 1] + AlignmentA;
                    AlignmentB = "-" + AlignmentB;
                    n = n - 1;
                }
                else if (n == 0 && m > 0)
                {
                    AlignmentA = "-" + AlignmentA;
                    AlignmentB = alineSeqArray[m - 1] + AlignmentB;
                    m = m - 1;
                }
                else
                {
                    //Remembering that the scoring scheme is +3 for a match, -2 for a mismatch, and -1 for a gap and +2 for matrix match
                    if (alineSeqArray[m - 1] == refSeqArray[n - 1])
                        scroeDiag = 3;
                    else
                        scroeDiag = -2;

                    if (m > 0 && n > 0 && scoringMatrix[m, n] == scoringMatrix[m - 1, n - 1] + scroeDiag)
                    {
                        AlignmentA = refSeqArray[n - 1] + AlignmentA;
                        AlignmentB = alineSeqArray[m - 1] + AlignmentB;
                        m = m - 1;
                        n = n - 1;
                    }
                    else if (n > 0 && scoringMatrix[m, n] == scoringMatrix[m, n - 1] - 1)
                    {
                        AlignmentA = refSeqArray[n - 1] + AlignmentA;
                        AlignmentB = "-" + AlignmentB;
                        n = n - 1;
                    }
                    else if (m > 0 && scoringMatrix[m, n] == scoringMatrix[m - 1, n] + -1)
                    {
                        AlignmentA = "-" + AlignmentA;
                        AlignmentB = alineSeqArray[m - 1] + AlignmentB;
                        m = m - 1;
                    }
                    /* else //2 char in alignseq map to 1 char in refseq
                     {
                         AlignmentA = refSeqArray[n - 1] + AlignmentA;
                         AlignmentB = alineSeqArray[m - 1] + alineSeqArray[m - 2] + AlignmentB;
                         n = n - 1;
                         m = m - 2;
                     }*/
                }
            }
        }

        private static int GetScore(string[] lines, string currentchar1, string prevchar1, string currentchar2,
            string prevchar2)
        {
            foreach (var line in lines)
            {
                var combinationChars = line.Split(' ');
                if (combinationChars[0] == currentchar1 && combinationChars[1] == currentchar2)
                {
                    matchcharcount = 1;
                    return matchdefinedWeights;
                }

                if (combinationChars[0] == string.Concat(prevchar1, currentchar1) &&
                    combinationChars[1] == currentchar2)
                {
                    matchcharcount = 2;
                    return matchdefinedWeights - addWeights;
                }

                if (combinationChars[1] == string.Concat(prevchar1, currentchar1) &&
                    combinationChars[0] == currentchar2)
                {
                    matchcharcount = 2;
                    return matchdefinedWeights - addWeights;
                }

                if (combinationChars[0] == currentchar1 &&
                    combinationChars[1] == string.Concat(prevchar2, currentchar2))
                {
                    matchcharcount = 2;
                    return matchdefinedWeights - deleteWeights;
                }
            }
            return editWeights;
        }
    }
}