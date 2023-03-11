---
title: GitHub Actions and multi-architecture Docker deploys
permalink: /github-actions-and-multi-arch-docker-deploys
layout: post
date: 2023-03-01
category: post
section_1: >
  name: Build image and deploy to Docker Hub
  on:
    workflow_dispatch:
    push:
      branches:
        - main
  jobs:
---

I did two new things yesterday:

- Figured out how to use GitHub Actions (GHA) to deploy to Docker Hub
- Got multi-arch builds to deploy to the same tag

Here's the [GitHub Action workflow](https://github.com/becdetat/partsbin/blob/main/.github/workflows/docker-image.yml) if you just want to see the code. I'll spend the rest of this post explaining it part by part.

A GitHub Action is defined by a YAML file. In this case I'm using it for continuous delivery—creating a Docker image containing a [small Blazor application I've been working on](https://partsbin.page). Because I want to be able to run this application both on ARM64 (ie. Apple M1/M2) and AMD64 machines (and some time in the future Raspberry Pi and other SBCs), to keep it simple I need to be able to build for the different architectures automatically and have them combined into a single Docker image tag.

This turned out to be harder than I expected but easier than I dreaded...


## Setting up

<div class="code-section">
  {% include copy_button.html target="#section-1" %}
  <pre id="section-1">
name: Build image and deploy to Docker Hub
on:
  workflow_dispatch:
  push:
    branches:
      - main
jobs:</pre>
</div>

This first section sets the name of the action. It then specfies what events trigger the action (`on:`).

There are two triggers. The first is `workflow_dispatch`, which adds a button to the action page that lets you do a manual deploy.

The second is a `push` trigger, which happens whenever a push is made to the `main` branch. So far I've only been working on the main branch, but now that I've got this set up I'll work using a PR model—even though I'm currently the sole contributor to this project.

The last line (`jobs:`) is where we start declaring the jobs that make up the action.


## Enter the Matrix, Neo
I need to run two builds—a GitHub hosted runner (`ubuntu-latest`) and a self-hosted runner on my MacBook Pro (`macOS`). I needed a self-hosted runner because I want an ARM64 build, so I (and hopefully others) can run the application on M1 and M2 based Macs. It doesn't seem that any ARM64 GitHub hosted runners exist at the moment—there are some third-party runners but since I happen to be typing on a hefty MBP I figured self-hosting a little runner would be fine.

Eventually I'll probably also set up a Raspberry Pi build, as the app can also be used on a small home or workshop server.

Setting up my self-hosted runner was incredibly easy. Nothing like the old days of setting up TeamCity or Octopus Deploy runners and having to mess with port forwarding.

Here are [the instructions for adding a self-hosted runner](https://docs.github.com/en/actions/hosting-your-own-runners/adding-self-hosted-runners). There are [additional steps required to run it as a service](https://docs.github.com/en/actions/hosting-your-own-runners/configuring-the-self-hosted-runner-application-as-a-service?platform=mac), which for some reason are hidden away...

<div class="code-section">
  {% include copy_button.html target="#section-2" %}
  <pre id="section-2">
  build-and-push:
    strategy:
      matrix:
        os: [ubuntu-latest, macOS]
    {% raw %}runs-on: ${{ matrix.os }}{% endraw %}
    steps:</pre>
</div>

The `strategy` and `matrix` parts sets up a matrix of build options. The builds execute in parallel, however there are ways to limit how many builds run at the same time (to reduce resource load). I didn't need to set any limits as there are only two parallel builds.

You could specify any number of matrix elements and GHA will run the job for each multiple in the matrix. For example:

<div class="code-section">
  {% include copy_button.html target="#section-3" %}
  <pre id="section-3">
matrix:
    os: [ubuntu-latest, macOS]
    node_version: [10, 11]</pre>
</div>

This would run the job four times, once for each combination of `os` and `node_version`, _in parallel_.

For my purposes I only needed to run it on each OS (and therefore underlying architecture—ARM64 and AMD64).

The `runs-on` value specifies which OS the job is currently running on.


## Build and push the OS-specific images
Remember that these steps will run two times, in parallel, one on an `ubuntu-latest` environment and one in a `macOS` environment.

This first step checks out the repository to the runner. The `uses` instruction pulls in an action to do this automagically (without having to authenticate then `git pull` etc).

<div class="code-section">
  {% include copy_button.html target="#section-4" %}
  <pre id="section-4">
      - 
        name: Checkout
        uses: actions/checkout@v3</pre>
</div>

The next step builds the code, using the [`Dockerfile`](https://github.com/becdetat/partsbin/blob/main/src/Dockerfile) which is part of the project. Note that the build gets tagged with the current OS for the running job.

<div class="code-section">
  {% include copy_button.html target="#section-5" %}
  <pre id="section-5">
      -
        name: Build the image
        {% raw %}run: docker build -t becdetat/partsbin:latest-${{ matrix.os }} ./src{% endraw %}</pre>
</div>

The final two steps in this job logs in to the Docker Hub and pushes the image.

Keep in mind that the image being pushed is for a single architecture (ARM64 or AMD64), so it gets tagged accordingly, as either `becdetat/partsbin:latest-ubuntu-latest` or `becdetat/partsbin:latest-macOS`.

The `secrets` being used for the username and password are configured in GitHub on the Settings page for the repository, under "Secrets and variables" and then "Actions".

<div class="code-section">
  {% include copy_button.html target="#section-6" %}
  <pre id="section-6">
      - 
        name: Log into Docker Hub
        uses: docker/login-action@f054a8b539a109f9f41c372932f1ae047eff08c9
        with:
          {% raw %}username: ${{ secrets.DOCKER_USERNAME }}{% endraw %}
          {% raw %}password: ${{ secrets.DOCKER_PASSWORD }}{% endraw %}
      -
        name: Push image to Docker Hub
        uses: docker/build-push-action@ad44023a93711e3deb337508980b4b5e9bcdc5dc
        with:  
          context: ./src
          push: true
          {% raw %}tags: becdetat/partsbin:latest-${{ matrix.os }}{% endraw %}</pre>
</div>

So at this point I have two images, one for `ubuntu-latest` and one for `macOS`, pushed up to Docker Hub. This is great, but if someone wants to use my app they would need to make sure they're pulling the image that corresponds to their system architecture. I want to get smarter than that by creating a combined manifest.


## Combine and push a manifest
This is a separate job, because it needs to execute after the `build-and-push` job. In fact, jobs declared within a GitHub Action will execute in parallel with each other by default, so the `needs: build-and-push` item is needed to declare that the job should only execute after the `build-and-push` step completes.

Every job needs to specify what runner it can run on. The value isn't as critical as the previous job, but `ubuntu-latest` is a good default option.

<div class="code-section">
  {% include copy_button.html target="#section-7" %}
  <pre id="section-7">
  create-combined-manifest:
    needs: build-and-push
    runs-on: ubuntu-latest
    steps:</pre>
</div>

The first step logs into Docker Hub. This is exactly the same in the previous job.
<div class="code-section">
  {% include copy_button.html target="#section-8" %}
  <pre id="section-8">
      - 
        name: Log into Docker Hub
        uses: docker/login-action@f054a8b539a109f9f41c372932f1ae047eff08c9
        with:
          {% raw %}username: ${{ secrets.DOCKER_USERNAME }}{% endraw %}
          {% raw %}password: ${{ secrets.DOCKER_PASSWORD }}{% endraw %}</pre>
</div>

The next step creates a combined manifest, containing the `:latest-macOS` and `:latest-ubuntu-latest` image tags. There are actions and other tools that do this, however the Docker CLI has these commands now so it's easy enough to just use it directly. I believe that `docker manifest` was only added to the CLI within last year (2022) or so.

The first image is the name for the combined manifest, the second and third (`macOS`, `ubuntu-latest`) are the ones that get added in to the combined manifest.
<div class="code-section">
  {% include copy_button.html target="#section-9" %}
  <pre id="section-9">
      - 
        name: Create manifest
        run: |
          docker manifest create \
            becdetat/partsbin:latest \
            becdetat/partsbin:latest-macOS \
            becdetat/partsbin:latest-ubuntu-latest</pre>
</div>

The final step is pretty sedate, given all the work that's just been done. It just pushes that combined manifest that was just created up to Docker Hub.
<div class="code-section">
  {% include copy_button.html target="#section-10" %}
  <pre id="section-10">
      -
        name: Push manifest
        run: docker manifest push becdetat/partsbin:latest</pre>
</div>

The result is extremely satisfying:

![Docker Hub screenshot showing becdetat/partsbin:latest image with both architectures included in the digest](/images/2023-03-01-github-actions-and-multi-arch-docker-deploys/docker-tag.png)

Executing a `docker pull becdetat/partsbin:latest` (or copying the example `Dockerfile` and running `docker up -d`) will now automatically pull down the correct image for the architecture of the machine you're using, for great joy.





