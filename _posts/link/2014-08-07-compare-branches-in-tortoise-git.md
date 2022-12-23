---
title: Compare branches in TortoiseGit
layout: post
date: 2014-08-07
category: link
---

Using a PR / peer review model when developing in a team is a great way keep quality high with a large number of changes. This model relies on being able to review diffs easily, something which Bitbucket is usually pretty good at.

Unfortunately Bitbucket falls down once PRs reach a certain size:

![This merge is too large to display](https://i.imgur.com/JRllPwb.png)

_i haz a sadz_

Ideally, PRs should be small enough that this doesn't crop up, but when you're performing large refactors or working on significant feature branches the ideal isn't always possible.

TortoiseGit to the rescue: [Compare (Diff) branches in Tortoise Git, or how to preview changes before doing a merge](https://wikgren.fi/compare-diff-branches-in-tortoise-git-or-how-to-preview-changes-before-doing-a-merge/).

Basically:

- *shift*-right click the folder
- Select `TortoiseGit -> Browse Reference`
- Select the two branches to compare using control (usually the current branch and `master`)
- Right-click and select `Compare selected refs`




