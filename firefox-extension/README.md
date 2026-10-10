# ScratchDownloader Firefox extension

Sends download links to the running ScratchDownloader app, which listens on `127.0.0.1:47653`.

- Browser downloads are intercepted and opened in the app's "New download" dialog (toggle in the toolbar popup).
  If the app is not running, the browser downloads normally.
- Clicking a magnet link is handed directly to ScratchDownloader.
- Right-click a link/media > "Download link with ScratchDownloader".

## Install
Temporary: `about:debugging` > This Firefox > Load Temporary Add-on > select `manifest.json`.
Permanent: `npx web-ext build`, then sign via addons.mozilla.org (unlisted).
