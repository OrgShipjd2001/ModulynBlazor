@echo off

set variableFile=globalvars.env

REM create variableFile for pipeline
if not %2.==. set variableFile=%2

REM override - force setting the variables
if not %1.==. if "%1"=="force" goto Force
if "%force%"=="force" goto Force

REM only set the variables once
if not "%setvarScriptDir%"=="" goto Done

:Force
if not "%variableFile%"=="" if exist %variableFile% erase /f /q %variableFile%
set scripterror=false

set setvarScriptDir=%~dp0
echo setvarScriptDir: %setvarScriptDir%

pushd %setvarScriptDir%..
set rootDir=%cd%
popd
echo rootDir: %rootDir%
if not "%variableFile%"=="" echo rootDir=%rootDir% >> %variableFile%

set binaryDir=%rootDir%\binaries
echo binaryDir: %binaryDir%
if not "%variableFile%"=="" echo binaryDir=%binaryDir% >> %variableFile%

set buildDir=%binaryDir%\build
echo buildDir: %buildDir%
if not "%variableFile%"=="" echo buildDir=%buildDir% >> %variableFile%

set buildDebugDir=%buildDir%\Debug
echo buildDebugDir: %buildDebugDir%
if not "%variableFile%"=="" echo buildDebugDir=%buildDebugDir% >> %variableFile%

set buildReleaseDir=%buildDir%\Release
echo buildReleaseDir: %buildReleaseDir%
if not "%variableFile%"=="" echo buildReleaseDir=%buildReleaseDir% >> %variableFile%

set publishDir=%binaryDir%\Publish
echo publishDir: %publishDir%
if not "%variableFile%"=="" echo publishDir=%publishDir% >> %variableFile%

set pubRelDir=%publishdir%\Release
echo pubRelDir: %pubRelDir%
if not "%variableFile%"=="" echo pubRelDir=%pubRelDir% >> %variableFile%

set pubDebDir=%publishdir%\Debug
echo pubDebDir: %pubDebDir%
if not "%variableFile%"=="" echo pubDebDir=%pubDebDir% >> %variableFile%

set nugetDir=%binaryDir%\Nuget
echo nugetDir: %nugetDir%
if not "%variableFile%"=="" echo nugetDir=%nugetDir% >> %variableFile%

set zipDir=%binaryDir%\zip
echo zipDir: %zipDir%
if not "%variableFile%"=="" echo zipDir=%zipDir% >> %variableFile%

for /f "tokens=1,2,3* delims=<>" %%i in (%rootDir%\CM\version\assemblyversion.props) do if "%%j"=="FileVersion" set version=%%k
echo Version: %version%

:Done
set force=