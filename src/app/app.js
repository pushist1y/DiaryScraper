'use strict';
const electron = require('electron');
const dialog = electron.remote.dialog;
const currentWindow = electron.remote.getCurrentWindow()

var app = angular.module('ContactsApp', ['720kb.datepicker']);

document.addEventListener('DOMContentLoaded', function () {
    angular.bootstrap(document, ['ContactsApp']);
});

document.getElementById('btnSelectDirectory').addEventListener('click', _ => {
    //alert("test");




});

app.controller('ContactsCtrl', function (ContactsService) {
    var ctrl = this;
    ctrl.Title = 'Contacts List';

    LoadContacts();

    function LoadContacts() {
        ContactsService.Get()
            .then(function (contacts) {
                ctrl.Contacts = contacts
            }, function (error) {
                ctrl.ErrorMessage = error
            });
    }
});

app.controller('ScrapeCtrl', function ($scope) {
    var ctrl = this;
    ctrl.Title = 'Scrape controller';

    $scope.workingDir = "qq";
    $scope.dateStart = "2000-01-01";

    $scope.btnSelectDirectoryClick = () => {
        dialog.showOpenDialog(currentWindow, {
            properties: ['openDirectory']
        }, (filePaths) => {
            $scope.workingDir = filePaths[0];
            $scope.$apply();
        });
    };
});

app.service('ContactsService', function ($http) {
    var svc = this;
    var apiUrl = 'http://localhost:5000/api';

    svc.Get = function () {
        return $http.get(apiUrl + '/test')
            .then(function success(response) {
                return response.data;
            });
    }
});