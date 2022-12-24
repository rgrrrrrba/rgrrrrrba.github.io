---
title: Convention tests - Controller actions require an attribute
layout: post
date: 2013-08-31
category: archived
---

I looove convention tests. The idea is you have some assumption about the system that you want to test and enforce. In this case, I want all controller actions (bar some exceptions) to be decorated with the `AuthorizeAttribute`.

I'm using [NUnit](https://nunit.org/) for the tests and [Shouldly](https://shouldly.github.io/) to help with assertions. I'm also using [Autofac](https://autofac.org/), which shouldn't affect the tests except that it gives me a nice extension method.


### Test cases in NUnit
NUnit has a concept of [test cases](https://nunit.org/index.php?p=testCase&r=2.6.2) where a [test case source](https://nunit.org/index.php?p=testCaseSource&r=2.6.2) provides test casees to a test method. Each test case is executed in the test method as an individual test.

Here's a simple example of a test cases. It tests integers from 1 to 10 and fails on any odd values:

    public class TestCaseExample
    {
        [Test]
        [TestCaseSource("TestCases")]
        public void TheNumberShouldBeEven(int i)
        {
            (i%2).ShouldBe(0);
        }

        public IEnumerable<TestCaseData> TestCases()
        {
            return Enumerable.Range(1, 10)
                             .Select(i => new TestCaseData(i))
                ;
        }
    }

Note that the `TestCases()` method needs to be public and the `TestCaseSource` attribute allows NUnit to find the test case source by name.


### Finding all controller actions
Convention tests generally use reflection to build up a set of test cases. In this case I want all controller actions. First I find all classes that are `Controller`s. Then I find all methods in those classes that return something that is an `ActionResult`. It is a reasonable assumption that all of these methods are the controller actions that I'm interested in:

	public IEnumerable<TestCaseData> TestCases()
	{
		return typeof (MainController).Assembly
			.GetExportedTypes()
			.Where(t => t.IsAssignableTo<Controller>())
			.Where(t => !t.IsAbstract)
			.SelectMany(t => t.GetMethods())
			.Where(m => m.ReturnType.IsAssignableTo<ActionResult>())
			.Select(m => new TestCaseData(m).SetName(
				string.Format("{0}.{1}", m.DeclaringType.FullName, m.Name)))
			.ToArray();
	}

`typeof (MainController).Assembly` gives the assembly that contains the controllers. `GetExportedTypes()` is all types that are visible outside the assembly.

`.Where(t => t.IsAssignableTo<Controller>())` limits the classes to those that subclass `Controller`. `Type.IsAssignableTo` is an extension method provided by Autofac which just inverts `Type.IsAssignableFrom`. This line could be replaced with `.Where(t => typeof(Controller).IsAssignableFrom(t))`.

`.Where(t => !t.IsAbstract)` ignores any classes that are abstract. We're only interested in controllers that could be instantiated. Any methods declared in an abstract base type that don't pass the convention test should be picked up when testing subclasses of the abstract base type.

`.SelectMany(t => t.GetMethods())` selects all of the methods (`MethodInfo`) exposed by the controllers.

`.Where(m => m.ReturnType.IsAssignableTo<ActionResult>())` limits the methods to those that return a subclass of `ActionResult`. Getting close now ;-)

Finally each method is bundled in to a `TestCaseData` instance which allows setting the name of the test case. `TestCases()` could just return `IEnumerable<MethodInfo>` but finding failing test sources would be a problem.


### Testing the cases
Ease test case is tested seperately. All that needs to be done is check that the method is decorated with the required attribute.

	[Test]
	[TestCase("TestCases")]
	public void MethodShouldHaveAuthorizeAttribute(MethodInfo methodInfo)
	{
		methodInfo
			.GetCustomAttributes<AuthorizeAttribute>()
			.ShouldNotBeEmpty();
	}

### Ignoring some controllers

I have a `LogInController` which I wish to ignore, so that anybody can at least see the log in page:

	public IEnumerable<TestCaseData> TestCases()
	{
		return typeof (MainController).Assembly
			.GetExportedTypes()
			.Where(t => t.IsAssignableTo<Controller>())
			.Where(t => !t.IsAbstract)
			
			// Ignore some types:
			.Where(t => !IsIgnored(t))

			.SelectMany(t => t.GetMethods())
			.Where(m => m.ReturnType.IsAssignableTo<ActionResult>())
			.Select(m => new TestCaseData(m).SetName(
				string.Format("{0}.{1}", m.DeclaringType.FullName, m.Name)))
			.ToArray();
	}

	readonly Type[] _ignoredTypes = new[] 
		{
			typeof(LogInController) 
		};

	bool IsIgnored(Type type)
	{
		return _ignoredTypes.Contains(type);
	}

