---
title: Bundling and deploying Azure WebJobs in a Web App with Octopus and Nimbus
layout: post
date: 2015-08-27
category: archived
---

<aside class="pull-right well" style="width: 20em">
	"Nimbus is a .NET client library to add an easy to develop against experience against the Azure Service Bus or the Windows Service Bus."
</aside>

I've been working on a site hosted in Azure. Up until a few days ago it consisted of a single, monolithic web app, but I had been implementing it against [Nimbus][nimbus]'s [message][nimbus-mc] and [infrastructure][nimbus-infra] contract libraries, with a simple in-process bus implementation. This paid off because I reached a point where I needed to offload some asynchronous work to the service layer while keeping things simple in the site layer, and converting the entire application to use Service Bus took a few hours all up.

That left me with needing somewhere to host the service layer. Load and scalability isn't an issue so I went with a [WebJob][azure-webjob].

WebJobs are very lightweight and easy to create. They run as part of an Azure web app, so they can be scaled up and out as far as the web app can. This could be a limiting factor in some scenarios. Fortunately a web job that wraps a Nimbus/Service Bus could easily be converted to a different, heavier-weight deployment type, such as a Windows service for use in a VM, a worker role or on-premises.

Getting a WebJob deployed is fairly easy, but everything has to be correct or the job just won't run, and there isn't any support for diagnosing why. As far as Azure is concerned it just doesn't exist.

I'm creating a continuous WebJob that uses Nimbus for messaging. The Azure WebJobs API has its own methods for registering endpoints, and for creating triggered or scheduled jobs. Scott Hanselman has a great blog post explaining [how to use the API in this way][hanselman-introducing-windows-azure-webjobs]. The WebJob will basically be a container for a Nimbus message pump.


## Creating the WebJob executable

<aside class="pull-left well" style="width:15em">
	The .exe name is important. <code>MyApplicationWorker.exe</code> is ok but <code>MyApplication.Worker.exe</code> is not.
</aside>

For use in a C#/.NET application, WebJobs are created as console applications. The name of the generated executable can't have any punctuation - `MyApplicationWorker.exe` is ok but `MyApplication.Worker.exe` isn't. You can still use punctuation in the project name and default namespace, just set the "Assembly name" in the project properties to something valid.

The application references the `Microsoft.Azure.WebJobs` NuGet package, and the `Main` method is trivial:

    private static void Main(string[] args)
    {
        var container = IoC.LetThereBeIoC();

        var connectionString = ConfigurationManager.AppSettings["WebJobStorageConnectionString"];
        var configuration = new JobHostConfiguration(connectionString);
        var host = new JobHost(configuration);

        host.RunAndBlock();
    }

The first line creates the IoC container, which includes all the Nimbus setup. The Nimbus bus then just sits in the background while the app is running, handling messages as they appear on the bus.

The middle section sets up the WebJob host. It requires a connection string to an Azure Table Storage instance. I tried this with the local storage emulator for testing but it requires an actual instance in Azure - the WebJobs API apparently requires features that are not available in the local storage emulator.

`host.RunAndBlock()` then effectively puts the application into an infinite loop and lets it sit around to handle messages.

When starting the application locally it complains that "No functions found". This is because WebJobs have their own message handling model based on Service Bus, by decorating parameters on static methods. Since I'm using Nimbus to handle the messaging via Service Bus there are no WebJob-specific handlers needed.


## Deploying

Because WebJobs are part of a web site, they don't need their own deployment pipeline. Instead the entire job application is copied into a `jobs` folder within the web site:

- `MySite\`
	- `app_data\`
		- `jobs\`
			- `continuous\`
				- `MyWorkerJob\`

The `MyWorkerJob` has to be _exactly the same_ as the `.exe` name. So `MyWorkerJob.exe` needs to be deployed to `app_data/jobs/continuous/MyWorkerJob`. Jobs can be scheduled as `continuous` or `triggered`. The Nimbus work needs to run continuously so it gets deployed to `App_Data\jobs\continuous`.

For Octopack to include the jobs, the job path just needs to be added to the project's `.nuspec`:

	<?xml version="1.0"?>
	<package xmlns="https://schemas.microsoft.com/packaging/2010/07/nuspec.xsd">
		...
		<files>
			...
			<file src="..\My.Worker\bin\release\**" target="app_data\jobs\continuous\MyWorker" />
		</files>
	</package>

The solution's build order needs to have the job's built before the web site (so the job's artifacts are available when building the site). You can do that by right-clicking the project, selecting 'Project Build Order', and adding the WebJob as a dependency of the web site.

That's pretty much all there is to it. When the solution is built and packed, the WebJob will be included in the `.nupkg` file, and Octopus will automatically patch the WebJob's `App.config` when configuring the site. It's also a good idea to logging using something like [Seq][getseq] to see the WebJob start up with the site.


## Resources &amp; further reading:

- [Introducing Windows Azure WebJobs - Scott Hanselman][int-windows-azure-webjobs]
- [Azure WebJobs are awesome and you should start using them right now! - Troy Hunt][azure-webjobs-are-awesome]
- [Run Background tasks with WebJobs - Azure][run-background-tasks-with-webjobs]
- [My stance on Azure Worker Roles - Rob Moore][my-stance-on-azure-worker-roles]
- [How to deploy Azure WebJobs - Amit Apple][how-to-deploy-azure-webjobs]
- [Deploying custom services as Azure Webjobs - Liam McLennan][mclennan-deploying-custom-services-as-azure-webjobs]


[nimbus]: https://nimbusapi.github.io/
[nimbus-mc]: https://www.nuget.org/packages/Nimbus.MessageContracts/
[nimbus-infra]: https://www.nuget.org/packages/Nimbus.InfrastructureContracts/
[azure-webjob]: https://azure.microsoft.com/en-us/documentation/articles/web-sites-create-web-jobs/
[int-windows-azure-webjobs]: https://www.hanselman.com/blog/IntroducingWindowsAzureWebJobs.aspx
[azure-webjobs-are-awesome]: https://www.troyhunt.com/2015/01/azure-webjobs-are-awesome-and-you.html
[run-background-tasks-with-webjobs]: https://azure.microsoft.com/en-us/documentation/articles/web-sites-create-web-jobs/
[my-stance-on-azure-worker-roles]: https://robdmoore.id.au/blog/2014/07/22/my-stance-on-azure-worker-roles/
[how-to-deploy-azure-webjobs]: https://blog.amitapple.com/post/74215124623/deploy-azure-webjobs/#.VbjV2W6qpBf
[hanselman-introducing-windows-azure-webjobs]: https://www.hanselman.com/blog/IntroducingWindowsAzureWebJobs.aspx
[mclennan-deploying-custom-services-as-azure-webjobs]: https://withouttheloop.com/articles/2015-06-23-deploying-custom-services-as-azure-webjobs/
[getseq]: https://getseq.net/