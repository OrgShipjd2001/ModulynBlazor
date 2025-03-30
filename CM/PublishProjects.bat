@echo off

if not %1.==. if "%1"=="/force" set force=true

set pubProjScriptDir=%~dp0
call %pubProjScriptDir%\setvariables.bat %force%

if exist %PublishDir% rmdir /s /q %PublishDir%

pushd %rootDir%

dotnet publish ModulynServer\ModulynServer.csproj -o %pubDebDir%\ModulynServer --no-self-contained -c Debug -r win-x64
dotnet publish ModulynInterface\ModulynInterface.csproj -o %pubDebDir%\ModulynInterface --no-self-contained -c Debug
dotnet publish Test\TestModule\TestModule.csproj -o %pubDebDir%\Modules\TestModule --no-self-contained -c Debug -r win-x64

dotnet publish ModulynServer\ModulynServer.csproj -o %pubRelDir%\ModulynServer --no-self-contained -c Release -r win-x64
dotnet publish ModulynInterface\ModulynInterface.csproj -o %pubRelDir%\ModulynInterface --no-self-contained -c Release
dotnet publish Test\TestModule\TestModule.csproj -o %pubRelDir%\Modules\TestModule --no-self-contained -c Release -r win-x64

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

:Done