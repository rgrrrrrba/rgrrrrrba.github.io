---
title: Set up TeamCity on an Azure virtual machine - Redux
layout: post
date: 2014-12-13
category: archived
---

**NOTE:** This is a repost of an [earlier post](set-up-teamcity-on-an-azure-instance.html) with some refinements based on feedback and tips from my friend and collegue [Rob Moore](https://robdmoore.id.au/). 

**DISCLAIMER:** I'm learning in the open here. I have barely used Azure and have only ever used already established TeamCity instances, so I'm really just messing around with two new things at the same time here. I'm sure I'm missing a heap of very important points.


## Set up the VM


### Get started on Azure

<img src="https://i.imgur.com/AsSmaMo.png" style="float:right"/> Setting up a new Azure account is pretty much as easy as hitting <https://windowsazure.com> and starting a free trial. You land in a sweet management console that is a lot easier to use than I expected, based on my experiences with Office 365 (early versions), OWA and SharePoint. [Troy Hunt](https://www.troyhunt.com/) recently released an awesome Azure demo video - [https://worldsgreatestazuredemo.com/](https://worldsgreatestazuredemo.com/) - which is a good introduction to Azure.


### Create a new virtual machine

Select the VIRTUAL MACHINES tab and click NEW from the bottom menu. Select FROM GALLERY and pick 'Windows Server 2012 R2 Datacenter' (or whatever is most current). Hit next and give it a cool name. I left the size on the standard tier at A1 for now and selected SE Asia as the location of the VM. According to the [Azure pricing calculator](https://azure.microsoft.com/en-us/pricing/details/virtual-machines/) this costs around AU$68 per month. Enter a username and password, which you will need to remote into the VM. Next through the rest of the pages and the VM will soon be provisioned:

![https://i.imgur.com/SzdGki7.png](https://i.imgur.com/SzdGki7.png)

Click that arrow to start managing the VM. 


### Open up the firewall

We'll want to open up port 80 for TeamCity. It will actually run on port 8080 but I'll use IIS to reverse proxy it to port 80. This should make it easier to move to SSL in the future. Select the ENDPOINTS tab and ADD a new one. 

![https://i.imgur.com/nelGyMd.png](https://i.imgur.com/nelGyMd.png)

Note that you're ADDing an endpoint. The NEW button will start creating a new VM. It's like Microsoft wants you to keep adding more and more VMs.

Accept the first screen to add a stand-alone endpoint. Pick HTTP from the NAME drop-down, which will automatically fill in the port number. Complete the screen.

This opens port 80 on the *external* firewall. The server's internal firewall will still need to be configured (below).


### Attach a new disk

<img src="https://i.imgur.com/4Znf3hr.png" style="float: left; padding-right: 1em;"/> The VM has two drives configured by default - C:, which contains the Windows installation, and D:, which is 'temporary storage'. I'm going to attach a third disk that will contain the TeamCity host (and later Octopus Deploy). The TeamCity agent will use D: as it is local to the VM, so it should be faster for building the residents. Attach a new disk by selecting the ATTACH option from the bottom menu. Select Empty Disk.  Accept the defaults, I gave it a size of 20 GB. Apparently resizing the disk later on involves downloading the VHD, using a tool to resize it, and uploading it again. Seems a bit awkward. 20 GB should do for now. Select 'READ/WRITE' under the 'HOST CACHE PREFERENCE', which should speed up access. The disk will be attached and the status should return to running.


### Remote in and start configuring the VM

Go to the DASHBOARD tab and select CONNECT from the bottom menu. This will download an .RDP file which will should open in Remote Desktop. Enter the username and password and you should get connected to the server's desktop.


#### IE Enhanced Security
Turn off IE Enhanced Security from the Server Manager, in the Local Server Tab. This will let you use Internet Explorer to download TeamCity (or to install Chrome to download TeamCity and lolcats).

![IE Enhanced Security](https://i.imgur.com/N5ouyt1.png)


#### Open port 80 in the firewall

<img src="https://i.imgur.com/376xLMi.png" style="float:right"/> Open the Windows Firewall with Advanved Security MMC snap-in. Easiest way to find it is to search for `firewall` from the start screen. Create a new inbound rule.

![https://i.imgur.com/MlQbntP.png](https://i.imgur.com/MlQbntP.png)

Select Port, specify local TCP port 80, Allow the connection, apply the rule to all profiles, and call the rule `TeamCity` (so you can find it again ;-) ).


#### Format and assign the empty disk

The empty disk that we attached needs to be formatted and assigned to a drive letter in the server as well. You can think of attaching an empty disk as plugging in a new disk to any normal machine - the disk still needs be configured in the OS.

In the Server Manager, select File and Storage Services, then Disks (under Volumes). The new disk should be online with a partition type of 'Unknown'. Right-click the disk and select New Volume...

![File and Storage Services - New Volume](https://i.imgur.com/vgarRGg.png)

Next through everything and accept the defaults. It should be assigned to drive E:.


## Whew...

Just sit back for a moment and look at what we've achieved. We've got a new VM running the latest Windows Server OS, firewalls configured and remote desktop enabled. I haven't even taken out my credit card yet. Compare with the amount of work needed to boot up a new physical server in a network, or even provisioning a new VM in Hyper-V. Squeeeee? Indeed.


## Install TeamCity

Download the 'default free professional edition' of TeamCity from [here](https://www.jetbrains.com/teamcity/download/). The version at the time of writing was 9.0.

Start up the installer. Select `e:\TeamCity` as the destination folder (or wherever the new disk was assigned). Leave the components as default, which installs a build agent and the server with Windows services. Change the TeamCity data directory to somewhere on the new disk - I used `e:\TeamCity-data`.

After installation the Build Agent properties configuration appears. Change the systemDir, workDir and tempDir to point to `D:\TeamCity\buildAgent\___` (the temporary drive). You have to select each of the paths and edit them in turn. Save this then select the SYSTEM account. It would probably be more secure to set up a restricted user account but the system account should do for now. Do the same for the TeamCity Agent service account. 'Next' through and the services will start. After you finish the installation, 'TeamCity First Start' should open in IE / your browser of choice.

Start working through this wizard. I used the 'Internal (HSQLDB)' database type. Initializing the server components can take a few minutes so be patient! It's a good idea to configure the admin credentials in TeamCity before setting up the reverse proxy (below) so that your new server isn't exposed on the 'first start' wizard.


## Set up the reverse proxy in IIS

First you need to install IIS. Open the Server Manager and select 'Add roles and features'. Select 'Role-based or feature-based installation'. Then 'Select a server from the server pool' and make sure the correct VM is selected. In the list of roles, check 'Web Server (IIS)'. If it prompts you to add the 'IIS Management Console', click 'Add Features'. Click 'Next'. Leae the other features at the default values and continue. Leave the role services for Web Server (IIS) at the default. Next then Install.

I used [this ServerFault answer](https://serverfault.com/a/152411) to set up the reverse proxy. It requires the [URL Rewrite](https://www.iis.net/download/URLRewrite) and [Application Request Routing](https://www.iis.net/download/ApplicationRequestRouting) IIS modules. Either follow those links or use [Web Platform Installer](https://www.microsoft.com/web/downloads/platform.aspx) to install them. Start up IIS Manger and select `<your server>/Sites/Default Web Site`. There should be a URL Rewrite modul. Open it, right-click on the inbound rules list and select 'Add Rules'. Select 'Reverse Proxy' then OK. Allow enabling proxy functionality. Enter `localhost:8080` in the inbound rule server name, then OK. This creates a `web.config` file in `c:\inetpub\wwwroot` that looks like this:

	<?xml version="1.0" encoding="UTF-8"?>
	<configuration>
	    <system.webServer>
	        <rewrite>
	            <rules>
	                <rule name="ReverseProxyInboundRule1" stopProcessing="true">
	                    <match url="(.*)" />
	                    <action type="Rewrite" url="https://localhost:8080/{R:1}" />
	                </rule>
	            </rules>
	        </rewrite>
	    </system.webServer>
	</configuration>

At this point, <https://your-server-name.cloudapp.net> should publicly resolve to the new TeamCity instance.



## References

- [virtew - Setup TeamCity on an Azure Virtual Machine for Windows 8 Metro Style Apps](https://blog.virtew.com/2012/08/18/setup-teamcity-on-an-azure-virtual-machine-for-windows-8-metro-style-apps/)
- [World's Greatest Azure Demo](https://worldsgreatestazuredemo.com/)


## See also

- [Jake Ginnivan’s blog - TeamCity UI Test Agent](https://jake.ginnivan.net/teamcity-ui-test-agent/) - Jake uses Sysinternals Autologon and TightVNC to get a VM with an open desktop session so that automated UI tests will work

