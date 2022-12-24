---
title: Uninstrumented statistical .NET profiling using open source tools
layout: post
date: 2014-12-07
category: archived
---

Profilers are unusual tools in that you generally never need one, right up to just past the point where users are melting down and you desperately need one. Usually you get a condition that involves many different pieces of code and internal services and libraries put together in way that quickly grinds your application to a painful halt. Those conditions are (hopefully) pretty rare and in my experience come up when you change something in one area that turns out to badly affect a seemingly unrelated area.

This post is about tools for profiling client applications and applications running in-process (like a console application wrapped around a TopShelf service or self-hosting OWIN). So far I haven't had a need for any ASP.NET profiling but it looks like [MiniProfiler](https://miniprofiler.com/) looks compelling - although it doesn't appear to uninstrumented call profiling, which is probably unlikely to help in that environment anyway.

Since I don't tend to use or need a profiler that often (although automated profiling should really happen as part of continuous integration) I don't have a profiler of choice. I'm told that I should use windbg but because I don't want to break my mind I'll avoid that as long as a simpler tool solves the immediate problem. JetBrains' [dotTrace](https://www.jetbrains.com/profiler/) product has been recommended to me fairly consistently, so the second last time I needed to diagnose one of those 'crashing halt' issues in a WPF app I downloaded the trial. It was great, I found the issue pretty quickly, but the only view that I actually used was the call tree, which shows the methods that have the most amount of CPU time in a tree. This view lets you crawl down these methods to find the callers, and their callers, and eventually the source of the inefficient operation.

The ten day trial expired without me needing a profiler again, and it was another two months before I had a similar requirement, so I didn't end up purchasing it. A week ago I again needed a profiler to figure out a condition that hung the UI thread in a WPF application. I'm definitely not against paying for tooling and the [ReSharper Ultimate](https://www.jetbrains.com/resharper/buy/index.jsp?product=ultimate) bundle is only $100 more than standalone ReSharper, but I told somebody yesterday that I usually used an open source tool that gave me the call tree, which was all I usually needed. I figured that I should probably write it up, so here we are.


### Basics of statistical call time profiling

The idea here is to record the amount of time spent in any method. The total time in a given method is the time spent executing the actual code in that method plus the total time spent in other methods called by that method, including time spend waiting for things (blocked on IO). In a tree view you can visualise this in two ways: descending into methods that a given method calls, or ascending into callers of a given method. To find a method that is a target for optimisation, you would find a method that has a higher than expected usage and either descend down the callers to find an inefficient method, or ascending into callers of the method to find an inefficient use of the called code - as an example, code executed inefficiently inside a loop. I tend to go from one view to the other to help identify a call chain that might contain a target then work my way up or down.


### The rancid depths of WinForms profiling

For my sins I spent several years developing a LOB WinForms application, which because reasons[^1] it needed a fair bit of profiling and massaging.

Initially I used a tool called [nprof](https://code.google.com/p/nprof/). The 0.9.1 version introduced me to the call tree view, but hadn't been updated since 2006 and I believe had issues after I moved to .NET 3. The 0.11 version was a bit more recent but appears to have been a rewrite three years later (2009). It turned out to be quite buggy and difficult to get the information I needed. Shortly thereafter the nprof homepage indicated that development had stopped and pointed to [SlimTune](https://code.google.com/p/slimtune/) as a successor.


### SlimTune

SlimTune also appears to be abandoned, with the latest 0.3.0 version released in February 2011 and the source untouched since then. Luckily it still works with a WPF app using .NET 4.5.1, which was good enough for me. There are also options to connect to ASP.NET / IIS, CLR services and native applications but I haven't used any of these options.

This isn't a pretty application (it's described as early beta) and needs a bit of hand-holding to get the results. It wants to open ports 3000 and 3001 in the firewall, which it uses to communicate between the profiler and the application being profiled.

![](https://i.imgur.com/QeAVxHM.png)

You can't connect to an application that wasn't launched by SlimTune itself, since it needs to attach the instrumentation at startup. This means that you can't connect to an already running application, including one that you've F5ed from Visual Studio. You can start an application from SlimTune and connect to it later, say if you have to navigate to the state that you want to profile and then connect, to limit the amount of data in the profile results to reduce any noise.

The results can be visualised in real time or after the fact. The profile results are saved to a SQLite file that can be opened and visualised later.

SlimTune has 5 visualisers:

- Function Details, which shows a pie chart with the top utilisation of methods called in the selected method
- NProf-Style TreeViews, a split view which shows the CPU time spent per method as a % of the parent (descending into methods that the given method calls) and the total time per method as a % of the total (descending into methods that call the given method)
- Per-Thread Call Trees, this is the view 'recommended' by SlimTune and shows all of the threads used by the application, descending down into called methods
- Performance Counters, this shows an empty chart so I assume it is incomplete
- Query Debugger, this throws an exception so it is definitely incomplete

I generally use the NProf-style TreeViews as it is comprehensive and the split view allows navigation in both directions, however the per-thread call trees is also useful in identifying blocking code on a specific thread.


### Ignotum per ignotius

Should you use SlimTune over dotTrace? If you're happy to spend a little bit of money and want Visual Studio integration, nope. dotTrace is a great application with heaps of features that SlimTune doesn't even come closing to touching on. Uninstrumented open source statistical .NET profiling is a desolate wasteland and dotTrace is a better decision by far (remembering I haven't tried any other commercial profilers).

That said, if you need a profiler once a month at most and don't want or need VS integration, SlimTune is a viable substitute. Just don't expect too much.



[^1]: A handrolled ORM, cache, IoC container and business layer (with n-level undo! I had been reading a lot of [Rockford Lhotka](https://www.lhotka.net/)'s work) 


