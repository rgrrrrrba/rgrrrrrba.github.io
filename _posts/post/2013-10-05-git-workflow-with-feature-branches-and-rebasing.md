---
title: Git workflow with feature branches and rebasing
layout: post
date: 2013-10-05
category: archived
---


### 1. Start a feature branch and do work in it

1. `git checkout -b name_of_branch`
2. do things
3. `git com -m "message"`
4. repeat 2-3
5. `git push origin name_of_branch`
6. repeat 2-5


### 2. Rebase from master

You do this to prepare to a pull request. Usually I've just `git pull origin master` and do a merge commit, but this leaves the history a bit cleaner. It basically reverts to the branch point, pulls from master, then reapplies the changesets in the branch.

So instead of:
	a
	b
	|\    branch
	| c
	| d
	e |
	|\|
	| f   merge commit
	g/    pull request

You get:
	a
	b
	e
	|\    branch
	| c
	| d
	g/    pull commit

1. `git pull --rebase origin master`
2. There may be conflicts as the changesets are reapplied. Resolve them with `git mergetool` or similar, then `git rebase --continue` and repeat until everything is applied. The branch name should go yellow if using PoshGit. Some of those merges can be brutal.
3. Test the rebased code by rebuilding, running tests, manual tests, etc. Commit any changes that are required.


### 3. Push the rebased branch

The push needs to be 'forced' to overwrite the exising branch (since it has been updated - rebased). So you NEVER do to the `master` branch as it rewrites history.

`git push -f origin name_of_branch


### 4. Submit pull request or merge to master

The feature branch should now be ok to create a pull request or merge to master.

