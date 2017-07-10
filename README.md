# strabo: text recognition in scanned maps

## note for developers 
When building Strabo on Windows, choose x64 as the target platform to allow Strabo use large memory spaces.

## note for developers using emgucv 3.1
You will need to install emgucv using nuget. Visual Studio 2017 will take care of the opencv dlls.

## note for developers using opencv 2.x
1. Some classes in emgucv 2.x are changed in 3.1 so you will need to refactor some of the Strabo classes. Using emgucv 3.1 is highly recommended.

2. The opencv dlls need to be in the same version of the emgucv library. Strabo works with opencv 2.410.

3. The opencv dlls need to be compiled on the same platform as Strabo.

4. The debug folder needs to have both x86 and x64 folders for opencv and tesseract dlls.

5. Refresh (uninstall and install) tesseract and emgucv using Nuget to have the x86 and x64 folders created or when initializing tesseract is a problem.


