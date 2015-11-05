



## Creating a new web app on Azure

The easiest way to bootstrap a new wiki is by cloning [`frankenwiki.nancy.demo`](https://github.com/frankenwiki/frankenwiki.nancy.demo), a demonstration Frankenwiki site that uses Nancy and Autofac with an ASP.NET host. The resulting site should be fairly trivial to host as an Azure web app. The [frankenwiki.com](http://frankenwiki.com) site itself is created this way.

	git clone https://github.com/frankenwiki/frankenwiki.nancy.demo.git your-site.com

Wipe out the Git history and optionally push everything to your own repository:

	cd your-site.com
	rmdir /S .git
	git init
	git remote add origin git@github.com:you/your-site.com.git
	git add -A
	git commit -m "copy from demo site"
	git push

Renaming the project to something more relevant (eg `FrankenwikiDotCom`), and adjust the namespaces. Edit `Web.config` and change the `owin:AppStartup` app setting to match the fully-qualified name of the startup class (eg. `FranenwikiDotCom.Startup`). Make sure the site works - you should just be able to F5 the solution.

From there it should be trivial to create an Azure web app and set up deployment using source control. It _should_ be, but for some reason the Frankenwiki organisation isn't appearing in the list of available repositories. I used TeamCity and Octopus to deploy it anyway. I won't go through the steps here - there's a great post about the Octopus Deploy side of it on [their blog](https://octopusdeploy.com/blog/deploy-aspnet-applications-to-azure-websites).

Now that you've got a 



