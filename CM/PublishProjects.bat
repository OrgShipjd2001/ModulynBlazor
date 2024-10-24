@echo off

set scriptDir=%~dp0
set rootDir=%scriptDir%..
set publishDir=%rootDir%\Publish
set pubRelDir=%publishdir%\Release
set pubDebDir=%publishdir%\Debug

if exist %PublishDir% rmdir /s /q %PublishDir%

pushd %rootdir%

dotnet publish ModulynServer\ModulynServer.csproj -o %pubDebDir%\ModulynServer --no-self-contained -c Debug -r win-x64
dotnet publish ModulynInterface\ModulynInterface.csproj -o %pubDebDir%\ModulynInterface --no-self-contained -c Debug -r win-x64
dotnet publish Test\TestModule\TestModule.csproj -o %pubDebDir%\Modules\TestModule --no-self-contained -c Debug -r win-x64

dotnet publish ModulynServer\ModulynServer.csproj -o %pubRelDir%\ModulynServer --no-self-contained -c Release -r win-x64
dotnet publish ModulynInterface\ModulynInterface.csproj -o %pubRelDir%\ModulynInterface --no-self-contained -c Release -r win-x64
dotnet publish Test\TestModule\TestModule.csproj -o %pubRelDir%\Modules\TestModule --no-self-contained -c Release -r win-x64

popd

:Done