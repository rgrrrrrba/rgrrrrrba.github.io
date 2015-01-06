---
title: ObjectMother - Part 1 - What is ObjectMother?
layout: post
date: 2014-05-08
category: post
---

This is the first part in an five part series of posts about the ObjectMother test factory pattern.

1. Introduction - What is ObjectMother?
2. Basic implementation
3. Issues with implementations seen in the wild
4. A more robust implementation
5. Focussed tests and executable documentation - Conclusion



ObjectMother is an established but little used pattern in testing. As [described by Martin Fowler](http://martinfowler.com/bliki/ObjectMother.html), an object mother is essentially a well-structured minimal example of a factory.

Fowler's sparing description is essentially of a pattern whereby a pre-canned instance of an object used for testing - usually a domain object - can be created and retrieved using a common and shared interface.


## Basic example of the ObjectMother pattern

As a basic example, say we're testing a payment processor:

    var processor = new PaymentProcessor();
    var person = new Person("Ben", "Scott", new DateTime(2012, 1, 1), PayGrade.Developer, EmploymentType.FullTime);

    var payment = processor.Process(person, ..);

    // asserts, etc.

The line that we're interested is where `person` gets instantiated. Every time we need to test something that operates on a person object, we need to create a new instance of a person. `Person`'s constructor looks like this:

    public Person(string firstName, string lastName, DateTime startDate, PayGrade paygrade, EmploymentType employmentType)
    {
        // ...
    }

All the payment processor is actually interested in is the pay grade and the employment type:

    public class Payment Processor
    {
        public Payment Process(Person person)
        {
            var multiplier = GetMultipler(person.EmploymentType);
            switch (person.PayGrade) 
            {
                //...
            }
            // ...
        }
    }

Obviously the payment processor could just take the pay grade and employment type as arguments but that is an implementation detail. Other properties on `Person` may also be used, so we'll assume that the processor requires a person to process, and what happens with those other properties aren't what we're interested in testing in this example.

So back to what I mentioned above:

> Every time we need to test something that operates on a person object, we need to create a new instance of a person.

This is problematic for at least two reasons:

1. Duplication of code, and
2. Reduced clarity of tests

Reducing the duplication of code is the main selling point of ObjectMother that Fowler alludes to. Centralising the instantiation of these test objects reduces duplication and makes the test objects well known.

In my opinion, improving the clarity of the tests is a bigger 
