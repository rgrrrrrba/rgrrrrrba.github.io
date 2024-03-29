---
title: Mercurial - Undoing an uncommitted merge
layout: post
date: 2010-07-12
category: archived
---

I've moved from Subversion to Mercurial at work to get past some merge problems SVN was giving me. After a few teething problems everything is working out fantastic. I'm also mainly using the command line rather than a GUI front end which is surprisingly efficient.

I just did a merge to a stable branch then realised I had forgotten a change that really should be done on the branch I was merging from (just a version number update). Unfortunately you can’t seem to merge revisions over the top of an existing uncommitted merge so I needed to roll back the merge and start again. In Subversion I would just revert the changes, but Mercurial is a bit smarter about its merges.

To see the parents of the working copy, run `hg parents`. After doing the merge there should be two parents. There are two steps in rolling back the merge. Note the period (.) at the end of both of these commands:

	hg revert --all -r .
	hg update -C -r .

Calling `hg parents` should now just show the original parent.

