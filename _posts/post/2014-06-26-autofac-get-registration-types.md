---
title: Resolving a list of type registrations from Autofac
layout: post
date: 2014-06-26
category: archived
---

I needed to get all types that implemented a given interface that were registered with Autofac. Not a set of factories, that would be as simple as this:

	class Consumer
	{
		public Consumer(IEnumerable<Func<IFoo>> fooFactories)
		{
			// ...
		}
	}

What I really need is something like `Consumer(IEnumerable<Type> fooTypes)` where the types are everything implementing `IFoo`, but of course Autofac can't resolve that.

Inspired by [this StackOverflow answer](https://stackoverflow.com/a/9503695/149259), here's an extension method that does what I need:

    public static class LifetimeScopeExtensions
    {
        public static IEnumerable<Type> GetImplementingTypes<T>(this ILifetimeScope scope)
        {
            return scope.ComponentRegistry
                .RegistrationsFor(new TypedService(typeof (T)))
                .Select(x => x.Activator)
                .OfType<ReflectionActivator>()
                .Select(x => x.LimitType);
        }
    }

To use it my consumer just takes an `ILifetimeScope` dependency:

	public Consumer(ILifetimeScope scope) 
	{
		var fooTypes = scope.GetImplementingTypes<IFoo>();
	}

Note that I needed to register the types both as the base type `IFoo` (for the implementing type resolution) and as self, so I could later resolve using `scope.Resolve(fooType)`. My registration looks like this:

            builder.RegisterAssemblyTypes(typeof (IFoo).Assembly)
                .Where(t => t.IsAssignableTo<IFoo>())
                .Where(t => !t.IsAbstract)
                .As<IFoo>()
                .AsSelf()
                .InstancePerDependency();


![](https://media.giphy.com/media/xQzml5M6C8Wly/giphy.gif)

