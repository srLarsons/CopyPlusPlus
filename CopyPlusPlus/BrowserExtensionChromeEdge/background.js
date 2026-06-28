const KEY = "copyPlusPlusItems";
const MAX_SAVED_ITEMS = 100;
const MAX_PASTE_MENU_ITEMS = 15;
const PASTE_MENU_ID = "copypp-paste";
const DELETE_MENU_ID = "copypp-delete";
const PASTE_ITEM_PREFIX = "copypp-paste-item-";
const DELETE_ITEM_PREFIX = "copypp-delete-item-";
const EMPTY_PASTE_ITEM_ID = "copypp-paste-empty";
const EMPTY_DELETE_ITEM_ID = "copypp-delete-empty";
const PASTE_CONTEXTS = ["editable", "page", "selection"];
let menuInitialization = Promise.resolve();
let areBaseMenusReady = false;

chrome.runtime.onInstalled.addListener(() => {
  initializeMenus();
});

chrome.runtime.onStartup.addListener(() => {
  initializeMenus();
});

function createMenu(properties) {
  return new Promise((resolve) => {
    chrome.contextMenus.create(properties, () => {
      if (chrome.runtime.lastError) {
        console.warn(chrome.runtime.lastError.message);
      }

      resolve();
    });
  });
}

function removeMenu(id) {
  return new Promise((resolve) => {
    chrome.contextMenus.remove(id, () => {
      resolve();
    });
  });
}

function removeAllMenus() {
  return new Promise((resolve) => {
    chrome.contextMenus.removeAll(() => {
      resolve();
    });
  });
}

function initializeMenus() {
  menuInitialization = menuInitialization
    .then(rebuildMenus)
    .catch((error) => console.warn(error));

  return menuInitialization;
}

async function rebuildMenus() {
  areBaseMenusReady = false;
  await removeAllMenus();

  await createMenu({
    id: "copypp-save-selection",
    title: "copy++ - Save selected text",
    contexts: ["selection"]
  });

  await createMenu({
    id: "copypp-save-image",
    title: "copy++ - Save image URL",
    contexts: ["image"]
  });

  await createMenu({
    id: "copypp-paste-all",
    title: "copy++ - Paste all saved items",
    contexts: PASTE_CONTEXTS
  });

  await createMenu({
    id: PASTE_MENU_ID,
    title: "copy++ - Paste",
    contexts: PASTE_CONTEXTS
  });

  await createMenu({
    id: DELETE_MENU_ID,
    title: "copy++ - Delete Item",
    contexts: ["page", "selection", "editable", "image"]
  });

  await createMenu({
    id: "copypp-clear",
    title: "copy++ - Clear saved items",
    contexts: ["page", "selection", "editable", "image"]
  });

  areBaseMenusReady = true;
  await refreshPasteMenuItems();
}

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
  while (items.length > MAX_SAVED_ITEMS) {
    items.shift();
  }

  await setItems(items);
  if (areBaseMenusReady) {
    await refreshPasteMenuItems();
  }
}

function combineItems(items) {
  return items.map((item, index) => {
    if (item.type === "text") return item.value;
    if (item.type === "imageUrl") return `[Image ${index + 1}] ${item.value}`;
    return item.value || "";
  }).filter(Boolean).join("\n\n");
}

function getRecentPasteItems(items) {
  return items.slice(-MAX_PASTE_MENU_ITEMS).reverse();
}

function itemToText(item, index) {
  if (item.type === "text") return item.value || "";
  if (item.type === "imageUrl") return item.value ? `[Image ${index + 1}] ${item.value}` : "";
  return item.value || "";
}

function collapseWhitespace(value) {
  return (value || "").replace(/\s+/g, " ").trim();
}

function truncate(value, maxLength) {
  if (value.length <= maxLength) return value;
  return `${value.slice(0, maxLength - 3)}...`;
}

function getMenuItemTitle(item, menuIndex) {
  const label = item.type === "imageUrl" ? "Image URL" : "Text";
  const value = collapseWhitespace(item.value) || "(empty)";
  return `${menuIndex + 1}. ${label}: ${truncate(value, 48)}`;
}

async function refreshPasteMenuItems() {
  for (let index = 0; index < MAX_PASTE_MENU_ITEMS; index++) {
    await removeMenu(`${PASTE_ITEM_PREFIX}${index}`);
    await removeMenu(`${DELETE_ITEM_PREFIX}${index}`);
  }

  await removeMenu(EMPTY_PASTE_ITEM_ID);
  await removeMenu(EMPTY_DELETE_ITEM_ID);

  const items = getRecentPasteItems(await getItems());

  if (items.length === 0) {
    await createMenu({
      id: EMPTY_PASTE_ITEM_ID,
      parentId: PASTE_MENU_ID,
      title: "No saved items yet",
      contexts: PASTE_CONTEXTS,
      enabled: false
    });
    await createMenu({
      id: EMPTY_DELETE_ITEM_ID,
      parentId: DELETE_MENU_ID,
      title: "No saved items yet",
      contexts: ["page", "selection", "editable", "image"],
      enabled: false
    });
    return;
  }

  for (let index = 0; index < items.length; index++) {
    const item = items[index];

    await createMenu({
      id: `${PASTE_ITEM_PREFIX}${index}`,
      parentId: PASTE_MENU_ID,
      title: getMenuItemTitle(item, index),
      contexts: PASTE_CONTEXTS
    });

    await createMenu({
      id: `${DELETE_ITEM_PREFIX}${index}`,
      parentId: DELETE_MENU_ID,
      title: getMenuItemTitle(item, index),
      contexts: ["page", "selection", "editable", "image"]
    });
  }
}

async function sendPasteMessage(tabId, frameId, text) {
  const options = Number.isInteger(frameId) ? { frameId } : undefined;
  await chrome.tabs.sendMessage(tabId, { action: "copypp-paste-text", text }, options);
}

async function pasteAllToTab(tabId, frameId) {
  const items = await getItems();
  const combinedText = combineItems(items);
  await sendPasteMessage(tabId, frameId, combinedText);
}

async function pasteItemToTab(tabId, frameId, menuIndex) {
  const items = getRecentPasteItems(await getItems());
  const item = items[menuIndex];

  if (!item) return;

  await sendPasteMessage(tabId, frameId, itemToText(item, menuIndex));
}

async function deleteItem(menuIndex) {
  const items = await getItems();
  const itemIndex = items.length - 1 - menuIndex;

  if (itemIndex < 0 || itemIndex >= items.length) return;

  items.splice(itemIndex, 1);
  await setItems(items);

  if (areBaseMenusReady) {
    await refreshPasteMenuItems();
  }
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
    await pasteAllToTab(tab.id, info.frameId);
  }

  if (typeof info.menuItemId === "string" && info.menuItemId.startsWith(PASTE_ITEM_PREFIX)) {
    const menuIndex = Number(info.menuItemId.slice(PASTE_ITEM_PREFIX.length));
    await pasteItemToTab(tab.id, info.frameId, menuIndex);
  }

  if (typeof info.menuItemId === "string" && info.menuItemId.startsWith(DELETE_ITEM_PREFIX)) {
    const menuIndex = Number(info.menuItemId.slice(DELETE_ITEM_PREFIX.length));
    await deleteItem(menuIndex);
  }

  if (info.menuItemId === "copypp-clear") {
    await setItems([]);
    if (areBaseMenusReady) {
      await refreshPasteMenuItems();
    }
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

initializeMenus();
