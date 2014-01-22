---
title: Get running fast on GitHub and BitBucket
layout: post
date: 2014-01-22
category: link
---

I use [GitHub for Windows](http://windows.github.com/) to get up and running quickly with GitHub on a new machine. Opening a Powershell instance from GH4W gives me a relatively nice msysgit instance. Then the `github_rsa.pub` file in `C:\Users\**username**\.ssh` can be used added to BitBucket to get that running with the same credentials.

I just tried out [Cmder](http://bliker.github.io/cmder/) with great success as a Powershell replacement but the msysgit instance used in that doesn't work with the public key named `github_rsa.pub`. I just copied `github_rsa.pub` to `id_rsa.pub` and `github_rsa` to `id_rsa`. Working well so far.

**update** hmm looks like this is a work in progress....

Run this to configure git to push the current branch by default:

	git config --global push.default current

Also check this out: <https://help.github.com/articles/set-up-git>

Looks like password caching is an issue, but only with non-SSH remote urls. If `.git/config` looks like this it shoud use the above keys:

	[remote "origin"]
		url = git@github.com:rgrrrrrba/rgrrrrrba.github.io.git

