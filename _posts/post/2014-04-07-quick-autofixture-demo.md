---
title: A Quick AutoFixture Demo
layout: post
date: 2014-04-07
category: archived
---

**Note:** This is a very basic exploration of AutoFixture, written while I tried out a couple of things. It is not authoritive or representative of best practices. Comments are welcome! As are [pull requests](https://github.com/rgrrrrrba/rgrrrrrba.github.io)!

[AutoFixture](https://github.com/AutoFixture/AutoFixture) is a tool for automating the creation of test objects. It is written by [Mark Seemann](https://blog.ploeh.dk/) ([@ploeh](https://twitter.com/ploeh)).

I'm just working through a couple of LINQPad tests here. `Fixture` and the `Create()` method are AutoFixture. `Dump()` belongs to LINQPad.

    class Foo 
    {
    }

    void Main()
    {
        new Fixture()
            .Create<Foo>()
            .Dump();
    }

| UserQuery+Foo |
|---------------|
|               |

Here the fixture creates a default instance of `Foo`. Nothing spectacular.

If I plug in a a simple integer dependency things get interesting.

    class Foo 
    {
        public int Number{ get; set; }
    }

    void Main()
    {
        new Fixture()
            .Create<Foo>()
            .Dump();
    }

| UserQuery+Foo |    |
|---------------|----|
| Number        | 49 |

AutoFixture provides a random dummy value to all publicly available properties. Make `Number` have a private setter and AutoFixture can't see it. It will however fill the constructor with values.

    class Foo 
    {
        public int Number{ get; private set; }
        
        public Foo(int number) 
        {
            Number = number;
        }
    }

    void Main()
    {
        new Fixture()
            .Create<Foo>()
            .Dump();
    }

| UserQuery+Foo |    |
|---------------|----|
| Number        | 38 |


By default, AutoFixture will use the greediest constructor it can find that it can fulfil. It will also work through those parameters, constructing them in the same way. Similar to an IoC framework like Autofac.

    class Foo 
    {
        public int Number{ get; private set; }
        public Bar MyBar { get; private set; }
        
        public Foo(int number, Bar bar) 
        {
            Number = number;
            MyBar = bar;
        }
    }

    class Bar
    {
        public int BarNumber { get; set; }
    }

    void Main()
    {
        new Fixture()
            .Create<Foo>()
            .Dump();
    }

| UserQuery+Foo |                     |
|---------------|---------------------|
| Number        | 4                   |
| MyBar         | UserQuery+Bar       |
|               | -> BarNumber == 198 |

You can see here that `Foo` and `Bar` both received different numbers - 4 and 198 respectively. The fixture's behavior can be defined by adding a registration, such that every time it resolves a certain type it will return the same value. This example registers the integer value `999`.

    void Main()
    {
        var fixture = new Fixture();
        
        fixture.Register(() => 999)
        
        fixture.Create<Foo>()
            .Dump();
    }

| UserQuery+Foo |                     |
|---------------|---------------------|
| Number        | 999                 |
| MyBar         | UserQuery+Bar       |
|               | -> BarNumber == 999 |

Complex types can be registered in the same way. This registers an instance of `Bar` with a `BarNumber` of `1234`. The `Foo.Number` field is given a dummy value.

    void Main()
    {
        var fixture = new Fixture();

        var bar = new Bar { BarNumber = 1234 };
        fixture.Register(() => bar);
        
        fixture.Create<Foo>()
            .Dump();
    }

| UserQuery+Foo |                      |
|---------------|----------------------|
| Number        | 194                  |
| MyBar         | UserQuery+Bar        |
|               | -> BarNumber == 1234 |


## More information

Mark Seeman's blog articles under the [AutoFixture tag](https://blog.ploeh.dk/tags.html#AutoFixture-ref) are the most comprehensive source of information about AutoFixture. 
