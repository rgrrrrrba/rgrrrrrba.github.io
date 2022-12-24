---
title: Build pipeline for React
layout: post
date: 2015-08-27
category: archived
---

I'm going to eshew Bower and just use NPM as the package manager. I've already got [Node and NPM installed][gulp-hello-world]. The example site I'm creating is `survey-thing` - a simple thing for [creating surveys][survey-thing-repo]. I also have a [stub package][react-gulp-bootstrap-stub-project] which just includes a script for setting up the environment and a simple landing page.

I'm going to use all the shinies - [Browserify][browserify] for CommonJS modules and [JSX][jsx-in-depth] instead of the seperate `.js`/`.html` structure of a typical AngularJS application, using [Babel][babel] for ES6 features and [SASS][sass] for stylesheet preprocessing.


## Primer: Nancy as a static server

The site will be hosted in a simple [Nancy app][nancy] so the built React application will be output to `src\SurveyThing\app`. Host this in Nancy with a static convention - this example is for an ASP.NET site using OWIN.

Install some NuGet packages (`Microsoft.Owin`, `Microsoft.Owin.Host.SystemWeb`, `Nancy` and `Nancy.Owin`).

Add these values to the `web.config` file, within the `configuration` element:

	<appSettings>
		<add key="owin:HandleAllRequests" value="true" />
	</appSettings>
	<system.webServer>
		<modules runAllManagedModulesForAllRequests="true"/>
	</system.webServer>

Add `Startup` and `Bootstrapper` classes:

	public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app
                .UseNancy(new NancyOptions
                {
                    Bootstrapper = new Bootstrapper()
                })
                .UseStageMarker(PipelineStage.MapHandler);
        }
    }

    public class Bootstrapper : DefaultNancyBootstrapper
    {
        protected override void ConfigureConventions(NancyConventions nancyConventions)
        {
            Conventions.StaticContentsConventions.Clear();
            Conventions.StaticContentsConventions.AddDirectory("/", "app");

            base.ConfigureConventions(nancyConventions);
        }
    }

If you create `\app\test.html` inside the Nancy site's root it should be served from `https://localhost:PORT/test.html`. To serve `\app\index.html` when requesting `https://localhost:PORT` a static route needs to added:

    public class StaticModule : NancyModule
    {
        public StaticModule()
        {
            Get["/"] = _ => Response.AsFile("app/index.html");
        }
    }


## Set up Gulp

I want to use:

- `gulp` for the build tool
- `sass` for CSS generation
- `gulp-pleeease` to do [nice things with CSS][pleeease] including minification
- `babelify` for ES6 and JSX support
- `browserify` for a CommonJs module system
- `vinyl-source-stream` reduces the reliance on gulp plugins (so we can use `babelify` instead of `gulp-babel`)
- `react` obviously ;-)
- `gulp-streamify`
- `uglify` minify JS (used later)
- `yargs` get command line args (used later)

		npm install --save gulp
		npm install --save gulp-concat
		npm install --save gulp-sass
		npm install --save gulp-pleeease
		npm install --save babelify
		npm install --save browserify
		npm install --save vinyl-source-stream
		npm install --save react
		npm install --save gulp-streamify
		npm install --save uglify

There's a fair bit happening in [the `gulpfile.js` script][my-gulpfile] but here are some highlights.

The `'code'` step uses browserify to set up the CommonJS module system and uses `app/layout/index.jsx` as the entry point to the application. Babel is used to take advantage of ES6 and the script is minified.

	 gulp.task('code', function(){
		browserify({
			entries: 'app/layout/layout.jsx',
			extensions: ['.jsx'],
			debug: true
		})
			.transform(babelify)
			.bundle()
			.pipe(source('site.js'))
			.pipe(gulp.dest(destinationPath));
	});

CSS preprocessing is done using SASS. The entry point is `app/site.scss`.

	gulp.task('css', function(){
		gulp
			.src([
				'app/site.scss',
				'app/**/*.css'
			])
			.pipe(sass().on('error', sass.logError))
			.pipe(pleeease())
			.pipe(concat('site.css'))
			.pipe(gulp.dest(destinationPath));
	});

The template HTML file is copied verbatim to the destination path.

	gulp.task('html', function(){
		gulp
			.src('app/index.html')
			.pipe(gulp.dest(destinationPath));
	});

The default gulp task runs all the main tasks (`'vendor'` is not shown, it just bundles up vendor CSS and JS into `vendor.css` and `vendor.js` respectively).

	gulp.task('default', ['code', 'css', 'html', 'vendor']);

The `'watch'` task splits up the workload to keep live rebuilds snappy:

	gulp.task('watch', ['default'], function() {
		gulp.watch(
			['app/**/*.jsx'], 
			['code']);
		gulp.watch(
			['app/**/*.css', 'app/**/*.scss'],
			['css']);
		gulp.watch(
			['app/index.html'],
			['html']);
	});


## React app structure

The structure is pretty trivial at this point:

	app/
		index.html
		site.scss
		/layout
			layout.jsx

`index.html` references the stylesheets and scripts, and includes a `div` with an ID that will be used by the React app:

	<!DOCTYPE html>
	<html>
		<head>
			<title>Survey Thing</title>
			<link rel="stylesheet" type="text/css" href="site.css"/>
			<link rel="stylesheet" type="text/css" href="vendor.css"/>
		</head>
		<body>
			<div id="content"></div>

			<script src="site.js"></script>
			<script src="vendor.js"></script>
		</body>
	</html>

In `layout.jsx`, 'Hello world' is rendered into the `div` using inline markup:

	import React from 'react';

	React.render(
		<h1>Hello, world!</h1>,
		document.getElementById("content")
	);

Running `gulp` will run the `'default'` task, which should build the app and write the artifacts to `\src\SurveyThing\app`. Running `gulp watch` will run the `'watch'` task and rebuild whenever the monitored files change.

If everything works, we should have a happy 'hello world' page. I made mine pink <strike>because pink is cool</strike> to test SASS:

![](https://i.imgur.com/rMdt9t7.png)


## Minifying the `site.js` file

Without minification the `site.js` file is huge (1.5M). To conditionally minify the script I check for a `--release` argument:

	var yargs = require('yargs');

	var buildRelease = yargs.argv.release || false;

	gulp.task('code', function(){
		var pipeline = browserify({
			entries: 'app/layout/layout.jsx',
			extensions: ['.jsx'],
			debug: true
		})
			.transform(babelify)
			.bundle()
			.pipe(source('site.js'));

		if (buildRelease) {
			pipeline.pipe(streamify(uglify()));
		}

		pipeline
			.pipe(gulp.dest(destinationPath));
	});

Now running `gulp --release` results in a much more managable 185k file.


## fin

Check out the [survey-thing][survey-thing-repo] repo for updates and lols.


## Resources &amp; further reading:

- [pawelpabich/random-reactjs-hacks - Pawel Pabich][pawel-random-js-hacks]
- [Stub projects - React with Gulp and Bootstrap][react-gulp-bootstrap-stub-project]


[pawel-random-js-hacks]: https://github.com/pawelpabich/random-reactjs-hacks
[pleeease]: https://pleeease.io/
[my-gulpfile]: https://github.com/becdetat/survey-thing/blob/master/gulpfile.js
[survey-thing-repo]: https://github.com/becdetat/survey-thing
[gulp-hello-world]: hello-world-its-gulp.html
[browserify]: https://browserify.org/
[jsx-in-depth]: https://facebook.github.io/react/docs/jsx-in-depth.html
[babel]: https://babeljs.io/
[sass]: https://sass-lang.com/
[nancy]: https://nancyfx.org/
[react-gulp-bootstrap-stub-project]: https://github.com/becdetat/stub-projects/tree/master/react-with-gulp-and-bootstrap


