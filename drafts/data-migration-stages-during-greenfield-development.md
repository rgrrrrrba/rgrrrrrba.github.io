---
title: Stages of database migration techniques during greenfield development
layout: post
date: 2014-03-04
category: post
---



## Denial:

- automatic migrations using EF can be useful
- you don't know the shape of the domain
- exploratory phase
- quick turnaround, such agile
- easy to merge with other developers as you don't have to worry about the data structure
- At this stage deciding on the database or lack thereof can even be deferred
	- flat files, document database, whatever
	- even the ORM being used is open to change

## Anger

Your domain and infrastructure is getting more stable but the lack of process is causing frustration. you need to do some consolidation to bring things together. Maybe two developers have different ideas and the lack of cohesion causes problems.

Automatic EF migrations will start to cause issues with versioning and database stability.

You may be tempted to move to manual (non-automatic) EF migrations
	- Create an initial migration with the current state of the world
	- Create manual migrations to manage mutating the state of the world
	- Manual migrations cause versioning and merge issues
	- Frustration will increase between developers
	- overhead increases exponentially

This is a retrograde stage and should be avoided. The problem is an over-reliance with the tooling. EF for example ...


## Bargaining

Some of the ideas in the last stage have merit:
- Creating an initial state of the world, and
- Applying migrations to mutate that state, to manage change

The problem is the reliance on tooling to assist with these migrations. Move to a more manual system such as DBUp or FluentMigrations.

- data can still potentially get blown away with each change to the schema
- onus is placed on the developer to maintain the state of the database so that it continues to work with the ORM and domain

This is also a good stage to seperate data migrations out into a separate process. An example is a "reset the world" type script that is built and executed independently of the rest of the application. This adds a manual step, but at this stage it is still less work than the manually blowing apart and recreating the database using tooling which is likely to be the process prior to this stage.

Thought could probably be put into recreating test data between world resets. This could be via a second suite of migrations that insert test data, or it could be an established backup of the world that is restored and migrated as part of a a "reset the world with data" script.


## Depression

Although we now have a relatively stable data layer and a robust migration infrastructure, having to carry around that established backup becomes a burden, especially when running migrations that are destructive. If you're still struggling with the migrations at all, you're losing valuable time. By now you should be thinking about the production environment and ongoing maintenance and development of the system.


## Acceptance

