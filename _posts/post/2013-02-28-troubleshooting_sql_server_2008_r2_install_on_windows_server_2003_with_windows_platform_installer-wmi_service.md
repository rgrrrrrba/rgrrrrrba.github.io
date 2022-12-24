---
title: Troubleshooting SQL Server 2008 R2 install on Windows Server 2003 with Windows Platform Installer – WMI service
layout: post
date: 2013-02-28
type: regular
category: archived
---

Short story. Trying to install SQL Server 2008 R2 Express on a Windows Server 2003 (SP2) machine using [WebPI](https://www.microsoft.com/web/downloads/platform.aspx) – usually the best way to get server bits by far – but it fails with a meaningless message after a lengthy reboot that interrupted a small office.

I tried the [standalone installer](https://www.microsoft.com/en-us/download/details.aspx?id=30438) and got a more helpful error regarding the Windows Management Instrumentation (WMI) service. In Services the WMI service appeared to be running fine but [this MSDN thread](https://social.msdn.microsoft.com/Forums/en/sqlexpress/thread/bae90cfd-702d-427f-a4df-c66cc8c4d56d) reveals the problem and gives a solution. It’s an old problem that I’ve seen before relating to corrupt WMI bits. The forum post provides the following batch file that reregisters the service:

	@echo on
	cd /d c:\temp
	if not exist %windir%\system32\wbem goto TryInstall
	cd /d %windir%\system32\wbem
	net stop winmgmt
	winmgmt /kill
	if exist Rep_bak rd Rep_bak /s /q
	rename Repository Rep_bak
	for %%i in (*.dll) do RegSvr32 -s %%i
	for %%i in (*.exe) do call :FixSrv %%i
	for %%i in (*.mof,*.mfl) do Mofcomp %%i
	net start winmgmt
	goto End

	:FixSrv
	if /I (%1) == (wbemcntl.exe) goto SkipSrv
	if /I (%1) == (wbemtest.exe) goto SkipSrv
	if /I (%1) == (mofcomp.exe) goto SkipSrv
	%1 /RegServer

	:SkipSrv
	goto End

	:TryInstall
	if not exist wmicore.exe goto End
	wmicore /s
	net start winmgmt

	:End 

Go right ahead and blindly run this script _Found On The Internet&trade;_ on your mission critical server but remember. Your mileage. It may vary.

