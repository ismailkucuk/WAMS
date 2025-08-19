@echo off
echo ========================================
echo     WAM Deployment Script
echo ========================================
echo.

echo Building and Publishing WAM Application...
echo.

REM Clean previous builds
echo [1/4] Cleaning previous builds...
dotnet clean --configuration Release
if errorlevel 1 goto error

REM Build the application
echo [2/4] Building application...
dotnet build --configuration Release
if errorlevel 1 goto error

REM Publish for Windows x64 (Self-contained)
echo [3/4] Publishing for Windows x64 (Self-contained)...
dotnet publish --configuration Release --runtime win-x64 --self-contained true --output "./deploy/windows-x64" -p:PublishSingleFile=true -p:PublishReadyToRun=true
if errorlevel 1 goto error

REM Publish for Windows x86
echo [4/4] Publishing for Windows x86...
dotnet publish --configuration Release --runtime win-x86 --self-contained true --output "./deploy/windows-x86" -p:PublishSingleFile=true -p:PublishReadyToRun=true
if errorlevel 1 goto error

echo.
echo ========================================
echo     Deployment Completed Successfully!
echo ========================================
echo.
echo Published files location:
echo   - Windows x64: ./deploy/windows-x64/
echo   - Windows x86: ./deploy/windows-x86/
echo.
echo You can copy these folders to target machines.
echo No .NET installation required on target machines!
echo.
pause
goto end

:error
echo.
echo ========================================
echo     ERROR: Deployment Failed!
echo ========================================
echo.
pause

:end 