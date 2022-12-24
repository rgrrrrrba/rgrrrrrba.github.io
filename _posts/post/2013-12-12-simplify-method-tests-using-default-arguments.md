---
title: Try This One Simple Trick To Drastically Reduce Method Test Complexity And Reveal Intent Using Default Arguments
layout: post
date: 2013-12-12
category: archived
---

### YOU WON'T BELIEVE YOUR EYES

How search engine. Such marketing. Wow.

A trap I fall into when writing tests is big 'arrange' or 'act' sections to test a single 'thing'. Here's a trivial example:

	[Test]
	public void Member_first_name_is_updated_correctly()
	{
		var member = new Member(....);

		member.Modify("First", "Last", "0400123456", new DateTime(2000, 1, 1));

		Assert.That(member.FirstName, Is.EqualTo("First");
	}

The member instantiation could be taken care of using an [object mother pattern](https://martinfowler.com/bliki/ObjectMother.html) but the complexity of the `Modify()` invocation (passing a dummy last name, phone number, etc) hides the intent of the test. That is, given a member, *when modifying the first name*, the first name matches what it was modified to.

Revealing that intent can be done by creating a proxy method over `member.Modify`, which uses default parameters to allow the test to succinctly express the *when*.

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

The `dateOfBirth` parameter is interesting. Default parameters have to be compile-time constants. `new DateTime(2000, 1, 1)` is mutable, i.e. not a constant. We default to a magic value (`default(DateTime)`) which is then converted to the default value for the `member.Modify()` call. So to actually pass in `default(DateTime)` another proxy method is needed:

	static void ModifyMemberWithDefaultDateOfBirth(Member member) 
	{
		member.Modify("first", "last", "0400123456", default(DateTime));
	}

which is annoying and not completely DRY, but at least the test is nice. A better way would be to use `DateTime?`:

	static void ModifyMember(.... , DateTime? dateOfBirth = null)
	{
		dateOfBirth = dateOfBirth ?? new DateTime(2000, 1, 1);
		member.Modify(...);
	}

Of course this assumes that `member.Modify()` doesn't accept a nullable date of birth.

These proxy methods would quickly need to be used in multiple test fixtures, so could be appropriate to move them to the member's object mother. They _could_ be made into extension methods, but that may make things more confusing as the tests would appear to operate on the member in a way that isn't supported in the application itself. If indeed your tests are intended to be documentation for the application. Which they should be. Y'all.

HT to my home boy [Rob Moore](https://robdmoore.id.au/) ([@robdmoore](https://twitter.com/robdmoore)) for showing me this ONE SIMPLE TRICK.


