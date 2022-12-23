---
title: Git - reapply a commit using cherry-pick
layout: post
date: 2014-03-26
category: link
---

How do I reapply a commit, so I actually want the commit run twice? Say I do something cool, then do something uncool, and want to do that cool thing again, without having to mess around with history?

Say I pull in some dependency which adds a readme file somewhere, then I delete the readme file, then I update the dependency which pulls in the file again.

> we use Git's cherry-pick command, which applies it as a patch and immediately commits it with the same commit message):

	git cherry-pick 89bec60

[*Turns out*](https://definitivedrupal.org/suggestions/using-git-re-apply-old-over-written-change).



