const KEY = "copyPlusPlusItems";

chrome.runtime.onInstalled.addListener(() => {
  chrome.contextMenus.create({
    id: "copypp-save-selection",
    title: "copy++ - Save selected text",
    contexts: ["selection"]
  });

  chrome.contextMenus.create({
    id: "copypp-save-image",
    title: "copy++ - Save image URL",
    contexts: ["image"]
  });

  chrome.contextMenus.create({
    id: "copypp-paste-all",
    title: "copy++ - Paste all saved items",
    contexts: ["editable", "page", "selection"]
  });

  chrome.contextMenus.create({
    id: "copypp-clear",
    title: "copy++ - Clear saved items",
    contexts: ["page", "selection", "editable", "image"]
  });
});

async function getItems() {
  const result = await chrome.storage.local.get(KEY);
  return result[KEY] || [];
}

async function setItems(items) {
  await chrome.storage.local.set({ [KEY]: items });
}

async function addItem(item) {
  const items = await getItems();
  items.push({ ...item, savedAt: new Date().toISOString() });
  await setItems(items);
}

function combineItems(items) {
  return items.map((item, index) => {
    if (item.type === "text") return item.value;
    if (item.type === "imageUrl") return `[Image ${index + 1}] ${item.value}`;
    return item.value || "";
  }).filter(Boolean).join("\n\n");
}

async function pasteAllToTab(tabId) {
  const items = await getItems();
  const combinedText = combineItems(items);
  await chrome.tabs.sendMessage(tabId, { action: "copypp-paste-text", text: combinedText });
}

chrome.contextMenus.onClicked.addListener(async (info, tab) => {
  if (!tab?.id) return;

  if (info.menuItemId === "copypp-save-selection") {
    await addItem({ type: "text", value: info.selectionText || "" });
  }

  if (info.menuItemId === "copypp-save-image") {
    await addItem({ type: "imageUrl", value: info.srcUrl || "" });
  }

  if (info.menuItemId === "copypp-paste-all") {
    await pasteAllToTab(tab.id);
  }

  if (info.menuItemId === "copypp-clear") {
    await setItems([]);
  }
});

chrome.commands.onCommand.addListener(async (command, tab) => {
  if (!tab?.id) return;

  if (command === "copy-plus-plus-save") {
    const [result] = await chrome.scripting.executeScript({
      target: { tabId: tab.id },
      func: () => window.getSelection()?.toString() || ""
    });
    if (result?.result) await addItem({ type: "text", value: result.result });
  }

  if (command === "copy-plus-plus-paste") {
    await pasteAllToTab(tab.id);
  }
});
