---
title: Scope for MonoTouch UIAlertViews
layout: post
date: 2013-08-09
category: archived
---

I'm currently working on a MonoTouch app - after a month I'm finally starting to come around to the stack. 
One issue I've been having is a crazy native code crash when I display a `UIAlertView`. The problem turned
out to be objects going out of scope.

The first tip is to use a delegate for the alert view and keep it in the class scope:

    public class MyAlertViewDelegate {
      readonly Action _callback;
      public MyAlertViewDelegate(Action callback) {
        _callback = callback;
      }
      public override Clicked(UIAlertView alertview, int buttonIndex) {
        _callback();
      }
    }
    
    public class ParentClass {
      MyAlertViewDelegate _myAlertViewDelegate;
      
      void ShowAlert() {
        _myAlertViewDelegate = new MyAlertViewDelegate(() => {});
        var alert = new UIAlertView("title", "message", _alertViewDelegate, "Ok");
        alert.Show();
      }
    }

The second tip is to make sure the ParentClass instance doesn't go out of scope. This is probably by giving 
it a lifetime scope in the IoC container. For example in TinyIoC:

    container.Register<ParentClass>().AsSingleton();

