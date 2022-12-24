---
title: Push a Git repository to TFS
layout: post
date: 2013-09-20
category: archived
---

I've been working on a project using Git and the time has come to move it to TFS. This is for TFS 2012. TFS 2013 should include support for Git out of the box but it looks like that only if the TFS project is created as a Git repo from the start - I may be wrong on this - but these instructions should work regardless. I'm pushing my Git repository in to an empty TFS project.

I've used [Chocolatey NuGet](https://chocolatey.org/packages/gittfs) to install [git-tfs](https://github.com/git-tfs/git-tfs) (`cinst gittfs`).

1. To keep the TFS migration separate from all my hard work I created a new directory outside of the existing project.
2. `cd MyProjectTfs` and then clone down the empty TFS project: `git tfs clone https://path.to.tfs/tfs/CollectionName $/MyProject/Trunk`
3. `cd Trunk` so that we're working in the cloned TFS repository
4. Pull down the changesets from the existing project: `git pull ..\..\MyGitRepo`
5. Check in the changesets to TFS: `git tfs checkin`

Simple as that. `git-tfs` keeps the history so all your glorious commit messages are there for the huddled non-Git masses.

To keep working with the original upstreams you can copy the references from the `.git\config`. Mine looks like this:

	[tfs-remote "default"]
		url = https://tfs.redacted.com/tfs/CollectionName/
		repository = $/MyProject/Trunk
		fetch = refs/remotes/default/master
	[remote "origin"]
		url = git@bitbucket.org:belfryimages/redacted.git
		fetch = +refs/heads/*:refs/remotes/origin/*
	[branch "master"]
		remote = origin
		merge = refs/heads/master

The TFS and Git references are both the respective defaults which is fine because `git push/pull` works on the Git repository while `git tfs checkin/pull` works on the TFS server. So the commit/push workflow is just:

	git commit -m "git ftw!1!1!!"
	git push
	git tfs checkin

And everything is pushed to both remote repositories. Once everything is working you can just copy the `.git` folder back in to the original, `git reset --hard`, and get back to work. Allons-y!
