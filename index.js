const { app, BrowserWindow, ipcMain, Menu, shell } = require('electron');
const { download } = require('electron-dl');
const { autoUpdater } = require('electron-updater');
const path = require('path');
const fs = require('fs');
const cp = require('child_process');

let mainWindow;

function createWindow() {
    mainWindow = new BrowserWindow({
        width: 1100,
        height: 800,
        webPreferences: {
            nodeIntegration: true,
        }
    });
    mainWindow.loadFile('index.html');
    // Menu.setApplicationMenu(null);


    ipcMain.on("download_app", (event, info) => {
        info.properties.onProgress = status => mainWindow.webContents.send("download progress", status, info.app);

        var url = "";
        if (process.platform == "win32") {
            url = info.app.download_win;
        } else if (process.platform == "darwin") {
            url = info.app.download_mac;
        } else if (process.platform == "linux") {
            url = info.app.download_linux;
        }

        download(BrowserWindow.getFocusedWindow(), url, info.properties)
            .then(dl => mainWindow.webContents.send("download complete", dl.getSavePath(), info.app));
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

ipcMain.on('open_exe', (event, path) => {
    if (process.platform == "win32") {
        shell.openExternal('file://' + path);
    } else {
        fs.chmodSync(path, '755');
        cp.spawn(path);
    }
    // child(path, function (err, data) {
    //     if (err) {
    //         console.error(err);
    //         return;
    //     }

    //     console.log(data.toString());
    // });
});

ipcMain.on('write_to_file', (event, path, text) => {
    fs.writeFile(path, text, () => { mainWindow.webContents.send("refresh_apps"); });
});

ipcMain.on('delete_app', (event, app_json) => {
    var folder = path.join(app.getPath("userData"), 'Installs', app_json.name);
    fs.rmdir(folder, { recursive: true }, () => { mainWindow.webContents.send("refresh_apps"); });
});

ipcMain.on('get_config', (event) => {
    var filePath = path.join(app.getPath("userData"), "config.json");
    var ret = {};
    if (checkFileExistsSync(filePath)) {
        var file = fs.readFileSync(filePath);
        ret = JSON.parse(file);
    }
    else {
        ret = { access_code: "" };
    }
    event.sender.send('get_config', ret);
});

ipcMain.on('set_config', (event, config) => {
    var filePath = path.join(app.getPath("userData"), "config.json");
    fs.writeFile(filePath, JSON.stringify(config), () => { });
});


ipcMain.on('apps_installed', (event, apps_json) => {

    // var promises = [];
    // apps_json.forEach(app => {
    //     s => new Promise(r => fs.access(s, fs.constants.F_OK, e => r(!e)))
    //     let checkFileExists = s => new Promise(r => fs.access(s, fs.constants.F_OK, e => r(!e)))
    //     checkFileExists("Some File Location")
    //         .then(bool => console.log(`file exists: ${ bool }`))
    // });

    apps_json.forEach(app_json => {
        ret = getInstallPath(app_json);
        app_json.installed = ret.installed;
        app_json.exe = ret.exe;
        app_json.installed_version = ret.installed_version;
    });

    event.sender.send('apps_installed', apps_json);
});


function getInstallPath(app_json) {

    var installsFolder = path.join(app.getPath("userData"), 'Installs');
    // var files = fs.readdirSync(installsFolder);
    // files.map(function (fileName) {
    //     return {
    //         name: fileName,
    //         time: fs.statSync(path.join(installsFolder, fileName)).mtime.getTime()
    //     };
    // })
    //     .sort(function (a, b) {
    //         return a.time - b.time;
    //     })
    //     .map(function (v) {
    //         return v.name;
    //     });
    // var folder = "";
    // files.forEach(file => {
    //     var parts = file.split("_v");
    //     if (parts.length == 2 && parts[0] == app_json.folder) {
    //         folder = file;
    //         return;
    //     }
    // });
    // var folder = path.join(installsFolder, folder);
    var folder = path.join(installsFolder, app_json.name);

    var versionFile = path.join(folder, "velappshub_appversion.txt");
    if (checkFileExistsSync(versionFile) && checkFileExistsSync(folder)) {
        var installed_version = fs.readFileSync(versionFile, 'utf8');

        var ext = "";
        if (process.platform == "win32") {
            ext = ".exe";
        } else if (process.platform == "darwin") {
            ext = ".app";
        } else if (process.platform == "linux") {
            ext = ".x86_64"
        } else {
            console.log("Unsupported platform.");
            return { installed: false, exe: "" };
        }

        var files = fs.readdirSync(folder);
        var validFiles = [];
        files.forEach(file => {
            if (path.extname(file) == ext && !file.endsWith("UnityCrashHandler64.exe")) {
                validFiles.push({ installed: true, exe: path.join(folder, file), installed_version: installed_version });
            }
        });
        if (validFiles.length == 1) {
            return validFiles[0];
        } else if (validFiles.length > 1) {
            console.log("Multiple exes detected. Don't know which one to use: " + validFiles);
        }
    }
    return { installed: false, exe: "" };
}

function checkFileExistsSync(filepath) {
    let flag = true;
    try {
        fs.accessSync(filepath, fs.constants.F_OK);
    } catch (e) {
        flag = false;
    }
    return flag;
}

autoUpdater.on('update-available', () => {
    mainWindow.webContents.send('update_available');
});

autoUpdater.on('update-downloaded', () => {
    mainWindow.webContents.send('update_downloaded');
});

ipcMain.on('restart_app', () => {
    autoUpdater.quitAndInstall();
});