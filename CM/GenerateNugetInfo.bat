@echo off

if not %1.==. if "%1"=="/force" set force=force

set genNugtScriptDir=%~dp0
call %genNugtScriptDir%\setvariables.bat %force%

pushd %rootDir%

mkdir %nugetDir% >NUL
mkdir %nugetDir%\Data >NUL
copy /y %rootDir%\License %nugetDir%\Data\License.txt
copy /y %rootDir%\ReadMe.md %nugetDir%\Data\ReadMe.md
copy /y %rootDir%\Resources\ModulynBlazor.jpg %nugetDir%\Data\ModulynBlazor.jpg

echo.
echo Retrieve Nuget package dependency info

REM for readability, set the list in a variable (list is comma delimited)
mkdir %nugetDir%\Data\websvr >NUL
set csprojList=ModulynServer\ModulynServer.csproj
powershell %rootDir%\cm\scripts\generate_dependencies.ps1 -csprojFiles %csprojList% -outputDir %nugetDir%\Data\websvr

mkdir %nugetDir%\Data\interface >NUL
set csprojList=ModulynInterface\ModulynInterface.csproj
powershell %rootDir%\cm\scripts\generate_dependencies.ps1 -csprojFiles %csprojList% -outputDir %nugetDir%\Data\interface

popd
