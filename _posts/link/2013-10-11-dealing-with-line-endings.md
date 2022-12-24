---
title: Git - dealing with line endings
layout: post
date: 2013-10-11
category: archived
---

[Github Help - Dealing with line endings](https://help.github.com/articles/dealing-with-line-endings)

[Re-normalizing a repository](https://help.github.com/articles/dealing-with-line-endings#re-normalizing-a-repository):

	git rm --cached -r .
	# Remove everything from the index.

	git reset --hard
	# Write both the index and working directory from git's database.

	git add .
	# Prepare to make a commit by staging all the files that will get normalized.
	# This is your chance to inspect which files were never normalized. You should
	# get lots of messages like: "warning: CRLF will be replaced by LF in file."

	git commit -m "Normalize line endings"
	# Commit

