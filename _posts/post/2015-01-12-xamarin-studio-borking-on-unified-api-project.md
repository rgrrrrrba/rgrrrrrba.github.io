---
title: Xamarin Studio borking on a Unified API project
layout: post
date: 2015-01-12
category: archived
---

I created a Unified API iOS Binding Project in Xamarin Studio (but this may happen for all unified API projects). This is in Xamarin Studio 5.5.4. When I went to build the project I got an error, something like this:

	Error: could not import 'blah'

[Turns out](https://forums.xamarin.com/discussion/27217) the generated project file [has some errors](https://forums.xamarin.com/discussion/comment/87535/#Comment_87535):

> Hi, i did succeed in the end. Here was the solution for me: For some reason, the binding project with Unified API has some errors in it's project file. After creating the project, i saved it. and opened it with a text editor.
> Look for the Importproject element that claims to import the ObjCruntime, this line has a faulty path. It should be:

	<Import Project="$(MSBuildExtensionsPath)\Xamarin\Xamarin.ObjcBinding.CSharp.targets" />

> Also, i added this to the propertygroups:

	<TargetFrameworkIdentifier>Xamarin.iOS</TargetFrameworkIdentifier>

(Note: add the framework identifier to each of the `PropertyGroup` elements)



