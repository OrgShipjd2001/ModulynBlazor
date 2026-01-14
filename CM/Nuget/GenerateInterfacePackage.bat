@echo off

setlocal

set packVer=%1
set sourceDir=%2
set nuspecFile=%3
if "%nugetdir%"=="" set nugetDir=%sourceDir%\..\..\Nuget

echo packVer: %packVer%
echo sourceDir: %sourceDir%
echo nuspecFile: %nuspecFile%
echo nugetDir: %nugetDir%

REM Create nuspec
echo Create NuSpec file
if EXIST %nuspecFile% erase /f /q %nuspecFile%

REM Interface Package
echo ^<?xml version="1.0" encoding="utf-8"?^> >> %nuspecFile%
echo ^<package xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd"^> >> %nuspecFile%
echo ^<metadata^> >> %nuspecFile%
echo ^<id^>Modulyn.Interface^</id^> >> %nuspecFile%
echo ^<version^>%packVer%^</version^> >> %nuspecFile%
echo ^<description^>A library containing the interfaces needed generate modules for the Modulyn Server^</description^> >> %nuspecFile%
echo ^<authors^>Infinite Build^</authors^> >> %nuspecFile%
echo ^<repository type="git" url="https://github.com/InfiniteBuild/ModulynBlazor.git" /^> >> %nuspecFile%
echo ^<readme^>docs\ReadMe.md^</readme^> >> %nuspecFile%
echo ^<license type="file"^>License.txt^</license^>  >> %nuspecFile%
echo ^<icon^>images/ModulynBlazor.jpg^</icon^> >> %nuspecFile%
echo ^<developmentDependency^>true^</developmentDependency^> >> %nuspecFile%
echo ^<dependencies^> >> %nuspecFile%
for %%i in ("%nugetDir%\data\interface\dependencies*.xml") do (
    type %%i >> %nuspecFile%
	echo. >> %nuspecFile%
)
echo ^</dependencies^> >> %nuspecFile%
echo ^</metadata^> >> %nuspecFile%
echo ^<files^> >> %nuspecFile%

echo ^<file src="%sourceDir%\ModulynInterface\**" target="lib\net10.0"/^> >> %nuspecFile%
echo ^<file src="%nugetdir%\Data\ReadMe.md" target="docs\" /^> >> %nuspecFile%
echo ^<file src="%nugetdir%\Data\LICENSE.txt" target="" /^> >> %nuspecFile%
echo ^<file src="%nugetdir%\Data\ModulynBlazor.jpg" target="images\" /^> >> %nuspecFile%

echo ^</files^> >> %nuspecFile%
echo ^</package^> >> %nuspecFile%

:Done
endlocal
