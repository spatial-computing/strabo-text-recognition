/*******************************************************************************
 * Copyright 2017 University of Southern California
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
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Emgu.CV;
using Emgu.CV.ML;
using Emgu.CV.ML.MlEnum;
using Emgu.CV.Structure;
using Strabo.Core.ImageProcessing;

namespace Strabo.Core.MachineLearning
{
    public class ConnectedComponentClassifier
    {
        public static string svmDataPath = Path.GetDirectoryName(@".\libdata\SVM_training\");

        public void Apply(string intermediatePath, string inputFileName, string outputFileName, bool open)
        {
            //1.Get the input image
            var srcimage = new Image<Gray, byte>(Path.Combine(intermediatePath, inputFileName));
            var srcimg = new Bitmap(Path.Combine(intermediatePath, inputFileName));
            srcimg = ImageUtils.ConvertGrayScaleToBinary(srcimg, 254);
            srcimg = ImageUtils.InvertColors(srcimg);
            //2.Do open or close operation here
            Bitmap closed_img = null;
            //if (open)
            //{
            //Image<Gray, Byte> closed_image = new Image<Gray, byte>(srcimage.Width, srcimage.Height);
            //StructuringElementEx element = new StructuringElementEx(3, 3, 1, 1, Emgu.CV.CvEnum.CV_ELEMENT_SHAPE.CV_SHAPE_CROSS);
            //CvInvoke.cvMorphologyEx(srcimage, closed_image, IntPtr.Zero, element, Emgu.CV.CvEnum.CV_MORPH_OP.CV_MOP_OPEN, 1);
            //closed_image.Save(Path.Combine(intermediatePath, "CDAInput_BinaryClosed.png"));
            //closed_img = closed_image.ToBitmap();
            //closed_img = ImageProcessing.ImageUtils.InvertColors(closed_img);
            //}
            //else
            {
                closed_img = srcimg;
            }
            //3.Get each CC in CDAInput image with features
            var fetcher = new CCfetcher();
            var CCs = fetcher.GetConnectedComponents(closed_img);
            //4.Classify each CC
            var classification = ClassifyWorker(CCs, true);
            //this sum is only for statistics, not really useful
            var sum = 0;
            foreach (var var in classification)
                if (var) sum++;
            //5.Get the output
            var dst_bool = new bool[srcimg.Height, srcimg.Width];
            var i = 0;
            var labels = fetcher.objectLabels;
            for (var y = 0; y < srcimg.Height; y++)
            for (var x = 0; x < srcimg.Width; x++, i++)
                dst_bool[y, x] = classification[labels[i]];
            //6.Save the output image
            var dstimg = ImageUtils.ArrayBool2DToBitmap(dst_bool);
            dstimg.Save(intermediatePath + outputFileName);
        }

        /*
         * Given the list of connected components(CC), classify each CC as number or noise.
         * @param CCs: list of CC objects.
         * @param loadLocalModel: whether or not use existing trained SVM model. If not, train the model on the fly.
         * @return a boolean array, each entry indicates if the CC is number (True) or noise (False).
         */
        public bool[] ClassifyWorker(List<CCforClassification> CCs, bool loadLocalModel)
        {
            //Initialization
            var result = new bool[CCs.Count + 1];
            result[0] = false;
            SVM svm_classifier = null;
            if (loadLocalModel)
            {
                svm_classifier = new SVM();
                // svm_classifier.load(Path.Combine(svmDataPath, "SVM_model.xml")); emgucv 2.9
                var fs = new FileStorage(Path.Combine(svmDataPath, "SVM_model.xml"), FileStorage.Mode.Read);
                svm_classifier.Read(fs.GetFirstTopLevelNode());
                fs.ReleaseAndGetString();
            }
            //else svm_classifier = SVMTrainer(10, true);
            //Classify each CC
            var counter = 0;
            for (var i = 0; i < CCs.Count; i++)
            {
                //Currently only use two features for each CC
                var feature = new Matrix<float>(1, 2);
                feature.Data[0, 0] = CCs[i].rectangularity;
                feature.Data[0, 1] = CCs[i].eccentricity;
                //Predict the CC is number or not
                var score = svm_classifier.Predict(feature);
                //If the score is greater than 0.5, classify as true
                result[i + 1] = score >= 0.5;
                if (score >= 0.5) counter++;
            }
            return result;
        }

        /*
         * Legacy method. Currently not used. It classifies single CC.
         * @param CC: the CC to be classified
         * @param usingsvm: true if use SVM for classification, false if use naive threshold.
         * @return true if the CC is number, otherwise false
         */
        public bool Classifier(CCforClassification CC, bool usingsvm)
        {
            if (usingsvm)
            {
                //Load the SVM from disk
                var predictor = new SVM();
                //predictor.Load(Path.Combine(svmDataPath, "SVM_model.xml"));//path!
                var fs = new FileStorage(Path.Combine(svmDataPath, "SVM_model.xml"), FileStorage.Mode.Read);
                predictor.Read(fs.GetRoot());
                fs.ReleaseAndGetString();
                //Construct the feature matrix
                var sample = new Matrix<float>(1, 4);
                sample.Data[0, 0] = CC.pixelCount;
                sample.Data[0, 1] = CC.MBRArea;
                sample.Data[0, 2] = CC.rectangularity;
                sample.Data[0, 3] = CC.eccentricity;
                //Do the prediction and return result
                var result = predictor.Predict(sample);
                if (result < 0.5) return false;
                return true;
            }
            //Naive classification using threshold, not robust
            if (CC.pixelCount <= 20) return false;
            if (CC.MBRArea <= 30) return false;
            if (CC.rectangularity <= 0.25 || CC.rectangularity >= 0.9) return false;
            if (CC.eccentricity >= 4) return false;
            return true;
        }

        /*
         * Train the SVM on the fly, and save the trained model.
         * @param C: parameter C for SVM.
         * @param preprocess: true if the pre-processing is performed
         * @return the SVM object trained in this method
         */
        public SVM SVMTrainer(float C, bool preprocess)
        {
            var dataPath = svmDataPath;
            //Do the preprocess
            if (preprocess) SVMTrainingPreprocessor(dataPath);
            //Read the training data in the txt file
            var feature_lines = new List<string>();
            using (var feature_file = new StreamReader(Path.Combine(dataPath, "features.txt")))
            {
                string line;
                while ((line = feature_file.ReadLine()) != null)
                    feature_lines.Add(line);
            }
            //Get the number of training samples and the dimension of each training sample
            var trainSampleCount = feature_lines.Count;
            var dimensions = feature_lines[0].Split().Length - 1;
            //Read the features in strings and save in matrices
            var trainData = new Matrix<float>(trainSampleCount, dimensions);
            var trainClasses = new Matrix<int>(trainSampleCount, 1);
            for (var i = 0; i < trainSampleCount; i++)
            {
                /*
                 * Each line represents a training sample, for each line, the format is:
                 * [feature #1] [feature #2] .. [feature #dimension] [sample class #(1 or 0)]
                 */
                var features = feature_lines[i].Split();
                for (var j = 0; j < dimensions; j++)
                    trainData.Data[i, j] = float.Parse(features[j]);
                trainClasses.Data[i, 0] = int.Parse(features[dimensions]);
            }
            //Initialize the SVM
            var model = new SVM();
            //Set the training parameters
            //SVMParams p = new SVMParams();
            //p.KernelType = Emgu.CV.ML.MlEnum.SVM_KERNEL_TYPE.RBF;//No Gaussian???
            //p.SVMType = Emgu.CV.ML.MlEnum.SVM_TYPE.C_SVC;
            //p.C = C;
            //p.Gamma = 10;
            //p.TermCrit = new MCvTermCriteria(300, 0.001);

            model.SetKernel(SVM.SvmKernelType.Rbf);
            model.Type = SVM.SvmType.CSvc;
            model.TermCriteria = new MCvTermCriteria(300, 0.001);
            model.C = C;
            model.Gamma = 10;
            //Do the training
            var td = new TrainData(trainData, DataLayoutType.RowSample, trainClasses);
            var trained = model.TrainAuto(td, 5);

            //bool trained = model.TrainAuto(trainData, trainClasses, null, null, model.MCvSVMParams, 5);
            Console.WriteLine("SVM training finished!");
            //Save the model on disk
            model.Save(dataPath + "SVM_model2.xml");
            Console.WriteLine("SVM saved to disk file");

            return model;
        }

        /*
            Use preprocessor to get the features of training samples for SVM.
            All the images in the "pos" folder are assumed to be positive, and all
            the images in the "neg" folder are negative. All these features with 
            labels of 1/0 are stored in the features.txt file.
        */
        public void SVMTrainingPreprocessor(string dataPath)
        {
            //Assume dataPath is ..\\SVM_training\\
            var posDataPath = dataPath + "pos\\";
            var negDataPath = dataPath + "neg\\";
            string line;
            var pos_list = new List<string>();
            var neg_list = new List<string>();
            var list_file = new StreamReader(posDataPath + "pos_list.txt");
            while ((line = list_file.ReadLine()) != null)
                pos_list.Add(line);
            list_file.Close();
            list_file = new StreamReader(negDataPath + "neg_list.txt");
            while ((line = list_file.ReadLine()) != null)
                neg_list.Add(line);
            list_file.Close();

            var featureFilePath = dataPath + "features.txt";
            using (var feature_file = new StreamWriter(featureFilePath))
            {
                foreach (var filename in pos_list)
                {
                    var srcimg = new Bitmap(posDataPath + filename);
                    srcimg = ImageUtils.ConvertGrayScaleToBinary(srcimg, 254);
                    srcimg = ImageUtils.InvertColors(srcimg);
                    var fetcher = new CCfetcher();
                    var CCs = fetcher.GetConnectedComponents(srcimg);
                    for (var j = 0; j < CCs.Count; j++)
                        feature_file.WriteLine(CCs[j] + " 1");
                }
                foreach (var filename in neg_list)
                {
                    var srcimg = new Bitmap(negDataPath + filename);
                    srcimg = ImageUtils.ConvertGrayScaleToBinary(srcimg, 254);
                    srcimg = ImageUtils.InvertColors(srcimg);
                    var fetcher = new CCfetcher();
                    var CCs = fetcher.GetConnectedComponents(srcimg);
                    for (var j = 0; j < CCs.Count; j++)
                        feature_file.WriteLine(CCs[j] + " 0");
                }
            }
        }

        //New writen methods, should be in the ImageUtil module
        //Check if a coordinate is in the bound of image.
        public static bool CheckInBound(Bitmap src, int y, int x)
        {
            return 0 <= x && x < src.Width && 0 <= y && y < src.Height;
        }

        //Do the image closing for binary image.
        public static Bitmap BinaryImageClosing(Bitmap src, int radius)
        {
            var src_bool = ImageUtils.BitmapToBoolArray2D(src, 0); //what is margin???
            var after_dilation = new bool[src.Height, src.Width];
            var after_erosion = new bool[src.Height, src.Width];
            for (var y = 0; y < src.Height; y++)
            for (var x = 0; x < src.Width; x++)
            {
                after_dilation[y, x] = false;
                after_erosion[y, x] = true;
            }
            for (var y = 0; y < src.Height; y++)
            for (var x = 0; x < src.Width; x++)
                if (src_bool[y, x])
                    for (var j = -radius; j <= radius; j++)
                    for (var i = -radius; i <= radius; i++)
                        if (CheckInBound(src, y + j, x + i))
                            after_dilation[y + j, x + i] = true;
            for (var y = 0; y < src.Height; y++)
            for (var x = 0; x < src.Width; x++)
                if (!after_dilation[y, x])
                    for (var j = -radius; j <= radius; j++)
                    for (var i = -radius; i <= radius; i++)
                        if (CheckInBound(src, y + j, x + i))
                            after_erosion[y + j, x + i] = true;
            return ImageUtils.ArrayBool2DToBitmap(after_erosion);
        }

        public class CCforClassification
        {
            public float eccentricity;
            public Point massCenter;
            public Rectangle maxBoundingRectangle;
            public float MBRArea;
            public RotatedRect minBoundingRectangle;
            public double orientation = -1;
            public int pixelCount;
            public int pixelId;
            public float rectangularity;

            ///Next step:
            ///1.Compute the histogram along length of mbr
            ///2.Try to use other shape descriptors
            ///3.Try to train a SVM

            //Used in writing the feature to txt file
            public override string ToString()
            {
                //return pixelCount + " " + MBRArea + " " + rectangularity + " " + eccentricity;
                return rectangularity + " " + eccentricity;
            }
        }

        /*
         * This class is basically inherited from the class MyConnectedComponentsAnalysisFast.
         */
        public class CCfetcher
        {
            public ushort[] objectLabels; // narges  
            private ushort objectsCount;
            private int[] objectSize;

            private void Process(Bitmap srcImg)
            {
                // get source image size
                var width = srcImg.Width;
                var height = srcImg.Height;
                // allocate labels array
                objectLabels = new ushort[width * height];
                // initial labels count
                ushort labelsCount = 0;
                // create map
                var maxObjects = (width / 2 + 1) * (height / 2 + 1) + 1;
                var map = new int[maxObjects];
                // initially map all labels to themself
                for (var i = 0; i < maxObjects; i++)
                    map[i] = i;
                // lock source bitmap data
                var srcData = srcImg.LockBits(
                    new Rectangle(0, 0, width, height),
                    ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);
                var srcStride = srcData.Stride;
                var srcOffset = srcStride - width;
                // do the job
                unsafe
                {
                    var src = (byte*) srcData.Scan0.ToPointer();
                    var p = 0;
                    // label the first pixel
                    // 1 - for pixels of the first row
                    if (*src != 0)
                        objectLabels[p] = ++labelsCount; // label start from 1
                    ++src;
                    ++p;
                    // label the first row
                    for (var x = 1; x < width; x++, src++, p++)
                        // check if we need to label current pixel
                        if (*src != 0)
                            if (src[-1] != 0)
                                // label current pixel, as the previous
                                objectLabels[p] = objectLabels[p - 1];
                            else
                                // create new label
                                objectLabels[p] = ++labelsCount;
                    src += srcOffset;
                    // 2 - for other rows
                    // for each row
                    for (var y = 1; y < height; y++)
                    {
                        // for the first pixel of the row, we need to check
                        // only upper and upper-right pixels
                        if (*src != 0)
                            if (src[-srcStride] != 0)
                                // label current pixel, as the above
                                objectLabels[p] = objectLabels[p - width];
                            else if (src[1 - srcStride] != 0)
                                // label current pixel, as the above right
                                objectLabels[p] = objectLabels[p + 1 - width];
                            else
                                // create new label
                                objectLabels[p] = ++labelsCount;
                        ++src;
                        ++p;
                        // check left pixel and three upper pixels
                        for (var x = 1; x < width - 1; x++, src++, p++)
                            if (*src != 0)
                            {
                                // check surrounding pixels
                                if (src[-1] != 0)
                                    // label current pixel, as the left
                                    objectLabels[p] = objectLabels[p - 1];
                                else if (src[-1 - srcStride] != 0)
                                    // label current pixel, as the above left
                                    objectLabels[p] = objectLabels[p - 1 - width];
                                else if (src[-srcStride] != 0)
                                    // label current pixel, as the above
                                    objectLabels[p] = objectLabels[p - width];
                                if (src[1 - srcStride] != 0)
                                    if (objectLabels[p] == 0)
                                        // label current pixel, as the above right
                                    {
                                        objectLabels[p] = objectLabels[p + 1 - width];
                                    }
                                    else
                                    {
                                        int l1 = objectLabels[p];
                                        int l2 = objectLabels[p + 1 - width];
                                        if (l1 != l2 && map[l1] != map[l2])
                                        {
                                            // merge
                                            if (map[l1] == l1)
                                                // map left value to the right
                                            {
                                                map[l1] = map[l2];
                                            }
                                            else if (map[l2] == l2)
                                                // map right value to the left
                                            {
                                                map[l2] = map[l1];
                                            }
                                            else
                                            {
                                                // both values already mapped
                                                map[map[l1]] = map[l2];
                                                map[l1] = map[l2];
                                            }
                                            // reindex
                                            for (var i = 1; i <= labelsCount; i++)
                                                if (map[i] != i)
                                                {
                                                    // reindex
                                                    var j = map[i];
                                                    while (j != map[j])
                                                        j = map[j];
                                                    map[i] = j;
                                                }
                                        }
                                    }
                                if (objectLabels[p] == 0)
                                    // create new label
                                    objectLabels[p] = ++labelsCount;
                            }
                        // for the last pixel of the row, we need to check
                        // only upper and upper-left pixels
                        if (*src != 0)
                            if (src[-1] != 0)
                                objectLabels[p] = objectLabels[p - 1];
                            else if (src[-1 - srcStride] != 0)
                                objectLabels[p] = objectLabels[p - 1 - width];
                            else if (src[-srcStride] != 0)
                                objectLabels[p] = objectLabels[p - width];
                            else
                                objectLabels[p] = ++labelsCount;
                        ++src;
                        ++p;
                        src += srcOffset;
                    }
                }
                // unlock source images
                srcImg.UnlockBits(srcData);
                // allocate remapping array
                var reMap = new ushort[map.Length];
                // count objects and prepare remapping array
                objectsCount = 0;
                for (var i = 1; i <= labelsCount; i++)
                    if (map[i] == i)
                        reMap[i] = ++objectsCount;
                // second pass to compete remapping
                for (var i = 1; i <= labelsCount; i++)
                    if (map[i] != i)
                        reMap[i] = reMap[map[i]];
                // repair object labels
                for (int i = 0, n = objectLabels.Length; i < n; i++)
                    objectLabels[i] = reMap[objectLabels[i]];
            }

            // Get array of objects rectangles
            public List<CCforClassification> GetConnectedComponents(Bitmap srcImg)
            {
                Process(srcImg);
                var labels = objectLabels;
                var maxLabel = 0;
                for (var j = 0; j < labels.Length; j++)
                    if (labels[j] > maxLabel) maxLabel = labels[j];
                int count = objectsCount;
                objectSize = new int[count + 1];
                var center_x = new double[count + 1];
                var center_y = new double[count + 1];
                // LBF off for now
                //LineOfBestFit[] lbf = new LineOfBestFit[count + 1];
                // image size
                var width = srcImg.Width;
                var height = srcImg.Height;
                int i = 0, label;
                // create object coordinates arrays
                var x1 = new int[count + 1];
                var y1 = new int[count + 1];
                var x2 = new int[count + 1];
                var y2 = new int[count + 1];
                var sample_x = new int[count + 1];
                var sample_y = new int[count + 1];
                for (var j = 1; j <= count; j++)
                {
                    x1[j] = width;
                    y1[j] = height;
                }
                // walk through labels array, skip one row and one col
                for (var y = 0; y < height; y++)
                for (var x = 0; x < width; x++, i++)
                {
                    // get current label
                    label = labels[i];
                    // skip unlabeled pixels
                    if (label == 0)
                        continue;
                    objectSize[label]++;
                    // check and update all coordinates
                    center_x[label] += x;
                    center_y[label] += y;
                    sample_x[label] = x; // record the last point as a sample
                    sample_y[label] = y;
                    if (x < x1[label])
                        x1[label] = x;
                    if (x > x2[label])
                        x2[label] = x;
                    if (y < y1[label])
                        y1[label] = y;
                    if (y > y2[label])
                        y2[label] = y;
                }
                //Get the list of pixel locations to calculate minimum bounding rectangles
                var positions = new List<PointF[]>();
                //Store the next bias to insert in each list in positions
                var nextPos = new int[count];
                for (var j = 1; j <= count; j++)
                    positions.Add(new PointF[objectSize[j]]);
                i = 0;
                for (var y = 0; y < height; y++)
                for (var x = 0; x < width; x++, i++)
                {
                    label = labels[i] - 1; //because label starts from 1
                    if (label == -1) continue;
                    ///not sure if the label number is always less than count ????
                    positions[label][nextPos[label]] = new PointF(x, y);
                    nextPos[label] += 1;
                }
                var CCs = new List<CCforClassification>();
                for (var j = 1; j <= count; j++)
                {
                    var b = new CCforClassification();
                    b.maxBoundingRectangle = new Rectangle(x1[j], y1[j], x2[j] - x1[j] + 1, y2[j] - y1[j] + 1);
                    //Calculate the minimum bounding rectangles for each CC
                    //from this webpage: http://www.emgu.com/wiki/index.php/Minimum_Area_Rectangle_in_CSharp
                    b.minBoundingRectangle = CvInvoke.MinAreaRect(positions[j - 1]);
                    b.pixelCount = objectSize[j];
                    b.massCenter = new Point(Convert.ToInt32(center_x[j] / b.pixelCount),
                        Convert.ToInt32(center_y[j] / b.pixelCount));
                    //b.area = (x2[j] - x1[j] + 1) * (y2[j] - y1[j] + 1);
                    b.MBRArea = b.minBoundingRectangle.Size.Height * b.minBoundingRectangle.Size.Width;
                    b.rectangularity = b.pixelCount / b.MBRArea;
                    b.eccentricity = Math.Max(b.minBoundingRectangle.Size.Height, b.minBoundingRectangle.Size.Width) /
                                     Math.Min(b.minBoundingRectangle.Size.Height, b.minBoundingRectangle.Size.Width);
                    b.pixelId = j;
                    if (b.maxBoundingRectangle.Width == 1)
                    {
                        b.orientation = 90;
                        //b.m = Double.NaN;
                        //b.b = x1[j]; // x = 4
                    }
                    else if (b.maxBoundingRectangle.Height == 1)
                    {
                        b.orientation = 180;
                        //b.m = 0;
                        //b.b = y1[j]; // y = 4
                    }
                    CCs.Add(b);
                }
                return CCs;
            }
        }
    }
}