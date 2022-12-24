---
title: The Greatest string.FormatWith() Extension Method Implementation In The World
layout: post
date: 2014-04-06
category: archived
---

This is in fact the Greatest `string.FormatWith()` Extension Method Implementation In The World. I have taken it with me for a few projects now. I found it in Perth, ~~probably thanks to my brother-in-code [Rob Moore](https://robdmoore.id.au/)~~ **correction** turns out credit is due to [Matt Kocoj](https://hammerproject.com/), and I brought it back along the breadth of our mighty continent. A 'Journey to the West', if you will. Less awesome implementations of `FormatWith()` are just simple inversions of `string.Format()`:

	public static string FormatWith(string s, params object[] args)
	{
		return string.Format(s, args);
	}

which you can use thusly:

	"This is {0} the {1} {2} in the {3}".FormatWith(
		"not", "greatest", "string.FormatWith() implementation", "world")

![](https://media.giphy.com/media/B9bZxUmVr3ZNS/giphy.gif)

Nice, but let's be honest, pitiful. Feel the power of this implementation.

    public static class StringExtensions
    {
        public static string FormatWith(this string format, params object[] args)
        {
            args = args ?? new object[0];
            string result;
            var numberedTemplateCount = (from object match in new Regex(@"\{\d{1,2}\}").Matches(format) select match.ToString()).Distinct().Count();

            if (numberedTemplateCount != args.Length)
            {
                var argsDictionary = args[0].ToDictionary();

                if (!argsDictionary.Any())
                {
                    throw new InvalidOperationException("Please supply enough args for the numbered templates or use an anonymous object to identify the templates by name.");
                }

                result = argsDictionary.Aggregate(format, (current, o) => current.Replace("{" + o.Key + "}", (o.Value ?? string.Empty).ToString()));
            }
            else
            {
                result = string.Format(format, args);
            }

            if (result == format)
            {
                throw new InvalidOperationException("You cannot mix template types. Use numbered templates or named ones with an anonymous object.");
            }

            return result;
        }
    }

    public static class ObjectExtensions
    { 
        public static IDictionary<string, object> ToDictionary(this object o)
        {
            if (o == null) return new Dictionary<string, object>();

            return TypeDescriptor
                .GetProperties(o).Cast<PropertyDescriptor>()
                .ToDictionary(x => x.Name, x => x.GetValue(o));
        }
    }

This can be used in the same way as before:

	"This is {0} the {1} {2} in the {3}".FormatWith(
		"definitely", "greatest", "string.FormatWith() implementation", "world, but why?")

Or it can go full [super-saiyan](https://dragonball.wikia.com/wiki/Super_Saiyan):

	"{who} is {what} who {action} with {target}!".FormatWith(new{
		who = "Hero",
		what = "he alone",
		action = "vies",
		target = "powers supreme"})

![](https://media.giphy.com/media/6KlLzO38CkLjG/giphy.gif)

**WARNING**: Now this is probably not the most performant code. If you're going to run this in a tight loop you may want to go with ye olde `string.Format()`. The other 99.99% of the time, go forth and be awesome.

### Updates

- 2014-04-23: `ToDictionary()` is now in it's own extension class, the named template conversion is simpler, and the numbered template conversion just uses `string.Format`.

