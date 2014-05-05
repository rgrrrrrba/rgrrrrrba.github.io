---
title: Get running fast on GitHub and BitBucket
layout: post
date: 2014-01-22
category: link
---

### GH4W

I use [GitHub for Windows](http://windows.github.com/) to get up and running quickly with GitHub on a new machine. Opening a Powershell instance from GH4W gives me a relatively nice msysgit setup. Then the `github_rsa.pub` file in `C:\Users\**username**\.ssh` can be used added to BitBucket to get that running with the same credentials.

### TortoiseGit
I use [TortoiseGit](code.google.com/p/tortoisegit) to help stage commits and for visualising the log. I can bypass clicking around in Explorer by adding this batch file (named `tgit.bat`) to somewhere in my path (I usually add `c:\bin` to the path for this reason). This is based on a [post by Oren Eini](http://ayende.com/blog/4749/executing-tortoisegit-from-the-command-line).

	@start "TortoiseGit" "C:\Program Files\TortoiseGit\bin\TortoiseGitProc.exe" /command:%1 /path:.


### Switch to Cmder

I just tried out [Cmder](http://bliker.github.io/cmder/) with great success as a Powershell replacement but the msysgit instance used in that doesn't work with the public key named `github_rsa.pub`. I just copied `github_rsa.pub` to `id_rsa.pub` and `github_rsa` to `id_rsa`. Working well so far.

**update** hmm looks like this is a work in progress....

Run this to configure git to push the current branch by default:

	git config --global push.default current

Also check this out: <https://help.github.com/articles/set-up-git>

Looks like password caching is an issue, but only with non-SSH remote urls. If `.git/config` looks like this it shoud use the above keys:

	[remote "origin"]
		url = git@github.com:rgrrrrrba/rgrrrrrba.github.io.git


### Beyond Compare

I also needed to set my merge and diff tool. I use (and pay for) [Beyond Compare 3](http://www.scootersoftware.com/moreinfo.php) and the [instructions here](http://www.scootersoftware.com/support.php?zz=kb_vcs) were helpful. The commands to use are:

	git config --global merge.tool bc3
	git config --global mergetool.bc3.path "C:/Program Files (x86)/Beyond Compare 3/BCompare.exe"                 
	git config --global diff.tool bc3
	git config --global difftool.bc3.path "C:/Program Files (x86)/Beyond Compare 3/BCompare.exe"                 

Notice that you need to use forward-slashes in the path to BC. `~/.gitconfig` should end up like this:

	[merge]
		tool = bc3
	[mergetool "bc3"]
		path = C:/Program Files (x86)/Beyond Compare 3/BCompare.exe
	[diff]
		tool = bc3
	[difftool "bc3"]
		path = C:/Program Files (x86)/Beyond Compare 3/BCompare.exe

TortoiseGit needs to be configured to use Beyond Compare as its diff and merge tool. Do this in TortoiseGit's Settings screen (right-click in Explorer, TortoiseGit, Settings):

![](http://i.imgur.com/fhkbebQ.png)


