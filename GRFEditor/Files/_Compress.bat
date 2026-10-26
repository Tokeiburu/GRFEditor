@echo off

set c= -breakOnExceptions true true

REM Breaking on exceptions will allow you to see which commands
REM have issues; every batch file uses it as its first option

set c=%c% -gzip "gpf.ico" "Compressed\gpf.ico"
set c=%c% -gzip "grf.ico" "Compressed\grf.ico"
set c=%c% -gzip "rgz.ico" "Compressed\rgz.ico"
set c=%c% -gzip "spr.ico" "Compressed\spr.ico"
set c=%c% -gzip "grfkey.ico" "Compressed\grfkey.ico"
set c=%c% -gzip "default.pal" "Compressed\default.pal"
set c=%c% -gzip "gsf.ico" "Compressed\gsf.ico"


:PROGRAM
set c=%c%

..\GrfCL.exe %c%
exit