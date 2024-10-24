@echo off

set scriptDir=%~dp0
set rootDir=%scriptDir%..
set publishDir=%rootDir%\Publish
set nugetDir=%PublishDir%\Nuget
set nugetexe=%rootDir%\buildtools\nuget\nuget.exe
set targetDir=%PublishDir%\Release

if "%version%"=="" for /f "tokens=1,2,3* delims=<>" %%i in (%scriptDir%\version\assemblyversion.props) do if "%%j"=="FileVersion" set version=%%k

mkdir %nugetDir% >NUL

REM Create nuspec
echo Create NuSpec file
if EXIST %nugetDir%\interface.nuspec erase /f /q %nugetDir%\interface.nuspec
if EXIST %nugetDir%\webserver.nuspec erase /f /q %nugetDir%\webserver.nuspec

REM Interface Package
echo ^<?xml version="1.0" encoding="utf-8"?^> >> %nugetDir%\interface.nuspec
echo ^<package xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd"^> >> %nugetDir%\interface.nuspec
echo ^<metadata^> >> %nugetDir%\interface.nuspec
echo ^<id^>ModulynInterface^</id^> >> %nugetDir%\interface.nuspec
echo ^<version^>%version%^</version^> >> %nugetDir%\interface.nuspec
echo ^<description^>A library to generate modules for the Modulyn Server^</description^> >> %nugetDir%\interface.nuspec
echo ^<authors^>Jared Shipley^</authors^> >> %nugetDir%\interface.nuspec
echo ^<repository type="git" url="https://github.com/OrgShipjd2001/ModulynBlazor.git" /^> >> %nugetDir%\interface.nuspec
echo ^<readme^>docs\README.md^</readme^> >> %nugetDir%\interface.nuspec
echo ^<dependencies^> >> %nugetDir%\interface.nuspec
echo ^<group targetFramework=".NET8.0" /^> >> %nugetDir%\interface.nuspec
echo ^</dependencies^> >> %nugetDir%\interface.nuspec
echo ^</metadata^> >> %nugetDir%\interface.nuspec
echo ^<files^> >> %nugetDir%\interface.nuspec

echo ^<file src="%targetDir%\ModulynInterface\**" target="lib\net8.0"/^> >> %nugetDir%\interface.nuspec
echo ^<file src="%rootDir%\README.md" target="docs\" /^> >> %nugetDir%\interface.nuspec

echo ^</files^> >> %nugetDir%\interface.nuspec
echo ^</package^> >> %nugetDir%\interface.nuspec


REM Deployment Package
echo ^<?xml version="1.0" encoding="utf-8"?^> >> %nugetDir%\webserver.nuspec
echo ^<package xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd"^> >> %nugetDir%\webserver.nuspec
echo ^<metadata^> >> %nugetDir%\webserver.nuspec
echo ^<id^>ModulynServer^</id^> >> %nugetDir%\webserver.nuspec
echo ^<version^>%version%^</version^> >> %nugetDir%\webserver.nuspec
echo ^<description^>The Modulyn Server framework^</description^> >> %nugetDir%\webserver.nuspec
echo ^<authors^>Jared Shipley^</authors^> >> %nugetDir%\webserver.nuspec
echo ^<repository type="git" url="https://github.com/OrgShipjd2001/ModulynBlazor.git" /^> >> %nugetDir%\webserver.nuspec
echo ^<readme^>docs\README.md^</readme^> >> %nugetDir%\webserver.nuspec
echo ^<dependencies^> >> %nugetDir%\webserver.nuspec
echo ^<group targetFramework=".NET8.0" /^> >> %nugetDir%\webserver.nuspec
echo ^</dependencies^> >> %nugetDir%\webserver.nuspec

echo ^</metadata^> >> %nugetDir%\webserver.nuspec
echo ^<files^> >> %nugetDir%\webserver.nuspec

echo ^<file src="%targetDir%\ModulynServer\**" target="content"/^> >> %nugetDir%\webserver.nuspec
echo ^<file src="%rootDir%\README.md" target="docs\" /^> >> %nugetDir%\webserver.nuspec

echo ^</files^> >> %nugetDir%\webserver.nuspec
echo ^</package^> >> %nugetDir%\webserver.nuspec

echo Generate Nuget package
erase /f /q %nugetDir%\*.nupkg
%nugetexe% pack %nugetDir%\interface.nuspec -OutputDirectory %nugetDir%
%nugetexe% pack %nugetDir%\webserver.nuspec -OutputDirectory %nugetDir%

:Done
