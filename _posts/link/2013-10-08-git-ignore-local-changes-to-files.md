---
title: Git - ignore local changes to files
layout: post
date: 2013-10-08
category: link
---

[GIT: ignoring changes in tracked files](https://blog.pagebakers.nl/2009/01/29/git-ignoring-changes-in-tracked-files/)

> just run the following command on the file or path you want to ignore the changes of:

	git update-index --assume-unchanged <file>

> If you wanna start tracking changes again run the following command:

	git update-index --no-assume-unchanged <file>