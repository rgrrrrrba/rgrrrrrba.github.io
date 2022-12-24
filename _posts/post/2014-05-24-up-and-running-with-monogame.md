---
title: Up and Running with MonoGame
layout: post
date: 2014-05-24
category: archived
---

*or, XNA is dead! Long live XNA!*

**WARNING!** Learning in the open&trade;! There are a few moving parts when starting with MonoGame, so I'm just documenting the steps I took.

### Microsoft XNA

[Microsoft XNA](https://en.wikipedia.org/wiki/Microsoft_XNA) is/was a set of tools and a frameworks that provided a relatively easy way to create games using the CLR, sitting on top of DirectX.

Microsoft stopped active development of XNA around 2011, and last year [it became obvious](https://www.polygon.com/2013/1/31/3939230/microsoft-has-no-plans-for-future-versions-of-xna-software) that XNA would be retired. The former XNA home page now redirects to MSDN's [Games development page](https://msdn.microsoft.com/dn629515), which (apart from not actually having any useful information about game development on Microsoft's platforms) is mainly spruiking Microsoft's partnership with [Unity](https://unity3d.com/).


### A quick note about Unity

Unity is both a game engine and a component oriented game development environment, with Mono-based scripting using C#. What this means for the average code monkey such as I is that when firing up Unity you are confronted with a drafting board. No comforting `10 PRINT 'HELLO, WORLD!'` here folks.

![](https://media.giphy.com/media/zjQrmdlR9ZCM/giphy.gif)


### MonoGame to the rescue!

[MonoGame](https://monogame.net) is an open-source rewrite of XNA 4 (the last version), using the same namespaces and with support for both DirectX 11 and OpenGL. Since it is based on Mono, it allows applications to be written and ported to most platforms, including PS4, Wii U, Xbox 360, Windows Desktop and Store, Android, iOS, Windows Phone, Mac OSX and Linux. The big player missing from that list is Xbox One, which currently does not support applications targeting the CLR.

`<aside>` Unity apparently *will* (soon) support Xbox One so I guess it deploys native code, much like how Xamarin can deploy native code written in C# to target platforms that don't support the CLR such as iOS. Which begs the question, why not just do the same in MonoGame? A question for smarter cookies than myself. `</aside>`

MonoGame also has some official M$ love with several games published by Microsoft Studio.

Given these weighty credentials, I put the key in the ignition and powered up for a full throttle hello world.


### Obligatory false start

Don't try this at home.

I cloned the [samples repo](https://github.com/Mono-Game/MonoGame.Samples) and just tried to run the `WindowsDX` (Windows desktop DirectX) solution. No dice. Couldn't copy resource files, couldn't resolve all the referenced assemblies.

MonoGame has some [NuGet packages](https://www.nuget.org/packages/MonoGame/) so I pulled them into the project. I got it building but still had the same problem with the resource files.

I then installed MonoGame using the installer. This let me create new projects using MonoGame's templates, so I created a new project and started following [this tutorial series](https://blogs.msdn.com/b/tarawalker/archive/2012/12/04/windows-8-game-development-using-c-xna-and-monogame-3-0-building-a-shooter-game-walkthrough-part-1-overview-installation-monogame-3-0-project-creation.aspx) by [Tara Walker](https://blogs.msdn.com/b/tarawalker/). The first part went well, which involved showing a simple image on a surface. The tutorial links to an `.xnb` file containing the image, which loads correctly. So far so good, I've got stuff on the screen, I'm a game developer.

The third part of the series goes through creating that `.xnb` file from a source image. This is where I became unstuck.


### Content Pipeline

Images and other assets are relatively easy to use in the web and desktop application space. To add an image to a web site, you drop a JPEG, PNG or GIF onto a server and add a `<img>` tag to your markup. To add an image to a WPF, Store, Phone or WinForms application, you add the file to the project as an embedded resource and then use it in a similar way.

Asset management is a much bigger part of game development. There are textures, meshes, sound effects, background music, graphics shader programs and probably a heap of other stuff that I will never understand, and it these assets need to be available to the game code in a way that is efficient and managable. For example, a texture used for a wall may have a number of resolutions, ranging from a very small resolution without much detail for display at a distance to a very large resolution with a lot of detail for display at a short distance.

The content pipeline is a preprocessing step that takes the game assets and converts them into a format suitable for the engine. In MonoGame's (and XNA's) case, this is the `.xnb` file.

`<aside>` Note that I don't have a full knowledge of the reasons and techniques used in the content pipeline, this is just my understanding of the ideas.` </aside>`

MonoGame doesn't have its own content pipeline management system, although it does use XNA's `.xnb` format. For this reason we have to use XNA.

![](https://i.imgur.com/Lhpog1k.png)

XNA 4 only targets VS2010 so it can be tricky to get it working with VS2013. I found a nice post [here](https://rbwhitaker.wikidot.com/setting-up-xna), Mr Whitaker has written a [script](https://bitbucket.org/rbwhitaker/xna-beyond-vs-2010/downloads/XnaFor2013.ps1) that downloads the XNA installer and sets it up in whatever versions of Visual Studio from 2010 to 2013 that it can find. I went through the script manually and got it going. Here are the steps:

1. Download the installer from here: <https://download.microsoft.com/download/E/C/6/EC68782D-872A-4D58-A8D3-87881995CDD4/XNAGS40_setup.exe>
2. Close Visual Studio
3. Open a shell where the installer was downloaded
4. Execute: `XNAGS40_setup.exe /extract:XNA`, this will extract the installer files into a `.\XNA` folder
5. Browse to that folder
6. Run `redists.msi`, this installs some more installers to `X:\Program Files (x86)\Microsoft XNA\XNA Game Studio`. Yo dawg.
7. Browse to `X:\Program Files (x86)\Microsoft XNA\XNA Game Studio\Setup`
8. Execute: `XLiveRedist.msi`
9. Browse to `X:\Program Files (x86)\Microsoft XNA\XNA Game Studio\Redist\XNA FX Redist`
10. Execute: `xnaliveproxy.msi` (this actually failed for me but it hasn't caused any dramas so far)
11. Execute: `xnags_platform_tools.msi`
12. Execute: `xnags_shared.msi`
13. Execute: `xnags_visualstudio.msi`. This installs the VS2010 extension, we'll get back to this one.
14. Browse back to the `XNA` folder in step 4 (possibly order is important)
15. Execute: `arpentry.msi`
16. Browse to `X:\Program Files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\Extensions\Microsoft`
17. Find the `XNA Game Studio 4.0` folder. This is the VS2010 extension installed in step 13.
18. Copy the folder to `X:\Program Files (x86)\Microsoft Visual Studio 12.0\Common7\IDE\Extensions\Microsoft`
19. Edit the `extension.vsixmanifest` file in the new `XNA Game Studio 4.0` folder (this is in `Program Files` so you'll probably need to open notepad as an administrator)
20. Find the line that says `Version="10.0"` and change it to `Version="12.0"`
21. Insert a new line after that line and add `<Edition>WDExpress</Edition>`
22. Clear your cached extensions by browsing to `%userprofile%\AppData\Local\Microsoft\VisualStudio\12.0` and deleting the `Extensions` folder
23. Restart Visual Studio

Now when you create a new project you should have XNA Game Studio 4.0 templates available. Create a new `Empty Content Project (4.0)` and compile it to make sure the content pipeline is working.


### Bringing MonoGame back into the picture

As I said above, I also installed the MonoGame binaries, which adds the MonoGame templates to Visual Studio. Download the bits from [MonoGame's Downloads page](https://www.monogame.net/downloads/). 

I kept working through [Tara Walker's tutorial series](https://blogs.msdn.com/b/tarawalker/archive/2012/12/04/windows-8-game-development-using-c-xna-and-monogame-3-0-building-a-shooter-game-walkthrough-part-1-overview-installation-monogame-3-0-project-creation.aspx) and to prove that the content pipeline is working, here's a handy screenshot ([Github repo at this revision](https://github.com/becdetat/monogame-tw-tutorial/tree/9945c675303c35e61888de6816a5b165c074cada)):

![](https://i.imgur.com/pE2DmVf.png)


### Resources

- [RB Whitaker - Setting up XNA](https://rbwhitaker.wikidot.com/setting-up-xna)
- [RB Whitaker - Accessing the XNA Content Pipeline](https://rbwhitaker.wikidot.com/monogame-accessing-the-xna-content-pipeline)
- [MonoGame Tutorials](https://www.monogame.net/documentation/?page=tutorials_md)
- [Tara Walker's "Building a Shooter Game" tutorial series, part 1](https://blogs.msdn.com/b/tarawalker/archive/2012/12/04/windows-8-game-development-using-c-xna-and-monogame-3-0-building-a-shooter-game-walkthrough-part-1-overview-installation-monogame-3-0-project-creation.aspx)
- [MonoGame - Downloads](https://www.monogame.net/downloads/)
- Pluralsight ($) (note I haven't watched all of these):
    - [Introduction to 2D Game Programming with XNA](https://pluralsight.com/training/courses/TableOfContents?courseName=xna&highlight=john-sonmez_xna-m1-introduction*1,6#xna-m1-introduction) by John Sonmez
    - [Cross Platform Game Development with MonoGame](https://pluralsight.com/training/courses/TableOfContents?courseName=monogame&highlight=john-sonmez_monogame-m1-introduction*4!john-sonmez_monogame-m6-wp7!john-sonmez_monogame-m2-building-pong#monogame-m1-introduction)
- [My repo going through TW's above tutorial](https://github.com/cdetat/monogame-tw-tutorial)


