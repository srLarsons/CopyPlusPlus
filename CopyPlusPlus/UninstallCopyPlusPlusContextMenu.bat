@echo off
reg delete "HKCU\Software\Classes\DesktopBackground\Shell\CopyPlusPlus" /f
reg delete "HKCU\Software\Classes\Directory\Background\Shell\CopyPlusPlus" /f
reg delete "HKCU\Software\Classes\Directory\Shell\CopyPlusPlus" /f
reg delete "HKCU\Software\Classes\*\Shell\CopyPlusPlus" /f
reg delete "HKCU\Software\Microsoft\Windows\CurrentVersion\Run" /v "CopyPlusPlus" /f
echo Removed copy++ context menu entries and startup entry.
pause
