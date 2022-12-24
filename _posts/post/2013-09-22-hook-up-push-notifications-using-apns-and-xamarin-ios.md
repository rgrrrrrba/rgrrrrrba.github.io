---
title: Hook up push notifications using APNS and Xamarin.iOS
layout: post
date: 2013-09-22
category: archived
---

I'm adding push notifications to an iOS app, sent from an ASP.NET MVC application. I'm developing the iOS application on a Mac using Xamarin Studio and the ASP.NET application on Windows using Visual Studio. I haven't taken this past a sandbox environment. The server component uses [PushSharp](https://github.com/Redth/PushSharp), a library that handles the interface to Apple's APNS as well as Google and Microsoft's equivalent services for Android and Windows phones.

This post skims over some important frameworks:

- [PushSharp](https://github.com/Redth/PushSharp)
- [RestSharp](https://restsharp.org/)
- ASP.NET Web API

### 1. Create certificates and profiles

Create an app ID for the application. This is then used to create a provisioning profile and certificate. A wildcard id/profile won't do as the APN service needs an explicit App ID.

> If you plan to incorporate app services such as Game Center, In-App Purchase, Data Protection, and iCloud, or want a provisioning profile unique to a single app, you must register an explicit App ID for your app.
>
> To create an explicit App ID, enter a unique string in the Bundle ID field. This string should match the Bundle ID of your app.

Make sure `Push Notifications` is selected in the `App Services` section.

Generate, install and download a certificate and provisioning profile for the app ID.

You need to use the new provisioning profile for the application and set the bundle ID of the application to the app ID you created. You _don't_ need to include the App Id Prefix.


### 2. Export the certificate as a `.p12` file

You need a copy of the development certificate for the server (ASP.NET MVC) part of the application to talk to the APN service. This is done using Keychain Access. Pick the **Certificates** category on the left. This is crazy but you can't export a certificate as a `.p12` if you select the certificate with **All Items** selected. You _have_ to pick **Certificates**. Right-click or control-click the new certificate, it should be named something like `Apple Development IOS Push Services: com.your.application`. Select Export and make sure `Personal Information Exchange (.p12)` is selected. Export the certificate somewhere and copy it to the server project (the ASP.NET application). You will be prompted for a password. Make a note of the password.

### 3. Hook up PushSharp

Install PushSharp into the ASP.NET MVC application via [NuGet](https://www.nuget.org/packages/PushSharp). Add the `.p12` certificate somewhere so it is available to the project, for example as an embedded resource. If you open the project's properties, select 'Resources' then 'Add Resource', you can refer to the certificate directly in code as below.

In the application's startup code set up a `PushBroker`:

	var production = false;
	var password = "see step 2";
	var certificate = Properties.Resources.com_your_application; // or File.ReadAllBytes(pathToCertificate);

	var pushBroker = new PushBroker();
	pushBroker.RegisterAppleService(new ApplePushChannelSettings(
		production,
		certificate,
		password));
	// register pushBroker with an IOC container


### 4. Endpoint for recording device tokens

The server application needs some way of recording device tokens for the clients. Each device token is unique to the device so the token needs to be recorded against the user so that it can be used to push notifications to the user. Remember each user could have multiple devices so you may need to record multiple device token per user.

I just set up a simple Web API endpoint to record the device token. The endpoint will be called by the client in the next step.

    public class SetDeviceTokenController : ApiController
    {
        public HttpResponseMessage Post(SetDeviceTokenPostModel model)
        {
        	// save the device token for the user
        	//...

            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }

    public class SetDeviceTokenPostModel
    {
        public Guid UserId { get; set; }
        public string DeviceToken { get; set; }
    }

### 5. Client registration and device token

In the startup code for the client app you need to register the the notification types that will be sent from the server:

	UIApplication.SharedApplication.RegisterForRemoteNotificationTypes(notificationTypes);

`notificationTypes` is an OR masked set of `UIRemoteNotificationType`, which has the following possible values:

	Alert
	Badge
	NewsstandContentAvailability
	None
	Sound

You can register for a notification type even if the server never needs to send that type of notification, so registering `UIRemoteNotificationType.Alert | UIRemoteNotificationType.Badge | UIRemoteNotificationType.Sound` is a pretty safe bet.

Calling this registration the first time causes iOS to open a confirmation dialog for the user. Once the notifications are confirmed a method in the `AppDelegate` is called. Override the method to get the device token and send it to the server. This method may be called many times if the notification settings are changed or the app is restarted. You only need to send the token if it has changed from the last time it was sent. I'm using a `_settingsProvider` which uses a SQLite database to persist the last device ID sent - the `_settingsProvider` also has the base URL of my server's API. There is also a method that can be overriden if the registration failed. The endpoint is called using RestSharp in the `SendDeviceToken` method.

	public override void RegisteredForRemoteNotifications(UIApplication application, NSData deviceToken) {
	{
		var oldDeviceToken = _settingsProvider.DeviceToken;
		var newDeviceToken = GetCurrentDeviceToken(deviceToken);

		if (newDeviceToken != oldDeviceToken) {
			_settingsProvider.DeviceToken = newDeviceToken;
			SendDeviceToken();
		}
	}

	public override void FailedToRegisterForRemoteNotifications (UIApplication application, NSError error)
	{
		_log.LogInformation (error.ToString ());
	}

	static string GetCurrentDeviceToken(NSData deviceToken) {
		var strFormat = new NSString("%@");
		var dt = new NSString(MonoTouch.ObjCRuntime.Messaging.IntPtr_objc_msgSend_IntPtr_IntPtr(
			new MonoTouch.ObjCRuntime.Class("NSString").Handle, 
			new MonoTouch.ObjCRuntime.Selector("stringWithFormat:").Handle, 
			strFormat.Handle, 
			deviceToken.Handle));
		var currentDeviceToken = dt.ToString().Replace("<", "").Replace(">", "").Replace(" ", "");

		return currentDeviceToken;
	}

	void SendDeviceToken() {
		var client = new RestClient(_settingsProvider.ApiBaseUrl);

		var request = new RestRequest("SetDeviceToken", Method.POST);
		request.RequestFormat = DataFormat.Json;
		request.AddBody(new {
			UserId = _settingsProvider.UserId,
			DeviceToken = _settingsProvider.DeviceToken
		});

		var response = client.Execute(request);
		if (response.StatusCode != System.Net.HttpStatusCode.OK) {
			// handle the failure and reset the device token so it will try again next time:
			//...
			_settingsProvider.DeviceToken = "";
		}
	}


### 6. Sending a push notification

Assuming everything is configured correctly a notification can be pushed to a device from the server like so:

	_pushBroker.QueueNotification(new AppleNotification()
		.ForDeviceToken(...)
		.WithAlert("Alert text"));

The client app must have the notifications allowed by the user and the app must _not_ be in the foreground. If it is in the foreground the push notification has to be handled and displayed by the app itself, [as described here](https://roycornelissen.wordpress.com/2011/05/12/push-notifications-in-ios-with-monotouch/).


### References

- <https://github.com/Redth/PushSharp/blob/master/Client.Samples/PushSharp.ClientSample.MonoTouch/PushSharp.ClientSample.MonoTouch/AppDelegate.cs>
- <https://roycornelissen.wordpress.com/2011/05/12/push-notifications-in-ios-with-monotouch/>
- <https://developer.apple.com/library/mac/documentation/NetworkingInternet/Conceptual/RemoteNotificationsPG/Chapters/ApplePushService.html>
- <https://help.adobe.com/en_US/as3/iphone/WS144092a96ffef7cc-371badff126abc17b1f-7fff.html>
- <https://code.google.com/p/apns-sharp/wiki/HowToCreatePKCS12Certificate>