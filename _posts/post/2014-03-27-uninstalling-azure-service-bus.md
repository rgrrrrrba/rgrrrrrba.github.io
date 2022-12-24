---
title: Uninstalling Azure Service Bus after (accidently) deleting the databases
layout: post
date: 2014-03-27
category: archived
---

I accidently blew away my databases (while upgrading the SQL Server version) and my local Azure Service Bus stopped working. You can't reinstall or restore easily AFAIK so I just wanted to uninstall Azure Service Bus and start again. Unfortunately you can't uninstall without leaving the farm, and you can't leave the farm without the databases.

The answer is to remove the registry key for the service bus, which is in `HKLM\Software\Microsoft\Service Bus`. That tells the unstaller that there is nothing to see and allows uninstallation.

Found at <https://social.msdn.microsoft.com/Forums/windowsazure/en-US/f5096a7a-9605-4231-b093-b7d278be7c20/cant-uninstall-service-bus> (similar but unrelated root cause).

