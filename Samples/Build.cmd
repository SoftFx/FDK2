@echo off

call VsMSBuildCmd.bat || goto END

MSBuild.exe /t:Build /p:Configuration=Release Samples.sln || goto END

:END