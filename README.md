# strabo: text recognition in scanned maps

## note for devolopmers
1. The opencv dlls need to be in the same version of the emgucv library. Strabo works with opencv 2.410.

2. The opencv dlls need to be compiled on the same platform as Strabo.

3. The debug folder needs to have both x86 and x64 folders for opencv and tesseract dlls.

4. Refresh (uninstall and install) tesseract and emgucv using Nuget to have the x86 and x64 folders created or when initializing tesseract is a problem.
