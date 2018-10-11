var automationDashboard = angular.module('AutomationDashboard', [])

automationDashboard.controller('dashboardController', ['$scope', '$http', function ($scope, $http) {

    $scope.isCreate = false;    
    $scope.isList = false;
    $scope.isDelete = false;
    $scope.isDeletePrompt = false;
    $scope.isHome = true;
    $scope.isHomePrompt = true;
    $scope.isDeleting = false;
    $scope.isBuilding = false;
    $scope.isAdvanced = false;

    $scope.showError = false;
    $scope.showSuccess = false;
    $scope.errorMessage = 'Something Went Wrong!';
    $scope.successMessage = 'Something Went Wrong!';

    $scope.Environments = ["Dev", "QA", "Prod"]
    $scope.Locations = ["South India", "West Europe", "Central US"]

    $scope.DevOpsAutomation = {}  

    $scope.Customer = {}

    $scope.baseApiUrl = window.location.protocol + "//" + window.location.host + "/api/Dashboard";

    $scope.Tasks = [
        {
            'TaskId': 1,
            'TabId': 11,
            'TaskName': 'Register',
            'Status': 'Not Started',
            'ManualRunDuration': '30',
            'AutomatedRunDuration': '0',
            'Details': [                
                'Adding Provider Web App in Azure Active Directory',
                'Adding Consumer Web App in Azure Active Directory',
                'Creating Service Principal for Provider in Azure Active Directory',
                'Creating Service Principal for Consumer in Azure Active Directory'
                
            ]
        },
        {
            'TaskId': 2,
            'TabId': 12,
            'TaskName': 'Provision',
            'Status': 'Not Started',
            'ManualRunDuration': '30',
            'AutomatedRunDuration': '0',
            'Details': [
                'Creating Common App Service in Azure',
                'Creating Provider Web App with AppSettings in Azure',
                'Creating Consumer Web App with AppSettings in Azure',
                'Creating Provider Storage in Azure'
            ]
        },
        {
            'TaskId': 3,
            'TabId': 13,
            'TaskName': 'CheckIn',
            'Status': 'Not Started',
            'ManualRunDuration': '30',
            'AutomatedRunDuration': '0',
            'Details': [
                'Check In Provider Configuration to Team Foundation Server',
                'Check In Consumer Configuration to Team Foundation Server'
            ]
        },
        {
            'TaskId': 4,
            'TabId': 14,
            'TaskName': 'Build',
            'Status': 'Not Started',
            'ManualRunDuration': '20',
            'AutomatedRunDuration': '0',
            'Details': [
                'Building Provider',                
                'Creating Artifacts for Provider',
                'Building Consumer',
                'Creating Artifacts for Consumer'
            ]
        },
        {
            'TaskId': 5,
            'TabId': 15,
            'TaskName': 'Test',
            'Status': 'Not Started',
            'ManualRunDuration': '20',
            'AutomatedRunDuration': '0',
            'Details': [
                'Running Unit Tests for Provider'
            ]
        },
        {
            'TaskId': 6,
            'TabId': 16,
            'TaskName': 'Release',
            'Status': 'Not Started',
            'ManualRunDuration': '30',
            'AutomatedRunDuration': '0',
            'Details': [
                'Downloading Artifacts for Provider',
                'Deploying Provider',
                'Downloading Artifacts for Consumer',
                'Deploying Consumer'
            ]
        },
        {
            'TaskId': 7,
            'TabId': 17,
            'TaskName': 'Seed',
            'Status': 'Not Started',
            'ManualRunDuration': '20',
            'AutomatedRunDuration': '0',
            'Details': [
                'Seeding Application User Data to Provider Table Storage',
                'Seeding Application Event Data to Provider Table Storage'
            ]
        },
        {
            'TaskId': 8,
            'TabId': 18,
            'TaskName': 'Mail',
            'Status': 'Not Started',
            'ManualRunDuration': '05',
            'AutomatedRunDuration': '0',
            'Details': [
                'Sending Mail With Details to Your Email Address'
            ]
        }      
    ]

    $scope.Duration = {
        'Manual': '135',
        'Automated': '0'
    }

    //Important Data
    $scope.passCode = '';
    $scope.rowKey = '';
    $scope.orgCode = '';

    $scope.Create = function (obj) {
        $scope.isCreate = false;
        $scope.isList = true;

        $scope.passCode = obj.PassCode;

        // Posting Customer Data and Getting RowKey
        $http.post($scope.baseApiUrl + '/Begin', obj)
            .success(function (data) {

                $scope.rowKey = data;
                $scope.Tasks[0].Status = 'In Progress';

                // Registering Applications in AAD
                $http.get($scope.baseApiUrl + '/Register?passcode=' + $scope.passCode + '&key=' + $scope.rowKey)
                    .success(function (response) {
                        $scope.Tasks[0].AutomatedRunDuration = response;
                        $scope.Tasks[0].Status = 'Completed';
                        $scope.Tasks[1].Status = 'In Progress';

                        // Provisionig Resources in Azure
                        $http.get($scope.baseApiUrl + '/Provision?passcode=' + $scope.passCode + '&key=' + $scope.rowKey).success(function (response) {
                            $scope.Tasks[1].AutomatedRunDuration = response;
                            $scope.Tasks[1].Status = 'Completed';
                            $scope.Tasks[2].Status = 'In Progress';

                            // Check In Files to TFS
                            $http.get($scope.baseApiUrl + '/CheckIn?passcode=' + $scope.passCode + '&key=' + $scope.rowKey).success(function (response) {
                                $scope.Tasks[2].AutomatedRunDuration = response;
                                $scope.Tasks[2].Status = 'Completed';
                                $scope.Tasks[3].Status = 'In Progress';

                            // Building & Running Test Cases in TFS
                            $http.get($scope.baseApiUrl + '/Build?passcode=' + $scope.passCode + '&key=' + $scope.rowKey).success(function (response) {
                                $scope.Tasks[3].AutomatedRunDuration = response;
                                $scope.Tasks[3].Status = 'Completed';
                                $scope.Tasks[4].Status = 'In Progress';

                                // Running Unit Tests
                                $http.get($scope.baseApiUrl + '/Test?passcode=' + $scope.passCode + '&key=' + $scope.rowKey).success(function (response) {
                                    $scope.Tasks[4].AutomatedRunDuration = response.TotalTime;
                                    $scope.Tasks[4].Status = 'Completed';
                                    $scope.Tasks[4].Details.push(response.PassPercentage + "% of the Unit Tests Passed for Provider");
                                    $scope.Tasks[4].Details.push("Code Coverage for Provider is " + response.CodeCoverage + "%");
                                    $scope.Tasks[5].Status = 'In Progress';

                                // Deploying Resources to Azure
                                $http.get($scope.baseApiUrl + '/Release?passcode=' + $scope.passCode + '&key=' + $scope.rowKey).success(function (response) {
                                    $scope.Tasks[5].AutomatedRunDuration = response;
                                    $scope.Tasks[5].Status = 'Completed';
                                    $scope.Tasks[6].Status = 'In Progress';

                                    // Seeding Data to Tables
                                    $http.get($scope.baseApiUrl + '/Seed?passcode=' + $scope.passCode + '&key=' + $scope.rowKey).success(function (response) {
                                        $scope.Tasks[6].AutomatedRunDuration = response;
                                        $scope.Tasks[6].Status = 'Completed';
                                        $scope.Tasks[7].Status = 'In Progress';

                                        // Sending Mail to Customer
                                        $http.get($scope.baseApiUrl + '/Mail?passcode=' + $scope.passCode + '&key=' + $scope.rowKey)
                                            .success(function (response) {
                                                $scope.Tasks[7].AutomatedRunDuration = response;
                                                $scope.Tasks[7].Status = 'Completed';   

                                                //$scope.showSuccess = true;
                                                //$scope.successMessage = "All Done! Have a Good Day!";

                                                //Calculating Total Duration
                                                $scope.Duration.Automated = parseInt((parseInt($scope.Tasks[0].AutomatedRunDuration) +
                                                    parseInt($scope.Tasks[1].AutomatedRunDuration) +
                                                    parseInt($scope.Tasks[2].AutomatedRunDuration) +
                                                    parseInt($scope.Tasks[3].AutomatedRunDuration) +
                                                    parseInt($scope.Tasks[4].AutomatedRunDuration) +
                                                    parseInt($scope.Tasks[5].AutomatedRunDuration) +
                                                    parseInt($scope.Tasks[6].AutomatedRunDuration) +
                                                    parseInt($scope.Tasks[7].AutomatedRunDuration)) / 60);                                    

                                            })
                                            .error(function (err) {
                                                $scope.Tasks[7].Status = 'Cancelled';                                    
                                                $scope.showError = true;
                                                $scope.errorMessage = err.Message;
                                            })
                                    })
                                .error(function (err) {
                                    $scope.Tasks[6].Status = 'Cancelled';
                                    $scope.showError = true;
                                    $scope.errorMessage = err.Message;
                                })
                            })
                            .error(function (err) {
                                $scope.Tasks[5].Status = 'Cancelled';
                                $scope.showError = true;
                                $scope.errorMessage = err.Message;
                            })
                        })
                        .error(function (err) {
                            $scope.Tasks[4].Status = 'Cancelled';
                            $scope.showError = true;
                            $scope.errorMessage = err.Message;
                            })
                        })
                    })
                    .error(function (err) {
                        $scope.Tasks[3].Status = 'Cancelled';
                        $scope.showError = true;
                        $scope.errorMessage = err.Message;
                    })
                })
                .error(function (err) {
                    $scope.Tasks[2].Status = 'Cancelled';
                    $scope.showError = true;
                    $scope.errorMessage = err.Message;
                    })
                })
                .error(function (err) {
                    $scope.Tasks[1].Status = 'Cancelled';
                    $scope.showError = true;
                    $scope.errorMessage = err.Message;
                })
            })
            .error(function (err) {
                $scope.Tasks[0].Status = 'Cancelled';
                $scope.showError = true;
                $scope.errorMessage = err.Message;
            })
    }

    $scope.OpenHome = function () {
        $scope.isCreate = false;
        $scope.isDelete = false;
        $scope.isList = false;
        $scope.isHome = true;
        $scope.isDeletePrompt = false;
        $scope.EnvList = null;
        $scope.showError = false;
        $scope.showSuccess = false;
    }

    $scope.OpenCreate = function () {
        $scope.isCreate = true;
        $scope.isDelete = false;
        $scope.isList = false;
        $scope.isHome = false;
        $scope.isDeletePrompt = false;
        $scope.EnvList = null;
        $scope.showError = false;
        $scope.showSuccess = false;
    }

    $scope.OpenList = function () {
        $scope.isCreate = false;
        $scope.isDelete = false;
        $scope.isList = true;
        $scope.isHome = false;
        $scope.isDeletePrompt = false;
        $scope.EnvList = null;
        $scope.showError = false;
        $scope.showSuccess = false;
    }

    $scope.OpenDelete = function () {
        $scope.isCreate = false;
        $scope.isDelete = true;
        $scope.isList = false;
        $scope.EnvList = null;
        $scope.isHome = false;
        $scope.isDeletePrompt = false;
        $scope.EnvList = null;
        $scope.showError = false;
        $scope.showSuccess = false;
    }

    $scope.Fetch = function (passCode) {
        $scope.passCode = passCode;
        $http.get($scope.baseApiUrl + '/Fetch?passcode=' + passCode)
            .success(function (data) {
                if (data.length > 0) {                
                    $scope.EnvList = [];
                    $scope.EnvList = data;
                    $scope.showError = false;
                    $scope.showSuccess = false;
                }            
            })
            .error(function (err) {
                $scope.showError = true;
                $scope.showSuccess = false;
                $scope.errorMessage = err.Message;
            })
    } 

    $scope.ShowAdvancedOptions = function () {
        $scope.isAdvanced = !$scope.isAdvanced;
    }
    

    $scope.Delete = function (selectedEnv) {        
        var Key = selectedEnv.RowKey
        selectedEnv.isDeleting = true;
        $http.get($scope.baseApiUrl + '/Delete?passcode=' + $scope.passCode + '&key=' + Key)
            .success(function (response) {
                $scope.isDeletePrompt = false;
                $scope.EnvList = [];
                $scope.showError = false;
                $scope.showSuccess = true;
                $scope.successMessage = "Deleted!";
                $scope.isDeleting = false;
            })
            .error(function (err) {
                $scope.showError = true;
                $scope.showSuccess = false;
                $scope.errorMessage = err.Message;
                $scope.isDeleting = false;
            })
    }

    $scope.Build = function (selectedEnv) {
        var Key = selectedEnv.RowKey
        selectedEnv.isBuilding = true;
        $http.get($scope.baseApiUrl + '/Build?passcode=' + $scope.passCode + '&key=' + Key)
            .success(function (response) {  
                $scope.showError = false;
                $scope.showSuccess = true;
                $scope.successMessage = "Build Done!";         
                $http.get($scope.baseApiUrl + '/Release?passcode=' + $scope.passCode + '&key=' + Key)
                    .success(function (response) {                        
                        $scope.showError = false;
                        $scope.showSuccess = true;
                        $scope.successMessage = "Release Done!";
                        $scope.isBuilding = false;
                    })
                    .error(function (err) {
                        $scope.showError = true;
                        $scope.showSuccess = false;
                        $scope.errorMessage = err.Message;
                        $scope.isBuilding = false;
                    })
            })
            .error(function (err) {
                $scope.showError = true;
                $scope.showSuccess = false;
                $scope.errorMessage = err.Message;
                $scope.isBuilding = false;
            })
    }
}])
