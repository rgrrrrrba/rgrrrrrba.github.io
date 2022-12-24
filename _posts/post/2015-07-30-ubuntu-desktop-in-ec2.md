---
title: Ubuntu Desktop in AWS EC2
layout: post
date: 2015-07-30
category: archived
---

This turned out to be much harder than I had hoped. Creating and connecting to a Windows Server VM is trivial in Azure but I thought I would try creating an Ubuntu VM with a desktop in AWS for the exercise (and hopefully in less time than it would take to download an Ubuntu ISO and set up a local VM - I failed). The main time sink was messing around with the SSH keys, which is admittedly a good thing because it's got to be, excuse me, pretty damn secure. The other delay was in properly configuring X-Windows to show the Ubuntu desktop. Again this is probably a good thing because an OOTB Ubuntu instance is quite lean and most server-y things can be done via SSH rather in a GUI. That's not what I was intending for this exercise though.

I won't go into [creating an AWS account][aws-home] but wow, that UI. I thought Azure was arcane.

To create a VM you find a section called 'Create instance', which lets you press a button called 'Launch Instance', which launches a virtual server, which is known as an Amazon EC2 instance. In Azure this is a big blue plus sign.

![pressss meeeee][launch-instance-button]

Now select your VM image. I picked "Ubuntu Server 14.04 LTS (HVM), SSD Volume Type". It's free! For eligible! I selected the 't2.micro' instance type, which is also free. This is exactly how much I want to spend. I left everything else as default and clicked 'Review and Launch', mainly because I didn't realise there were other things to configure.

By default a new security group will be created when launching a new instance, named something like `launch-wizard-1`. This is 'open to the world', meaning that any IP address could connect to the instance if it has the proper credentials. A security group is basically a set of firewall rules. The only port open by default is 22 for SSH, which requires a private key. Because I'm using SSH tunnelling to forward the VNC port I don't actually have to change the security group but you could limit port 22 to your static IP if you've got one.

Now hit Launch. The next step lets you create a public & private key pair. Select 'Create a new key pair' and give it a nice name. Press Download Key Pair to download the private key then continue. If you hit 'View instances' you can see the new VM get provisioned. It's not that exciting.

I followed [these instructions][ec2-putty] to use PuTTY to SSH into the VM but you could just use `ssh` directly. I ended up needing to use SSH directly later on to create an SSH tunnel anyway.

	ssh ubuntu@<PUBLIC DNS> -i <KEYFILE>.pem

The value for `-i` is the path to the .pem file downloaded previously.

I followed [these instructions][set-up-ubuntu-on-ec2] to install the Ubuntu desktop and a TightVNC server, but I ended up with the grey screen of an empty X-Windows session. I needed some [extra work][make-vnc-server-work-with-ubuntu-desktop] to get it going. You should just do the following instead ;)

Update `apt-get` and install lots of things:

	sudo apt-get update
	sudo apt-get intall ubuntu-desktop
	sudo apt-get intall tightvncserver
	sudo apt-get install gnome-panel gnome-settings-daemon metacity nautilus gnome-terminal

Launch VNC server to create an initial configuration file:

	vncserver :1

Open the configuration file in VIM:

	vim ~/.vnc/xstartup

Edit the configuration file to look like this, using `i` to enter insert mode, then `<escape>` `:wq` to save and exit:

	#!/bin/sh

	export XKL_XMODMAP_DISABLE=1
	unset SESSION_MANAGER
	unset DBUS_SESSION_BUS_ADDRESS

	[ -x /etc/vnc/xstartup ] && exec /etc/vnc/xstartup
	[ -r $HOME/.Xresources ] && xrdb $HOME/.Xresources
	xsetroot -solid grey

	vncconfig -iconic &
	gnome-panel &
	gnome-settings-daemon &
	metacity &
	nautilus &
	gnome-terminal &

Kill and restart the VNC server to apply the settings. This needs to happen each time the VNC / X-Windows configuration is updated.

	vncserver -kill :1
	vncserver :1

After setting up the VNC server you need to create an SSH tunnel. Open a local console that has `ssh.exe` in the path (Cmder didn't have it but vanilla PowerShell did). The command to run is[^how-to-specify-a-private-key-in-ssh]:

	ssh ubuntu@<PUBLIC DNS> -L 5902/127.0.0.1/5901 -i <KEYFILE>.pem

`-L` sets up the tunnel, from port `5902` on `127.0.0.1` (localhost) to port `5901` on the remote server. Note that I'm setting my local endpoint to port 5902 - 5901 didn't work for me.

![][ssh-tunnel]

If you're on Windows, [download Tight-VNC][download-tight-vnc] instead of using apt-get to install VNC. Connect to `127.0.0.1::5902` and use the password you gave above. You should now see your new shiny Ubuntu desktop:

![][new-shiny-ubuntu]

If you see an empty grey window like this:

![][grey-screen-of-x-windows-voidness]

, or if parts of the Ubuntu desktop seem to be missing, you will need to work on the VNC / X-Windows configuration. Make sure you've edited the `xstartup` file for the user that the tunnel is logged in as for a start.

Quick note: to delete an instance you just need to terminate it. It doesn't disappear from the list immediately but apparently it will. *refresh* nope, still there.




[aws-home]: https://aws.amazon.com/
[launch-instance-button]: https://i.imgur.com/SOJx4Np.png
[ssh-tunnel]: https://i.imgur.com/iYRRB3k.png
[ec2-putty]: https://docs.aws.amazon.com/AWSEC2/latest/UserGuide/putty.html?console_help=true
[set-up-ubuntu-on-ec2]: https://xmodulo.com/how-to-set-up-ubuntu-desktop-vm-on-amazon-ec2.html
[^how-to-specify-a-private-key-in-ssh]: https://xmodulo.com/how-to-specify-private-key-file-in-ssh.html
[download-tight-vnc]: https://www.tightvnc.com/download.php
[make-vnc-server-work-with-ubuntu-desktop]: https://askubuntu.com/a/475036/29199
[grey-screen-of-x-windows-voidness]: https://i.imgur.com/crHgZFM.png
[new-shiny-ubuntu]: https://i.imgur.com/m7PRgMm.png
