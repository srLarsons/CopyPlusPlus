@echo off
setlocal

REM Installs copy++ context-menu entries and starts the app.
REM Run this BAT from the same folder as WpfMultiCopyClipboard.exe after publishing/building the app.

set "APP_EXE=%~dp0WpfMultiCopyClipboard.exe"

if not exist "%APP_EXE%" (
    echo Could not find WpfMultiCopyClipboard.exe in:
    echo %~dp0
    echo.
    echo First publish or build the WPF app, then run this BAT from the output folder.
    pause
    exit /b 1
)

REM Desktop background menu, e.g. where Refresh appears.
reg add "HKCU\Software\Classes\DesktopBackground\Shell\CopyPlusPlus" /ve /d "copy++" /f
reg add "HKCU\Software\Classes\DesktopBackground\Shell\CopyPlusPlus" /v "Icon" /d "\"%APP_EXE%\"" /f
reg add "HKCU\Software\Classes\DesktopBackground\Shell\CopyPlusPlus" /v "Position" /d "Bottom" /f
reg add "HKCU\Software\Classes\DesktopBackground\Shell\CopyPlusPlus\command" /ve /d "\"%APP_EXE%\" --show" /f

REM Folder background menu, e.g. right-click empty area inside a folder.
reg add "HKCU\Software\Classes\Directory\Background\Shell\CopyPlusPlus" /ve /d "copy++" /f
reg add "HKCU\Software\Classes\Directory\Background\Shell\CopyPlusPlus" /v "Icon" /d "\"%APP_EXE%\"" /f
reg add "HKCU\Software\Classes\Directory\Background\Shell\CopyPlusPlus" /v "Position" /d "Bottom" /f
reg add "HKCU\Software\Classes\Directory\Background\Shell\CopyPlusPlus\command" /ve /d "\"%APP_EXE%\" --show" /f

REM Folder item menu, e.g. right-click a folder.
reg add "HKCU\Software\Classes\Directory\Shell\CopyPlusPlus" /ve /d "copy++" /f
reg add "HKCU\Software\Classes\Directory\Shell\CopyPlusPlus" /v "Icon" /d "\"%APP_EXE%\"" /f
reg add "HKCU\Software\Classes\Directory\Shell\CopyPlusPlus\command" /ve /d "\"%APP_EXE%\" --show" /f

REM File item menu, e.g. right-click a file.
reg add "HKCU\Software\Classes\*\Shell\CopyPlusPlus" /ve /d "copy++" /f
reg add "HKCU\Software\Classes\*\Shell\CopyPlusPlus" /v "Icon" /d "\"%APP_EXE%\"" /f
reg add "HKCU\Software\Classes\*\Shell\CopyPlusPlus\command" /ve /d "\"%APP_EXE%\" --show" /f

REM Start copy++ automatically at Windows login so global hotkeys work in all apps.
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Run" /v "CopyPlusPlus" /d "\"%APP_EXE%\" --minimized" /f

REM Launch now so Ctrl+C+C and Ctrl+V+V start working immediately.
start "" "%APP_EXE%" --minimized

echo.
echo Installed: copy++
echo Added to desktop background, folder background, folder item, and file item context menus.
echo Global hotkeys work while the app is running. This installer also adds copy++ to Windows startup.
echo.
echo Important: Standard right-click menus inside apps such as Notepad, Word, Chrome, etc. are controlled by those apps.
echo A WPF app cannot inject a universal menu item into every application's private context menu.
echo Use Ctrl+C+C and Ctrl+V+V there instead.
echo.
echo If Explorer does not update immediately, restart Explorer or sign out/in.
pause
