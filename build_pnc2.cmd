:: @echo off
SET OUTDIR=.\src\Panther2\bin

call .\pnc.cmd .\src\Panther2\main.pn /o %OUTDIR%\panther2.exe  /r "D:\code\Panther2\src\Panther.StdLib\bin\Debug\net6.0\Panther.StdLib.dll" /r "C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\6.0.5\ref\net6.0\System.Console.dll" /r "C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\6.0.5\ref\net6.0\System.Runtime.dll" /r "C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\6.0.5\ref\net6.0\System.Runtime.Extensions.dll" %*

copy "D:\code\Panther2\src\Panther.StdLib\bin\Debug\net6.0\Panther.StdLib.dll" %OUTDIR%\
:: copy "C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\6.0.5\ref\net6.0\System.Console.dll" %OUTDIR%\
copy "C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\6.0.5\ref\net6.0\System.Runtime.dll" %OUTDIR%\
:: copy "C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\6.0.5\ref\net6.0\System.Runtime.Extensions.dll" %OUTDIR%\

.\src\Panther2\bin\panther2.exe