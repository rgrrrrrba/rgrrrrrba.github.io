---
title: Xamarin Studio can't find my Android device
layout: post
date: 2014-03-23
category: link
---

**Know what?** This doesn't work. Try setting the USB transfer mode (Settings, Storage, (in the menu) USB computer connection) to 'Camera (PTP)'. Found in [this StackOverflow answer](https://stackoverflow.com/a/16797152/149259). I'll leave the below for reference in case it comes in handy.

### Messing with Android Developer Bridge (ADB)

I installed Xamarin Studio on Windows 8 to start on some Android development. I was able to debug directly on my little oldish Nexus 7 (running Android 4.4.2) (Xamarin's instructions are [here](https://docs.xamarin.com/guides/android/getting_started/installation/set_up_device_for_development/)):

- after enabling debug mode on the device:
	- 'Settings'
	- 'About tablet'
	- Tap 'Build number' _seven_ times
	- Go back, and 'Developer options' will be at the bottom of the 'Settings' screen
	- Within 'Developer options', check 'USB debugging'
- and installing the OEM drivers for the tablet:
	- My device is an Asus Nexus 7, so I downloaded the drivers from [ASUS](https://www.asus.com/Tablets_Mobile/Nexus_7/#support)
	- In 'Device Manager' under 'Other Devices', right click the device and 'Update Driver Software'
	- Browse to and select the downloaded OEM driver

This worked fine, yay for me. The problem was, after restarting my system (after an upgrade to Windows 8.1 (how seamless is that now by the way?)) Xamarin Studio wasn't seeing my device. It was still showing up in Device Manager correctly and debug mode was still enabled, but something broke.

Turns out the something is the Android Debug Bridge, or ADB. I found a reference deep in [this document (WARNING PDF)](https://docs.xamarin.com/guides/android/troubleshooting/offline.pdf) from Xamarin:

![](https://i.imgur.com/uWvypuM.png)

Helpful but not extremely helpful. That link to `adb program` is broken by the way. I found some nicer information about the Android Debug Bridge on [Android's developer site](https://developer.android.com/tools/help/adb.html).

Next step is actually finding the ADB program, which is part of the Android SDK. I looked all over my machine for the SDK. _Turns out_ Xamarin installs the SDK in the user's local AppData directory, in `X:\Users\username\AppData\Local\Android\android-sdk`. Then `adp.exe` lives in the `platform-tools` directory.

After all that, I dropped a batch file on my desktop to restart the bridge, in case this happens again:

	@C:\Users\bec\AppData\Local\Android\android-sdk\platform-tools\adb.exe kill-server
	@C:\Users\bec\AppData\Local\Android\android-sdk\platform-tools\adb.exe start-server
	@pause

Results:

![](https://i.imgur.com/fqjVl9L.png)
