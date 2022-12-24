---
title: Class and method invariants and validation
layout: post
date: 2015-02-24
category: archived
---

I was chatting to my man [Tod](https://twitter.com/todthomson) and we came up with a cool pattern for validating both class and method invariants in a way that seems to scale.

Given a simple class `Person`, with some methods for updating from another `Person` and updating just the name:

	public class Person
	{
		public string Name { get; private set; }
		private int _age;

		public Person(string name, int age)
		{
			Name = name;
			_age = age;
		}

		public void UpdateFrom(Person person)
		{
			Name = person.Name;
			_age = person._age;
		}

		public void UpdateName(string name)
		{
			Name = name;
		}
	}

The private `_age` is just to demonstrate a private field.

First add some validation on the constructor. I'm using my [Check](https://github.com/becdetat/check) library but this could be implemented in any way that throws an exception if the rule isn't set.

	public Person(string name, int age)
	{
		// method invariants
		Check.That(() => name).IsNotNullOrEmpty();
		Check.That(() => age >= 0);

		Name = name;
		_age = age;
	}

This is what I'm calling a _method invariant_ - a set of rules that validate the input to a method.

We also want to validate the state of a person at the end of the method.

	static void Validate(Person person)
	{
		Check.That(() => person.Name).IsNotNullOrEmpty();
		Check.That(() => person._ge >= 0);
	}

This should happen at the end of each method that changes the state of the person. For example, the constructor becomes:

	public Person(string name, int age)
	{
		// method invariants
		Check.That(() => name).IsNotNullOrEmpty();
		Check.That(() => age >= 0);

		Name = name;
		_age = age;

		// validate myself
		Validate(this);
	}

The `UpdateName()` method needs to do the same thing:

	public void UpdateName(string name)
	{
		Check.That(() => name).IsNotNullOrEmpty();

		Name = name;

		Validate(this);
	}

The `UpdateFrom()` method can validate the entire incoming person:

	public void UpdateFrom(Person person)
	{
		Validate(person);

		Name = person.Name;
		_age = person._age;

		Validate(this);
	}

Shweet.