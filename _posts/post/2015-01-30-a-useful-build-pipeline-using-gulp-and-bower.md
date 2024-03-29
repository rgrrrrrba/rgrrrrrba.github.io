---
title: A useful build pipeline using Gulp and Bower
layout: post
date: 2015-01-30
category: archived
---

**DISCLAIMER:** I'm learning in the open here. This is the first time I've used Gulp and Bower and I'm sure I'm missing a heap of really important stuff. Enjoy!

- Part 1: [Hello World! It's Gulp!](hello-world-its-gulp.html)
- Part 2: A useful build pipeline using Gulp and Bower
- Part 3: [Cleaning and simplifying the Gulp pipeline](cleaning-and-simplifying-the-gulp-pipeline.html)

Big thanks to my colleague [Gert JvR](https://blog.gertjvr.com/) whose [ng-template](https://github.com/gertjvr/ng-template) project I am deconstructing.

- [`gulpfile.js` as of this post](https://github.com/becdetat/nancy-gulp-bower-angular-learnings/blob/8a723f7f95880974b15cbe054891a3db7e32e336/gulpfile-unclean.js)
- [cleaned `gulpfile.js`](https://github.com/becdetat/nancy-gulp-bower-angular-learnings/blob/8a723f7f95880974b15cbe054891a3db7e32e336/gulpfile.js) which I will use from here on


## Have you any Bootstrap?

I want to use Bootstrap, but...

![](https://cdn.meme.am/instances/500x/58510881.jpg)

Bower is a JavaScript package manager. So is NPM, in fact we'll use NPM to install Bower. The difference is that NPM is designed as a server-side (or developer-side) package manager, whereas Bower is only a front-end (client-side) package manager. NPM [can be used for client-side package management](https://browserify.org) but hopefully it will be easier to manage the two scenarios independently by using the package manager designed for the task.

Install Bower to the project (and globally) using NPM:

	npm install --save-dev -g bower

Now create `bower.json` by running `bower init` and following the instructions. Bower should not be ready to install Bootstrap:


	bower install bootstrap --save

This installs all of Bootstrap (including the separate jQuery dependency) into `/bower_components`. It also adds a reference to the dependency in `bower.json` - if it doesn't you may have forgotten the `--save` argument.

I then copied the [minimal Bootstrap HTML](https://getbootstrap.com/getting-started/#template) into `src/client/index.html`. This won't work because we're not copying or linking in the CSS correctly.


## Vendor CSS

There are two types of CSS - vendor and site-specific - and each will be handled slightly differently. Vendor CSS is anything that comes from a Bower package, and site-specific CSS will be anything in `/src/client/css`.

I'll start by adding a dependency task to the `rev-and-inject` task:

	gulp.task('rev-and-inject', ['vendorcss'], function() {
		// existing rev-and-inject task

In my [last post](hello-world-its-gulp.html) I declared the `config` object within `gulpfile.js`. I immediately regret this decision and move it into its own file - `gulp-config.json`. Now I need to explicitly add the CSS files that will be included in the site:

	{
		"paths": {
			"client": "src/client/",
			"server": "src/server/",
			"dist": "src/client-dist",
			"vendorcss": [
				"bower_components/bootstrap/dist/css/bootstrap.css",
				"bower_components/bootstrap/dist/css/bootstrap-theme.css"
			]
		}
	}

The `config` object is now initialised using `require()`:

	var config = require('./gulp-config.json');

Now we get to install some more dependencies!

[`gulp-concat`](https://www.npmjs.com/package/gulp-concat)

	npm install --save-dev gulp-concat

> Concatenates files

Pull in the `concat` dependency at the top of `gulpfile.js`:

	var concat = require('gulp-concat');

Now add the `vendorcss` task:

	gulp.task('vendorcss', function(){
	return gulp
		// set source
		.src(config.paths.vendorcss)
		// write to vendor.min.css
		.pipe(concat('vendor.min.css'))
		// write to dest
		.pipe(gulp.dest(config.paths.destination));
	});

This takes all of the vendor CSS files specified in `gulp-config.json` and bundles them into `/src/site-dist/vendor.min.css`. Very exciting but it hasn't minified the CSS yet. Time for some more plugins:

[`gulp-bytediff`](https://www.npmjs.com/package/gulp-bytediff)
	
	npm install --save-dev gulp-bytediff

> Compare file sizes before and after your gulp build process.

`bytediff` is just used to output the file size reduction from minification.

[`gulp-minify-css`](https://www.npmjs.com/package/gulp-minify-css)

	npm install --save-dev gulp-minify-css

> Minify css with clean-css.

Add the `bytediff` and `minify-css` dependencies at the top of `gulpfile.js`:

	var bytediff = require('gulp-bytediff');
	var minifyCss = require('gulp-minify-css');
	 
Then add the minify and bytediff steps to the pipeline (in `gulp.task('vendorcss'..`):

	return gulp
		// set source
		.src(config.paths.vendorcss)
		// write to vendor.min.css
		.pipe(concat('vendor.min.css'))

		// start tracking size
		.pipe(bytediff.start())
		// minify css
		.pipe(minifyCss())
		// stop tracking size and output it using bytediffFormatter
		.pipe(bytediff.stop(bytediffFormatter))

		// write to dest
		.pipe(gulp.dest(config.paths.destination));

The `bytediff.stop(bytediffFormatter)` uses a new function to format the file size difference. This function needs to be added:

	function bytediffFormatter(data) {
		var formatPercent = function(num, precision) {
			return (num * 100).toFixed(precision);
		};
	    var difference = (data.savings > 0) ? ' smaller.' : ' larger.';
	    
	    return data.fileName + ' went from ' +
	        (data.startSize / 1000).toFixed(2) + ' kB to ' + (data.endSize / 1000).toFixed(2) + ' kB' +
	        ' and is ' + formatPercent(1 - data.percent, 2) + '%' + difference;
	}

Now when I run `gulp build` the CSS is minified:

	[09:10:18] Starting 'vendorcss'...
	[gulp] [09:10:18] Compressing, bundling and copying vendor CSS
	[09:10:18] vendor.min.css went from 164.02 kB to 135.50 kB and is 17.39% smaller.
	[09:10:18] Finished 'vendorcss' after 298 ms
	[09:10:18] Starting 'rev-and-inject'...
	[09:10:18] Finished 'rev-and-inject' after 5.79 ms
	[09:10:18] Starting 'build'...
	[09:10:18] gulp-notify: [Gulp notification] Build complete
	[09:10:18] Finished 'build' after 48 ms

The `index.html` now needs a reference to the minified CSS file. It could be hard-coded to `vendor.min.css` but that is subject to change if the build script changes. So we need to _inject_ the path to the `vendor.min.css` artifact directly into `index.html` as it is being written.

Install yet another plugin:

[`gulp-inject`](https://www.npmjs.com/package/gulp-inject)

	npm install --save-dev gulp-inject

> A javascript, stylesheet and webcomponent injection plugin for Gulp, i.e. inject file references into your index.html

Add the new `inject` dependency to the top of `gulpfile.js`:

	var inject = require('gulp-inject');

Now in the `rev-and-inject` task add a local method that wraps `inject()` with some common options:

	var localInject = function(pathGlob, name) {
		var options = {
			// Strip out the 'src/client-dist-app' part from the path to vendor.min.css
			ignorePaths = config.paths.destination,
			// Don't read file being injected, just get the path
			read: false,
			// add a prefix to the injected path
			addPrefix: config.paths.buildPrefix
		};
	};

<aside>The `read: false` option is interesting, if it is set to true you can use a transform to [inject the contents](https://www.npmjs.com/package/gulp-inject/#injecting-files-contents) of the file into the output.</aside>

There is a new `buildPrefix` value in the config that needs to be added to `gulp-config.json`:

	{
		"paths": {
			// ...
			"buildPrefix": "app",
			// ...

This is needed because when the site will get hosted by Nancy, it will be available at `{yoursite}/app`. So the injected path will be `/app/content/vendor.min.css`. In a minute I'll set up a static server using Node.js for testing the output.

The inject step now needs to be added to the `rev-and-inject` task pipeline:

	gulp.task('rev-and-inject', ['vendorcss'], function() {
		var indexPath = path.join(config.paths.source, 'index.html');

		var localInject = //...

		return gulp
			.src([].concat(indexPath))

			// inject into inject-vendor:css
			.pipe(localInject(
				path.join(config.paths.destination, 'vendor.min.css'),
				'inject-vendor'))

			.pipe(gulp.dest(config.paths.distribution))
	});

Now in `/src/client/index.html` we just need to replace the link to `bootstrap.min.css` to the `inject-vendor:css` placeholder:

	<title>Bootstrap 101 Template</title>

	<!-- inject-vendor:css -->
	<!-- endinject -->

Now, running `gulp build` should inject the correct path into `/src/client-dist/index.html`:

	<!-- inject-vendor:css -->
	<link rel="stylesheet" href="/app/vendor.min.css">
	<!-- endinject -->


### Use Node.js to serve the static website

At the moment the output is going to `/src/client-dist`. When the site is eventually hosted on Nancy it will be served from `/app`, so the injected paths currently all start with `/app`, which means that the build output can't be viewed properly yet. I'm going to set up a quick, static server to publish the site. More dependencies!

[`connect`](https://www.npmjs.com/package/connect)
	
	npm install --save-dev connect

> High performance middleware framework

[`serve-static`](https://www.npmjs.com/package/serve-static)

	npm install --save-dev serve-static
	
> Serve static files

Add the new dependencies at the top of `gulpfile.js`:

	var connect = require('connect');
	var serveStatic = require('serve-static');
	
Now add a new task:

	gulp.task('serve', function(){
		var sourcePath = path.join(__dirname, config.paths.destination);
		var port = 12857;
		var serveFromPath = '/' + config.paths.buildPrefix;

		log('Hosting ' + sourcePath + ' at https://localhost:' + port + serveFromPath);

		connect()
			.use(serveFromPath, serveStatic(sourcePath))
			.listen(port);
	});

Now running `gulp serve` will serve the static content from <https://localhost:12857/app>. I can leave that running in one console while rebuilding in another.

Interestingly, this way of serving a static site could probably be used all the way through to production, as the interaction with the server is all done on the client side via REST calls.


## Site-specific CSS

In `gulpfile.js` add a new `css` task:

	gulp.task('css', function() {
		return gulp
			// set source (src/**/*.css)
			.src([path.join(config.paths.client, '**/*.css')])
			// write to site.min.css
			.pipe(concat('site.min.css'))
			// start tracking size
			.pipe(bytediff.start())
			// minify the css
			.pipe(minifyCss())
			// stop tracking size and output it
			.pipe(bytediff.stop(bytediffFormatter))
			// write to dest/content
			.pipe(gulp.dest(config.paths.destination));
	});

This is getting a bit familiar. Instead of using a set of explicit tasks from `gulp-config.json` I've just assumed that anything named `*.css` anywhere in the client should be injected into the static site distribution. The concatenated, minified output gets written to `/src/client-dist/content/site.min.css`. Now in the `rev-and-inject` task the `css` task needs to be added to the prerequisites:

	gulp.task('rev-and-inject', ['vendorcss', 'css'], function(){
		// ...

And the path to the new `site.min.css` needs to be injected (this goes after the `inject-vendor:css` injection):

	// inject into inject:css
	.pipe(localInject(config.paths.destination))

Note that there is no name placeholder used. This will inject into the default `inject:css` placeholder, which needs to be added to `index.html` after the existing `inject-vendor:css` placeholder:

	<!-- inject:css -->
	<!-- endinject -->

Now if you add some CSS files to `/src/client` they will be injected into `index.html`.


## Vendor JavaScript

One more dependency:

[`gulp-uglify`](https://www.npmjs.com/package/gulp-uglify)

	npm install --save-dev gulp-uglify

> Minify files with UglifyJS.

Vendor JS is configured the same way vendor CSS is, in `gulp-config.json`:

		"vendorcss": [
			// ...
		],
		"vendorjs": [
			"bower_components/jquery/dist/jquery.js",
			"bower_components/bootstrap/dist/bootstrap.js"
		]

`uglify` is used instead of `minifyCss`. Add the dependency at the top of `gulpfile.js`:

	var uglify = require('gulp-uglify');

Now create the `vendorjs` task:

	gulp.task('vendorjs', function(){
		return gulp
			// set source
			.src(config.paths.vendorjs)
			// write to vendor.min.js
			.pipe(concat('vendor.min.js'))
			// start tracking size
			.pipe(bytediff.start())
			// uglify js
			.pipe(uglify())
			// stop tracking size and output it using bytediffFormatter
			.pipe(bytediff.stop(bytediffFormatter))
	 
			// write to dest
			.pipe(gulp.dest(config.paths.destination));
	});

In `rev-and-inject`, the `vendorcss` prerequisite task needs to be added:

	gulp.task('rev-and-inject', ['vendorcss', 'css', 'vendorjs'], function(){
		// ...	

And the newly minified `content/script/vendor.min.js` needs to be injected (after the `inject:css` injection):

	// inject into inject-vendor:js
	.pipe(localInject(
		path.join(config.paths.destination, 'vendor.min.js'),
		'inject-vendor'))

Now the `inject-vendor:css` placeholder needs to be added to `index.html` at the end of the `<body>` element:

	<!-- inject-vendor:css -->
	<!-- endinject -->


## Site-specific JavaScript

To support AngularJS, the site-specific JS task will need a couple of extra steps, but I'll leave that for the next post. Meanwhile, it will be similar to the site-specific CSS task, bundling and minifying all `*.js` files in `/src/client`.

	gulp.task('js', function() {
		return gulp
			// set source (src/**/*.js)
			.src([path.join(config.paths.client, '**/*.js')])
			// write to site.min.js
			.pipe(concat('site.min.js'))
			// start tracking size
			.pipe(bytediff.start())
			// uglify js
			.pipe(uglify())
			// stop tracking size and output it using bytediffFormatter
			.pipe(bytediff.stop(bytediffFormatter))
	 
			// write to dest
			.pipe(gulp.dest(config.paths.destination));
	});

In `rev-and-inject`, the `js` prerequisite task needs to be added:

	gulp.task('rev-and-inject', ['vendorcss', 'css', 'vendorjs'], function(){
		// ...	

And `content/script/site.min.js` needs to be injected (after the `inject-vendor:js` injection):

	// inject into inject:js
	.pipe(localInject(
		path.join(config.paths.destination, 'site.min.js')))

## Fonts and images

Site assets that aren't CSS or JS need to be processed as well. Fonts are pretty straightforward, I'll just copy everything in `content/fonts`:

	gulp.task('fonts', function(){
		log('Copy fonts');

		return gulp
			.src([path.join(config.paths.client, 'content/fonts/*')])
			.pipe(gulp.dest(path.join(config.paths.destination, 'content/fonts')));
	});

Since this can be done outside of the `rev-and-inject` process, it gets added to the `build` task:

	gulp.task('build', ['rev-and-inject', 'fonts'], function() {
		// ...

Images could be a straight copy as well, or you can pass them through an image optimization plugin. Install two more dependencies:

[`gulp-cache`](https://www.npmjs.com/package/gulp-cache)

	npm install --save-dev gulp-cache

> A cache proxy task for Gulp

[`gulp-imagemin`](https://www.npmjs.com/package/gulp-imagemin)

	npm install --save-dev gulp-imagemin

> Minify PNG, JPEG, GIF and SVG images

`imagemin` is an image minifier. This performs some compression on PNG images:

	gulp.task('images', function(){
		log('Compress, cache and copy images');

		return gulp
			.src([path.join(config.paths.client, 'content/images/*')])
			.pipe(cache(imagemin({
				optimizationLevel: 3
			})))
			.pipe(gulp.dest(path.join(config.paths.destination, 'content/images')));
	});

This task also gets added as a prerequisite to the `build` task:

	gulp.task('build', ['rev-and-inject', 'fonts', 'images'], function() {
		// ...


## Revisioning and cache-busting

Revisioning is a way of cache-busting (forcing the browser to reload assets) by appending a hash to the filename. Since this hash is unique for a particular revision of the file (as it is a hash of the file's contents) as long as the source file doesn't change, the revisioned file name will stay the same and will reload from the browser's cache. This uses the `gulp-rev` and `gulp-rev-replace` plugins:

[`gulp-rev`](https://www.npmjs.com/package/gulp-rev)

	npm install --save-dev gulp-rev

> Static asset revisioning by appending content hash to filenames: unicorn.css => unicorn-098f6bcd.css

[`gulp-rev-replace`](https://www.npmjs.com/package/gulp-rev-replace)

	npm install --save-dev gulp-rev-replace

> Rewrite occurences of filenames which have been renamed by gulp-rev

Add the new dependencies to the top of `gulpfile.js`:

	var rev = require('gulp-rev');
	var revReplace = require('gulp-rev-replace');

Now the `build` task gets a bit of a rewrite:

	var indexFilter = filter('index.html');
	var cssFilter = filter("**/*.min.css");
	var jsFilter = filter("**/*.min.js");
	var manifestFilter = filter('rev-manifest.json');

	return gulp
		// 1. set source (/src/client/)
		.src([].concat(
			path.join(config.paths.client, 'index.html'), 
			path.join(config.paths.destination, '*.min.css'),
			path.join(config.paths.destination, '*.min.js')))

		// 2. add the revision to the css files
		.pipe(cssFilter)
		.pipe(rev())
		.pipe(gulp.dest(config.paths.destination))
		.pipe(cssFilter.restore())

		// 3. add the revision to the js files
		.pipe(jsFilter)
		.pipe(rev())
		.pipe(gulp.dest(config.paths.destination))
		.pipe(jsFilter.restore())

		// 4. inject css and js
		.pipe(indexFilter)
		.pipe(localInject(path.join(config.paths.destination, 'vendor.min.css'), 'inject-vendor'))
		.pipe(localInject(path.join(config.paths.destination, 'site.min.css')))
		.pipe(localInject(path.join(config.paths.destination, 'vendor.min.js'), 'inject-vendor'))
		.pipe(localInject(path.join(config.paths.destination, 'site.min.js')))
		.pipe(gulp.dest(config.paths.destination))
		.pipe(indexFilter.restore())

		// 5. substitute in new revved filenames
		.pipe(revReplace())
		.pipe(gulp.dest(config.paths.destination));

I've numbered the stages of this pipeline. 

In step 1 we select `index.html` and the `*.min.css` and `*.min.js` files.

In step 2 we filter down to just the `*.min.css` files, then apply the revisioning hash to the filenames (using `rev()`):

	// filter to *.min.css
	.pipe(cssFilter)
	// add the revision to the files
	.pipe(rev())
	// write the files
	.pipe(gulp.dest(config.paths.destination))
	// clear the filter
	.pipe(cssFilter.restore())

Step 3 is the same as step 2 except for `*.min.js`.

In step 4 we filter down to just `index.html` and do the existing CSS and JS injections.

In step 5 we substitute the newly revisioned filenames into `index.html`.


## And finally...

The end result looks like this:

![](https://i.imgur.com/j3WY60e.png)

`index.html` points to the concatenated, minified, and hashed files:

	<!DOCTYPE html>
	<html lang="en">
		<head>
			<!-- inject-vendor:css -->
			<link rel="stylesheet" href="/app/vendor.min-a491bda8.css">
			<!-- endinject -->

			<!-- inject:css -->
			<link rel="stylesheet" href="/app/site.min-238af6ba.css">
			<!-- endinject -->
			</head>
		<body>
			<h1>Hello, world!</h1>

			<!-- inject-vendor:js -->
			<script src="/app/vendor.min-8e07c5e8.js"></script>
			<!-- endinject -->

			<!-- inject:js -->
			<script src="/app/site.min-5b54178e.js"></script>
			<!-- endinject -->
		</body>
	</html>

I'm not entirely happy with this so [next I'll try to simplify things](cleaning-and-simplifying-the-gulp-pipeline.html).



## Further reading and resources

- <https://github.com/johnpapa/ng-demos/blob/master/grunt-gulp/build-gulp/gulpfile.js>
- <https://github.com/gertjvr/ng-template>
- <https://gulpjs.com/>
- <https://nodejs.org/api/>
- [`gulpfile.js` as of this post](https://github.com/becdetat/nancy-gulp-bower-angular-learnings/blob/8a723f7f95880974b15cbe054891a3db7e32e336/gulpfile-unclean.js)
- [cleaned `gulpfile.js`](https://github.com/becdetat/nancy-gulp-bower-angular-learnings/blob/8a723f7f95880974b15cbe054891a3db7e32e336/gulpfile.js) which I will use from here on


