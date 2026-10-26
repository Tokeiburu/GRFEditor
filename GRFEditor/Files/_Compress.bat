@echo off

set c= -breakOnExceptions true true

REM Breaking on exceptions will allow you to see which commands
REM have issues; every batch file uses it as its first option

REM set c=%c% -gzip ".dll" "Compressed\.dll"
set c=%c% -gzip "ICSharpCode.AvalonEdit.dll" "Compressed\ICSharpCode.AvalonEdit.dll"
set c=%c% -gzip "Be.Windows.Forms.HexBox.dll" "Compressed\Be.Windows.Forms.HexBox.dll"
set c=%c% -gzip "Gif.Components.dll" "Compressed\Gif.Components.dll"
set c=%c% -gzip "TokeiLibrary.dll" "Compressed\TokeiLibrary.dll"
set c=%c% -gzip "Utilities.dll" "Compressed\Utilities.dll"
set c=%c% -gzip "Lua.dll" "Compressed\Lua.dll"
set c=%c% -gzip "GRF.dll" "Compressed\GRF.dll"
set c=%c% -gzip "ActImaging.dll" "Compressed\ActImaging.dll"
set c=%c% -gzip "Lua.dll" "Compressed\Lua.dll"
set c=%c% -gzip "ColorPicker.dll" "Compressed\ColorPicker.dll"
set c=%c% -gzip "GrfToWpfBridge.dll" "Compressed\GrfToWpfBridge.dll"
set c=%c% -gzip "OpenTK.dll" "Compressed\OpenTK.dll"
set c=%c% -gzip "OpenTK.GLControl.dll" "Compressed\OpenTK.GLControl.dll"
set c=%c% -gzip "K4os.Compression.LZ4.dll" "Compressed\K4os.Compression.LZ4.dll"
set c=%c% -gzip "System.IO.Hashing.dll" "Compressed\System.IO.Hashing.dll"
set c=%c% -gzip "System.Memory.dll" "Compressed\System.Memory.dll"
set c=%c% -gzip "System.Runtime.CompilerServices.Unsafe.dll" "Compressed\System.Runtime.CompilerServices.Unsafe.dll"
set c=%c% -gzip "WpfAvalonUtility.dll" "Compressed\WpfAvalonUtility.dll"
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