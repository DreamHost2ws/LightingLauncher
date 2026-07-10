const { app, BrowserWindow } = require("electron");

function createWindow() {
  const win = new BrowserWindow({
    width: 1200,
    height: 700
  });

  win.loadURL("https://google.com");
}

app.whenReady().then(createWindow);
