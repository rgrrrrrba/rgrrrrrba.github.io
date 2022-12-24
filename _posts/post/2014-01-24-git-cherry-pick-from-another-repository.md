---
title: Git cherry-pick from another repository
layout: post
date: 2014-01-24
category: archived
---

Somehow.. somehow... I managed to overwrite a commit by force-pushing a branch that didn't include said commit.

The commit was made in a local copy of the repository that I was working on in parallel with my normal working copy. So,

    c:\source\repo        <-- working copy
    c:\source\repo-copy   <-- copy with the commit I need

I didn't want to mess around with rebasing the `repo-copy` to merge in the remote branch and totally muck everything up.

The solution is to cherry pick the commit from the working copy. First add the local copy as a remote to the working copy:

    c:\source\repo> git remote add localcopy ../repo-copy

This adds a remote called `localcopy` which points to the local repo copy. Then, fetch from `localcopy`:

    c:\source\repo> git fetch localcopy
    remote: Counting objects: 500, done.                                                                                   
    remote: Compressing objects: 100% (306/306), done.                                                                     
    emote: Total 444 (delta 329), reused 149 (delta 132)                                                                   
    Receiving objects: 100% (444/444), 58.53 KiB | 0 bytes/s, done.                                                        
    Resolving deltas: 100% (329/329), completed with 35 local objects.                                                     
    From ..\repo-copy\                                                                                                  
     * [new branch]      xxxx -> repo-copy/xxxx              
     ...

`<aside>`What happened with line 3 of the response? `emote`? strange.`</aside>`

Then cherry-pick the appropriate commit:

    c:\source\repo-copy> git cherry-pick <commit-hash>

I had to resolve some conflicts but it all looks ok.


