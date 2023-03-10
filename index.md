---
layout: default
title: Rebecca Scott's Blog
---

<h2>Blog</h2>
<ul class="posts">
	{% for post in site.categories.post %}
		<li><span>{{ post.date | date_to_string }}:</span> <a href="{{ post.url }}">{{ post.title }}</a></li>
	{% endfor %}
</ul>


