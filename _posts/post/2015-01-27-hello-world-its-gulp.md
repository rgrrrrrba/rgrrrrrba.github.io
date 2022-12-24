---
title: Hello World! It's Gulp!
layout: post
date: 2015-01-27
category: archived
---

**DISCLAIMER:** I'm learning in the open here. This is the first time I've used Gulp and I'm sure I'm missing a heap of really important stuff. Enjoy!

- Part 1: Hello World! It's Gulp!
- Part 2: [A useful build pipeline using Gulp and Bower](a-useful-build-pipeline-using-gulp-and-bower.html)
- Part 3: [Cleaning and simplifying the Gulp pipeline](cleaning-and-simplifying-the-gulp-pipeline.html)

[Gulp](https://gulpjs.org) is a Node.js based build tool. It executes `gulpfile.js` in the project root to set up a build pipeline, doing things like bundling, minification, and artifact copying.

This is useful in a workflow where you have an essentially static website that uses a client-side framework such as AngularJS, backed onto a web service that exposes functionality via something like a REST API. Build tools like Gulp and JS packagement management tools such as [Bower](https://bower.io) can be used to manage these complex client-side sites.

The static site that I'll hopefully end up generating in the next post will be used with a [Nancy](https://nancyfx.org) website. The end result of this post is a Gulp build script that simply copies an `index.html` file from `/src/client` to `/src/client-dist`.


## Prerequisites

Install Node and NPM. The easiest way may be via [Chocolatey](https://chocolatey.org), this does both:

	cinst nodejs.install

After installation, you may need to add `C:\Program Files\nodejs` to the system path and create a new folder in `C:\Users\bec_000\AppData\Roaming` named `npm`.

Get NPM to create a `package.json` file in the project root by running `npm install` and working through the wizard. Now install Gulp using NPM:

	npm install --save-dev gulp

The `--save-dev` flag adds the dependencies to `package.json`. This means that when you open the repository in a new environment you can just do `npm install` to automatically install the project's NPM dependencies. 

Note that Gulp itself should also be installed globally so you can use `gulp` on the command line:

	npm install -g gulp


##  helloworlding Gulp

Create a file `gulpfile.js` in the project root. Start out by importing the Gulp module:

	var gulp = require('gulp');

`gulp.task()` defines a task that's available from the command line.

	gulp.task('hello', function() {
		console.log('Hello world!')
	});

If you run `gulp hello`:

	λ gulp hello
	[10:22:08] Using gulpfile c:\source\angular-learnings\gulpfile.js
	[10:22:08] Starting 'hello'...
	Hello world!
	[10:22:08] Finished 'hello' after 316 μs

`gulp.task` also lets you run prerequisite tasks:

	gulp.task('hello', ['one', 'two', 'three'], function() {
		console.log('Hello world!')
	});

	gulp.task('one', function(){
		console.log('one');
	});

	gulp.task('two', function(){
		console.log('two');
	});

	gulp.task('three', function(){
		console.log('three');
	});

Now `gulp hello` does this:

	[10:24:49] Starting 'one'...
	one
	[10:24:49] Finished 'one' after 200 μs
	[10:24:49] Starting 'two'...
	two
	[10:24:49] Finished 'two' after 151 μs
	[10:24:49] Starting 'three'...
	three
	[10:24:49] Finished 'three' after 154 μs
	[10:24:49] Starting 'hello'...
	Hello world!
	[10:24:49] Finished 'hello' after 135 μs

Now make you a build pipeline. Empty out `gulpfile.js` and start again, partner.

![](https://media.giphy.com/media/a1wyl0YQrCGm4/giphy.gif)


## Lots of scripting just to copy a file!

I'm just going to start out with a simple build pipeline that basically copies `index.html` to the server.

Install some more NPM packages. 

[`gulp-load-plugins`](https://www.npmjs.com/package/gulp-load-plugins)

	npm install --save-dev gulp-load-plugins

> Loads in any gulp plugins and attaches them to the global scope, or an object of your choice.

Eg.:

	var gutil = require('gulp-load-plugins')([
		'colors', 'env', 'log', 'pipeline'
	]);

[`gulp-notify`](https://www.npmjs.com/package/gulp-notify)

	npm install --save-dev gulp-notify

> gulp plugin to send messages based on Vinyl Files or Errors to Mac OS X, Linux or Windows using the node-notifier module. Fallbacks to Growl or simply logging

[`gulp-filter`](https://www.npmjs.com/package/gulp-filter)

	npm install --save-dev gulp-filter

[`chalk`](https://www.npmjs.com/package/chalk)

	npm install --save-dev chalk

> Terminal string styling done right

[`dateformat`](https://www.npmjs.com/package/dateformat)

	npm install --save-dev dateformat

> A node.js package for Steven Levithan's excellent dateFormat() function.

[`del`](https://www.npmjs.com/package/del)

	npm install --save-dev del
	
> Delete files/folders using globs

Whew, that's a bunch of dependencies. At the top of `gulpfile.js`, pull them in using `require()` and get some utility dependencies into scope:

	var gulp = require('gulp');
	var notify = require('gulp-notify');
	var filter = require('gulp-filter');
	var plugins = require('gulp-load-plugins')();
	var del = require('del');
	var path = require('path');

	var gutil = plugins.loadUtils([
		'colors', 'log'
	]);

	var log = gutil.log;
	var colors = gutil.colors;

To centralise the build paths, add this next:

	var config = {
		"paths": {
			"source": "src/client",
			"distribution": "src/client-dist"
		}
	};

This could be put into another file like `gulp-config.json` and pulled in with a `require()` but for now this will do.

I'll split out the actual copy process into a gulp task called `rev-and-inject`. This will eventually be more involved including adding a revision number for cache busting and injecting minified and bundled resources.

	gulp.task('rev-and-inject', function() {
		var indexPath = path.join(config.paths.source, 'index.html');

		return gulp
			// set source
			.src([].concat(indexPath))
			// write to dest
			.pipe(gulp.dest(config.paths.distribution))
	});

The `build` task calls `rev--and-inject` before displaying a notification (using a toast!):

	gulp.task('build', function(){
		return gulp
			.src('')
			.pipe(notify({
				onLast: true,
				message: 'Build complete'
		}));
	});

In `src/client` I've added an `index.html` just for testing. Run `gulp build`:

	[15:05:05] Starting 'rev-and-inject'...
	[15:05:05] Finished 'rev-and-inject' after 24 ms
	[15:05:05] Starting 'build'...
	[15:05:05] gulp-notify: [Gulp notification] Build complete
	[15:05:05] Finished 'build' after 35 ms

You can add a quick `clean` task too, which will delete the `src/client-dist` folder:

	gulp.task('clean', function(){
		log('Cleaning: ' + config.paths.distribution);

		del([].concat(config.paths.distribution));
	});

[Next I'll add some value to the build pipeline](a-useful-build-pipeline-using-gulp-and-bower.html) by minifying and bundling JS and CSS, and injecting the results into `index.html`. The result will be a static website set up for some AngularJS work.


## Further reading and resources

- <https://github.com/johnpapa/ng-demos/blob/master/grunt-gulp/build-gulp/gulpfile.js>
- <https://github.com/gertjvr/ng-template>
- <https://gulpjs.com/>
- <https://nodejs.org/api/>


