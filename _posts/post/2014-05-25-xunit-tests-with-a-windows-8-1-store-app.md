---
title: xUnit tests in a Windows 8.1 Store App
layout: post
date: 2014-05-25
category: archived
---

At the time of writing the current version of xUnit (1.9.2) does not support Windows 8.1 Store applications. The pre-release version (2.0.0-beta-build2650) however has the core library built as a PCL (Portable Class Library). So to add a xUnit test assembly to a Windows 8.1 Store application we need to add a PCL library and add the pre-release xUnit NuGet package.

Add a new project. Under `Visual C#`, then `Store Apps`, then `Windows Apps`, select `Class Library (Windows)`:

![](https://i.imgur.com/L9hKws2.png)

Install the pre-release version of xUnit by either picking `Include Prelease` in the Package Manager, or open the Package Manager Console (`Tools`, `NuGet Packager Manager`, `Package Manager Console`), select the new test assembly project, and execute `install-package xunit -Pre`.

![](https://i.imgur.com/kbtfRkN.png)

I also use the [xUnit.net Test Support](https://github.com/xunit/resharper-xunit) test runner for Resharper, which has a pre-release version that supports xUnit 2. Install that by opening Resharper's `Extension Manager` and selecting `Include Prerelease` before searching for xUnit:

![](https://i.imgur.com/eIp0x04.png)

Results are fields of green:

![](https://i.imgur.com/gMjneQL.png)