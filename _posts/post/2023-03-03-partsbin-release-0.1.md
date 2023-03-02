---
title: partsbin release 0.1
permalink: /partsbin-release-0-1
layout: post
date: 2023-03-03
category: post
---

### tl;dr
Go to [partsbin.page](https://partsbin.page) and check out my new project!

### partsbin
I've made (and released!) my first personal open-source project in literally years. Last year I was working for Automattic on some open source projects, but this is a purely personal project.

It's called, simply enough, **partsbin**. I've got a strong interest in hobby electronics, and while it's fun getting hundreds of parts in small packages from overseas off eBay and AliExpress, it's a pain to try to organise them and then figure out if you've got a specific part for a project you want to do. So I kicked off this project a little over a week ago, in between other things.

I decided to use server-side [Blazor](https://dotnet.microsoft.com/en-us/apps/aspnet/web-apps/blazor) for this app, with some help from [Blazored](https://github.com/Blazored). This is the second time I've used Blazor, I would say I'm still a beginner at the stack, but I'm amazed at how easy it is to put together a working prototype, and then flesh it out into a full app. I had the bare bones of the app (basic navigation, adding and removing a part) finished in two (non-full, partial) days over a weekend. That included deciding on a really simple, file based in-process data store.

After a bit of investigation I decided on [LiteDB](https://www.litedb.org/), which is a JSON store that checks all the above boxes. It just deeply serialises a POCO into and out of a binary JSON format. I'm not sure how it would handle simultaneous reads/writes, the documentation isn't very clear on that, but partsbin is really intended just for one or two users working on the same web app anyway.

I wanted to be able to write notes for each part, and really wanted to use a rich text editor for that. Blazored has a rich text editor component, but it seems like it just wraps [Quill](https://quilljs.com/) and isn't well maintained. I couldn't get it going, but I found a cool [blog post](https://www.puresourcecode.com/dotnet/blazor/create-a-blazor-component-for-quill/) that explained how to wrap Quill for Blazor. The post's implementation seems almost identical to what the Blazored component was doing, but since I was able to set it up step by step I had more control and got it working. First principles, Clarice!

I then wanted search capabilities. I knew I wanted to stick with an in-proc search engine, rather than having to add an external dependenct (especially as I had avoided that with the data layer). [Lucene.net](https://lucenenet.apache.org/) was a good choice, I've used it before. Unfortunately the last stable release is a few years old (3.??) and the current version (4.??-beta) is what a lot of the examples and documentation are for. So I ended up going with the 4.??-beta build and using the new documentation. This still took much of a day, as I had to learn (or possibly re-learn) a lot of Lucene.net techniques. But I got it successfully indexing on several important fields per part, and performing a fuzzy search over all of those fields to find parts. I'm pretty happy with how it turned out. And the data store just lives in the same place as the LiteDb file. 

Once I was happy with the app (and yes, this involved completely rewriting navigation...) I turned my eye to distribution. I knew I wanted to use a Docker image from the start. My last post explores the fun of [doing a multi-architecture build for Docker](/github-actions-and-multi-arch-docker-deploys), and of course immediately after I posted it someone pointed out a much simpler way of building the `Dockerfile` to build and deploy the image. I'll play with this once I'm happy that everything is stable with this first release.

So, have at it. The main project page is [partsbin.page](https://partsbin.page) and there is an [installation guide](https://partsbin.page/installation-guide) explaining how to use `docker compose` to build a configurable, easily reproducible system with storage mapped somewhere hopefully safe.