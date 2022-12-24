---
title: JavaScript namespace generator
layout: post
date: 2013-09-05
category: archived
---

The project I'm working on at the moment is using [Knockout](https://knockoutjs.com/) with some view models that are three deep. Since all the projects scripts are being bundled together that's a lot of JS, so I'm using namespaces to keep things tidy.

The namespaces are being declared like this:

	var Foo = Foo || {};
	Foo.Bar = Foo.Bar || {};
	Foo.Bar.Derp = Foo.Bar.Derp || {};

and finally the view model gets declared like:

	Foo.Bar.Derp.ViewModel = function () {
		...
	}

The problem is that namespace declaration has to be written, updated and maintained for every js file in the project.

I wrote this helper method which autogenerates the namespace declaration and inserts it in a script element:

	var namespace = function(declr) {
		var current = null;
		var cmd = 'var ';
		$.each(declr.split('.'), function(i){
			current = current ? current + '.' : '';
			current += this;
			cmd += current + ' = ' + current + ' || {};\n';
		});
		$('body').append('<script type="text/javascript">' + cmd + '<' + '/script>');
	};

Each file's namespace declaration then becomes:

	namespace('Foo.Bar.Derp.Chicken.Derp.Bork.Bork.Bork');

	Foo.Bar.Derp.Chicken.Derp.Bork.Bork.Bork.Bwuhahaha = function() {
		alert ('Namespaced, dude');
	}

	Foo.Bar.Derp.Chicken.Derp.Bork.Bork.Bork.Bwuhahaha();

Because of the way the declarations are written, the generated namespace declarations won't collide:

	namespace('Foo.Bar.Derp.Chicken.Derp.Bork.Bork.Bork.Oh.No');

	Foo.Bar.Derp.Chicken.Derp.Bork.Bork.Bork.Oh.No.Wai = function() {
		alert ('Yes wai');
	}

	Foo.Bar.Derp.Chicken.Derp.Bork.Bork.Bork.Oh.No.Wai();

The generated HTML litters the place with the extra scripts, but that's the browser's problem amiright?

