const { app, BrowserWindow, ipcMain } = require('electron');
const { download } = require('electron-dl');
const { autoUpdater } = require('electron-updater');

let mainWindow;

function createWindow() {
    mainWindow = new BrowserWindow({
        width: 1280,
        height: 800,
        webPreferences: {
            nodeIntegration: true,
        },
        // icon: 'img/vel_logo.ico'
    });
    mainWindow.loadFile('index.html');



    ipcMain.on("download", (event, info) => {
        // info.properties.onProgress = status => window.webContents.send("download progress", status);
        download(BrowserWindow.getFocusedWindow(), info.url, info.properties)
            .then(dl => mainWindow.webContents.send("download complete", dl.getSavePath()));
    });

    mainWindow.on('closed', function () {
        mainWindow = null;
    });
    mainWindow.once('ready-to-show', () => {
        autoUpdater.checkForUpdatesAndNotify();
    });
}

app.on('ready', () => {
    createWindow();
});

app.on('window-all-closed', function () {
    if (process.platform !== 'darwin') {
        app.quit();
    }
});

app.on('activate', function () {
    if (mainWindow === null) {
        createWindow();
    }
});

ipcMain.on('app_version', (event) => {
    event.sender.send('app_version', { version: app.getVersion() });
});

ipcMain.on('user_path', (event) => {
    event.sender.send('user_path', { path: app.getPath("userData") });
});

autoUpdater.on('update-available', () => {
    mainWindow.webContents.send('update_available');
});

autoUpdater.on('update-downloaded', () => {
    mainWindow.webContents.send('update_downloaded');
});

ipcMain.on('restart_app', () => {
    autoUpdater.quitAndInstall();
});