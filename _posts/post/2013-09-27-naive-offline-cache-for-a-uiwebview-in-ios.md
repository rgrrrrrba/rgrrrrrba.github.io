---
title: Naive offline cache for a UIWebView in iOS
layout: post
date: 2013-09-27
category: archived
---

On Github I've got a [NSUrlCache implementation that adds simple offline caching](https://github.com/belfryimages/naive-ios-urlcache). Dropping this in to an iOS application will allow `UIWebView`s to work offline.

It doesn't include precaching (although that could be done by bundling cached files with the app) or any smarts around cache invalidation or caching while online. In fact if the device is online it will _always_ use the online resource and update the cached item. The only thing it adds is offline capability.

The cache is assigned to the static `NSUrlCache.SharedCache` so add this to your `AppDelegate.FinishedLaunching` or similar:

	var cachePath = Path.Combine(Environment.GetFolderPath (Environment.SpecialFolder.MyDocuments), "urlcache");
	NSUrlCache.SharedCache = new OfflineUrlCache(cachePath);

This will only affect anything that uses `NSUrl`s like the `UIWebView`. So anything that is in .NET world like RestSharp won't be affected. Which is usually what you would want.
