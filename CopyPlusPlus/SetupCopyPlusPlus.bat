@echo off
setlocal

set "PAYLOAD=%~dp0CopyPlusPlusPayload.zip"
set "INSTALL_DIR=%LOCALAPPDATA%\Programs\CopyPlusPlus"

if not exist "%PAYLOAD%" (
    echo Missing setup payload: %PAYLOAD%
    exit /b 1
)

powershell -NoProfile -ExecutionPolicy Bypass -Command "$ErrorActionPreference='Stop'; $target=$env:LOCALAPPDATA + '\Programs\CopyPlusPlus'; New-Item -ItemType Directory -Force -Path $target | Out-Null; Expand-Archive -LiteralPath '%PAYLOAD%' -DestinationPath $target -Force"
if errorlevel 1 (
    echo Setup failed while extracting files.
    exit /b 1
)

call "%INSTALL_DIR%\InstallCopyPlusPlusContextMenu.bat" /quiet
if errorlevel 1 (
    echo Setup failed while registering desktop integration.
    exit /b 1
)
