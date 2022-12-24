---
title: GitHub branches and pull request workflow
layout: post
date: 2012-11-30
type: regular
category: archived
---

This is a workflow for contributing a pull request (PR) to a project on GitHub using feature branches:

1. Open an issue on the target project through GitHub.
2. Make a note of the issue number.
3. Make sure your local copy is up to date via `git pull upstream master`.
    - If you are pushing to a branch, such as the target’s `gh-pages` branch, make sure you're on the correct branch (`git checkout -b gh-pages`) and pull from the upstream branch (`git pull upstream gh-pages`).
4. Create a new branch: `git checkout -b issue123_name_of_branch`.
5. Make changes, make commits, etc. Commits should reference the issue by including the issue number: `git com -m "#123 fixes some issue"`.
6. Push to a new branch on your GH fork: `git push -u origin issue123_name_of_branch`.
7. Open the new branch on GitHub and create a pull request.
8. Checkout local master to return to the master branch, before your feature branch: `git checkout master`.
9. Once the PR has been accepted pull from upstream (`git pull upstream master`) and push back to your master (`git push`).

If you are pushing to a branch when creating the pull request (for example to `gh-pages`) you have to select the source branch to compare to. The branch name has to be typed manually. The branch selection screen is also a bit dodgy with text selection (for me in any case).

![Branch selection](/images/2012-11-30-github_branches_and_pull_request_workflow/branch-selection.png)

![Choose a ref to start at](/images/2012-11-30-github_branches_and_pull_request_workflow/choose_a_ref_to_start_at.png)
