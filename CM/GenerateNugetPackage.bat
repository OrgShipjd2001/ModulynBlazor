@echo off

if not %1.==. if "%1"=="/force" set force=true

set genPckScriptDir=%~dp0
call %genPckScriptDir%\SetVariables.bat %force%

set prerelease=-localDev

mkdir %nugetDir% >NUL

if EXIST %nugetDir%\*.nuspec erase /f /q %nugetDir%\*.nuspec
erase /f /q %nugetDir%\*.nupkg

call %rootdir%\cm\Nuget\GenerateInterfacePackage.bat %version%%prerelease% %buildReleaseDir% %nugetDir%\interface.nuspec
call %rootdir%\cm\Nuget\GenerateWebSvrPackage.bat %version%%prerelease% %buildReleaseDir% %nugetDir%\WebSvr.nuspec

%rootDir%\buildtools\nuget\nuget.exe pack %nugetDir%\interface.nuspec -OutputDirectory %nugetDir%
%rootDir%\buildtools\nuget\nuget.exe pack %nugetDir%\WebSvr.nuspec -OutputDirectory %nugetDir%
