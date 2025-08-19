@echo off
echo ========================================
echo     WAM Installer Builder
echo ========================================
echo.

echo [1/5] Cleaning previous builds...
if exist "bin\Release" rmdir /s /q "bin\Release"
if exist "WAM_Installer.exe" del "WAM_Installer.exe"

echo [2/5] Building WAM Application...
dotnet clean --configuration Release
dotnet build --configuration Release
if errorlevel 1 goto error

echo [3/5] Publishing self-contained application...
dotnet publish --configuration Release --runtime win-x64 --self-contained true --output "bin\Release\net8.0-windows\publish" -p:PublishSingleFile=false -p:PublishReadyToRun=true
if errorlevel 1 goto error

echo [4/5] Checking NSIS installation...
where makensis >nul 2>nul
if errorlevel 1 (
    echo ERROR: NSIS not found in PATH!
    echo Please install NSIS from: https://nsis.sourceforge.io/Download
    echo After installation, add NSIS to your PATH or use full path to makensis.exe
    pause
    goto end
)

echo [5/5] Creating installer with NSIS...
makensis create_installer.nsi
if errorlevel 1 goto error

echo.
echo ========================================
echo     Installer Created Successfully!
echo ========================================
echo.
echo Installer file: WAM_Installer.exe
echo File size: 
for %%I in (WAM_Installer.exe) do echo %%~zI bytes
echo.
echo You can now distribute this single file!
echo Recipients just need to:
echo 1. Download WAM_Installer.exe
echo 2. Right-click and "Run as administrator"
echo 3. Follow the installation wizard
echo 4. Launch WAM from Start Menu or Desktop
echo.
pause
goto end

:error
echo.
echo ========================================
echo     ERROR: Build Failed!
echo ========================================
echo.
pause

:end 