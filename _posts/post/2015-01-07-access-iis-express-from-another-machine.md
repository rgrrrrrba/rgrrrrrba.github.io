---
title: Access IIS Express from another machine
layout: post
date: 2015-01-07
category: archived
---

By default, IIS Express (via Visual Studio) will only allow connections from the local machine. This is a Good Thing but sometimes you need to test sites and services from another machine. I've found several guide that explain how to do this but they always seem to miss some steps. This will show how to share an IIS Express site via the host's IP address. Using the machine name is also possible as is using the HOSTS file to fake a domain (like `api.mysite.example.com`) but both of these scenarios are outside the scope of these instructions. This has only been tested on computers on the same subnet which should be sufficient for most test scenarios.


## 1. Site works locally

First, your project's Web properties should look something like this:

![](https://i.imgur.com/yDI4IZs.png)

This is the default for a new website. The port may be different and 'Apply server settings to all users (store in project file)' doesn't have to be checked. The site should also run locally without issues. Make a note of the port.


## 2. Set up HTTP.sys

HTTP.sys is a component of Windows (Vista and above) that handles HTTP requests. The url that is going to be shared needs to be reserved in HTTP.sys's access control list (ACL). Open an _administrative_ console. If the console doesn't have admin rights, this won't work. Find out your IP address and run this code to reserve the url in the ACL.

	netsh http add urlacl url=https://192.168.0.6:60985/ user=everyone

You should get back this message:

	URL reservation successfully added

If not, check that the console has admin rights and that the url hasn't already been reserved in the ACL. If you need to, the reservation can be deleted:

	netsh http delete urlacl url=https://192.168.0.6:60985/


## 3. Open up the firewall

Open the Windows Firewall (or whatever firewall you may have) and create an inbound rule allowing the above port (eg. 60985). Make sure it's **incoming**! Yes I burned time when I accidently made it outgoing. Uncheck 'Public' if you don't want the port to be open at cafes and airports.


## 4. Add the new site to ISS Express's configuration

At this point, the port should actually be available from another computer, but IIS Express will only respond to requests for `localhost`. Edit `%USERPROFILE%\Documents\iisexpress\config\applicationhost.config` and find the site definition for your project. The easiest way might be to search for the port as it should be unique across the IIS Express instance.  Add a new binding to the site for the external facing address:

![](https://i.imgur.com/vz9OSBl.png)

Make sure the application isn't running and kill IIS Express:

![](https://i.imgur.com/sfSkFrQ.png)


## 5. <strike>Profit!</strike> Troubleshooting!

Restart Visual Studio as administrator and relaunch the application. It should now work on an external machine. If it doesn't work:

- See if you can access the site locally using the IP address. If you cannot, make sure the IIS Express configuration is correct and that it did in fact restart.
- Make sure the firewall is configured correctly - it should be an _inbound_ rule allowing traffic on the required port from your subnet.
- Check the error on the remote machine.
	- If it is a timeout (`x.x.x.x took too long to respond`) it's probably the host's firewall or ACL, or an unrelated network issue.
	- if it is a 503 Service Unavailable (which should return immediately) it's probably the IIS Express configuration.
- If Visual Studio can't run the project locally saying something like 'The site https://x.x.x.x:41234 could not be created', make sure the ACL reservation was created.