---
title: UTF issue when running Jekyll on Windows
layout: post
date: 2013-06-23
type: regular
category: archived
---

**UPDATE:** Running `chcp 65001` seems to work just as well, rather than messing with the environment.

I ran in to an issue when trying to set up Jekyll for this site on my Windows PC:

	D:\source\belfry_images\belfryimages.github.com>jekyll serve --watch
	Configuration file: D:/source/belfry_images/belfryimages.github.com/_config.yml
	            Source: D:/source/belfry_images/belfryimages.github.com
	       Destination: D:/source/belfry_images/belfryimages.github.com/_site
	      Generating... ←[31m  Liquid Exception: incompatible character encodings: UTF-8 and CP850 in default.html←[0m
	error: incompatible character encodings: UTF-8 and CP850. Use --trace to view backtrace

The important thing here is the `incompatible character encodings: UTF-8 and CP850`. I tried various things like blowing away the site and resaving the files using UTF-8 to no avail. I eventually found a blog post (in French) that gives an answer ([translated by Google](https://translate.google.com/#auto/en/Ces%20erreurs%20peuvent%20avoir%20un%20impact%20sur%20l'affichage%20du%20blog.%20Et%20m%C3%AAme%20dans%20certains%20cas%2C%20cela%20peut%20emp%C3%AAcher%20le%20bon%20fonctionnement%20de%20Jekyll.%20Il%20existe%20une%20parade%20%C3%A0%20ce%20probl%C3%A8me.%20Il%20suffit%20simplement%20de%20lancer%20les%20commandes%20suivantes%20avant%20de%20lancer%20le%20serveur%20Jekyll%20%3A%0A%0Aset%20LC_ALL%3Den_US.UTF-8%0Aset%20LANG%3Den_US.UTF-8%0ACette%20commande%20permet%20de%20dire%20%C3%A0%20Windows%20que%20l'encodage%20a%20utilis%C3%A9%20est%20l'UTF-8.%20Si%20vous%20relancez%20Jekyll%20apr%C3%A8s%20cette%20manipulation%2C%20ces%20errurs%20auront%20disparus.)):

> These errors can have an impact on the blog posting. And in some cases, it may prevent the proper functioning of Jekyll. There is a workaround to this problem. Simply run the following commands before starting the Jekyll server:

	set LC_ALL = en_US.UTF-8
	set LANG = en_US.UTF-8

> This command is used to tell Windows that the encoding used is UTF-8. If you raise Jekyll after handling these errurs are gone.

Add the `LC_ALL` and `LANG` settings to _System -> System Properties -> Environment Variables_ (under either _User variables_ or _System variables_) and restart your shell.

Wooo. Go Jekyll.
