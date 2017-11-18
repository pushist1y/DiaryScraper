/// <reference path="node_modules/electron/electron.d.ts" />

import { app, BrowserWindow, screen } from 'electron';
import * as path from 'path';
import * as url from 'url';
import * as os from 'os';
import { spawn, ChildProcess } from 'child_process';

let mainWindow: BrowserWindow;
let apiProcess: ChildProcess = null;

function createWindow() {
    mainWindow = new BrowserWindow({
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
        mainWindow = null
    });


}

app.on('ready', startApi);

app.on('window-all-closed', function () {
    // On OS X it is common for applications and their menu bar
    // to stay active until the user quits explicitly with Cmd + Q
    if (process.platform !== 'darwin') {
        app.quit()
    }
});

app.on('activate', function () {
    // On OS X it's common to re-create a window in the app when the
    // dock icon is clicked and there are no other windows open.
    if (mainWindow === null) {
        createWindow()
    }
});

function startApi() {
    //  run server
    let apipath = path.join(__dirname, '..\\api\\DiaryScraperCore\\bin\\dist\\win\\DiaryScraperCore.exe')
    if (os.platform() === 'darwin') {
        apipath = path.join(__dirname, '..//api//DiaryScraperCore//bin//dist//osx//DiaryScraperCore')
    }
    if (os.platform() === 'linux') {
        apipath = path.join(__dirname, '..//api//DiaryScraperCore//bin//dist//linux//DiaryScraperCore')
    }
    apiProcess = spawn(apipath)

    apiProcess.stdout.on('data', (data) => {
        writeLog(`stdout: ${data}`);
        if (mainWindow == null) {
            createWindow();
        }
    });
}

app.once("window-all-closed", app.quit);
app.once("before-quit", () => {
    writeLog('exit');
    apiProcess.kill();
});


function writeLog(msg) {
    console.log(msg);
}