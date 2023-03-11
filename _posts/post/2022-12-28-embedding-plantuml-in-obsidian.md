---
title: Embedding PlantUML in Obsidian
permalink: /embedding-plantuml-in-obsidian
layout: post
date: 2022-12-28
category: post
---

## Just the facts
1. Install the [PlantUML plugin for Obsidian](obsidian://show-plugin?id=obsidian-plantuml)
2. Use Docker to get a local PlantUML server: `docker pull plantuml/plantuml-server; docker run -d -p 8180:8080 plantuml/plantuml-server:jetty`
3. Set the PlantUML plugin's "Server URL" config setting to `http://localhost:8180`
4. Write some PlantUML in an Obsidian file:

<pre>
```plantuml
Bob -> Alice : hello
```
</pre>


## The full story

I recently (re)discovered [PlantUML](https://plantuml.com/) thanks to colleague and all round nice person [Shaw Innes](https://shawinnes.com/). I was reviewing and documenting a large number of applications for a client, and needed to quickly generate and maintain a basic diagram for each application's dependencies and consumers. It ended up being around 50 diagrams. [C4](https://c4model.com/) was a good candidate for the structure of the diagrams, and Shaw had been working on using PlantUML to create C4 diagrams. In fact, he recently did a talk at the 2022 Brisbane DDD about documentation as code, which included PlantUML and C4 along with a lot more content. The slides are available on Shaw's blog [here](https://shawinnes.com/documentation-for-developers/).

For reference, the PlantUML extension that Shaw recommended has a GitHub repo [here](https://github.com/plantuml-stdlib/C4-PlantUML). To use it in a PUML file the relevant URLs need to be included. See the example near the end of this post.

To manage the documentation for that client I created a `.puml` file for each application I reviewed. Using the [PlantUML extension for VS Code](https://marketplace.visualstudio.com/items?itemName=jebbs.plantuml) I was able to get fairly quick feedback on the PUML as I was writing it, and then with a single click I could copy the generated PNG to the clipboard and paste it into the client's report. Then if I needed to change a diagram, I could easily update the PUML, copy the image, and just paste it back to the report. The PUML files were in a little local Git repo, which I copied over to the client's SharePoint at the end of the engagement—I didn't have (or need) write access to their source control systems. As most of the diagrams were structurally fairly similar, I could quickly copy a template file for a new diagram and have it in the report within a few minutes.

This worked pretty well, but the initial setup was painful—I needed to install the JRE locally so the PlantUML extension could generate the diagrams, and as for some reason Homebrew wasn't playing nice I had to eventually install it from Oracle's site. I did get it working eventually though. Generating the diagram also takes a good 10 seconds or so, enough to be annoying. I'm guessing this is partly because there's a debounce built in after an edit (which didn't seem to be configurable so I don't know how much of an impact it had), and partly because it probably has to spin up the PlantUML Java executable every time it runs.

At the end of the day though this was a really good experience, and I know I want to use PlantUML and C4 in the future, for documentation-as-code within a codebase, designing architecture diagrams, writing reports and proposals, messing around XKCD-style, etc.

I've been using [Obsidian](https://obsidian.md/) a _lot_. Part of what I'm doing is writing initial drafts of documents for various purposes. This is much closer to documentation-as-code, as I will probably end up setting up a small repo for each of these documents or projects I'm working on, and open them as vaults within Obsidian. Then I export the markdown file as a Word document to do final edits and share it around. See [the Obsidian section](/how-i-use-stream-deck#obsidian) in my previous post about my Stream Deck setup for a bit of a primer on how to use Pandoc to export to `.docx` within Obsidian.

What I really wanted to do was to embed PlantUML diagrams within the `.md` files and have Obsidian generate the image automatically. Since Obsidian has literally fifteen billion community plugins (ok, 759 at the time of writing) there was of course a [PlantUML plugin (Obsidian link)](obsidian://show-plugin?id=obsidian-plantuml) already available, written by [Johannes Theiner](https://github.com/joethei/).

The plugin doesn't include the PlantUML executable, or a server (which is the usual way of generating PlantUML diagrams). As I said above, I had to mess around with setting up the JVM when I was getting PlantUML working in VS Code, but since I already had that working I thought I would try to just use it. Unfortunately it didn't work, I struggled for longer than I should, and I eventually gave up for a couple of weeks.

I still had "PlantUML in Obsidian" on my TODO list though, and when I revisited it this morning I saw the following in the plugin's documentation:

>This plugin uses either the PlantUML Online Server, or a local .jar file for rendering.
>
>You can also host your own server (Docker / JEE / PicoWeb) and specify its address in the settings.
>
>Please note that using the local rendering method is not as performant as using a server.

The last point was very interesting—maybe this was to do with the relatively slow feedback loop I was experiencing when regenerating the diagram in VS Code. I had the idea that running the PlantUML through a server would in fact be _slower_ than running it through a local process. I also didn't realise there was a Docker image for the PlantUML server—I hadn't wanted to use the public server (even though as a consumer of open source technologies I appreciate its existence) largely because these can be commercially sensitive client or internal diagrams so I want to keep the content as private as possible.

Since I already have Docker installed for other projects I went with that:

<div class="code-section">
  {% include copy_button.html target="#section-1" %}
  <pre id="section-1">
docker pull plantuml/plantuml-server
docker run -d -p 8180:8080 plantuml/plantuml-server:jetty</pre>
</div>

<aside class="pull-right well" style="width:40%">
	I have grand plans of setting up a little Synology NAS for backups and serving media files that I'll never watch (although kidlette has a dinosaur documentary series he wants to revisit). One of the side-benefits of a Synology is that it can run Docker, so when I eventually buy the smallest practical NAS I can afford I'll move the PlantUML server onto that so I can access it on different machines, free up some resources on my laptop (and make it easier if I upgrade or repave), and put it behind a VPN (which the Synology also can do) so I can use it from a coffee shop or hospital or a police waiting room. It pays to be prepared.
</aside>

Then in the PlantUML plugin "Server URL" setting, I used `http://localhost:8180`. And...perfection.

To embed [PlantUML's 'canonical' example](https://plantuml.com):

<div class="code-section">
  {% include copy_button.html target="#section-2" %}
<pre style="width:50%" id="section-2">
```plantuml
Bob -> Alice : hello
```</pre>
</div>

Note that the `plantuml` code type is required for the plugin to find and process the block.

That block generates this diagram:

![](/images/2022-12-28-plantuml-in-obsidian/plantuml-example.png)

Here's a more complex C4-ish diagram, similar to those I was generating for the report I mentioned at the start of this post:

<div class="code-section">
  {% include copy_button.html target="#section-3" %}
<pre id="section-3">
@startuml Context

title Admin System

!include https://raw.githubusercontent.com/plantuml-stdlib/C4-PlantUML/master/C4_Context.puml
!include https://raw.githubusercontent.com/plantuml-stdlib/C4-PlantUML/master/C4_Container.puml

Person(office, "Office")
Person(admins, "Admins")
System_Boundary(system, "Admin") {
    Container(app, "Application", ".NET Core 3.1")
    ContainerDb(db_widgets, "MSSQL (Widgets)")
    ContainerDb(db_docs, "MSSQL (Docs)")
}
Container(cdn, "CDN")
System_Ext(primary_api, "Primary API")
System_Ext(secondary_api, "Secondary API")
System_Ext(file_share, "File Share")

Rel(office, app, "Uses")
Rel(admins, app, "Uses")
Rel(app, db_widgets, "Depends On")
Rel(app, db_docs, "Depends On")
Rel(app, cdn, "Depends On")
Rel(app, primary_api, "Depends On")
Rel(app, secondary_api, "Depends On")
Rel(app, file_share,"Depends On")

@enduml</pre>
</div>

The generated diagram is quite client-ready:

![](/images/2022-12-28-plantuml-in-obsidian/c4-example.png)

Exporting the files in Obsidian to `.pdf` and `.docx` work perfectly—in Word they are even resizable and work exactly as one would expect.

And the documentation gods rejoiced!

