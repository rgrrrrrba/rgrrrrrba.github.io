---
title: How to run DbUp in an AppHarbor ASP.NET MVC application
layout: post
date: 2013-08-31
category: archived
---

[Pawel Pabich](https://twitter.com/PawelPabich) has a good way to [run DbUp as part of AppHarbor's build process](https://www.pabich.eu/2011/03/automated-database-deployments-to.html) which looks interesting but I wanted to run the migrations manually. So here is a quick recipe for setting up [DbUp](https://dbup.github.io/) with a web hook.

Something important to note is that there is no security around this. It relies on an obfuscated controller path, so it is only really useful for bootstrapping an early project. The controller should be moved behind security as soon as possible.

I'm assuming that DbUp has been set up as per my post on [resetting the world](/reset-the-world.html). Specifically, the DbUp DLL should have an upgrader that looks like this:

	namespace ResetTheWorld
	{
	    public class DatabaseUpgrader
	    {
	        private const string ConnectionStringName = "xyz";

	        public static void Upgrade(IUpgradeLog upgradeLog = null)
	        {
	            var connectionString = ConfigurationManager.ConnectionStrings[ConnectionStringName].ConnectionString;
	            var upgrader = DeployChanges.To
					.SqlDatabase(connectionString)
					.WithScriptsEmbeddedInAssembly(Assembly.GetAssembly(typeof (DatabaseUpgrader)))
					.LogTo(upgradeLog ?? new ConsoleUpgradeLog())
					.Build();

	            var result = upgrader.PerformUpgrade();

	            if (!result.Successful)
	            {
	                throw new Exception("DB Upgrade failed", result.Error);
	            }
	        }

	        public static void SetConnectionString(string connectionString)
	        {
				typeof (ConfigurationElementCollection).GetField("bReadOnly", BindingFlags.Instance | BindingFlags.NonPublic)
					.SetValue(ConfigurationManager.ConnectionStrings, false);
				ConfigurationManager.ConnectionStrings.Add(new ConnectionStringSettings(ConnectionStringName,
					connectionString,
					"System.Data.SqlClient"));
	        }
	    }
	}

I've just created a controller with a long random name:

	public class njvfdklnvfjdkslndsieController : Controller
	{
		public ActionResult UpdateTheWorld()
		{
			var log = new HtmlLog();

			DatabaseUpgrader.Upgrade(log);

			ViewBag.Log = log.ToString();

			return View();
		}
	}

The `HtmlLog` is a basic replacement for DbUp's console log that just builds up a HTML string:

	class HtmlLog : IUpgradeLog
	{
		readonly StringBuilder _stringBuilder = new StringBuilder();
		public override string ToString()
		{
			return _stringBuilder.ToString();
		}

		public void WriteError(string format, params object[] args)
		{
			_stringBuilder.AppendFormat("<p class='error'>" + format + "</p>", args);
		}

		public void WriteInformation(string format, params object[] args)
		{
			_stringBuilder.AppendFormat("<p class='information'>" + format + "</p>", args);
		}

		public void WriteWarning(string format, params object[] args)
		{
			_stringBuilder.AppendFormat("<p class='warning'>" + format + "</p>", args);
		}
	}

And the view for the `UpdateTheWorld()` action just writes out the log, with added colours:

	@model dynamic

	@{
		ViewBag.Title = "UpdateTheWorld";
	}

	<style>
		p.error{color:red}
		p.information{color:#000}
		p.warning{color:orange}
	</style>

	<h1>UpdateTheWorld</h1>
	@Html.Raw(ViewBag.Log)

Now browsing to `https://<app>.apphb.com/njvfdklnvfjdkslndsie/UpdateTheWorld` will update the existing database. A `ResetTheWorld` action could be added which drops the database before running the scripts.

