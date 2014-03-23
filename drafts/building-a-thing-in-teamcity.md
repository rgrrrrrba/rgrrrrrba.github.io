title: Building a thing in TeamCity

**DISCLAIMER:** I'm learning in the open here. I have barely used Azure and have only ever used already established TeamCity instances, so I'm really just messing around with two new things at the same time here. I'm sure I'm missing a heap of very important points.


In my last post I [set up an Azure VM and installed TeamCity](set-up-teamcity-on-an-azure-instance.html). ~~In this post I'm going to configure TeamCity to build a pretty simple .NET library that has source control on [GitHub](http://github.org). Hopefully I'm going to get builds happening with every push to master and on every pull request that the library receives.~~



~~The library is [invariably](http://github.com/bendetat/invariably), a helper library for [class invariants](http://en.wikipedia.org/wiki/Class_invariant) which is a fancy term for 'throw an exception if some condition is true'. The selling point for invariably is the way it formats the exception message, but back to the story.~~



## Stop, stop stop.

I probably don't even need to write this. [Medhi Khalili](http://www.mehdi-khalili.com) already has a post on [Github and TeamCity](http://www.mehdi-khalili.com/continuous-integration-delivery-github-teamcity) that covers what I need.

So I'm just using Medhi's blog and a couple of other resources to set this up. There's a full list of resources at the end.

## Caveats

### GitHub pull requests getting build notifications

Medhi mentions TeamCity automatically adding notifications to GitHub pull requests, and it sounds like this is out of the box. However, Hadi's post that Medhi links to explains the feature is a plugin that needs to be added to TeamCity. The `TeamCity.GitHub` plugin project page is [here](https://github.com/jonnyzzz/TeamCity.GitHub) and the zip containing the plugin can be downloaded directly from [here](http://teamcity.jetbrains.com/guestAuth/repository/download/bt398/lastest.lastSuccessful/teamcity.github.zip). Copy that zip file (_as a zip file_) to the `plugins` folder inside the TeamCity data folder (`e:\TeamCity-data`) if you're following my previous post). Then restart the TeamCity server service.

![](http://i.imgur.com/LSgl6wn.png)

> It would be really cool if something similar existed for BitBucket pull requests as this is a big part of the workflow we are using in teams at $work.


## Resources and references

- [Medhi Khalili - Continuous Integration & Delivery for GitHub with TeamCity](http://www.mehdi-khalili.com/continuous-integration-delivery-github-teamcity)
- [Hadi Hariri - Automatically Building Pull Requests from GitHub with TeamCity](http://hadihariri.com/2013/02/06/automatically-building-pull-requests-from-github-with-teamcity/)