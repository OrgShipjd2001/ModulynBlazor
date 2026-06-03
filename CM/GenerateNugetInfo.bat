@echo off

if not %1.==. if "%1"=="/force" set force=force

set genNugtScriptDir=%~dp0
call %genNugtScriptDir%\setvariables.bat %force%

pushd %rootDir%

mkdir %nugetDir% >NUL
mkdir %nugetDir%\Data >NUL
copy /y %rootDir%\License %nugetDir%\Data\License.txt
copy /y %rootDir%\Resources\ModulynBlazor.jpg %nugetDir%\Data\ModulynBlazor.jpg

mkdir %nugetDir%\Data\websvr >NUL
copy /y %rootDir%\docs\PackageReadme\Modulyn.websvr.Readme.md %nugetDir%\Data\websvr\ReadMe.md

mkdir %nugetDir%\Data\interface >NUL
copy /y %rootDir%\docs\PackageReadme\Modulyn.Interface.Readme.md %nugetDir%\Data\interface\ReadMe.md

echo.
echo Retrieve Nuget package dependency info

REM for readability, set the list in a variable (list is comma delimited)
set csprojList=ModulynServer\ModulynServer.csproj
powershell %rootDir%\cm\scripts\generate_dependencies.ps1 -csprojFiles %csprojList% -outputDir %nugetDir%\Data\websvr

set csprojList=ModulynInterface\ModulynInterface.csproj
powershell %rootDir%\cm\scripts\generate_dependencies.ps1 -csprojFiles %csprojList% -outputDir %nugetDir%\Data\interface

popd
