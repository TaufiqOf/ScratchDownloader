const box = document.getElementById("capture");
browser.storage.local.get("captureAll").then(({ captureAll = true }) => (box.checked = captureAll));
box.addEventListener("change", () => browser.storage.local.set({ captureAll: box.checked }));
fetch("http://127.0.0.1:47653/ping")
  .then((r) => r.ok)
  .catch(() => false)
  .then((ok) => (document.getElementById("status").textContent = ok ? "Connected to ScratchDownloader" : "ScratchDownloader is not running"));
