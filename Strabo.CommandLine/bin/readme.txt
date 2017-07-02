Installation:
1. Unzip the Strabo release file to a folder where you have execute/write permissions.
2. If you don't have Visual C++ Redistributable for Visual Studio 2010 (x86) and 2012, download and install them from:
https://www.microsoft.com/en-us/download/details.aspx?id=30679
https://www.microsoft.com/en-us/download/details.aspx?id=5555

Running Strabo:

This is the command line version of Strabo. Here's examples that invokes Strabo for extracting labels from Tianditu English and Chinese text layer:
Strabo.Core.exe C:\Users\yaoyi\Documents\strabo-text-recognition\Strabo.CommandLine\data\USGS-15-CA-brawley-e1957-s1957-p1961.jpg C:\Users\yaoyi\Documents\strabo-text-recognition\Strabo.CommandLine\data\intermediate C:\Users\yaoyi\Documents\strabo-text-recognition\Strabo.CommandLine\data\output uscdl-usgs 8
More Details About Running Strabo:
1. Open acommand line window

2. Go to the Strabo folder (the unzipped folder that contains Strabo.Core.exe). For example:
        cd C:\Users\yaoyichi\Documents\github\strabo-command-line\Strabo.CommandLine\bin\
 
3. Strabo needs 5 parameters as input arguments:

Strabo.core.exe [input file path & name] [intermediate_folder] [output_folder] [string layer] [thread_number]

3.1 [input file path & name] is the full path and file name of your input map
3.2 The second and third parameters are two paths. "intermediate_folder" is the intermediate folder for storing log temporary files. "output_folder" is the output folder. 
3.3 The forthparameter is the name of the map seires. The map settings are in the config.txt file.
3.4 The last parameter is the number of threads allowed for Strabo to use. If your machine has 8 cores, you can set the thread number to 4. If your machine has 4 cores, you can set the thread number to 2.

4. The output is stored as GeoJSON files in your "output_folder". The GeoJSON file ending "ByPixels" is the results in image coordinates. You can directly drag both the input map and the GeoJSON file into QGIS for viewing the results.


