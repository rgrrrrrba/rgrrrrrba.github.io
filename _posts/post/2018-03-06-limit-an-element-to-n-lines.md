---
title: Limit an element to N lines
layout: post
date: 2018-03-06
category: archived
---

HTML layout tips (and jQuery) are sooooo 2008 but you know.

Gotta pay the bills.

So say you want an element to be trimmed responsively so it doesn't go over _n_ lines with a nice ellipsis. CSS's `text-overflow` won't cut it because it only works for 1 line (it needs `white-space: nowrap`). Plus I really wanted to trim by words, not characters, because _reasons_.

This code uses jQuery because it's already in my project, but there's no reason why this couldn't be changed to use native DOM methods.

```
function maintainElementLineCount($el, lineCount) {
  const originalText = $el.text()

  function _inner() {
    $el.text(originalText)

    const words = originalText.split(' ')

    while (words.length) {
      const elLineCount = $el.height() / parseFloat($el.css('line-height'))

      if (elLineCount <= lineCount) return

      words.pop()
      $el.text(words.join(' ') + '...')
    }
  }

  $(window).on('resize', _.throttle(() => _inner(), 100))
  setTimeout(() => _inner(), 100)
}
```

It works like so:

1. copy the original text
2. on resize (throttled to 100ms) and 100ms after the function is called, put the original text back in the element, then keep trimming off one word at a time until the height is correct.

Basically it just brute forces trimming the text until it's the correct height. Not especially efficient but it performs well wherever I've tested it - I'm starting with short original text so it only usually trims 5 to 10 words at most, and looping through the entire thing is ok. It could be optimised by figuring out if the resize was bigger or smaller, and working in that direction rather than just starting from the original text, which would be recommended if you're trying to trim lots of text into a small target area. There are also probably edge conditions that make it fall over but I haven't encountered anything serious.

Note that the height of a single line is taken from the CSS `line-height` property. There may be cases where this doesn't work correctly but our line heights are relatively sane.

This also won't work if the element's inner text changes dynamically, as the original text is recorded and closed over by the inner function. Either something would need to watch for changes to the element's contents, or the handler would need to be detached and set up again manually when changing the inner text.

To make this work using a data attribute (with static markup only as I haven't needed to use this with dynamic markup yet - I'll update this post if I ever need it):

```
$('[data-line-limit]').each(function() {
  const $el = $(this)
  const lineLimit = parseInt($el.data('line-limit'))

  maintainElementLineCount($el, lineLimit)
})
```

For example:

```
<p id="my_paragraph" data-line-limit="2">lots of text here</p>
```

