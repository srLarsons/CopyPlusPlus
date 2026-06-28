# WpfMultiCopyClipboard / copy++

A WPF clipboard manager with Windows shell integration and a browser extension.

## Features

- Press **Ctrl+C+C** to add the current selected clipboard content to the WPF app list.
- Press **Ctrl+V+V** to paste all saved WPF items together.
- Right-click inside the WPF app to paste all items, paste one of the 15 newest copied items, or delete a saved item.
- Supports text, file paths, and image clipboard content.
- Writes images with Bitmap, DIB, PNG, and HTML clipboard formats for better paste behavior in Word, Paint, and other apps.
- Adds a Windows context menu item named **copy++** to:
  - Desktop background right-click menu
  - Folder background right-click menu
  - Folder item right-click menu
  - File item right-click menu
- Adds the app to Windows startup so the global hotkeys work after login.
- Includes a Chrome / Edge browser extension that adds **copy++** to the browser right-click menu.

## Important Windows/browser limitation

The right-click menu inside apps such as Notepad, Word, Chrome, Edge, Visual Studio, etc. is owned by that app, not by Windows Explorer. A normal WPF app can add entries to Windows Explorer shell menus, but it cannot inject **copy++** into every application's private context menu.

For browsers, use the included extension in `BrowserExtensionChromeEdge`.

## Create Setup

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\BuildSetup.ps1
```

The setup file is created at:

```text
dist\CopyPlusPlusSetup.exe
```

Running the setup installs copy++ to:

```text
%LOCALAPPDATA%\Programs\CopyPlusPlus
```

It also registers the desktop, folder, and file context menus, adds copy++ to Windows startup, and starts the app minimized without a success dialog.

## Build WPF app

Open `WpfMultiCopyClipboard.csproj` in Visual Studio and build/publish it, or run:

```bat
dotnet publish -c Release -r win-x64 --self-contained false
```

Output folder example:

```text
bin\Release\net8.0-windows\win-x64\publish
```

## Install the Windows context menu item manually

After publishing, run this file from the same folder as `WpfMultiCopyClipboard.exe`:

- `InstallCopyPlusPlusContextMenu.bat`

Then double-click:

```text
InstallCopyPlusPlusContextMenu.bat
```

This uses `HKCU`, so administrator permission is not required.

If the menu does not appear immediately, restart Explorer or sign out/in.

## Install browser extension in Chrome or Edge

1. Open `chrome://extensions` or `edge://extensions`.
2. Enable **Developer mode**.
3. Click **Load unpacked**.
4. Select the folder:

```text
BrowserExtensionChromeEdge
```

After installing, the browser right-click menu will show:

- **copy++ - Save selected text**
- **copy++ - Save image URL**
- **copy++ - Paste all saved items**
- **copy++ - Paste** with the 15 newest saved browser items, read-only hover previews, and per-item delete
- **copy++ - Clear saved items**

Browser shortcut keys:

```text
Ctrl+Shift+C = save selected browser text
Ctrl+Shift+V = paste all saved browser items
```

Chrome and Edge do not allow extensions to replace the built-in Copy/Paste menu items. The extension adds its own **copy++** items.

## Uninstall Windows shell menu

Run:

```text
UninstallCopyPlusPlusContextMenu.bat
```
