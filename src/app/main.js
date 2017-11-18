"use strict";
/// <reference path="node_modules/electron/electron.d.ts" />
exports.__esModule = true;
var electron_1 = require("electron");
var path = require("path");
var url = require("url");
var os = require("os");
var child_process_1 = require("child_process");
var mainWindow;
var apiProcess = null;
function createWindow() {
    mainWindow = new electron_1.BrowserWindow({
        width: 800,
        height: 600,
        icon: path.join(__dirname, '../../assets/icons/png/64x64.png')
    });
    mainWindow.webContents.openDevTools();
    mainWindow.loadURL(url.format({
        pathname: path.join(__dirname, 'adist/index.html'),
        protocol: 'file:',
        slashes: true
    }));
    mainWindow.on('closed', function () {
        // Dereference the window object, usually you would store windows
        // in an array if your app supports multi windows, this is the time
        // when you should delete the corresponding element.
        mainWindow = null;
    });
}
electron_1.app.on('ready', startApi);
electron_1.app.on('window-all-closed', function () {
    // On OS X it is common for applications and their menu bar
    // to stay active until the user quits explicitly with Cmd + Q
    if (process.platform !== 'darwin') {
        electron_1.app.quit();
    }
});
electron_1.app.on('activate', function () {
    // On OS X it's common to re-create a window in the app when the
    // dock icon is clicked and there are no other windows open.
    if (mainWindow === null) {
        createWindow();
    }
});
function startApi() {
    //  run server
    var apipath = path.join(__dirname, '..\\api\\DiaryScraperCore\\bin\\dist\\win\\DiaryScraperCore.exe');
    if (os.platform() === 'darwin') {
        apipath = path.join(__dirname, '..//api//DiaryScraperCore//bin//dist//osx//DiaryScraperCore');
    }
    if (os.platform() === 'linux') {
        apipath = path.join(__dirname, '..//api//DiaryScraperCore//bin//dist//linux//DiaryScraperCore');
    }
    apiProcess = child_process_1.spawn(apipath);
    apiProcess.stdout.on('data', function (data) {
        writeLog("stdout: " + data);
        if (mainWindow == null) {
            createWindow();
        }
    });
}
electron_1.app.once("window-all-closed", electron_1.app.quit);
electron_1.app.once("before-quit", function () {
    writeLog('exit');
    apiProcess.kill();
});
function writeLog(msg) {
    console.log(msg);
}
