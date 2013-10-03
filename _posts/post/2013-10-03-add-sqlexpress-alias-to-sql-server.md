---
title: Add .\sqlexpress alias to SQL Server
layout: post
date: 2013-10-03
category: post
---

A lot of code bases are hard-coded to point to `.\sqlexpress` which is the default named instance for SQL Server Express. To add a `.\sqlexpress` alias to an unnamed SQL Server instance (developer edition in my case) open *Sql Server Configuration Manager* and follow these simple mind-blowing steps:

1. Open *SQL Server Network Configuration*
2. Open *Protocols for MSSQLSERVER*
3. Enable TCP/IP
4. Save. There will be a message to restart the server, we'll do that soon.
5. Go to *SQL Native Client 11.0 Configuration (32bit)*
6. Go to Aliases
7. Add an alias, with *Alias Name* of `.\sqlexpress` and *Server* of `.`
8. Repeat 7 & 8 for *SQL Native Client 11.0 Configuration*
9. Go to *SQL Server Services*, right click *SQL Server (MSSQLSERVER)* and select *Restart*

