---
title: Stable Diffusion on Radeon and Windows
permalink: /stable-diffusion-on-radeon-and-windows
layout: post
date: 2024-03-17
category: post
---

I'm not going to lie, I'm a bit of an AI n00b as far as _developing_ AI solutions goes.

I use ChatGPT a lot, and I've played around with Copilot, but that's about as far as I've gone down the AI rabbit hole.

So I thought I should really try to get Stable Diffusion running on my Windows desktop.

This guide is pretty specific to my setup and what I was trying to achieve. I didn't want to write Python scripts or train a model or anything. I just wanted Stable Diffusion running locally with a pre-canned model, and a front-end that would let me write a prompt and get back some images.

Plus I'm rocking a Radeon. Most of the AI solutions out there are optimised for Nvidia first. That reminds me, I should probably buy some Nvidia stock...

For reference I'm using a Radeon RX 6750 XT with 12GB of VRAM.

Here's the steps I took:

1. Install [Python 3.10.6](https://www.python.org/ftp/python/3.10.6/python-3.10.6-amd64.exe). When running through the installer, make sure you check the option to add Python to the path.
2. Open a fresh terminal and clone lshqqytiger's fork of the Stable Diffusion web UI project that uses DirectML instead of CUDA (the tech found on Nvidia cards):
    ```sh
    git clone https://github.com/lshqqytiger/stable-diffusion-webui-directml
    cd stable-diffusion-webui-directml
    git submodule init
    git submodule update
    ```
3. Open up `requirements-versions.txt` and add `torch-directml==0.2.0.dev230426` to the end
4. Run `.\webui.bat --no-half --use-directml`

This will install a bunch of stuff including the OpenAI GPT model. You'll end up with about 10GB of files downloaded. Once it's finished it will open a web interface that you can use to generate your own doggo photos.

Here's a pretty good one:

`Cate Blanchett as Lilith from Borderlands`

![Cate Blanchett as Lilith from Borderlands](/images/2024-03-17-getting-stable-diffusion-running-on-nvidia/cate-blanchett-as-lilith-from-borderlands.png)

I also did a bit of experimenting with different models from <https://civitai.com/>. This was interesting, I only played with a couple of models and I need to work on my prompt engineering. Some models didn't want to load, complaining about not having enough VRAM. I guess this is why the pros are using 16GB+ cards.

Anyway, this was a lot of fun.
