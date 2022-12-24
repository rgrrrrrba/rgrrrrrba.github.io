---
title: Copying React props to a child component
layout: post
date: 2018-03-02
category: archived
---

I just figured out something cool and thought I might blog it bacause it's been a while. I had a little React component that wrapped creating a menu link, adding some basic logic to apply a `current` class to the anchor if the `href` passed in matched the current path. I wanted to also pass arbitrary props down to the link - specifically a `data-method="delete"` attribute to one of the menu links which hooks into some nasty magic Rails stuff to convert an anchor click into a `DELETE` request.

Obviously I didn't want to add lots of explicit and optional prop values whenever I wanted to copy a prop down to the anchor. I found [this](https://zhenyong.github.io/react/docs/transferring-props.html) (which seems to be an old version of the React docs) which suggests using the ES6 spread operator to transfer props.

So here's my simple component:

```
const MenuItem = ({href, children, ...other}) => {
  const current = window.location.pathname === href ? 'current' : ''

  return <li><a href={href} className={current} {...other}>{children}</a></li>
}
```

I use it like this:

```
<MenuItem href="/profiles/me">My profile</MenuItem>
<MenuItem href="/users/sign_out" data-method="delete">Sign out</MenuItem>
```


