const API = "http://127.0.0.1:47653";

async function getEnabled() {
  const { captureAll = true } = await browser.storage.local.get("captureAll");
  return captureAll;
}

async function send(url) {
  const res = await fetch(`${API}/add`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ url }),
  });
  return res.ok;
}

function notifyFailure() {
  browser.notifications.create({
    type: "basic",
    title: "ScratchDownloader",
    message: "Could not reach ScratchDownloader. Is the application running?",
  });
}

// Intercept browser downloads and hand them over to the application.
browser.downloads.onCreated.addListener(async (item) => {
  if (!(await getEnabled())) return;
  const url = item.url;
  if (!/^(https?|ftp):/i.test(url)) return;
  try {
    if (!(await send(url))) return;
  } catch (e) {
    return; // app not running: keep the normal browser download
  }
  try {
    await browser.downloads.cancel(item.id);
    await browser.downloads.erase({ id: item.id });
  } catch (e) {}
});

browser.runtime.onMessage.addListener(async (message) => {
  if (message?.type !== "capture-magnet" ||
      typeof message.url !== "string" ||
      !/^magnet:\?/i.test(message.url)) return;

  try {
    if (!(await send(message.url))) notifyFailure();
  } catch (e) {
    notifyFailure();
  }
});

browser.contextMenus.create({
  id: "sd-link",
  title: "Download link with ScratchDownloader",
  contexts: ["link", "video", "audio", "image"],
});

browser.contextMenus.onClicked.addListener(async (info) => {
  const url = info.linkUrl || info.srcUrl;
  if (!url) return;
  try {
    if (!(await send(url))) notifyFailure();
  } catch (e) {
    notifyFailure();
  }
});
