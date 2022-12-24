---
title: Dropbox as a service on Windows Server 2008 file server
layout: post
date: 2013-01-21
type: regular
category: archived
---

I've set up a Dropbox account on a file server so that:

1. Remote users can use Dropbox to share files with each other and with users of the file share, and
2. Disaster recovery procedures that include backups of the file server cover the files hosted offsite in the Dropbox account.

By default, Dropbox installs as a desktop application. To get it to work as a Windows service it needs to be run within a utility in the Windows Server 2003 Resource Kit called `srvany.exe`. Since the only instructions I could find for setting this up were for [a Windows 2003 server](https://blog.dreamfactory.se/2011/01/20/dropbox-as-a-service/) I thought I would document the procedure for Windows 2008 here.

1. Install Dropbox on the file server as a desktop application.
2. Right-click the Dropbox notify icon and open preferences.
3. Uncheck _Show desktop notifications_ and _Start Dropbox on system startup_, save and close.
4. Right-click the Dropbox notify icon and select _Exit Application_.
5. Install the [Windows 2003 Resource Kit](https://www.microsoft.com/en-us/download/details.aspx?id=17657). I installed this on a Server 2003 box and copied `srvany.exe` to the file server, but it should install to 2008.
6. Open cmd / Powershell and run `sc \\server-name create Dropbox binPath= "<path to svrany.exe>" DisplayName= "DropBox"`. The spaces after the `=` are important.
7. Open regedit.
8. Under `HKEY_LOCAL_MACHINE\SYSTEM\Dropbox` create a new key called `Parameters`.
9. Under the `Parameters` key create a new string value `Application` and enter the full path to `dropbox.exe`.
10. Close regedit.
11. Open the Services management console.
12. Change the Dropbox service to automatic startup and set the logon user to the Administrator account.
13. Start the Dropbox service.

Note that the Dropbox service isn't running on the desktop so administrative tasks such as sharing folders need to be performed via Dropbox's web interface.