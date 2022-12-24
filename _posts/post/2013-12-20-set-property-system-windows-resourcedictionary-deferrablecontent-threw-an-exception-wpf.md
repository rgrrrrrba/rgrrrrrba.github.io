---
title: Set property System.Windows.ResourceDictionary.DeferrableContent threw an exception error in WPF
layout: post
date: 2013-12-20
category: archived
---

Ok so I'm working on a WPF launcher that loads a shell that loads a client. Three different appdomains. The launcher could load the shell. The shell could load the client. But when I tried to get the launcher to load the shell then the shell to load the client, I would get this lovely exception when the client instance was created:

    Set property 'System.Windows.ResourceDictionary.DeferrableContent' threw an exception

[Googling the exception](https://www.google.com.au/search?q=Set+property+'System.Windows.ResourceDictionary.DeferrableContent'+threw+an+exception.&oq=Set+property+'System.Windows.ResourceDictionary.DeferrableContent'+threw+an+exception.&aqs=chrome..69i57.519j0j4&sourceid=chrome&espv=210&es_sm=122&ie=UTF-8) gave a lot of talk about duplicate values in the resource dictionaries, which I burned a pile of time on. Then I noticed that the exception had an inner exception which had an inner exception and so on, and the final exception was about a UI component being created in a non-UI thread (`InvalidOperationException`, "The calling thread must be STA, because many UI components require this.").

Turned out I was doing this in the shell, to keep its UI responsive:

    var launchCompletedResetEvent = new AutoResetEvent(false);
    var worker = new BackgroundWorker();
    worker.DoWork += (s,e) => {
        ... // download and extract the client
    };
    worker.RunWorkerCompleted += (s,e) => {
        ... // launch the client
        launchCompletedResetEvent.Set();
    };
    worker.RunWorkerAsync();
    launchCompletedResetEvent.WaitOne();

The `launchCompletedResetEvent` mutex is so the shell didn't terminate before launching the client.

This was launching the client in a non-UI thread. For some reason this seems to have worked when running the shell directly but not when running the shell through the launcher. Maybe when I ran the shell directly I was bypassing the download step and launching the client on the main thread. Actually that makes sense.

I just had to move the client launch to after the `WaitOne` to get everything working:

    var launchCompletedResetEvent = new AutoResetEvent(false);
    var worker = new BackgroundWorker();
    worker.DoWork += (s,e) => {
        ... // download and extract the client
    };
    worker.RunWorkerCompleted += (s,e) => {
        launchCompletedResetEvent.Set();
    };
    worker.RunWorkerAsync();
    launchCompletedResetEvent.WaitOne();
    ... // launch the client

