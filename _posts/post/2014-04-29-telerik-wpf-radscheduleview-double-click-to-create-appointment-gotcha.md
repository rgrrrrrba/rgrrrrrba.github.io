---
title: Telerik's WPF RadScheduleView - Double-click to create an appointment gotcha
layout: post
date: 2014-04-29
category: archived
---

I'm using [Telerik's RadScheduleView](https://www.telerik.com/help/wpf/radscheduleview-overview.html) in a WPF project. Double-clicking the grid to create an appointment suddenly stopped working.

*Turns out* I had changed my custom appointment subclass's default constructor from `public` to `protected`. The schedule view needs a public default constructor or it will just silently not create new appointments:

    public class MyCustomAppointment
    {
        // this is required to double-click to create
        public MyCustomAppointment()
        {
            // ...
        }

        // non-default constructor can still be called in code
        public MyCustomAppointment(...)
        {
            // ...
        }
    }