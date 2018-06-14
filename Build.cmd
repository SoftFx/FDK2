@echo off

call VsMSBuildCmd.bat || goto END

MSBuild.exe /t:Build /p:Configuration=Release FDK.sln || goto END

:END