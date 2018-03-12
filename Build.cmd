@echo off

if [%1]==[] goto INVALID_COMMAND_LINE_ERROR

cd  Src
call Build.cmd || goto END

cd ..\Samples
call Build.cmd || goto END

cd ..
call Pack.cmd %1 || goto END

goto END

:INVALID_COMMAND_LINE_ERROR
echo Error: Package version is not specified

:END
