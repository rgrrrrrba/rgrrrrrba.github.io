---
title: Try This One Simple Trick To Drastically Reduce Method Test Complexity And Reveal Intent Using Default Arguments
layout: post
date: 2013-12-12
category: post
---

### YOU WON'T BELIEVE YOUR EYES

How search engine. Such marketing. Wow.

A trap I fall when writing tests is big arrange or acts to test a single thing. Here's a trivial example:

	[Test]
	public void Member_first_name_is_updated_correctly()
	{
		var member = new Member(....);

		member.Modify("First", "Last", "0400123456", new DateTime(2000, 1, 1));

		Assert.That(member.FirstName, Is.EqualTo("First");
	}

The member instantiation could be taken care of using an [object mother pattern](http://martinfowler.com/bliki/ObjectMother.html) but the complexity of the `Modify()` invocation (passing a dummy last name, phone number, etc) hides the intent of the test. That is, given a member, *when modifying the first name*, the first name matches what it was modified to.

Revealing that intent can be done by creating a proxy method over `member.Modify`, which uses default parameters to allow the test to express the *when* part more succinctly.

	[Test]
	public void Member_first_name_is_updated_correctly()
	{
		var member = new Member(...);

		ModifyMember(member, first: "First");

		Assert.That(member.FirstName, Is.EqualTo("First"));
	}

	static void ModifyMember(Member member, string first = "first", string last = "last", string phone = "0400123456", DateTime dateOfBirth = default(DateTime))
	{
		if (dateOfBirth == default(DateTime))
			dateOfBirth = new DateTime(2000, 1, 1);
		member.Modify(first, last, phone, dateOfBirth);
	}

Now the test only reveals the subject of the test - the first name.

The `dateOfBirth` parameter is interesting. Default parameters have to be compile-time constants. `new DateTime(2000, 1, 1)` is mutable, ie. not a constant. So we have to pass in a magic value (`default(DateTime)`) which is then converted to the default value for the `member.Modify()` call. So to actually pass in `default(DateTime)` another proxy method is needed:

	static void ModifyMemberWithDefaultDateOfBirth(Member member) 
	{
		member.Modify("first", "last", "0400123456", default(DateTime));
	}

which is annoying, but at least the test is nice.

These proxy methods would quickly need to be used in multiple test fixtures, so they could be moved to the object mother. They could be made into extension methods, but that could make things more confusing as the tests would appear to operate on the member in a way that isn't supported in the application itself. If indeed your tests are intended to be documentation for the application. YMMV.

HT to my main man [Rob Moore](http://robdmoore.id.au/) [@robdmoore](https://twitter.com/robdmoore) for showing me this ONE SIMPEL TRICK.


