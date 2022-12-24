---
title: Reset The World!
layout: post
date: 2013-08-31
category: archived
---

Resetting the world refers to resetting a build environment (usually just a database) to a known state. This could include running SSIS scripts to migrate data from existing systems, creating web sites, or setting up dependent services. For a simple system or a system in its infancy it usually just refers to creating or upgrading a database for development, and that is what I'm going to show now. There will be two execution paths here - the first drops any existing database and recreates it from the scripts, and the second just executes any new scripts on an existing database. This technique is taken from several projects that I've worked in at Readify. The scripts are run in a console - Powershell if you know what's good for you :trollface:

[DbUp](https://dbup.github.io/) is a fantastic tool for running scripts against a database in a known, repeatable way. In the past I've used a Ruby script to run my migrations but DbUp allows much more integration with the application.

My DbUp scripts are contained in a DLL which is called by the reset/update scripts in development. My database upgrader looks like this:

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

The `SetConnectionString` method is used by the reset script below. `"xyz"` is the name of the connection string to use as defined in the `app.config` or `web.config`. This gives a default execution mode that doesn't rely on running the scripts manually. `ConsoleUpgradeLog` is part of DbUp and an alternative impementation can be passed in. Both of these features will be used when [running the DbUp scripts on AppHarbor](/dbup-in-appharbor.html).

The script to upgrade an existing database - `UpdateTheWorld.bat` - is trivial, it just calls the reset script with a flag:

	@echo off
	powershell -ExecutionPolicy Bypass -File ResetTheWorld.ps1 -UpdateOnly

The `ResetTheWorld.ps1` script is tha bomb. `$SqlServer` needs to point to the local server, `$Database` is the name of the database on the server, and `$DataProjectName` is the name of the project that has DbUp, the `ResetTheWorld.DatabaseUpgrader` class (above) and the migration scripts. `$DataProjectPath` is the path to where the data project is stored, since I use a convention of all projects being in a `\src` subpath.

	param([switch]$UpdateOnly)

	# Configure these to point to your local database, server and script dll:
	$SqlServer= "(local)"
	$Database = "NameOfDatabase"
	$DataProjectName = "Project.Data"
	$DataProjectPath = "src\$DataProjectName"

	Write-Host -ForegroundColor Green "Building Data Project"
	. C:\Windows\Microsoft.NET\Framework\v4.0.30319\msbuild $DataProjectPath\$DataProjectName.csproj /verbosity:quiet /nologo /property:Platform=AnyCpu
	if(!$?) { exit 1; }

	[System.Reflection.Assembly]::LoadWithPartialName("Microsoft.SqlServer.SMO") | out-null;
	if(!$?) { 
		throw "Exception loading SMO";
	}

	$dataAssembly = [System.Reflection.Assembly]::LoadFrom("$DataProjectPath\bin\Debug\$DataProjectName.dll") | out-null;
	if(!$?) { throw "Error loading data dll"; }

	$SmoServer = New-Object Microsoft.SqlServer.Management.Smo.Server($SqlServer);

	if($UpdateOnly -eq $False)
	{
		if ($SmoServer.Databases[$Database] -eq $null)  
		{  
			Write-Host -ForegroundColor Yellow  "Database does not exist";
		} 
		else	
		{
			Write-Host -ForegroundColor Green  "Dropping database $Database on $SqlServer";
			$SmoServer.KillDatabase($Database);
			Write-Host -ForegroundColor Green  "Dropped";
		}

		Write-Host -ForegroundColor Green "Recreating Database"
		$databaseObj = New-Object ('Microsoft.SqlServer.Management.Smo.Database') -argumentlist $SmoServer, $Database;
		$databaseObj.Create();
		if(!$?) { exit 1; }
	}

	Write-Host -ForegroundColor Green "Setting Connection String"

	$connectionString = "Data Source=$SqlServer;Initial Catalog=$Database;Trusted_Connection=True;MultipleActiveResultSets=True"
	Write-Host -ForegroundColor DarkGray "Connection String:"
	Write-Host -ForegroundColor DarkGray "$connectionString"
	[ResetTheWorld.DatabaseUpgrader]::SetConnectionString($connectionString);
	if(!$?) { exit 1; }

	Write-Host -ForegroundColor Green "Updating Database"
	[ResetTheWorld.DatabaseUpgrader]::Upgrade();
	if(!$?) { exit 1; }


