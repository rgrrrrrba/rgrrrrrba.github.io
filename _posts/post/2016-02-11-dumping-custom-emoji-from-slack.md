---
title: Dumping custom emoji from Slack
layout: post
date: 2016-02-11
category: archived
---

Ok this is a little geeky and not really programmer-ary.

This uses some jQuery hackery and Powershell to dump all of the custom emoji out of a Slack instance. It doesn't require giving a script passwords because of the magic of jQuery and a good multiline text editor. I'm using Sublime for the below instructions.

Sign in to the correct Slack team and go to "Customize Slack", which opens the "Customize Your Team" page at the "Emoji" tab. This page includes jQuery so hit F12 to open the developer tools. Go to the console and paste this in:

	$('span[data-original]').toArray()
		.map(function(x){return x.attributes[0].nodeValue })
		.filter(function(x) { return x.indexOf('emoji.slack-edge') != -1 })
		.reduce(function(a,x) { return a + x + "\r\n" }, '')

This just pulls out the original (full size) image URLs from the page and dumps them out. It also strips out aliased emoji, as they have a different URL format so we can't quickly edit them in bulk, plus they hopefully aren't needed anyway. Copy all of the URLs and paste them into your favourite multiline text editor, making sure they're all consistently formatted. The source lines will look like this:

	https://emoji.slack-edge.com/T02CV8783/awesomeface/0641c08ed4fa8e61.png

They need to be edited to look like this:

	invoke-webrequest -Uri "https://emoji.slack-edge.com/T02CV8783/awesomeface/0641c08ed4fa8e61.png" -OutFile awesomeface.png

In exquisite detail, following are the keystrokes to accomplish this in Sublime Text 3 (and maybe lower). First select all of the URLs, then:

1. `<ctrl-shift-l>` (that's an L)
2. `<home>` `invoke-webrequest -Uri "`
3. `<end>` `" -OutFile .` (remember the period at the end)
4. `<ctrl-left-left-left-left>` (that's _four_ lefts)
5. `<shift-ctrl-right>`
6. `<ctrl-c>` `<end>` `<ctrl-v>`
7. `<ctrl-left-left-left-left-left-left>` (that's _six_ lefts)
8. `<left>` `<shift-home>`
9. `<ctrl-shift-right-right-right-right-right-right-right-right-right-right>` (that's an epic _ten_ rights)
10. `<shift-right>` `<ctrl-c>`
11. `<end>` `<ctrl-left>` `<left>` `<ctrl-v>`

Go through the file and fix any lines that may have been messed up. I ended up with two bad lines out of around 100 URLs.

Save the script to the folder where you want to dump the emoji as `[whatever].ps1`, then open Powershell or equivalant and cd to that folder. Run the script and the emoji should magically appear.

Go forth and emote, Slack-kin!


