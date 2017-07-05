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

using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

/// <summary>
/// Narges
/// </summary>
namespace Strabo.Core.TextRecognition
{
    public struct FieldAliases
    {
        public string DetectionCost { get; set; }

        public string OBJECTID { get; set; }

        public string Text { get; set; }

        public string Char_count { get; set; }

        public string Orientation { get; set; }

        public string Filename { get; set; }

        public string Susp_text { get; set; }

        public string Susp_char_count { get; set; }

        public string Mass_centerX { get; set; }

        public string Mass_centerY { get; set; }
    }

    public struct Spatial_Reference
    {
        public int wkid { get; set; }

        public int latestWkid { get; set; }
    }

    public struct Field_info
    {
        public string name { get; set; }

        public string type { get; set; }

        public string alias { get; set; }

        public int length { get; set; }
    }

    public class Geometry
    {
        public double[,,] rings { get; set; } = new double[1, 5, 2];
    }

    public class Attributes
    {
        public int OBJECTID { get; set; }

        public string Text { get; set; }

        public int Char_count { get; set; }

        public double Orientation { get; set; }

        public string Filename { get; set; }

        public string Susp_text { get; set; }

        public int Susp_char_count { get; set; }

        public double Mass_centerX { get; set; }

        public double Mass_centerY { get; set; }

        public float DetectionCost { get; set; }
    }

    public class Features
    {
        public Geometry geometry { get; set; } = new Geometry();

        public Attributes attributes { get; set; } = new Attributes();
    }

    public class FeatureInJSON
    {
        public List<Features> features = new List<Features>();
        public FieldAliases fieldAliases = new FieldAliases();
        public Field_info[] fields = new Field_info[10];
        public Spatial_Reference spatialReference = new Spatial_Reference();

        public string displayFieldName { get; set; }

        public string geometryType { get; set; }
    }

    public class Geojson
    {
        public FeatureInJSON featureInJson { get; set; }

        public void writeJsonFile(string path)
        {
            var json = JsonConvert.SerializeObject(featureInJson);
            File.WriteAllText(path, json);
        }

        public FeatureInJSON readGeoJsonFile(string path)
        {
            var json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<FeatureInJSON>(json);
        }
    }
}