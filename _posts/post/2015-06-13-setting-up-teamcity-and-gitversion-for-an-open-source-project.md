---
title: Setting up TeamCity and GitVersion for an open source project
layout: post
date: 2015-06-25
category: archived
---

Note that I'm using TeamCity 9.0 (build 32060) and GitVersion 2.0.1. These steps may be different in future versions. GitVersion seems to be slated for a 3.0 release very soon.


## Practice makes perfect

I'm setting up TeamCity using GitVersion for a new open source project that I want to deploy via NuGet. I've used TeamCity a few times to set up basic builds but I've never got [SemVer](https://semver.org/) working in a nice way before, so I thought this would be a nice opportunity to try [GitVersion](https://github.com/ParticularLabs/GitVersion).

The Release configuration is triggered manually to deploy the last built version to NuGet. It would probably be nicer to do this from Octopus but for now I'll just use TeamCity.


## Preparing the build agent

I've started out with an [Azure VM configured with TeamCity 9](set-up-teamcity-on-an-azure-instance-redux.html). I first installed GitVersion on my build agent using [Chocolatey](https://chocolatey.org/). Install Chocolatey using an elevated Powershell console:

	iex ((new-object net.webclient).DownloadString('https://chocolatey.org/install.ps1'))

Then install GitVersion:

	cinst gitversion.portable

You may also have to install [msysgit](https://msysgit.github.io/). TeamCity has its own Git client built into the server but GitVersion needs to be able to access the Git history on the agent, which means the VCS checkout has to happen on the agent (configured below). Reboot the machine once this is done to make sure everything is on the path.


## TeamCity setup

Now start setting up the project in TeamCity.

1. Under _Administration_, create a new project
2. Create a build configuration called `CI`
3. In the VCS roots, just paste the HTTPS clone URL from Github into the _Repository URL_. Change the _Authentication method_ to _Password_ and enter your Github username and password. *Note:* I'm using HTTPS because GitVersion uses LitGit2Sharp, which doesn't support SSH at the time of writing :'-( (at least GitVersion doesn't support it AFAIK)
4. Click _Create_

Now create the first build step for GitVersion. I used [Jake Ginnivan's post on his typical TeamCity build setup](https://jake.ginnivan.net/blog/2014/07/09/my-typical-teamcity-build-setup/) as a guide.

1. Click _Add build step_
2. Select _Command Line_ as the _Runner type_
3. Change the _Run_ value to _Executable with parameters_
4. _Command executable_ is `GitVersion`
5. _Command parameters_ is `. /updateAssemblyInfo /assemblyVersionFormat MajorMinorPatch /output buildserver`

Note that there is a space between the `.` and the `/updateAssemblyInfo`:

![](https://i.imgur.com/stM7oSn.png)

*Note* with the 3.0 release of GitVersion the command parameters may be able to be removed in favour of a `GitVersionConfig.yaml` configuration file. Stay tuned.

Now create another build step to build the solution.

1. Under _Build Steps_ click _Auto-detect build steps_, which scans the repository and finds things to build. In this case it identified a _Visual Studio (sln)_ build step which just builds `PROJECT_NAME.sln`. Select the step then clicked _Use selected_.
2. Under _Version Control Settings_, change the _VCS checkout mode_ from _Automatically on server_ to _Automatically on agent_. This will check out the repository on the agent, which means the `.git` folder will exist and GitVersion should work properly.
3. Also check the _Clean build_ option.

To run GitVersion before building the solution:

1. Reorder build steps
2. Drag _GitVersion_ above _Visual Studio (sln)_
3. _Apply_

Now select _Triggers_ and _Add a new trigger_. Select _VCS Trigger_ then _Save_.


## I wonder what happens if I press this...

Running the configuration worked for me at this point, resulting in a build versioned `0.1.0+21` (there were 22 commits, so that's 21 commits since version `0.0.0`). If you get an error about not being able to find `GitVersion` or `git.exe` make sure the build agent has rebooted and that GitVersion and Git are on the path.

Next you can add a step to run tests. I'm using xUnit. This is just a _Command Line_ runner with the following custom script:

	packages\xunit.runner.console.2.0.0\tools\xunit.console.exe src\YOUR_PROJECT.Tests\bin\Release\YOUR_PROJECT.Tests.dll


## Releasing to NuGet

First you need to add a `nuspec` file alongside the library being released (add it to the project in Visual Studio) and push it up so TeamCity can see it. For example, `.\src\frankenwiki\Frankenwiki.nuspec`:

	<?xml version="1.0" encoding="utf-8"?> 
	<package> 
		<metadata> 
			<id>frankenwiki</id> 
			<title>Frankenwiki</title>
			<version>0.0.0</version> 
			<authors>Rebecca Scott</authors>
			<description>Markdown based statically generated wiki engine</description> 
			<language>en-US</language>
			<licenseUrl>https://github.com/frankenwiki/frankenwiki/blob/master/LICENSE.md</licenseUrl>
			<releaseNotes>https://github.com/frankenwiki/frankenwiki/releases</releaseNotes>
			<projectUrl>https://frankenwiki.com</projectUrl>
		</metadata>
		<files>
			<file src="bin\release\Frankenwiki.dll" target="lib\net451"/>
		</files>
	</package>

The easiest way to generate the NuGet package (`.nupkg`) seems to be [Octopack](https://docs.octopusdeploy.com/display/OD/Using+OctoPack). Install Octopack to the library being released and push the changes up to the repository. Now edit the CI configuration and in the _Visual Studio (sln)_ step  (the actual build step) show the advanced options and add this to the _Command line parameters_:

	/p:RunOctoPack=true

Now when a build happens, OctoPack will create the `.nupkg` file named something like `Frankenwiki.0.1.0.nupkg`. This package gets consumed in the next step. Trigger a build now to make sure everything works and the package is created as an artifact.

Create a new build configuration called _Release_ or _Promote_ or _Fly, my pretties, ah hahahaha!_:

1. Attach it to the existing VCS root created above
2. Don't use any of the detected build steps, just _configure build steps manually_
3. Pick _NuGet Publish_ as the runner type
4. In _Packages_, use a wildcard to specify the `.nupkg` file (so it is independent of the version). Eg. `Frankenwiki.*.nupkg`.
5. Paste in your [NuGet API key](https://docs.nuget.org/Create/creating-and-publishing-a-package#publishing-using-nuget-command-line)
6. Save

The last few steps are directly based on [Jake's post](https://jake.ginnivan.net/blog/2014/07/09/my-typical-teamcity-build-setup/). Go to _Build Features_ to set up labelling:

1. _Add build feature_
2. Select _VCS Labelling_
3. Select the existing VCS root
4. The default labelling pattern is `build-%system.build.number%`. Take out the `build-` part so it is just `%system.build.number%`
5. Save

Now go to _Dependencies_ to set up the build chain:

1. Add a new snapshot dependency
2. Pick the CI build configuration and any other configurations that run before the Release configuration
3. _Do not run new build if there is a suitable one_ and _Only use successful builds from suitable ones_ should both be ticked, if they aren't then tick them
4. Save
5. Add a new artifact dependency
6. Change _Get artifacts from_ to _Build from the same chain_
7. In _Artifacts rules_ use the same wildcard specification as above to select the `.nupkg` file
8. Check _Clean destination paths before downloading artifacts_
9. Save

Go to _General Settings_ and show advanced options. Change the _Build number format_ to the following:

	%dep.MyProject_Ci.build.number%

where `MyProject_Ci` is the build configuration ID of the CI step. Once you type in `%dep` it will suggest the available configurations.

Now you should be able to trigger a Release, which should successfully publish the package to NuGet! If everything works.


## Tell TeamCity to build feature branches and tags

TeamCity can build feature branches and tags. This lets GitVersion version feature branches to reduce surprises.

1. In _Project Settings_ select _VCS Roots_
2. Select the single VCS root created above
3. Make sure the advanced options are visible
4. In _Branch specification_, enter `+:refs/heads/*` ([Working with Feature Branches](https://confluence.jetbrains.com/display/TCD8/Working+with+Feature+Branches))
5. Check _Enable to use tags in the branch specification_
6. Save


## Using GitVersion

Jake's post about [Simple Versioning and Release Notes](https://jake.ginnivan.net/blog/2014/05/25/simple-versioning-and-release-notes/) has some great info about changing the version but a good one seems to be using a feature branching strategy.

Push a branch with the new version number in the name. For example:

	git checkout -b version-0.3.0
	git commit ...
	git push

Note that just pushing the branch won't trigger the branch build, there needs to be a non-empty commit.

![Feature branches building in TeamCity](https://i.imgur.com/SGxLhKx.png)

You can see that TeamCity has built a new release from the feature branch and GitVersion has versioned it at `0.3.0-beta.1+4`. Subsequent commits to this feature branch will increment the build number (eg. `0.3.0-beta.1+5`). When the feature branch is merged into master, the master version will become `0.3.0` and you can just manually run the Release configuration to deploy to NuGet.


## Extra tricks and gotchas


### Commits with number can have unexpected results

Don't add a branch or a commit with a version number in it unless you expect it to bump the version number. I merged a branch called `change-to-dotnet-4.5.1` which GitVersion helpfully interpreted as a version bump to `4.5.1`. I had to fix this by rewriting the commit comments to say `4dot5dot1`.


### Check the versioning scheme

If GitVersion report a particular version but Octopack generates nuspec files with a different version, check in the `AssemblyInfo.cs` file for a different version in the `AssemblyVersion` and `AssemblyFileVersion` attributes. This can be due to the versioning scheme, which can be set using the `/assemblyVersionFormat` parameter as above (or in `GitVersionConfig.yaml` once it is supported by GitVersion).

