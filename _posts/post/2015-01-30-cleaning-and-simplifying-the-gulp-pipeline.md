---
title: Cleaning and simplifying the Gulp pipeline
layout: post
date: 2015-01-30
category: archived
---

**DISCLAIMER:** I'm learning in the open here. This is the first time I've used Gulp and Bower and I'm sure I'm missing a heap of really important stuff. Enjoy!

- Part 1: [Hello World! It's Gulp!](hello-world-its-gulp.html)
- Part 2: [A useful build pipeline using Gulp and Bower](a-useful-build-pipeline-using-gulp-and-bower.html)
- Part 3: Cleaning and simplifying the Gulp pipeline

I wasn't entirely happy with how the [previous build script](https://github.com/becdetat/nancy-gulp-bower-angular-learnings/blob/8a723f7f95880974b15cbe054891a3db7e32e336/gulpfile.js) wrote out the minified JS and CSS files next to `index.html`, or how the vendor files had to be specified in the configuration, so I did some playing with the `gulp-useref` plugin and cleaned things up significantly.

I installed two new dependencies.

[`gulp-usered`](https://www.npmjs.com/package/gulp-useref)

	npm install --save-dev gulp-useref

> Parse build blocks in HTML files to replace references to non-optimized scripts or stylesheets.

[`gulp-if`](https://www.npmjs.com/package/gulp-if)

	npm install --save-dev gulp-if

> Conditionally run a task

I moved all the dependencies into `$`, deleted the dependencies I wouldn't need any more and added `useref`:

	var $ = {
		if: require('gulp-if'),
		notify: require('gulp-notify'),
		rev: require('gulp-rev'),
		revReplace: require('gulp-rev-replace'),
		useref: require('gulp-useref'),
		filter: require('gulp-filter'),
		uglify: require('gulp-uglify'),
		minifyCss: require('gulp-minify-css'),
		del: require('del'),
		path: require('path'),
		connect: require('connect'),
		serveStatic: require('serve-static'),
		log: require('gulp-load-plugins')()
			.loadUtils(['log'])
			.log
	};

Then I deleted the `rev-and-inject` task plus all of the CSS and JS tasks, and replaced the `build` task with this:

	gulp.task('build', ['fonts', 'images'], function() {
		var cssFilter = $.filter('**/*.css');
		var jsFilter = $.filter('**/*.js');
		var assets = $.useref.assets();

		return gulp
			.src($.path.join(config.paths.client, '*.html'))
			.pipe(assets)
			.pipe($.if('*.js', $.uglify()))
			.pipe($.if('*.css', $.minifyCss()))
			.pipe($.rev())
			.pipe(assets.restore())
			.pipe($.useref())
			.pipe($.revReplace())
			.pipe(gulp.dest(config.paths.destination))

			.pipe($.notify({
				onLast: true,
				message: 'Build complete'
			}))
			;
	});

`$useref.assets()` scans the `.html` files for JS and CSS references. The `gulp-if` plugin lets you do basic logic, this replaces the `gulp-filter` plugin to perform conditional minification. `rev` and `revReplace` do the same cache busting as before.

`index.html` now contains the relative references to the JS and CSS files. CSS is in the header:

	<head>
	    <!-- build:css content/site.min.css -->
	    <link href="../../bower_components/bootstrap/dist/css/bootstrap.css" rel="stylesheet">
	    <link href="../../bower_components/bootstrap/dist/css/bootstrap-theme.css" rel="stylesheet">
	    <link href="content/css/site.css"/>
	    <!-- endbuild -->

JS at the end of the body:

		<!-- build:js content/site.min.js -->
		<script type="text/javascript" src="../../bower_components/jquery/dist/jquery.js"></script>
		<script type="text/javascript" src="../../bower_components/bootstrap/dist/bootstrap.js"></script>
		<script type="text/javascript" src="content/script/site.js"></script>
		<!-- endbuild -->
	</body>

The output path is specified in the `build:css` / `build.js` placeholder. Another benefit of this approach is that the development `index.html` is actually usable as is - those `<script>` and `<link>` will resolve to the un-mangled originals.


## File watching

In the `serve` task (which sets up a static web server hosting the built site) I used `gulp.watch` to listen for changes in the source folder and trigger a build:

	return gulp.watch(config.paths.client + '/**', ['build']);

Now if a file changes, the files get rebuilt in the same process as the server. Because that server is just serving the entire source path any changes are available instantly.

Here is the [`gulpfile.js` as at this post](https://github.com/becdetat/nancy-gulp-bower-angular-learnings/blob/bfdcced8a1d664d0a933db64c255bfb7268913a0/gulpfile.js). Next I'll get some AngularJS happening.


## Further reading and resources

- <https://github.com/johnpapa/ng-demos/blob/master/grunt-gulp/build-gulp/gulpfile.js>
- <https://github.com/gertjvr/ng-template>
- <https://gulpjs.com/>
- <https://nodejs.org/api/>
- [`gulpfile.js` as of this post](https://github.com/becdetat/nancy-gulp-bower-angular-learnings/blob/bfdcced8a1d664d0a933db64c255bfb7268913a0/gulpfile.js)


