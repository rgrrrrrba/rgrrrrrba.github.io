---
layout: default
title: Links
permalink: /links/
---

## [~/](/)Links
These are some links that I've found useful through my travels.

<ul class="posts">
	{% for post in site.categories.link %}
		<li><span>{{ post.date | date_to_string }}:</span> <a href="{{ post.url }}">{{ post.title }}</a></li>
	{% endfor %}
</ul>
