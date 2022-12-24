---
title: A short, executable rant on why I dislike object initialization syntax
layout: post
date: 2015-05-22
category: archived
---

	void Main()
	{
		// Why I ~~Hate~ Dislike Object Initialization Syntax
		// (executable in LinqPad)
		
		// There are three reasons why I despise initialization syntax for all but the most trivial
		// applications. The first two are fairly common and obvious:

		// 1. By design, a class built for object initialization syntax has to have public setters,
		// meaning that the resulting object is mutable.

		// 2. Because a constructor isn't used, there is no way for business rules or domain
		// invariants to be enforced. It is trivial to miss a value or set a value to something
		// that breaks a business rule, which can be difficult and annoying to debug and prevent.

		// The third reason is a bit less common and seems to defy expectations. It involves using
		// array initialization syntax inside of an object initializer.
		
		// This throws a null reference exception:
		
		//var brokens = new Brokens { Ints = { 1, 2, 3 } };
		
		// This is because Ints isn't initialised, and array initialization syntax is just 
		// syntactic sugar for foo.Add(1). You can see this if you try to declare your collection
		// as a straight-up array:
		
		//var brokensWithArray = new BrokensWithArray { Ints = { 1,2,3 } };
		// Build error: Cannot initialize object of type 'int[]' with a collection initializer
		
		// To get the same syntax to work, the class must initialize Ints in the
		// default constructor (a non-default constructor won't work):
		
		var works = new Works { Ints = { 1, 2, 3 } };
		
		// But as far as the consumer is concerned, Brokens and Works are equivalent (they have
		// matching public interfaces). This means hours of fun debugging!
		
		// This syntax also works, by initializing the list before adding the values:
		
		var worksUsingBrokens = new Brokens { Ints = new List<int>() { 1, 2, 3 } };
		
		// This also works with a struct:
		
		var brokensStruct = new BrokensStruct { Ints = new List<int>() { 1, 2, 3 } };
		
		// But since structs can't have parameterless public constructors, they can never use
		// the simpler object initialization syntax:
		
		//struct WorksStruct {
		//	public List<int> Ints { get; set; }
		//	public WorksStruct() {
		//		Ints = new List<int>();
		//	}
		//}
		// Build error: Structs cannot contain explicit parameterless constructors

		// The moral of the story: In C#, prefer using an explicit, parameterised constructor
		// over object initialization syntax. If you need a default constructor (eg. for
		// serialization), mark it with [Obsolete] to indicate your deep dissatifaction with
		// the code you have been forced to write.
	}

	// Define other methods and classes here
	class Brokens {
		public List<int> Ints { get; set; }
	}

	class Works {
		public List<int> Ints { get; set; }
		
		public Works() {
			Ints = new List<int>();
		}
	}

	class BrokensWithArray {
		public int[] Ints { get; set; }
	}

	struct BrokensStruct {
		public List<int> Ints { get; set; }
	}
