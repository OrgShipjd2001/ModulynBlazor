@echo off

set scriptDir=%~dp0
set rootDir=%scriptDir%..
set publishDir=%rootDir%\Publish
set zipDir=%PublishDir%\zip
set targetDir=%PublishDir%\Release

call %rootDir%\CM\GenerateRelease.bat

echo.
echo Push packages to GitLabServer
for /f %%i in ('dir /b %publishdir%\Nuget\*.nupkg') do %rootDir%\buildtools\nuget\nuget.exe push %publishdir%\Nuget\%%i -Source github
