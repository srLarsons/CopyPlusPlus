@echo off
setlocal

set "QUIET=0"
if /I "%~1"=="/quiet" set "QUIET=1"

set "APP_EXE=%~dp0WpfMultiCopyClipboard.exe"

if not exist "%APP_EXE%" (
    echo Could not find WpfMultiCopyClipboard.exe in %~dp0
    if "%QUIET%"=="0" pause
    exit /b 1
)

reg add "HKCU\Software\Classes\DesktopBackground\Shell\CopyPlusPlus" /ve /d "copy++" /f
reg add "HKCU\Software\Classes\DesktopBackground\Shell\CopyPlusPlus" /v "Icon" /d "\"%APP_EXE%\"" /f
reg add "HKCU\Software\Classes\DesktopBackground\Shell\CopyPlusPlus" /v "Position" /d "Bottom" /f
reg add "HKCU\Software\Classes\DesktopBackground\Shell\CopyPlusPlus\command" /ve /d "\"%APP_EXE%\" --show" /f

reg add "HKCU\Software\Classes\Directory\Background\Shell\CopyPlusPlus" /ve /d "copy++" /f
reg add "HKCU\Software\Classes\Directory\Background\Shell\CopyPlusPlus" /v "Icon" /d "\"%APP_EXE%\"" /f
reg add "HKCU\Software\Classes\Directory\Background\Shell\CopyPlusPlus" /v "Position" /d "Bottom" /f
reg add "HKCU\Software\Classes\Directory\Background\Shell\CopyPlusPlus\command" /ve /d "\"%APP_EXE%\" --show" /f

reg add "HKCU\Software\Classes\Directory\Shell\CopyPlusPlus" /ve /d "copy++" /f
reg add "HKCU\Software\Classes\Directory\Shell\CopyPlusPlus" /v "Icon" /d "\"%APP_EXE%\"" /f
reg add "HKCU\Software\Classes\Directory\Shell\CopyPlusPlus\command" /ve /d "\"%APP_EXE%\" --show" /f

reg add "HKCU\Software\Classes\*\Shell\CopyPlusPlus" /ve /d "copy++" /f
reg add "HKCU\Software\Classes\*\Shell\CopyPlusPlus" /v "Icon" /d "\"%APP_EXE%\"" /f
reg add "HKCU\Software\Classes\*\Shell\CopyPlusPlus\command" /ve /d "\"%APP_EXE%\" --show" /f

reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Run" /v "CopyPlusPlus" /d "\"%APP_EXE%\" --minimized" /f

start "" "%APP_EXE%" --minimized

if "%QUIET%"=="0" (
    echo.
    echo Installed copy++ desktop, folder, and file context menus.
    echo The app will also start automatically at Windows login.
    echo.
    pause
)