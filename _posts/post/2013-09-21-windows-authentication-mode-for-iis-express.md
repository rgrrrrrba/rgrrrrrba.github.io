---
title: Enabling Windows authentication in IIS Express
layout: post
date: 2013-09-21
category: archived
---

So I'm changing a site developed using Forms authentication to use Windows authentication, testing in IIS Express.


### Thread.CurrentPrincipal.Identity.Name
The first thing to be aware of is that in Forms authentication you set the thread principal yourself when logging in. With Windows authentication `Thread.CurrentPrincipal.Identity.Name` is set automatically (as long as the rest of these instructions are correct). So nothing should have to be changed in the application code as long as you're already using `Thread.CurrentPrincipal.Identity.Name` with the Forms authentication.


### Web.config
The first step is to get rid of the Forms authentication section in the application's `Web.config`. Mine looked like this:

	<system.web>
    	...

		<!-- Comment out this to disable Forms authentication -->
		<authentication mode="Forms">
			<forms loginUrl="/LogIn" timeout="30">
				<credentials passwordFormat="Clear">
					<user name="admin" password="test" />
					<user name="state_admin" password="test" />
					<user name="api" password="test" />
				</credentials>
			</forms>
		</authentication>

	</system.web>


### IIS Express configuration
You can find the IIS Express configuration in `X:\Users\your_account\Documents\IISExpress\config\applicationhost.config`. First you need to find the name of the site. Search for the `<sites>` node and look for the site. It should be something like this:

    <site name="YourSite" id="6">
        <application path="/" applicationPool="Clr4IntegratedAppPool">
            <virtualDirectory path="/" physicalPath="C:\source\YourSolution\src\YourSite.Web" />
        </application>
        <bindings>
            <binding protocol="http" bindingInformation="*:57635:localhost" />
            <binding protocol="http" bindingInformation="*:57635:192.168.80.103" />
        </bindings>
    </site>

This site's name is `YourSite`. You need to add (or alter) the site's `system.webServer` overrides. Search for `location path="YourSite"` but chances are you need to create a new one. Add this right at the bottom of the file, just before the closing `</configuration>` tag:

    <location path="YourSite">
        <system.webServer>
            <security>
                <authentication>
                    <anonymousAuthentication enabled="false" />
                    <windowsAuthentication enabled="true" />
                </authentication>
            </security>
        </system.webServer>
    </location>

That should be all that is required to enable Windows authentication in an IIS Express ASP.NET application.


### Check the authentication mode
One thing you can change in the application code is a check for the authentication mode being used. This can be used to hide a "Log out" link for example. You can get the authentication mode using this method:

    public AuthenticationMode GetAuthenticationMode()
    {
        var configuration = WebConfigurationManager.OpenWebConfiguration("/");
        var authenticationSection = (AuthenticationSection)configuration.GetSection("system.web/authentication");
        return authenticationSection.Mode;
    }




