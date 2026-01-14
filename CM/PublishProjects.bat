@echo off

if not %1.==. if "%1"=="/force" set force=true

set pubProjScriptDir=%~dp0
call %pubProjScriptDir%\setvariables.bat %force%

if exist %PublishDir% rmdir /s /q %PublishDir%

pushd %rootDir%

echo Publish Debug
dotnet publish ModulynServer\ModulynServer.csproj -o %pubDebDir%\ModulynServer --no-self-contained -c Debug -r win-x64
if errorlevel 1 goto BuildError

dotnet build ModulynInterface\ModulynInterface.csproj -o %pubDebDir%\ModulynInterface --no-self-contained -c Debug
if errorlevel 1 goto BuildError

dotnet publish Test\TestModule\TestModule.csproj -o %pubDebDir%\Modules\TestModule --no-self-contained -c Debug -r win-x64
if errorlevel 1 goto BuildError

echo run migrations > %pubDebDir%\ModulynServer\runmigrations.txt

echo Publish Release
dotnet publish ModulynServer\ModulynServer.csproj -o %pubRelDir%\ModulynServer --no-self-contained -c Release -r win-x64
if errorlevel 1 goto BuildError

dotnet build ModulynInterface\ModulynInterface.csproj -o %pubRelDir%\ModulynInterface --no-self-contained -c Release
if errorlevel 1 goto BuildError

dotnet publish Test\TestModule\TestModule.csproj -o %pubRelDir%\Modules\TestModule --no-self-contained -c Release -r win-x64
if errorlevel 1 goto BuildError

echo run migrations > %pubRelDir%\ModulynServer\runmigrations.txt

goto BuildComplete

:BuildError
echo ERROR during build
set scripterror=true
goto Done

:BuildComplete
call %pubProjScriptDir%\GenerateNugetInfo.bat

:Done

popd

if "%scripterror%"=="true" exit /b 1
exit /b 0