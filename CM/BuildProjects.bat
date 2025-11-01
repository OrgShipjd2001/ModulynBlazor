@echo off

if not %1.==. if "%1"=="/force" set force=force


set pubProjScriptDir=%~dp0
call %pubProjScriptDir%\setvariables.bat %force%

if exist %buildDir% rmdir /s /q %buildDir%

pushd %rootDir%

echo Build Debug
dotnet build ModulynServer\ModulynServer.csproj -o %builddebugdir%\ModulynServer -p:Configuration=Debug;Platform=AnyCPU
if errorlevel 1 goto BuildError

dotnet build ModulynInterface\ModulynInterface.csproj -o %builddebugdir%\ModulynInterface -p:Configuration=Debug;Platform=AnyCPU
if errorlevel 1 goto BuildError

dotnet build Test\TestModule\TestModule.csproj -o %builddebugdir%\Modules\TestModule -p:Configuration=Debug;Platform=AnyCPU
if errorlevel 1 goto BuildError
echo run migrations > %builddebugdir%\ModulynServer\runmigrations.txt

echo Build Release
dotnet build ModulynServer\ModulynServer.csproj -o %buildreleasedir%\ModulynServer -p:Configuration=Release;Platform=AnyCPU
if errorlevel 1 goto BuildError
dotnet build ModulynInterface\ModulynInterface.csproj -o %buildreleasedir%\ModulynInterface -p:Configuration=Release;Platform=AnyCPU
if errorlevel 1 goto BuildError
dotnet build Test\TestModule\TestModule.csproj -o %buildreleasedir%\Modules\TestModule -p:Configuration=Release;Platform=AnyCPU
if errorlevel 1 goto BuildError
echo run migrations > %buildreleasedir%\ModulynServer\runmigrations.txt

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