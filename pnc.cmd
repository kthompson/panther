@echo off

REM Vars
set "SLNDIR=%~dp0src"

REM Restore + Build
dotnet build "%SLNDIR%\Panther.Compiler" --nologo || exit /b
dotnet build "%SLNDIR%\Panther.StdLib" --nologo || exit /b

REM Run
dotnet run -p "%SLNDIR%\Panther.Compiler" --no-build -- %*