@echo off

if [%1]==[] goto INVALID_COMMAND_LINE_ERROR

cd  Src
call Build.cmd || goto END

cd ..\Samples
call Build.cmd || goto END

cd ..
call Pack.cmd %1 || goto END

del "TickTrader FDK "%1".zip"
"C:\Program Files\7-Zip\7z.exe" a -tzip "TickTrader FDK "%1".zip" "TickTrader FDK "%1"" || goto END

goto END

:INVALID_COMMAND_LINE_ERROR
echo Error: Package version is not specified

:END