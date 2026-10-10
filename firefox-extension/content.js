document.addEventListener("click", (event) => {
  const anchor = event.target instanceof Element ? event.target.closest("a[href]") : null;
  if (!anchor || !/^magnet:\?/i.test(anchor.href)) return;

  event.preventDefault();
  event.stopImmediatePropagation();
  browser.runtime.sendMessage({ type: "capture-magnet", url: anchor.href })
    .catch((error) => console.error("ScratchDownloader could not capture magnet link:", error));
}, true);
