---
title: Vertical whitespace - a fairy tale
layout: post
date: 2016-07-19
category: archived
---

I saw this in a code review:

```
Dim thing As New Thing(someValue)
thing.DoSomething()
Return thing
```

_(names changed to protect the innocent)_

Apart from it being Visual Basic, there is nothing wrong with the code, but I'm classified as human and humans like to find things to complain about. Now this is absolutely personal preference and I normally would never bug anybody about it, but a trick I've been doing the past 18 months especially is to put a line between groups of different types of statements. Like this:

```
Dim thing As New Thing(someValue)

thing.DoSomething()

Return thing
```

Or, for a slightly more complex example:

```
function foo() {
    var a = 1
    var b = 2

    a = a + b
    b = a * a

    return b
}
```

I'm aware that I'm totally overthinking things and probably could use a holiday, but I've found it makes it easier to parse code at a higher level - this block of statements is to declare variables, this block is doing some operations, this block returns stuff. So if I need to go back in and change something I can find the block faster. It also makes it easier to move stuff around and it reads like a story.

> ### Chapter 1 - Mrs Premise
> 
> Once upon a time there was land called `foo`. In that land lived a thing called `a` whose value was `1`, and a thing called `b` whose value was `2`.
> 
> ### Chapter 2 - Mrs Essence
> 
> The thing called `a` had `b`'s value added to it. Then the thing called `b` had the square of `a`'s value added to it.
> 
> ### Chapter 3 - Mrs Conclusion
> 
> The value of the thing called `b` left the land called `foo` and lived happily ever after in the scope of the calling function.



