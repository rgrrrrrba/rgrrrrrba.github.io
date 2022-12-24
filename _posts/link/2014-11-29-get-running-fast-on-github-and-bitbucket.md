---
title: Get running fast on GitHub and BitBucket
layout: post
date: 2014-11-29
category: archived
---

These are the steps I use to set up a new PC with Git. I mainly use Git on the command line, with TortoiseGit for staging commits and visualising logs and Beyond Compare for diffs and merges.

### GH4W
I use [GitHub for Windows](https://windows.github.com/) to get up and running quickly with GitHub on a new machine. It takes care of setting up key files, messing with paths, and making everything play nice with Github. Opening a Powershell instance from GH4W gives me a relatively nice msysgit setup. Then the `github_rsa.pub` file in `C:\Users\**username**\.ssh` can be added to BitBucket and other services to get everything running with the same credentials. Generally I won't use GH4W after this as I'm mostly a command line junky, with gaps filled by TortoiseGit.


### Put `c:\bin` on the path
Create a folder called `bin` somewhere fairly central. I use `c:\bin`. Then put it on the path. Now you can drop scripts and executables into the `bin` folder and they will work in any console anywhere. To mess with the path:

1. Open the System control panel item (`Win`-`pause/break`)
2. Select Advanced system settings (opens the System Properties dialog)
3. Select Environment Variables
4. Update both of the Path variables (in User variables and System variables), add `;c:\bin` to the end of the values (don't delete the existing value)
5. 'OK' out of the dialogs. You will need to restart any open command prompts to get the path change to work, rebooting is usually the fastest way to make sure the change has gone through (logging out and in may work too).

![](https://i.imgur.com/hWvTzkl.png)


### TortoiseGit
I use [TortoiseGit](https://code.google.com/p/tortoisegit) to help stage commits and for visualising the log. I can bypass clicking around in Explorer by adding this batch file (named `tgit.bat`) to somewhere in my path (I usually add `c:\bin` to the path for this reason). This is based on a [post by Oren Eini](https://ayende.com/blog/4749/executing-tortoisegit-from-the-command-line).

	@start "TortoiseGit" "C:\Program Files\TortoiseGit\bin\TortoiseGitProc.exe" /command:%1 /path:.

TortoiseGit has a number of commands available from the command line. Use `tgit commit` to preview, stage and execute a commit, and `tgit log` to see the log.


### Switch to Cmder
I use [Cmder](https://bliker.github.io/cmder/) as a console replacement but the msysgit instance included in the full download doesn't work with the public key named `github_rsa.pub`. I just copied `github_rsa.pub` to `id_rsa.pub` and `github_rsa` to `id_rsa`. Cmder has support for aliases using the `alias` command but I usually use the `c:\bin` path with batch files to keep everything relatively portable. Install Cmder to `c:\bin\cmder` and drag `C:\bin\cmder\cmder.exe` onto the task bar for easy access. Set the startup directory to your usual source folder for easy access (`c:\source` is mine):

![](https://i.imgur.com/22KOl45.png)


### Fine-tune Git
Run this to add a Git alias that opens the global configuration in its default editor (Vim) (`git ec`):

	git config --global alias.ec "config --global -e"

Now that's in you can hand edit the config to add some or all of [Phil Haack's GitHub Flow aliases](https://haacked.com/archive/2014/07/28/github-flow-aliases/). I usually just use 'wipe', 'save' and 'undo':

	wipe = !git add -A && git commit -qm 'WIPE SAVEPOINT' && git reset HEAD~1 --hard
	save = !git add -A && git commit -m 'SAVEPOINT'
	undo = reset HEAD~1 --mixed

Run this to configure git to push the current branch by default, fix long path name issues, set `autocrlf` to true, and use the `wincred` credentials helper to store credentials for repositories that don't support SSH:

	git config --global push.default current
	git config --system core.longpaths true
	git config --global core.autocrlf true
	git config --global credential.helper wincred

Do this to get around long path issues:

	git config --system core.longpaths true

Also check this out: <https://help.github.com/articles/set-up-git>

Looks like password caching is an issue with Cmder's msysgit, but only with non-SSH remote urls. If `.git/config` looks like this it shoud use the above keys:

	[remote "origin"]
		url = git@github.com:rgrrrrrba/rgrrrrrba.github.io.git


### Beyond Compare
I use (and happily pay for) [Beyond Compare 4](https://www.scootersoftware.com/moreinfo.php) and the [instructions here](https://www.scootersoftware.com/support.php?zz=kb_vcs) were helpful. To get Git to use BC4 for diffs and merges:

	git config --global merge.tool bc4
	git config --global mergetool.bc4.path "C:/Program Files (x86)/Beyond Compare 4/BCompare.exe"                 
	git config --global diff.tool bc4
	git config --global difftool.bc4.path "C:/Program Files (x86)/Beyond Compare 4/BCompare.exe"                 

Notice that you need to use forward-slashes in the path to BC. `~/.gitconfig` should end up like this:

	[merge]
		tool = bc4
	[mergetool "bc4"]
		path = C:/Program Files (x86)/Beyond Compare 4/BCompare.exe
	[diff]
		tool = bc4
	[difftool "bc4"]
		path = C:/Program Files (x86)/Beyond Compare 4/BCompare.exe

Run these commands to tighten up things a bit by skipping confirmation prompts and removing merge backups:

	git config --global difftool.prompt false
	git config --global mergetool.prompt false
	git config --global mergetool.keepBackup false

TortoiseGit also needs to be configured to use Beyond Compare as its diff and merge tool. Do this in TortoiseGit's Settings screen (right-click in Explorer, TortoiseGit, Settings, or just use `tgit settings` if you added the alias):

![](https://i.imgur.com/fhkbebQ.png)


