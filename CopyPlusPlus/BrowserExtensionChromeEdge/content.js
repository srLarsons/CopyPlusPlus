let lastEditable = null;
let lastSelection = null;

function findEditable(target) {
  if (!(target instanceof Element)) return null;

  const editable = target.closest("textarea,input,[contenteditable]");
  if (!editable) return null;

  if (editable instanceof HTMLInputElement) {
    const textTypes = new Set(["", "email", "password", "search", "tel", "text", "url"]);
    return !editable.disabled && !editable.readOnly && textTypes.has(editable.type) ? editable : null;
  }

  if (editable instanceof HTMLTextAreaElement) {
    return !editable.disabled && !editable.readOnly ? editable : null;
  }

  return editable.isContentEditable ? editable : null;
}

function rememberEditable(event) {
  lastEditable = findEditable(event.target);
  lastSelection = null;

  if (lastEditable instanceof HTMLInputElement || lastEditable instanceof HTMLTextAreaElement) {
    lastSelection = {
      start: lastEditable.selectionStart ?? lastEditable.value.length,
      end: lastEditable.selectionEnd ?? lastEditable.value.length
    };
    return;
  }

  if (lastEditable?.isContentEditable) {
    const selection = window.getSelection();
    if (selection && selection.rangeCount > 0) {
      lastSelection = selection.getRangeAt(0).cloneRange();
    }
  }
}

function setNativeValue(element, value) {
  const descriptor = Object.getOwnPropertyDescriptor(Object.getPrototypeOf(element), "value");
  if (descriptor?.set) {
    descriptor.set.call(element, value);
  } else {
    element.value = value;
  }
}

function insertIntoTextControl(element, text) {
  element.focus({ preventScroll: true });

  const value = element.value;
  const start = lastSelection?.start ?? element.selectionStart ?? value.length;
  const end = lastSelection?.end ?? element.selectionEnd ?? value.length;
  const nextValue = value.slice(0, start) + text + value.slice(end);

  setNativeValue(element, nextValue);
  element.selectionStart = element.selectionEnd = start + text.length;
  element.dispatchEvent(new InputEvent("input", {
    bubbles: true,
    data: text,
    inputType: "insertText"
  }));

  return true;
}

function insertIntoContentEditable(element, text) {
  element.focus({ preventScroll: true });

  if (lastSelection instanceof Range) {
    const selection = window.getSelection();
    selection?.removeAllRanges();
    selection?.addRange(lastSelection);
  }

  const inserted = document.execCommand("insertText", false, text);
  if (inserted) {
    element.dispatchEvent(new InputEvent("input", {
      bubbles: true,
      data: text,
      inputType: "insertText"
    }));
  }

  return inserted;
}

function insertTextAtCursor(text) {
  const target = findEditable(document.activeElement) || lastEditable;
  if (!target) return false;

  if (target instanceof HTMLInputElement || target instanceof HTMLTextAreaElement) {
    return insertIntoTextControl(target, text);
  }

  if (target.isContentEditable) {
    return insertIntoContentEditable(target, text);
  }

  return false;
}

document.addEventListener("contextmenu", rememberEditable, true);

chrome.runtime.onMessage.addListener((message) => {
  if (message.action !== "copypp-paste-text") return;

  const text = message.text || "";
  if (!insertTextAtCursor(text)) {
    navigator.clipboard?.writeText(text).catch(() => {});
  }
});
