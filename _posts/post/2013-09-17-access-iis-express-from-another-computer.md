---
title: Access IIS Express from another computer
layout: post
date: 2013-09-17
category: archived
---

Here's some good resources:

- [Johan Driessen's blog - Accessing an IIS Express site from a remote computer](https://johan.driessen.se/posts/Accessing-an-IIS-Express-site-from-a-remote-computer)
- [Scott Hanselman - Working with SSL at Development Time is easier with IISExpress](https://www.hanselman.com/blog/WorkingWithSSLAtDevelopmentTimeIsEasierWithIISExpress.aspx)

Here the down-low:

- Open `c:\Users\YOUR_USERNAME\Documents\IISExpress\config\applicationhost.config`, find your site (`<site name="Application.Name" id=`, or search for the path to the site).
- Copy the line `<binding protocol="http" bindingInformation="*:PORT:localhost" />` and change `localhost` to your IP address. So it should look like:

 <!-- code -->

	<binding protocol="http" bindingInformation="*:PORT:localhost" />
	<binding protocol="http" bindingInformation="*:PORT:192.168.1.123" />

- Open PowerShell with elevated rights
- Run this to add the url to the ACL: `netsh http add urlacl url=https://192.168.1.123:PORT/ user=everyone`
- Run this to add an exception to the firewall: `netsh advfirewall firewall add rule name="MySiteIISExpress" dir=in protocol=tcp localport=PORT profile=private remoteip=localsubnet action=allow`
- **YOU'RE NOT DONE YET!** You need to stop the site from the IIS Express notify tray application before running it up in Visual Studio. Just stopping and starting the VS application doesn't work. This step isn't mentioned in any of the guides I've found.

The site should now be available within the local subnet (`255.255.255.0`) using the computer's IP address. Using the machine name should be fine, just replace the IP address with the machine name in the steps above (although I haven't tested this).


