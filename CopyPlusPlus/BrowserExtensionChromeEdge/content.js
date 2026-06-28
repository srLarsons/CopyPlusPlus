function insertTextAtCursor(text) {
  const active = document.activeElement;

  if (!active) {
    return false;
  }

  if (active instanceof HTMLInputElement || active instanceof HTMLTextAreaElement) {
    const start = active.selectionStart ?? active.value.length;
    const end = active.selectionEnd ?? active.value.length;
    active.value = active.value.slice(0, start) + text + active.value.slice(end);
    active.selectionStart = active.selectionEnd = start + text.length;
    active.dispatchEvent(new Event("input", { bubbles: true }));
    return true;
  }

  if (active.isContentEditable) {
    document.execCommand("insertText", false, text);
    return true;
  }

  return false;
}

chrome.runtime.onMessage.addListener((message) => {
  if (message.action === "copypp-paste-text") {
    const pasted = insertTextAtCursor(message.text || "");
    if (!pasted) {
      navigator.clipboard.writeText(message.text || "");
      alert("copy++ saved items were copied to the browser clipboard. Click in a text box and press Ctrl+V.");
    }
  }
});
