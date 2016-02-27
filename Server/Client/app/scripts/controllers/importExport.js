'use strict';
/**
 * @ngdoc function
 * @name sbAdminApp.controller:importExport
 * @description
 * # MainCtrl
 * Controller of the sbAdminApp
 */

angular.module('sbAdminApp').controller('importExport', function($scope,$http,$stateParams) {

	$http.get("http://handsfreeleveler.com:4446/api/getAI").then(function(res){
		$scope.exportedSettings = JSON.stringify(res.data);
	});

	$scope.exportedSettings = "";
	$scope.copyStatus = "Copy clicking this button";




	$scope.copied = function(){
		$scope.copyStatus = "Copied successfully!"	
	}
	$scope.copyfail = function(err){
		$scope.copyStatus = err;
	}

	$scope.import = function(json){
		$http.post("http://handsfreeleveler.com:4446/api/importAI", {ai:json});
	}
});