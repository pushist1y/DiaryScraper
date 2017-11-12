//import { setTimeout } from 'timers';

'use strict';
const electron = require('electron');
const dialog = electron.remote.dialog;
const currentWindow = electron.remote.getCurrentWindow()

var app = angular.module('ContactsApp', ['720kb.datepicker']);

document.addEventListener('DOMContentLoaded', function () {
    angular.bootstrap(document, ['ContactsApp']);
});

app.controller('ScrapeCtrl', function ($scope, $timeout, $interval, ScrapeService) {
    var ctrl = this;
    ctrl.Title = 'Выгрузка дневника';

    $scope.inputEnabled = true;
    $scope.workingDir = "";
    $scope.dateStart = "2017-02-01";
    $scope.dateEnd = "2017-02-28";
    $scope.diaryName = "";
    $scope.dateStartEnabled = false;
    $scope.dateEndEnabled = false;
    $scope.overwrite = false;
    $scope.diaryNamePattern = /^[\w-]*$/;
    $scope.diaryLogin = "";
    $scope.diaryPassword = "";
    $scope.inProgress = "notstarted";
    $scope.progressRefresh = undefined;
    $scope.taskError = "";
    $scope.btnCancelEnabled = true;
    $scope.diaryRequestDelay = 1000;

    $scope.btnSelectDirectoryClick = () => {
        dialog.showOpenDialog(currentWindow, {
            properties: ['openDirectory']
        }, (filePaths) => {
            $scope.workingDir = filePaths[0];
            $scope.$apply();
        });
    };

    $scope.btnStartClick = () => {
        $scope.currentTask = undefined;
        $scope.taskError = undefined;
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
                refreshProgress();

            }, 333);
        })
    };

    $scope.btnCancelClick = () => {
        $scope.btnCancelEnabled = false;
        let promise = cancelScrape();
        if (promise) {
            promise.then(() => {
                $scope.btnCancelEnabled = true;
            });
        } else {
            $scope.btnCancelEnabled = true;
        }
    };

    function stopRefresh() {
        if (angular.isDefined($scope.progressRefresh)) {
            $interval.cancel($scope.progressRefresh);
            $scope.progressRefresh = undefined;
        }
    }

    $scope.btnRestartClick = () => {
        $scope.inProgress = "notstarted";
        $scope.currentTask = undefined;
    };

    function cancelScrape() {
        if (!$scope.currentTask) {
            return false;
        }
        stopRefresh();
        return ScrapeService.Cancel($scope.currentTask.guidString)
            .then((data) => {
                $scope.currentTask = data;
                $scope.inProgress = "finished";
            });


    }

    function startScrape() {
        let taskDescriptor = {
            workingDir: $scope.workingDir,
            diaryUrl: "http://" + $scope.diaryName + ".diary.ru",
            overwrite: $scope.overwrite,
            requestDelay: $scope.diaryRequestDelay
        };
        if ($scope.dateStartEnabled) {
            taskDescriptor.scrapeStart = new Date($scope.dateStart);
        }
        if ($scope.dateEndEnabled) {
            taskDescriptor.scrapeEnd = new Date($scope.dateEnd);
        }
        return ScrapeService.Post(taskDescriptor, $scope.diaryLogin, $scope.diaryPassword)
            .then((returnedData) => {
                $scope.currentTask = returnedData;
            });
    }

    ctrl.isRefreshing = false;

    function refreshProgress() {
        if (ctrl.isRefreshing) {
            return;
        }
        if (!$scope.currentTask) {
            return;
        }
        ctrl.isRefreshing = true;
        ScrapeService.Get($scope.currentTask.guidString).then((data) => {
            $scope.currentTask = data;
            ctrl.isRefreshing = false;
            if ($scope.currentTask.error) {
                onError($scope.currentTask.error);
            }
            if ($scope.currentTask.status && $scope.currentTask.status >= 5) {
                stopRefresh();
                $scope.inProgress = "finished";
            }

            let percentProgress = 0;
            if ($scope.currentTask.progress.datePagesDiscovered > 0) {
                percentProgress = Math.round(100.0 * $scope.currentTask.progress.datePagesProcessed / $scope.currentTask.progress.datePagesDiscovered);
            }
            $("#scrapeProgress").width(percentProgress.toString() + "%");
            $("#scrapeProgress").text(percentProgress.toString() + "%");
        });
    }

    function onError(errorString) {
        stopRefresh();
        $scope.inProgress = "finished";
        $scope.taskError = errorString;
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

    svc.Cancel = function (id) {
        return $http.delete(apiUrl + '/scrape/' + id)
            .then(function success(response) {
                return response.data;
            });
    }
});