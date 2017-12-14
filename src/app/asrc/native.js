window.fs = require('fs');
window.os = require('os');
window.electron = require('electron');
window.packageJson = require('../package.json');

var shell = window.electron.shell;

document.addEventListener('click', function (event) {
  event = event || window.event;
  var target = event.target || event.srcElement;

  while (target) {
    if (target instanceof HTMLAnchorElement) {
      if (target.href.startsWith('http') || target.href.startsWith('mailto')) {
          event.preventDefault();
        shell.openExternal(target.href)
      }
      break;
    }

    target = target.parentNode;
  }
}, true)

