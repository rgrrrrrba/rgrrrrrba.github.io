---
title: AppHarbor - Early notes
layout: post
date: 2013-08-22
category: archived
---

Finally I have got around to checking out AppHarbor to quickly spin up a test project for something I'm working on. I've only got a couple of sites on [Heroku](https://www.heroku.com/) set up some time ago for comparison with cloud hosting so this isn't a pros/cons post.


### Initial setup
Setting up is really easy. AppHarbor's entry level plan is free for unlimited applications hosted as subdomains of `apphb.com`. I initially wanted to try out Azure but they only have a free trail for a month.

First you set up an account, then you can create applications within that account. The management console is pretty easy to use but you do need to do some exploration.


### DVCS integration
I've only tried Github integration but they also offer Bitbucket and CodePlex integration. When creating a new application the integration links are right there so you can set everything up via AppHarbor, but I picked the wrong Github repo, the links disappeared, and I ended up setting it up manually. I did that through Github:

- open the repo settings
- select Service Hooks
- select 'AppHarbor' from the list of available service hooks
- 'Application Slug' is the name of the apphb application
- 'Token' you get from AppHarbor by selecting 'BUILD URL' from the application menu, which copies the build URL to the clipboard. The build URL looks like `https://appharbor.com:443/applications/APPLICATION_NAME/builds?authorization=TOKEN`.

When this is set up you just need to push to Github and AppHarbor will magically build a new application.

After I had set all this up I found the links to do the integration via AppHarbour again. They are in the application settings under 'Deploy from external repositories' near the bottom.


### How it works - basics
So the repo that is linked to the repository has to either have one VS solution or if it has multiple solutions AppHarbor will only look for a solution named `AppHarbor.sln`. That solution should have a web project in it, which is what is built and deployed.

Any tests that are in the solution are also automatically run and the results recorded. So you basically get a simple CI server for free with the application. I just added a project which referenced NUnit and threw a test in and it all just worked.

AppHarbor supports NuGet package restore - just remember to enable it in the solution :-$

#### More information
- [Deploying your first application using Git](https://support.appharbor.com/kb/getting-started/deploying-your-first-application-using-git)


### Databases
Just to see how everything worked, the first application I deployed was the 'Single page application' starter project, which includes a whole swag of stuff and runs off a localdb instance (SQL Server Compact). Initially this didn't work. I got an error page in the application. The application itself was working but it couldn't create the SQL Server Compact database. Turns out the best way to deploy a basic database is by enabling the SQL Server addon. This is free up to 20 MB. It is called 'Sequelizer' in a lot of AppHarbor's documentation.

After installing it the configuration needs to be updated. The `Connectionstring alias` just needs to be set to whatever is in the application's `web.config` file - `DefaultConnection` by default. AppHarbor will inject the connection string into the app's config, or the connection string shown could just be used directly (although this would be publicly available in a public Github repo - so don't do this).

#### More information
- [AppHarbor FAQ - SQL Server Compact](https://support.appharbor.com/kb/getting-started/frequently-asked-questions#sql-server-compact)


### Builds
When you push a new version you have to refresh the application page to see the updated build status. A big gotcha is that the build status doesn't update when the build completes. A bigger gotcha is that the build has to be deployed manually by pressing a Deploy button. I guess this is a good thing as long as you know about it and the build doesn't take an hour. Older builds cn also be redeployed. Again, when deploying the status page has to be manually refreshed to see the status.

**UPDATE** Actually it looks like it does automatically deploy most of the time. Something to keep an eye on.


### Glimpse
For a lark I installed [Glimpse](https://getglimpse.com/), an awesome server side diagnostics tool. This is installed purely through NuGet and hooks in to the ASP.NET pipeline. Glimpse has to be configured to allow access to the Glimpse console from a remote session (ignore LocalPolicy) but once that is done everything works like a dream, including SQL queries via Glimpse's EF 4 plugin.

#### More information
- [Glimpse configuration](https://getglimpse.com/Help/Configuration)

