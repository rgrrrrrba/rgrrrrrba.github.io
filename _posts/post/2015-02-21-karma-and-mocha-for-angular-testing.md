---
title: Karma and Mocha for AngularJS testing
layout: post
date: 2015-02-21
category: archived
---

## Setting up Karma with Mocha, PhantomJS and Chai

I'm following the [installation guide](https://karma-runner.github.io/0.12/intro/installation.html) at [karma-runner.github.io](https://karma-runner.github.io).

I already have Node.js and NPM installed so I won't go through the process for that, but I've found the easiest way to get running is via [Chocolatey NuGet](https://chocolatey.org/packages/nodejs.install) using `choco install nodejs.install`.

First up, install Karma:

	npm install --save-dev karma

Now install some plugins for Karma. The installation instructions are for Jasmine and the Chrome launcher, but I want [Mocha](https://mochajs.org/) as the testing framework and [PhantomJS](https://phantomjs.org/) for a headless (window-less) test environment.

> *Huh? I thought Karma was the test environment*

Karma is a test *runner*. Much like how NUnit has a test runner .exe which can run test assemblies that use the NUnit test framework assemblies. In this case the test runner can run tests based on different test frameworks - in this case. Mocha. Because I'm setting up tests for client-side JavaScript - in particular, I'm going to use AngularJS - the tests need to be run inside a browser environment so that there is a usable DOM. PhantomJS is a WebKit based headless browser that will allow tests to run without opening a browser window.

So now the Karma plugins for Mocha and PhantomJS need to be installed. The plugins have Mocha and PhantomJS as dependencies, so only the plugins need to be installed. I also want to use [Chai](https://chaijs.com/) as the assertion library.

	npm install --save-dev karma-mocha
	npm install --save-dev karma-phantomjs-launcher
	npm install --save-dev karma-chai

To make it easier to run `karma` from the command line you can install `karma-cli` globally, which will run the local version without having to specify the path to karma (`node node_modules/karma/bin/karma`):

	npm install -g karma-cli

Karma needs a configuration file. Generate it using `karma init` and answer the questions. For this demo all of my code is going to live in `./source-and-tests`. If I were using a Gulp build chain this would probably need to be tweaked.

Which testing framework do you want to use ?
: `mocha`

Do you want to use Require.js ?
: `no`

Do you want to capture any browsers automatically ?
: `PhantomJS`

What is the location of your source and test files ?
: `source-and-tests/**/*.js`

Should any of the files included by the previous patterns be excluded ?
: _leave blank_

Do you want Karma to watch all the files and run the tests on change ?
: `yes`

This generates a file called `karma-conf.js`, which configures Karma for a test run. You can have multiple configuration files pointing to different test suites or browser configurations, which can be run by specifying the name of the configuration file (`karma start my.conf.ks`).

To get Chai included in the test pipeline, we need to edit `karma-conf.js` and add it to the `frameworks` setting:

    frameworks: ['mocha', 'chai'],

 Running `karma start` will execute the default `karma-conf.js` (or `karma-conf.coffee`). First we need a test to run. In `./source-and-tests/` I created `array-tests.js` which just contains the [first example](https://mochajs.org/#synchronous-code) from Mocha's documentation.

	describe('Array', function(){
	  describe('#indexOf()', function(){
	    it('should return -1 when the value is not present', function(){
	      assert.equal(-1, [1,2,3].indexOf(5));
	      assert.equal(-1, [1,2,3].indexOf(0));
	    })
	  })
	})

Running `karma start` should find and run this test, then watch for changes to the watched files and repeating.

![](https://i.imgur.com/cwTVVjT.png)


## Adding AngularJS to the mix

I'll use Bower to install AngularJS.

	npm install --save-dev bower 
	npm install -g bower

This installs Bower to `./node_modules/bower`, then installs it globally. Now we need to create a configuration file for Bower:

	bower init

You can just <kbd>enter</kbd> through the configuration, accepting all the defaults. This creates a `bower.json` file, which will save the dependencies added by Bower. Now use Bower to install AngularJS and angular-mocks:

	bower install --save angular
	bower install --save angular-mocks

This installs AngularJS to `./bower_components/angular` and angular-mocks to `./bower_components/angular-mocks`. The angular-mocks package gives us methods to resolve our application's components and create mocks of AngularJS services.

I'm not going through how to integrate AngularJS an actual website as there are a number of techniques ranging from ASP.NET MVC bundling and minification to more advanced build chains such as Gulp or Grunt. Instead I'll just show how to include AngularJS in the test suite, create a simple controller, and write a test against a property exposed by the controller.

To include AngularJS and angular-mocks in Karma's test run, edit the `files` config setting in `karma.conf.js`. Any future dependencies for the codebase and tests will need to be added here too, unless they are imported in some other way.

    files: [
		'bower_components/angular/angular.js',
    	'bower_components/angular-mocks/angular-mocks.js',
		'source-and-tests/**/*.js'
    ],

The controller to test is very simple at this stage (`MyController.js`):

	(function(){
		angular.module('my-module', []);

		angular
			.module('my-module')
			.controller('MyController', [
				function(){
					var self = this;

					self.firstName = '';
					self.lastName = '';

					self.getFullName = function(){
						return self.firstName + ' ' + self.lastName;
					};

					return self;
				}
		]);
	})();

This creates a module called `my-module` and creates a controller called `MyController` that exposes `firstName`, `lastName` and `getFullName()`. I want to test the result of `getFullName()` (`MyControllerTests.js`):

	describe('MyController', function(){
		beforeEach(module('my-module'));

		describe('getFullName()', function(){
			it('should handle names correctly', inject(function($controller){
				var myController = $controller('MyController');

				myController.firstName = 'George';
				myController.lastName = 'Harrison';

				myController.getFullName().should.equal('George Harrison');
			}));
		});
	});

This does some interesting things.

	beforeEach(module('my-module'));

This loads the `my-module` module before each test in the `MyController` suite.

	it('should handle names correctly', inject(function($controller){

This injects `$controller` into the test. `$controller` allows resolving registered controllers.

	var myController = $controller('MyController');

This resolves an instance of the `MyController` controller. The instance is then used as the test subject.


## $scope injection

The `$scope` that gets injected in to an Angular controller is just a JS object. I'll assign a value and a method to `$scope` for another test. The controller declaration changes to this:

	angular
		.module('my-module')
		.controller('MyController', [
			'$scope',
			function($scope){
				var self = this;

				// ...

				$scope.songs = [
					'Here Comes The Sun'
				];

				$scope.addSong = function(song) {
					$scope.songs.push(song);
				};

				return self;
			}
	]);

The existing test can just pass in an empty object to the controller resolution:

	var myController = $controller('MyController', {
		$scope: {}
	});

Now the new test can inject, use and inspect a mock scope:

	describe('addSong()', function(){
		it('should add songs', inject(function($controller) {
			var scope = {};
			var myController = $controller('MyController', {
				$scope: scope
			});

			scope.addSong('While My Guitar Gently Weeps');

			scope.songs.should.contain('While My Guitar Gently Weeps');
		}));
	});


## Injecting and mocking $http

So now I've got a web service that I call to populate something on `$scope`:

	angular
		.module('my-module')
		.controller('MyController', [
			'$scope', '$http',
			function($scope, $http){
				var self = this;

				// ...

				$scope.instruments = ['foo'];

				$http.get('api/get-instruments')
					.success(function(data) {
						$scope.instruments = data;
					});

				return self;
			}
	]);

The [`$httpBackend`](https://docs.angularjs.org/api/ngMock/service/$httpBackend) is an `angular-mocks` service that fakes the `$http` service:

	describe('get-instruments result', function(){
		it('should be added to scope', inject(function($controller, $httpBackend){
			var scope = {};
			$httpBackend
				.when('GET', 'api/get-instruments')
				.respond([
					'vocals', 'guitar', 'sitar'
				]);
			var myController = $controller('MyController', {
				$scope: scope
			});

			$httpBackend.flush();

			scope.instruments.should.contain('guitar');
		}));
	});

The `$httpBackend.flush()` simulates the async calls completing, so they can be tested synchronously.


## Simulating $http errors

If the call to `api/get-instruments` fails, I want to set a status to 'ERROR':

	$scope.instruments = ['foo'];
	$scope.status = '';

	$http.get('api/get-instruments')
		.success(function(data) {
			$scope.instruments = data;
		})
		.error(function(e) {
			$scope.status = 'ERROR';
		});

To simulate the error, you can just tell the `$httpBackend` to respond with an error code (500):

	describe('get-instruments with error', function(){
		it('should have a status with error', inject(function($controller, $httpBackend){
			var scope = {};
			$httpBackend
				.when('GET', 'api/get-instruments')
				.respond(500, '');
			var myController = $controller('MyController', {
				$scope: scope
			});

			$httpBackend.flush();

			scope.status.should.equal('ERROR');
		}));
	});


## Here is a cat doing some TDD

Please imagine that this cat is really stoked about now being able to test front-end JavaScript.

![Source: https://www.aaamovies.com/Pictures%5CTestCatProfilePicture.jpg](/images/tdd-cat.jpg)



