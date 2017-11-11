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

app.controller('ScrapeCtrl', function ($scope, $timeout, $interval, ScrapeService) {
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
    $scope.diaryLogin = "";
    $scope.diaryPassword = "";
    $scope.inProgress = "notstarted";
    $scope.progressRefresh = undefined;

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
        $timeout(() => {
            $scope.inputEnabled = true;
        }, 300);
        if ($scope.mainForm.$invalid) {
            angular.forEach($scope.mainForm.$error, function (field) {
                angular.forEach(field, function (errorField) {
                    errorField.$setTouched();
                });
            });
        }
        if (!$scope.mainForm.$valid) {
            return;
        }

        startScrape().then(() => {
            $scope.inProgress = "started";
            let progress = 0;
            $scope.progressRefresh = $interval(() => {
                progress += 1;
                $("#scrapeProgress").width(progress.toString() + "%");
                $("#scrapeProgress").text(progress.toString() + "%");
            }, 100, 100);
        })

        


    };

    $scope.btnCancelClick = () => {
        if (angular.isDefined($scope.progressRefresh)) {
            $interval.cancel($scope.progressRefresh);
            $scope.progressRefresh = undefined;
        }

        $scope.inProgress = "finished";
    };

    $scope.btnRestartClick = () => {
        $scope.inProgress = "notstarted";
    };

    function startScrape() {
        let taskDescriptor = {
            workingDir: $scope.workingDir,
            diaryUrl: "http://" + $scope.diaryName + ".diary.ru",
            overwrite: $scope.overwrite
        };
        if ($scope.dateStartEnabled) {
            taskDescriptor.scrapeStart = new Date($scope.dateStart);
        }
        if ($scope.dateEndEnabled) {
            taskDescriptor.scrapeStart = new Date($scope.dateEnd);
        }
        return ScrapeService.Post(taskDescriptor, $scope.diaryLogin, $scope.diaryPassword)
            .then((returnedData) => {
                $scope.currentTask = returnedData;
            });
    }
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

app.service('ScrapeService', function ($http) {
    var svc = this;
    var apiUrl = 'http://localhost:5000/api';

    svc.Get = function (id) {
        return $http.get(apiUrl + '/scrape/' + id)
            .then(function success(response) {
                return response.data;
            });
    }

    svc.Post = function (data, login, password) {
        var urlParams = $.param({
            login: login,
            password: password
        });
        return $http.post(apiUrl + '/scrape?' + urlParams, JSON.stringify(data))
            .then(function success(response) {
                return response.data;
            });
    }
});