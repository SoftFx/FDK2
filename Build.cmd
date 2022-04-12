@echo off

if [%1]==[] goto INVALID_COMMAND_LINE_ERROR

call VsMSBuildCmd.bat || goto END

MSBuild.exe -restore FDK.sln || goto END

MSBuild.exe /t:Build /p:Configuration=Release /p:BuildNumber=%1 FDK.sln || goto END

goto END

:INVALID_COMMAND_LINE_ERROR
echo Error: Build version is not specified
exit 42

:END