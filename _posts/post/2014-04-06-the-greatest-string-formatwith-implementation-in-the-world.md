---
title: The Greatest string.FormatWith() Extension Method Implementation In The World
layout: post
date: 2014-04-06
category: post
---

This is in fact the Greatest `string.FormatWith()` Extension Method Implementation In The World. I have taken it with me for a few projects now. I found it in Perth, probably thanks to my brother-in-code [Rob Moore](http://robdmoore.id.au/), and I brought it back along the breadth of our mighty continent. A 'Journey to the West', if you will. Less awesome implementations of `FormatWith()` are just simple inversions of `string.Format()`:

	public static string FormatWith(string s, params object[] args)
	{
		return string.Format(s, args);
	}

which you can use thusly:

	"This is {0} the {1} {2} in the {3}".FormatWith(
		"not", "greatest", "string.FormatWith() implementation", "world")

![](http://media.giphy.com/media/B9bZxUmVr3ZNS/giphy.gif)

Nice, but let's be honest, pitiful. Feel the power of this implementation.

    public static class StringExtensions
    {
        public static string FormatWith(this string format, params object[] args)
        {
            args = args ?? new object[0];
 
            var distinctNumberedTemplateMatches =
                (from object match in new Regex(@"\{\d{1,2}\}").Matches(format) select match.ToString())
                .Distinct().Count();
            if (distinctNumberedTemplateMatches != args.Length)
            {
                var argsDic = GetDictionaryFromAnonObject(args[0]);
 
                if (argsDic.Count < 1)
                {
                    throw new InvalidOperationException("Please supply enough args for the numbered templates or use an anonymous object to identify the templates by name.");
                }
 
                return argsDic.Aggregate(format, (current, o) => current.Replace("{{0}}".FormatWith(o.Key), o.Value.ToString()));
            }
 
            var validationInput = format;
            for (var i = 0; i < args.Length; i++)
            {
                format = format.Replace("{" + i + "}", args[i] == null ? string.Empty : args[i].ToString());
            }
            if (validationInput == format)
            {
                throw new InvalidOperationException(
                    "You can not mix template types. Use numbered templates or named ones with an anonymous object.");
            }
 
            return format;
        }
 
        private static IDictionary<string, object> GetDictionaryFromAnonObject(object args)
        {
            if (args == null)
            {
                return new Dictionary<string, object>();
            }
 
            return TypeDescriptor.GetProperties(args).Cast<PropertyDescriptor>()
                .ToDictionary(
                    property => property.Name, 
                    property => property.GetValue(args));
        }
    }

This can be used in the same way as before:

	"This is {0} the {1} {2} in the {3}".FormatWith(
		"definitely", "greatest", "string.FormatWith() implementation", "world, but why?")

Or it can go full [super-saiyan](http://dragonball.wikia.com/wiki/Super_Saiyan):

	"{who} is {what} who {action} with {target}!".FormatWith(new{
		who = "Hero",
		what = "he alone",
		action = "vies",
		target=  "powers supreme"})

![](http://media.giphy.com/media/6KlLzO38CkLjG/giphy.gif)

**WARNING**: Now this is probably not the most performant code. If you're going to run this in a tight loop you may want to go with ye olde `string.Format()`. The rest of the time, go forth and be awesome.

