@echo off

cd  Src || goto END
call Build.cmd || goto END

cd ..\Samples || goto END
call Build.cmd || goto END

cd ..

:END
