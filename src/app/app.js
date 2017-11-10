//import { setTimeout } from 'timers';

'use strict';
const electron = require('electron');
const dialog = electron.remote.dialog;
const currentWindow = electron.remote.getCurrentWindow()

var app = angular.module('ContactsApp', ['720kb.datepicker']);

document.addEventListener('DOMContentLoaded', function () {
    angular.bootstrap(document, ['ContactsApp']);
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

app.controller('ScrapeCtrl', function ($scope, $timeout, $interval) {
    var ctrl = this;
    ctrl.Title = 'Выгрузка дневника';

    $scope.inputEnabled = true;
    $scope.workingDir = "";
    $scope.dateStart = "2000-01-01";
    $scope.dateEnd = "2020-01-01";
    $scope.diaryName = "";
    $scope.dateStartEnabled = false;
    $scope.dateEndEnabled = false;
    $scope.overwrite = false;
    $scope.diaryNamePattern = /^[\w-]*$/;

    $scope.btnSelectDirectoryClick = () => {
        dialog.showOpenDialog(currentWindow, {
            properties: ['openDirectory']
        }, (filePaths) => {
            $scope.workingDir = filePaths[0];
            $scope.$apply();
        });
    };

    $scope.btnStartClick = () => {
        $scope.inputEnabled = false;
        if ($scope.mainForm.$invalid) {
            angular.forEach($scope.mainForm.$error, function (field) {
                angular.forEach(field, function (errorField) {
                    errorField.$setTouched();
                });
            });
        }

        let progress = 0;
        $interval(() => {
            progress += 1;
            $("#scrapeProgress").width(progress.toString() + "%");
            $("#scrapeProgress").text(progress.toString() + "%");
        }, 100, 100);
        
        $timeout(() => {
            $scope.inputEnabled = true;
        }, 3000);
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