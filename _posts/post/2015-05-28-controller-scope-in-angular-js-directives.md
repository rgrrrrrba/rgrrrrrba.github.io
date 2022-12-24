---
title: Controller scope in Angular JS directives
layout: post
date: 2015-06-28
category: archived
---

This is probably basic level Angular JS but I haven't seen it mentioned anywhere. I'm probably missing something fundamental about directive scope.

Say you've got this directive ([JSFiddle](https://jsfiddle.net/10qwqc5r/2/)):

	angular
	    .module('app', [])
	    .directive('thing', function() {
	        return {
	            restrict: 'E',
	            replace: true,
	            template: '<div><input ng-model="vm.name"/> Name: {{vm.name}}</div>',
	            controller: function() {
	                this.name = '';
	            },
	            controllerAs: 'vm'
	        };
	    });

Using it once works great:

	<div ng-app="app">
		<thing></thing>
	</div>

But if you use the directive multiple times, it becomes clear that the directive views all share the same controller:

	<div ng-app="app">
	    <thing></thing>
	    <thing></thing>
	    <thing></thing>
	    <thing></thing>
	    <thing></thing>
	</div>

![](https://i.imgur.com/Oqbl2Yy.png)

Typing in the first textbox affects all of the other directive views, ie. they are all pointing to the same controller.

In fact, if you have different directives with the same `controllerAs` value, you can see that the `vm` instance for each directive is set to the last directive's controller ([JSFiddle](https://jsfiddle.net/10qwqc5r/3/)):

	angular
	    .module('app', [])
	    .directive('firstDirective', function() {
	        return {
	            restrict: 'E',
	            replace: true,
	            template: '<div>first directive: <pre>{{vm}}</pre></div>',
	            controller: function() {
	                this.foo = 'Hi!';
	            },
	            controllerAs: 'vm'
	        };
	    })
	    .directive('secondDirective', function(){
	        return {
	            restrict: 'E',
	            replace: true,
	            template: '<div>second directive: <pre>{{vm}}</pre></div>',
	            controller: function() {
	                this.bar = 'There?';
	            },
	            controllerAs: 'vm'
	        };
	    });

	<div ng-app="app">
		<first-directive></first-directive>
		<second-directive></second-directive>
	</div>

![](https://i.imgur.com/9y6Rg6k.png)

If you change the name of the `controllerAs` alias - say to `firstDirectiveVm` and `secondDirectiveVm` - then the problem goes away, so Angular JS by default is setting `vm` globally each time a directive uses `controllerAs: 'vm'`, and going down the page, meaning the last `vm` wins. This can obviously be a pretty tricky problem to diagnose. Besides which, this workaround of changing each directive's `controllerAs` value won't work for multiple directives of the same type.

The solution is to set `scope` to `true` in the directive declaration ([JSFiddle](https://jsfiddle.net/10qwqc5r/4/)):

	angular
	    .module('app', [])
	    .directive('thing', function() {
	        return {
	            restrict: 'E',
	            replace: true,
	            template: '<div><input ng-model="vm.name"/> Name: {{vm.name}}</div>',
	            controller: function() {
	                this.name = '';
	            },
	            controllerAs: 'vm',
	            scope: true
	        };
	    });

![](https://i.imgur.com/jUlaSCJ.png)

A lot more can happen in that `scope` value than setting it to true. See the Angular JS docs for [isolating directive scope](https://docs.angularjs.org/guide/directive#isolating-the-scope-of-a-directive) for examples. Unfortunately, 'scope' seems to be an overloaded term in Angular JS world. This kind of 'scope' is talking about the scope of the element and attributes provided by the directive, in a way distinct from `$scope`, which is what I'm trying to avoid by using `controllerAs` in the first place.

It seems strange to me that shared scope is the default, and that you need to set `scope` to a non-falsy value to opt out of that. I'm sure I'm missing a lot of nuance around the reasons. In any case, setting `scope: true` seems to be the happy path. 

I just wish I hadn't wasted a full day rewriting an entire site before figuring out what was happening.

:-(




