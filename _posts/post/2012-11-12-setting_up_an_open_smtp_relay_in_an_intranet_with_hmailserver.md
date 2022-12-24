---
title: Setting up an open SMTP relay in an intranet with hMailServer
layout: post
date: 2012-11-12
type: regular
category: archived
---

**You need to do this because you have line of business software or printer/scanner/faxes that need to send emails within a corporate intranet and your email server is hosted offsite.**

This doesn't work with Office 365 acting as the external SMTP server. It should work but there's something about Office 365's use of TLS that stops the SMTP relay from authenticating properly in hMailServer. The same problem seems to happen with IIS's built-in SMTP relay so I think I may have been doing something wrong. In any case, although the main `.com.au`'s MX is Office 365, I also have a `.net.au` domain hosted on a shared server with a more straightforward (ie less secure) configuration, which I used as the relay.

1. Install [hMailServer](https://www.hmailserver.com/) on a Windows machine within the intranet. Make sure port 25 isn't blocked by a firewall on the machine. The default self-hosted SQL Server Compact database option in hMailServer should be fine.
2. Create an email address on the external mail server. Something like `relay@example.com`.
3. Create a wildcard route on hMailServer.
	- This is a domain of *.
	- The target SMTP host should be set to `mail.example.com` or wherever the external server is hosted. 
	- The TCP/IP port should be set to 25.
	- Under the Delivery tab check the _Server requires authentication_ box and fill in the user name and password for the relay email address.
4. The trick is that hMailServer doesn’t seem to support wildcard routes, even though it will let you create a route of *. To work around this you just add a rule that sends everything to the wildcard route:
	1. Name the rule `wildcard`
	2. Add a criteria: predefined field `From`, search type `Wildcard`, value is `*`
	3. Add an action: `Send using route to *`
5. Under _Advanced_, _IP Ranges_, add a range called `intranet` or similar:
	1. Set the lower and upper IP ranges to encompass your intranet (eg `192.168.1.0` to `192.168.1.255`)
	2. Set the priority to something greater than 15
	3. Under _Other_, uncheck _Anti-spam_, _Anti-virus_
	4. Under _Require SMTP authentication_, uncheck all of the boxes
6. Save everything

This can be tested by telnetting (or [PuTTy](https://www.chiark.greenend.org.uk/~sgtatham/putty/)ing) into port 25 of the machine, then sending an email by hand (see [Wikipedia’s entry for SMTP](https://en.wikipedia.org/wiki/Simple_Mail_Transfer_Protocol#SMTP_transport_example) for an example). Sending the email by hand is a very easy way to figure out configuration problems on the client-facing side of the SMTP relay and is a skill worth learning - also it makes you look like a boss if anybody nearby understands what you are doing. The email should be sent through the hMailServer SMTP relay, through to the external SMTP server (using the relay email address for authentication), then through to its destination. hMailServer also has pretty good logging which can help figure out any bugs or misconfiguration.

