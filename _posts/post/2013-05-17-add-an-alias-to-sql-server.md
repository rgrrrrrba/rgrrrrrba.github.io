---
title: Add an alias to SQL Server
layout: post
date: 2013-05-17
category: archived
---

Ok with a default install of SQL Server I missed setting up the named instance. Because of some messy hard-coded configuration I needed to access the server via `.\SQLEXPRESS`. To add an alias use SQL Server Configuration Manager and add an alias to both SQL Native Client 10.0 Configuration and SQL Native Client 10.0 Configuration (32bit) (if you’re using a 64 bit machine):

![Aliases](/images/2013-05-17-add-an-alias-to-sql-server/aliases.png)

The gotcha is that a default install of SQL Server Developer edition doesn’t have TCP/IP enabled. The only protocol enabled is Shared Memory so none of the available options for aliases don’t work OOTB. Enable TCP/IP to make magic:

![Protocols](/images/2013-05-17-add-an-alias-to-sql-server/protocols.png)

