---
layout: default
title: Archive
permalink: /archive/
---

## [~/](/)Archive
<ul class="posts">
	{% for post in site.categories.archived %}
		<li><span>{{ post.date | date_to_string }}:</span> <a href="{{ post.url }}">{{ post.title }}</a></li>
	{% endfor %}
</ul>
