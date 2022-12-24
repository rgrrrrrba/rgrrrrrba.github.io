---
title: Simplicity - pattern matching in C#
layout: post
date: 2015-05-15
category: archived
---

*TL;DR;* Check out my awesome new library for [pattern matching in C# - Simplicity](https://www.nuget.org/packages/Simplicity) ([GitHub](https://github.com/becdetat/Simplicity)) or just copy the contents of the [single file](https://github.com/becdetat/Simplicity/blob/master/src/Simplicity/PatternMatching.cs) into your project for near-instant gratification.

Pattern matching is a method of transforming data in some way, similar to `map` (`.Select()` in LINQ) but closer to a `switch` statement in structure. It is a first class language construct in many functional languages including F#:

	let name = "Rebecca"

	let result =
		match name with 
		| "Fiona" -> "It's Fiona!"
		| "Rebecca" -> "it me!"
		| "Steve" -> "Steve you rascal!"
		| _ -> "I don't know this person"
		
	// result = "it me!"

That's as deep an explanation of pattern matching in F# you're going to get from me at this point, but this is conceptually similar to this `switch` construct in C#:

	var name = "Rebecca";
	string result;

	switch (name) {
		case "Fiona":
			result =  "It's Fiona!";
			break;
		case "Rebecca":
			result = "it me!";
			break;
		case "Steve":
			result = "Steve you rascal!";
			break;
		default:
			result = "I don't know this person";
			break;
	}

This is of course pretty average to read, relies on `break` for execution control, and isn't 'pure' since `result` is mutated during execution.

<aside class="pull-right well" style="width: 42ex">
	<p>I also found a couple of extant libraries that provide similar (and possibly more) functionality:</p>
	<ul>
		<li><a href="https://github.com/johansson/PatternMatching">johansson/PatternMatching</a></li>
		<li><a href="https://github.com/Patrickkk/FunctionalSharp">Patrickkk/FunctionalSharp</a></li>
	</ul>
</aside>

When I found out about pattern matching I wanted to write code the same way in C#. I found a great [article by Matt Podwysocki](https://codebetter.com/matthewpodwysocki/2008/09/16/functional-c-pattern-matching/) and adapted and extended the code into a single-file library called [Simplicity](https://github.com/becdetat/Simplicity). Install it [using NuGet](https://www.nuget.org/packages/Simplicity) (`install-package PatternMatching`) or just [copy the single file](https://github.com/becdetat/Simplicity/blob/master/src/Simplicity/PatternMatching.cs) into your project.

Now for the fun part. Here's the above example using my library. It adds an generic extension method called `Match()` which is the usual entry point. The match statement is built up using a fluent interface.

	var name = "Rebecca";
	var result = name.Match()
		.With(x => x == "Fiona", "It's Fiona!")
		.With(x => x == "Rebecca", "Hey it's me!")
		.With(x => x == "Steve", "Steve you rascal!")
		.Else("I don't know this person")
		.Do();

The result can be an action, taking the value as a parameter:

	var name = "George";
	var result = name.Match()
		.With(x => x == "Fiona", "It's Fiona!")
		.With(x => x == "Rebecca", x => string.Format("Hey it's {0}!", x))
		.With(x => x == "Steve", "Steve you rascal!")
		.Else(x => string.Format("I don't know {0}", x))
		.Do();
	
	// result = "I don't know George"

The `Else` value is optional but if it falls through without matching and there's no `Else` value it throws an `IncompletePatternMatchException`:

	var name = "Elton";
	var result = name.Match()
		.With(x => x == "John", "matched")
		.With(x => x == "Paul", "matched")
		.Do();

F# has a wicked type system that C# can't match, but I can set the output type to dynamic:

	var question = "meaning of life";
    var result = question.Match().WithOutputType<dynamic>()
        .With(x => x.Contains("roads"), "Blowing in the wind")
        .With(x => x.Contains("life"), 42)
        .Else("Ask again later")
        .Do();

    // result = (int)42

The `Do()` call evaluates the patterns against the value being matched on, but there's also an implicit cast operator to the output type that removes the need much of the time. When `total` is calculated, the `gstRate` match is implicitly cast to `decimal` from the pattern match type, which is `PatternMatchOnValue<string, decimal>`.

	var country = "NZ";
	var gstRate = country.Match()
		.With("AU", 0.1m)
		.With("NZ", 0.15m)
		.Else(0.0m);
	
	var total = 2300.0m * (1.0m + gstRate);
	
	// total = 2645.0m

All of the examples up until now have used the `Match()` generic extension method to apply the match statement to the value that gets called with `Match()`. I've also implemented matching without an input value using the static `PatternMatch.Match()` method. This lets you write a match statement that can do things like match on different values or methods, or close over a local value as below:

	var eggs = 2;
	var basket = PatternMatch.Match()
		.With(() => eggs == 0, "No eggs")
		.With(() => eggs == 1, "One egg")
		.With(() => eggs > 1, string.Format("{0} eggs", eggs))
		.Else("Invalid number of eggs");
	
	var twoEggs = basket.Do();
	eggs = 0;
	var zeroEggs = basket.Do();
	eggs = int.MinValue;
	var invalidEggs = basket.Do();

	// twoEggs = "2 eggs"
	// zeroEggs = "No eggs"
	// invalidEggs = "Invalid number of eggs"

There's also a `.ToFunc()` method that gets rid of the `.Do()` call by transforming the match statement into a `Func<TOut>`:

	var footwear = "boots";
	var intention = PatternMatch.Match()
		.With(() => footwear == "red slippers", "following the Yellow Brick Road")
		.With(() => footwear == "boots", "walking")
		.With(() => footwear == "these shoes", "I don't think so")
		.ToFunc();
	
	var walking = intention();
	footwear = "these shoes";
	var noWayJose = intention();

	// walking = "walking"
	// noWayJose = "I don't think so"

This is getting quite DRY but closing over a local mutable value will have the purists climbing the walls, myself included. So the final form goes back to processing an input, deferred until the match statement is executed. The syntax is a little verbose because C# can't infer the types of the lambdas without help, but I'm pretty happy with the result - a reusable `Func<TIn, TOut>` value that is defined declaratively with no flow control or mutated state:

	var processName = PatternMatch.Match<string, string>()
		.With(x => x.StartsWith("A"), "Starts with A")
		.With(x => x.StartsWith("B"), "Starts with B")
		.With(x => x.StartsWith("C"), "Starts with C")
		.With(x => x.StartsWith("D"), "Starts with D")
		.With(x => x.StartsWith("E"), "Starts with E")
		.With(x => x.StartsWith("F"), x => string.Format("{0} starts with F", x))
		.Else("Unknown")
		.ToFunc();
		
	var alfred = processName("Alfred");
	var fiona = processName("Fiona");
	var bec = processName("Bec");
	var xerces = processName("Xerces");

	// alfred = "Starts with A"
	// fiona = "Fiona starts with F"
	// bec = "Starts with B"
	// xerces = "Unknown"

Note that this isn't especially performant! The match statement is built up as a list of expressions that are then looped through naively when evaluated. This could probably be improved using some form of caching. [Submit a pull request!](https://github.com/becdetat/Simplicity)