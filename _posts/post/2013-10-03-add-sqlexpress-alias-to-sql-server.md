---
title: Add .\sqlexpress alias to SQL Server
layout: post
date: 2013-10-03
category: archived
---

A lot of code bases are hard-coded to point to `.\sqlexpress` which is the default named instance for SQL Server Express. To add a `.\sqlexpress` alias to an unnamed SQL Server instance (developer edition in my case) follow these simple mind-blowing steps:

1. Open *Sql Server Configuration Manager*
	- You may not be able to find this from the Start screen. See [here](https://technet.microsoft.com/en-us/library/ms174212.aspx) for more details but I can just open the Run dialog (`win-R`) and enter `sqlservermanager11.msc` (for SQL Server 2012) or `sqlservermanager10.msc` (if you have 2010).
2. Open *SQL Server Network Configuration*
3. Open *Protocols for MSSQLSERVER*
4. Enable TCP/IP
5. Save. There will be a message to restart the server, we'll do that soon.
6. Go to *SQL Native Client 11.0 Configuration (32bit)*
7. Go to Aliases
8. Add an alias, with *Alias Name* of `.\sqlexpress` and *Server* of `.`
9. Repeat 7 & 8 for *SQL Native Client 11.0 Configuration*
10. Go to *SQL Server Services*, right click *SQL Server (MSSQLSERVER)* and select *Restart*

