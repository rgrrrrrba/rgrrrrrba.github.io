---
title: Create a shared mailbox in Office 365
layout: post
date: 2013-02-11
category: archived
---

I keep needing to find the instructions for this so I'm putting them all together in one place. The steps are gathered from here and here and assume that PowerShell is installed.

1. Connect PowerShell to das cloud:
	1. Get the credentials for an Office 365 administrator: `$LiveCred = Get-Credential`
	2. Create a session pointing to O365: `$Session = New-PSSession -ConfigurationName Microsoft.Exchange -ConnectionUri https://ps.outlook.com/powershell/ -Credential $LiveCred -Authentication Basic -AllowRedirection`
	3. There will probably be a yellow message warning that the connection has been redirected to `podXXX.outlook.com/...`
	4. `Import-PSSession $Session` – this shows a couple of scroll bars as the O365 commands are loaded in to the PS session.
2. Create the mail box: `New-Mailbox -Name "Shared mail box name" -Alias shared_mail_box -Shared`
	1. The `shared_mail_box` alias shown above is used as the name of the mail box, eg `helloworld@becdetat.com`
3. In the Exchange control panel for the organisation (<https://microsoftonline.com>, _Outlook_, _Options_, _See all options_, _Manage_, _Manage My Organization_) create a new distribution group for the people that will need to be able to access the shared mailbox. When creating the group make it a security group.
	1. note that the name of the security group cannot be the same as the shared mailbox created in step 2, I add ‘SG’ to the end od of the group name, eg `HelloWorldSG`
4. Give the security group access to the mailbox:
	1. `Add-MailboxPermission "Shared mail box name" -User HelloWorldSG -AccessRights FullAccess`
	2. `Add-RecipientPermission "Shared mail box name" -Trustee HelloWorldSG -AccessRights SendAs`

As the instructions say:

> Note It may take up to 60 minutes until users can access a new shared mailbox or until a new security group member can access a shared mailbox

I have seen it take a few days in some cases although that may be user error or bad caching. Once the permissions flow through the new mailbox can hopefully be added to Outlook.