# copy++ Browser Extension for Chrome / Edge

This adds **copy++** to the browser right-click menu.

## What works

- Right-click selected text → **copy++ - Save selected text**
- Right-click an image → **copy++ - Save image URL**
- Right-click inside a browser text field → **copy++ - Paste all saved items**
- **Ctrl+Shift+C** saves selected browser text
- **Ctrl+Shift+V** pastes all saved browser items

## Install in Chrome or Edge

1. Open `chrome://extensions` or `edge://extensions`.
2. Enable **Developer mode**.
3. Click **Load unpacked**.
4. Select this folder: `BrowserExtensionChromeEdge`.

## Browser limitation

Chrome and Edge do not allow extensions to replace the built-in Copy/Paste menu items. The extension can add its own **copy++** items to the browser right-click menu.

The browser extension uses browser storage. The WPF app's Windows-wide hotkeys still work separately across desktop apps.
