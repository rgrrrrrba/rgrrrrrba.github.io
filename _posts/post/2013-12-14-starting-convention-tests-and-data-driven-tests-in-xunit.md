---
title: Starting out with convention tests and data-driven tests in xUnit
layout: post
date: 2013-12-14
category: archived
---

I'm really interested in convention tests, and as I want to get into xUnit I thought I would try taking some patterns that I've used in NUnit and applying them in xUnit.

The test project I created while writing this post is [available on GitHub](https://github.com/becdetat/convention-tests-in-xunit).


## First steps

The first thing to notice is the xUnit has slightly different naming conventions compared to NUnit. This is a simple test in NUnit:

	public class ItShouldHaveTests
	{
		[Test]
		public void AndItDoes()
		{
			Assert.True(true);
		}
	}

The equivalent in xUnit:

    public class ItShouldHaveTests
    {
        [Fact]
        public void AndItDoes()
        {
            Assert.True(true);
        }
    }

So xUnit has `[Fact]`s while NUnit has `[Test]`s. Cool so far. Note that the class has to be public. In NUnit I would usually mark test fixtures with `[TestFixture]` and the class usually ends up `internal`, which Resharper's test runner will pick up. NUnit has been able to pick up public classes without the `[TestFixture]` decoration for the past couple of years IIRC but I've had mixed success with Resharper's test runner. Sometimes I would spend valuable seconds wondering why tests weren't running and looking dumb, so I would automatically add the attribute. xUnit doesn't have a `[TestFixture]` equivalent. It will just find all public classes that contain tests.


## Comparisons with NUnit

The xUnit Wiki has a nice [comparison page](https://xunit.codeplex.com/wikipage?title=Comparisons) showing differences between NUnit, MSTest and itself. 

Apart from the obvious `[Fact]` vs `[Test]` and not needing `[TestFixture]`, the big differences seem to be with how the two test frameworks handle fixture setup and teardown. NUnit uses `[SetUp]` and `[TearDown]` attributes to mark methods, while xUnit adds awesome to the mix by using the public constructor as the setup method. It just creates a new instance of the fixture class for each test. This completely obviates issues that have cropped up for me in NUnit around fixture state being shared between tests. Teardown is done by implementing `IDisposable`, in the `Dispose` method. I don't remember the last time I wrote a teardown method anyway, but using `IDisposable` is a really nice and obvious convention. Shared fixture state can be implemented with the `IUseFixture<T>` interface but I won't explore that here.


## A data-driven test

I used NUnit's data-driven tests to TDD a simple Fibonacci number generator (my favourite integer sequence). The implementation of the generator is [in the repo](https://github.com/becdetat/convention-tests-in-xunit/blob/master/src/MyApp.Shared/FibonacciGenerator.cs).

    [Test]
    [TestCase(0, 0)]
    [TestCase(1, 1)]
    [TestCase(2, 1)]
    [TestCase(3, 2)]
    [TestCase(4, 3)]
    [TestCase(5, 5)]
    [TestCase(6, 8)]
    [TestCase(7, 13)]
    [TestCase(8, 21)]
    [TestCase(16, 987)]
    public void TestNumber(int sequence, int result)
    {
        Assert.AreEqual(result, FibonacciGenerator.For(sequence));
    }

xUnit uses Theories for data driven tests, which sounds so cool. Interestingly, theories aren't part of xUnit out of the box. You need to install the `xunit.extensions` package. Apart from the naming differences everything is pretty much the same.

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(2, 1)]
    [InlineData(3, 2)]
    [InlineData(4, 3)]
    [InlineData(5, 5)]
    [InlineData(6, 8)]
    [InlineData(7, 13)]
    [InlineData(8, 21)]
    [InlineData(16, 987)]
    public void TestNumber(int sequence, int result)
    {
        Assert.Equal(result, FibonacciGenerator.For(sequence));
    }

xUnit uses some reflection magic to show the name of the parameters in the test runner output, so while NUnit shows `TestNumber(7, 13)`, xUnit shows `TestNumber(sequence: 7, result: 13)`.


## Theory data sources

`InlineData` is only one of the data sources that xUnit provides. There doesn't seem to be much documentation around the source types, so I'm working from the [acceptance tests in the xUnit source](https://github.com/xunit/xunit/tree/master/test/test.xunit1/xunit.extensions/DataTheories/AcceptanceTests).

### InlineData

As shown above, this is obviously on par with NUnit's `TestCase`. It takes a `params` array of objects and passes them into the test method.

### ClassData

This uses a class as the source for the test data. The data source class implements `IEnumerable<object[]>`.

    public class FibonacciTestUsingClassData
    {
        [Theory]
        [ClassData(typeof(FibonacciTestSource))]
        public void TestNumber(int sequence, int result)
        {
            Assert.Equal(result, FibonacciGenerator.For(sequence));
        }
    }

    class FibonacciTestSource : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
        	// ...
            yield return new object[] { 8, 21 };
            yield return new object[] { 16, 987 };
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

The test data still has to be an array of `object`, so I couldn't strongly type my test data. This is something I never liked about NUnit's data-driven tests, so it is disappointing that xUnit has the same limitation. However class data sources are obviously a great way of sharing test cases between fixtures.

### ExcelData

This is pretty cool. The data source can be an Excel spreadsheet. This looks like a great way to manage lots of test cases outside of code. I coded up an example, but the JET engine isn't on 64-bit Windows so it didn't work. This dependence on JET is a problem IMO, one which has been around since Windows Vista made 64-bit relatively mainstream back in 2007.

So I couldn't get to work, but I think this is how Excel driven tests would look:

	[Theory]
	[ExcelData(@"MyApp.Tests\FibonacchiTestData.xlsx", "select * from Data")]
	public void TestNumber(int sequence, int result)
	{
	    Assert.Equal(result, FibonacciGenerator.For(sequence));
	}

### PropertyData

`PropertyData` is very similar to `ClassData` but obviously on a property of the test class rather than a class of its own. This also looks closest to NUnit's `TestCaseSource`, which references a method that returns `IEnumerable<TestCaseData>`. xUnit doesn't have as much ceremony around the test data, although the enumerable still has return arrays of `object`s.

    public static IEnumerable<object[]> TestData
    {
        get
        {
        	// ...
            yield return new object[] { 8, 21 };
            yield return new object[] { 16, 987 };
        }
    }

    [Theory]
    [PropertyData("TestData")]
    public void TestNumber(int sequence, int result)
    {
        Assert.Equal(result, FibonacciGenerator.For(sequence));
    }


## A simple convention test

So it looks like `PropertyData` is the most appropriate for the simple convention test I'm going to put together.

This is going to be really basic. The convention I'm testing is:

> All classes that implement `IDomainObject` must have an empty constructor

This might be because those classes have to work with Entity Framework or nHibernate, or need to be deserialised in some way.

So the first part is defining the data source:

	public static IEnumerable<object[]> DomainObjectClasses
	{
		get
		{
			// ... returns Types
		}
	}

I like to think of this in terms of "get all types that inherit from `IDomainObject`". The `Type` class has an `IsAssignableFrom(Type)` method but for that to work I would have to invert the query, so something like:

    from t in [types] where typeof(IDomainObject).IsAssignableFrom(t)

That doesn't read very naturally so I like to invert the method with an extension method, and add some generic love to the mix:

    public static bool IsAssignableTo<T>(this Type fromType)
    {
        return typeof(T).IsAssignableFrom(fromType);
    }

**NOTE** Know what? This is included in Autofac. Loove Autofac.

I also need to exclude abstract types from the query, so that abstract types can take a parameter in their constructor but concrete implementations have to provide a default constructor. This excludes the interface itself as well. There are a number of interesting methods and properties given by the `Type` class.

The complete data source is this:

    public static IEnumerable<object[]> DomainObjectClasses 
    {
        get
        {
            return
                from type in typeof (IDomainObject).Assembly.GetTypes()
                where type.IsAssignableTo<IDomainObject>()
                where !type.IsAbstract
                select new [] { type };
        }
    }

The test itself is hot:

    [Theory]
    [PropertyData("DomainObjectClasses")]
    public void DomainObjectHasDefaultConstructor(Type type)
    {
        Assert.NotNull(type.GetConstructor(Type.EmptyTypes));
    }


## Er...

Fin.





