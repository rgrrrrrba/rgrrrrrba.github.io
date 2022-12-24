---
title: Web API JSON serialization of enums pro-tip
layout: post
date: 2013-09-06
category: archived
---

When serializing an enum value in Web API as JSON it will use the integer value of the enum by default. To make it serialize as the `ToString()` value (eg. `UserType.Admin` as `"Admin"` instead of `2`) add this to `WebApiConfig.cs` (in the `AppStart` directory):

	config.Formatters.JsonFormatter.SerializerSettings.Converters.Add(new StringEnumConverter());


