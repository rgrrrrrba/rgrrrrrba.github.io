---
title: A convention test for projects referencing other project references instead of NuGet packages
layout: post
date: 2015-01-17
category: archived
---

Earlier this week I was running into problems when adding a solution to a CI build server. Although there were no problems running the solution locally, the build server was complaining about not being able to resolve a reference for a project.

The solution was using NuGet package restore, which I happen to prefer over checking in the `packages` folder (at least for smaller projects with few developers), but I was confident that the packages were being restored correctly, including the unresolved reference.

The problem was that the project was referencing an assembly from the `/bin/debug` folder of another project, rather than the project referencing the correct NuGet package. This usually happens when using something like Resharper to automatically add a reference. The fix itself is easy:

1. Delete the reference from the project
2. Right-click the solution
3. Select 'Manage NuGet Packages for Solution'
4. 'Manage' the package
5. Add the project to the package:

![](https://i.imgur.com/Zr5S9VX.png)

What I really want to do is add a convention test to make sure this doesn't happen again. Why? Because this happens fairly infrequently, but when it does it can be hard to diagnose because it could potentially be caused by a number of things. In my experience, most of the time it is caused by a bad reference. Nevertheless I always seem to burn too much time figuring it out. In my opinion, the ROI of this convention test will probably make it worthwhile.


## Turns out

It's a bit tricky to get to the projects in a solution file. I didn't want to waste too much time in the internals of the build system so I found an [answer on Stack Overflow](https://stackoverflow.com/a/4634505/149259) that includes two wrapper classes for getting the solution, then iterating on the projects. The wrapper classes can be copied from [this gist](https://gist.github.com/becdetat/9a5a336d82b51ac0b564).

You'll need to add a reference to `Microsoft.Build`. Some of the classes that are used are actually deprecated, but this should work for long enough to get a good return on this test. The `Solution` wrapper class reads a `.sln` file and exposes a list of `SolutionProject` instance. Each `SolutionProject` exposes some of the properties of the project within the solution including the relative path, which I use to build a set of `Microsoft.Build.Project` instances for the convention test.


## Test cases

I'm using NUnit, so my [test cases](https://www.nunit.org/index.php?p=testCaseSource&r=2.5) come from a public method that returns an enumeration of `TestCastData` instances:

    public IEnumerable<TestCaseData> AllProjects
    {
        get
        {
            var solution = new Solution("../../../../MySolution.sln");
            var allProjects = solution.Projects
                .Where(x => x.RelativePath != ".nuget")
                .Where(x => x.ProjectName != "Microsoft.Build.Evaluation.Project")
                .ToArray();
            var allProjectNames = allProjects.Select(x => x.ProjectName).ToArray();
            
            return allProjects.Select(x =>
            {
                var project = new Project("../../../../" + x.RelativePath);
                var testCase = new TestCaseData(project, allProjectNames);
                testCase.SetName(x.ProjectName);

                return testCase;
            });
        }
    }

This:

1. Opens the solution file (as an instance of the above `Solution` wrapper). I've hard-coded the relative path to the solution because there's no need to get fancy - the tests are running in `src/MyProject/bin/[debug|release]/` relative to the solution file. If you don't keep the projects in a `/src` subdirectory then take out one of the `../` bits.
2. Get all of the projects (as an instance of the above `SolutionProject` wrapper), except for `.nuget` and `Microsoft.Build.Evaluation.Project`, which are included in the solution as project references.
3. Select out all the names of the projects. This is passed into each `TestCaseData` for comparison in the actual test.
4. Build up and return the `TestCaseData` enumeration:
	1. Construct a new `Microsoft.Build.Project` instance using the relative path. Note that this hasn't been tested with solution folder (it would _probably_ work because I would hope that the relative path includes the solution folder).
	2. Build a new `TestCaseData` instance with the project and the list of project names build up above.
	3. Set the name of the test case to the name of the project.


## Bangarang

That's a [Hook](https://www.imdb.com/title/tt0102057/) reference, not some 'popular' EDM song.

I'm using Shouldly for the assertion.

    [Test, TestCaseSource("AllProjects")]
    public void ProjectShouldNotReferenceAssembliesInOtherProjects(Project project, string[] allProjectNames)
    {
            var startsWithProjectName = new Func<string, string, bool>((x, projectName) => x.StartsWith("..\\" + projectName + "\\"));
            var isReferenceInAnotherProject = new Func<string, bool>(x => allProjectNames.Any(projectName => startsWithProjectName(x, projectName)));

            var badReferences = from projectItem in project.GetItems("Reference")
                                from metaData in projectItem.Metadata
                                let reference = metaData.EvaluatedValue
                                where isReferenceInAnotherProject(reference)
                                select reference;

            badReferences.ShouldBeEmpty();
    }

This was jammed together in LinqPad, but it's pretty straightforward. The first two lines set up some helpers to simplify the query. Then the bad references are determined by finding the references in the project, then checking if the reference is in another project using a fairly naive path check.

Failures look like this, showing the bad reference in the `Tests` assembly:

![](https://i.imgur.com/Hkc6y9a.png)

Viz.

<iframe width="420" height="315" src="//www.youtube.com/embed/E2VCwBzGdPM" frameborder="0" allowfullscreen></iframe>


