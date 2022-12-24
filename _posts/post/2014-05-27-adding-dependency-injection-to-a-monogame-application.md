---
title: Adding dependency injection to a MonoGame application
layout: post
date: 2014-05-27
category: archived
---

### Disclaimer

**This is not a tutorial!** This is an explanation of how I have modified the code that I ended up with at the end of the tutorials. These modifications lay the foundation for a cleaner codebase that I will extend upon in the future.

For people who are familiar with dependency injection, and more familiar with dependency injection than MonoGame, **this is not for you**. This is a basic explanation of how I pulled apart a slightly complex spaghetti-coded solution and added dependency injection to make it easier to work with in the future. Think of it as 'using dependency injection in a MonoGame app for people that don't do dependency injection but have started to learn MonoGame'. **I am only learning MonoGame**. These probably aren't best practices and will have numerous issues that expert game developers will laugh at. *Laugh away, experts.*

This also doesn't go into dependency injection in a lot of detail. There are plenty of places to learn about dependency injection but I found the best way to learn was to work in a basic system that uses dependency injection. Which I'm conveniently building here. You're welcome.


### Building a Shooter Game

I worked my way through all six of [Tara Walker's "Building a Shooter Game"](https://blogs.msdn.com/b/tarawalker/archive/2012/12/04/windows-8-game-development-using-c-xna-and-monogame-3-0-building-a-shooter-game-walkthrough-part-1-overview-installation-monogame-3-0-project-creation.aspx) tutorials, but the series ends abruptly and hasn't been updated for nine months. So I'm going to continue with my experimenting with the game, and I'll try to document the process as I go.

If you want to play along (sorry) you can either go through Tara's tutorials then tackle this yourself, or download my solution either [before](https://github.com/becdetat/monogame-tw-tutorial/commit/7398a2628bffb1c931e6755d9ba3a1737c320e36) or [after](https://github.com/becdetat/monogame-tw-tutorial/commit/26a12e3595604d807fbe0c76417e34fb99155007) these changes. I would recommend going through the tutorial at least to get an understanding of the way the game was built up to the final state. It only takes around 1-2 hours all up. You will need to [install and configure MonoGame with Visual Studio](up-and-running-with-monogame.html) before starting.

The first thing I wanted to do is to refactor the code so I can use dependency injection. I have two reasons for this:

- Refactoring and dependency injection will clean up a lot of the dependencies through the project
- Math is hard, and to be able to easily unit test my calculations I need the cleaner dependencies


### Math is hard, dependency injection is easy

I started by adding Autofac to the `Win8ShooterGame` solution - the solution that contains all of the game code. This is a Windows 8 Store App. Autofac should work whether you're targeting Win 8 store apps, Windows desktop games, or Windows Phone 8. I'm not sure about iOS but it might also work for Android, Linux and Mac OS X if you're targeting those platforms.

Then I had to decide where the DI container was going to be configured and live. The `Main()` method is usually a good place, but in a MonoGame application the `Main()` method just calls a factory that creates the game instance and runs it. There are no obvious entry points to configure the game. So I decided to do the configuration in the game instance itself. In my repo this is the `ShooterGame` class.

Initially I wanted to put the configuration in the game class's constructor. It already had the initial configuration and seemed like the obvious choice. However due to the game's lifecycle, few of the things I need to configure Autofac with exist in the constructor. In fact the best place I found was in the `LoadContent()` method. By this stage the constructor and `Initialize()` methods have already been executed. Here's the basic game lifecycle for reference:

	ShooterGame() -> Initialize() -> LoadContent() -> Update() -> Draw() -> UnloadContent()
	                                       ^------------/               

This seems a little late to be configuring the container so I hope this won't come back to bite me later, but it's working fine for now. Here is my trimmed down constructor:

    public ShooterGame()
    {
        _graphics = new GraphicsDeviceManager(this);
    }

Here's something interesting. Note that I still need the constructor to create the `GraphicsDeviceManager` instance. This instance isn't actually directly used anywhere in the code - I could remove it and the project would still compile, but as soon as I try to run it I get an exception:

	An unhandled exception of type 'System.InvalidOperationException' occurred in MonoGame.Framework.DLL
	Additional information: No Graphics Device Manager

The reason for this lies in the `GraphicsDeviceManager` constructor:

	public GraphicsDeviceManager(Game game)
	{
		if (game == null)
			throw new ArgumentNullException("The game cannot be null!");
		this._game = game;
		this._supportedOrientations = DisplayOrientation.Default;
		this._preferredBackBufferWidth = Math.Max(this._game.Window.ClientBounds.Height, this._game.Window.ClientBounds.Width);
		this._preferredBackBufferHeight = Math.Min(this._game.Window.ClientBounds.Height, this._game.Window.ClientBounds.Width);
		this._preferredBackBufferFormat = SurfaceFormat.Color;
		this._preferredDepthStencilFormat = DepthFormat.Depth24;
		this._synchronizedWithVerticalRetrace = true;
		if (this._game.Services.GetService(typeof (IGraphicsDeviceManager)) != null)
			throw new ArgumentException("Graphics Device Manager Already Present");
		this._game.Services.AddService(typeof (IGraphicsDeviceManager), (object) this);
		this._game.Services.AddService(typeof (IGraphicsDeviceService), (object) this);
	}

The last two lines assign `this` - the `GraphicsDeviceManager` - back to the game. This constructor has side effects. I'm sure there's reasons for this very nasty piece of work but I would rather not know them. The result is I can't create the `GraphicsDeviceManager` anywhere except for in the game's constructor.

Moving on.

    protected override void Initialize()
    {
        TouchPanel.EnabledGestures = GestureType.FreeDrag;
        base.Initialize();
    }

This hasn't changed. It enables the free drag gesture to light up on touch, and calls the `base.Initialize()` method. It seems that a lot of inherited classes need to call the base method. In my opinion this design is a bit fragile and another good reason to replace it with some abstractions.

Now here are the first few lines of my new `LoadContent()` method:

    protected override void LoadContent()
    {
        _container = AutofacConfig.Register(this);

        _spriteBatch = _container.Resolve<ISpriteBatch>();
        _enemyFactory = _container.Resolve<IEnemyFactory>();

        _player = _container.Resolve<IPlayer>();

        //...

`AutofacConfig` is a new class that I've created in the `Configuration` folder. I'll go through it soon. The `Register()` method returns an `IContainer`:


	public class ShooterGame : Game
	{
		// ...
        private IContainer _container;

This container is then used to resolve the parts of the game. I'll go through the rest of the `LoadContent()` method at the end of this post.


### Configuring Autofac

Here is the entire Autofac configuration class and registration method:

    public static class AutofacConfig
    {
        public static IContainer Register(Game game)
        {
            var builder = new ContainerBuilder();

            game.Content.RootDirectory = "Content";

            builder.RegisterInstance(new SpriteBatch(game.GraphicsDevice)).AsSelf();
            builder.RegisterInstance(game.Content).AsSelf();
            builder.RegisterInstance(game.GraphicsDevice).AsSelf();

            builder.RegisterAssemblyTypes(typeof (AutofacConfig).GetTypeInfo().Assembly)
                .Where(t => t.IsAssignableTo<IRegistering>())
                .AsImplementedInterfaces()
                .InstancePerLifetimeScope();

            return builder.Build();
        }
    }

First we make a container builder. This builder is used to configure the container before use. The content root directory is also set here, just to keep the configuration in one place.

    builder.RegisterInstance(new SpriteBatch(game.GraphicsDevice)).AsSelf();

Previously the `spriteBatch` was created in the game's `LoadContent()` method. Autofac is being configured in the `LoadContent()` method, so this is at the same stage in the application lifecycle. We tell Autofac to provide the created sprite batch instance whenever any component requests a sprite batch instance. This avoids having to pass the sprite batch object to anything that wants to draw itself. It also avoids keeping a reference to the sprite batch in the game object itself. There are some extra abstractions around the sprite batch and other objects which I will discuss below.

    builder.RegisterInstance(game.Content).AsSelf();

This line tells Autofac to pass the `game.Content` instance whenever any component requests a `ContentManager` instance. This avoids having to pass `Content` around wherever anything wants to load up a texture or a sound, or to have to do all of the loading in the game class's `LoadContent()` method then pass the loaded textures or sounds around the application.

    builder.RegisterInstance(game.GraphicsDevice).AsSelf();

This does the same thing for the game's `GraphicDevice` instance.

    builder.RegisterAssemblyTypes(typeof (AutofacConfig).GetTypeInfo().Assembly)
        .Where(t => t.IsAssignableTo<IRegistering>())
        .AsImplementedInterfaces()
        .InstancePerLifetimeScope();

This part is a bit more complicated. I'll go through it line by line.

    builder.RegisterAssemblyTypes(typeof (AutofacConfig).GetTypeInfo().Assembly)

This finds all types declared in the specified assembly - the `Win8ShooterGame` application.

    .Where(t => t.IsAssignableTo<IRegistering>())

This limits the search to types that implement the `IRegistering` interface - I'll explain this more later.

    .AsImplementedInterfaces()

This registers each of the found types by the interfaces that the type implements. Say we have `class Foo : IFoo, IRegistering`. If a component requests an `IFoo` dependency, Autofac will provide an instance of `Foo`.

    .InstancePerLifetimeScope();

This registers the types as a single instance per the scope of the container's lifetime. The lifetime is only a consideration if you resolve dependencies within nested lifetime scopes. So far for this application there is only ever going to be a single scope, so the instances are essentially singletons - the same instance of a `Foo` will be provided to all components that request it.


### Components and dependencies

It's probably a good time to step back and look at what is meant by 'injecting a dependency' and how components can 'request' dependencies.

At the top level our game class gets a container - `IContainer`. This container is used to resolve the game's high level dependencies:

	var player = _container.Resolve<IPlayer>()

To resolve the player object Autofac searches through its registrations and finds a concrete class that implements the `IPlayer` class.

	public class Player : IPlayer, IRegistering
	{
		//...

It creates an instance of the player, automatically resolving the dependencies required by the `Player` class's constructor.

    public Player(IContentManager contentManager, IViewport viewport, IAnimationFactory animationFactory)
    {
    	//...

This means that the game class doesn't have to know anything about the player class's dependencies, and the player class doesn't rely on particular implementations of its dependencies. Because the player class depends on interfaces, we can easily pass in mock dependencies in our unit tests.

Resolving a dependency using Autofac is slower than just creating a player manually, like `var player = new Player(_contentManager, this.Viewport, ....`. But a player is only created once at the start of the game, so the cost is negligible. For things that need to be created multiple times within the game loop (such as projectiles or particles) we will use other methods like factories, which will speed up object creation while still allowing dependency injection.


### The IRegistering interface

The `IRegistering` interface is an empty interface that is used as a marker for classes that should be registered with Autofac. Without using a marker interface, the registration will either apply across all types found in the assembly (which could include types that shouldn't be registered) or each type needs to be registered manually.

The scanning registration process isn't free but it would generally take much less than a second. This is another once-off cost of speed at the start of the application.


### Abstractions and wrappers

I introduced Autofac to the application so I could easily manage some changes to the code that will support adding unit tests.

An example is the `Player` class. I eventually want to add some tests around the player's screen edge detection. To do that before my changes I would have to pass in test implementations of the content manager, sprite batch and viewport. I don't know if those implementations would work in a test harness without actually creating a window with its own game loop. I avoid this complexity by passing in abstractions. For example, I could create a mock content manager that will provide a mock texture, without having to configure a real content manager with a texture.

MonoGame doesn't have any abstractions around the main elements, which makes testing using the build in system difficult. For example, the `SpriteBatch` class has a hard dependency on `GraphicsDevice` and none of the methods are virtual, so even subclassing wouldn't work. To get around these dependencies with my own game objects, I've created a small set of wrapper classes with interfaces to replace the MonoGame elements.

`ContentManagerWrapper` is a wrapper for the MonoGame `ContentManager` class:

	internal class ContentManagerWrapper
        : IContentManager, IRegistering
    {
        private readonly ContentManager _contentManager;

        public ContentManagerWrapper(ContentManager contentManager)
        {
            _contentManager = contentManager;
        }

        public ITexture2D Load(string assetName)
        {
            var texture = _contentManager.Load<Texture2D>(assetName);
            return new Texture2DWrapper(texture);
        }
    }

It exposes the `Load()` method, which just calls the underlying `ContentManager` instance's method and returns the `Texture2D` result wrapped in a `Texture2DWrapper`, which implements `ITexture2D`. It has the `IRegistering` marker interface so it will be automatically registered with Autofac when the application starts, and the `ContentManager` instance is provided by Autofac from the registered instance (the one belonging to the game). It also implements the `IContentManager` interface:

    public interface IContentManager
    {
        ITexture2D Load(string assetName);
    }

This means that classes that want to use the content manager just take a dependency on `IContentManager`:

    public class Player : IPlayer, IRegistering
    {
    	//...

        public Player(IContentManager contentManager, IViewport viewport, IAnimationFactory animationFactory)
        {
            _animation = animationFactory.Build(
                contentManager.Load(@"Graphics\shipAnimation"),
                115, 30, 8);

            _position = new Vector2(viewport.TitleSafeArea.X, viewport.TitleSafeArea.Y + viewport.TitleSafeArea.Height/2);
        }

    // ...

Here the `Player` class has a dependency on an `IContentManager` instance. When the application is running Autofac will provide an instance of `ContentManagerWrapper`, which just wraps the usual content manager, primed with the application's content. Calling `contentManager.Load()` calls the underlying content manager.

If I want to write some unit tests around the `Player` class I can just pass in a mock implementation of `IContentManager`, without having to worry about any underlying implementations. Here is an example using [NSubstitute](https://nsubstitute.github.io/):

	var texture = Substitute.For<ITexture2D>();

	var contentManager = Substitute.For<IContentManager>();
	content.Load(@"Graphics\shipAnimation").Returns(texture);

	var player = new Player(contentManager, ...);

	// test the Player instance

At the moment only enough is abstracted and exposed to get the game running. I've also created factories and abstractions for the the `Animation` and `ParallaxingBackground` classes created in the original tutorials.


### LoadContent() and the game structure

Dependency injection simplifies the game's structure because it takes care of injecting all of the dependencies of the different components that build up the game. The game class itself is the only place where Autofac is directly used to instantiate the elements that the game class is directly responsible for.

The game's content is loaded in the `LoadContent()` method as before, except for the addition of Autofac and the use of some factories:

    protected override void LoadContent()
    {
        _container = AutofacConfig.Register(this);

        _spriteBatch = _container.Resolve<ISpriteBatch>();
        _enemyFactory = _container.Resolve<IEnemyFactory>();

        _player = _container.Resolve<IPlayer>();

        var contentManager = _container.Resolve<IContentManager>();
        var parallaxingBackgroundFactory = _container.Resolve<IParallaxingBackgroundFactory>();

        _background1 = parallaxingBackgroundFactory.Build(contentManager.Load("Graphics/bgLayer1"), -1,
            GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
        _background2 = parallaxingBackgroundFactory.Build(contentManager.Load("Graphics/bgLayer2"), -2,
            GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);

        _mainBackground = contentManager.Load("Graphics/mainBackground");
        _mainBackgroundRect = new Rectangle(0, 0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
    }

`_enemyFactory` is a factory used to create 'enemies' without using Autofac 'in the loop'. The `EnemyFactory` implementation loads up the texture once, then creates a new enemy object when requested.

    public class EnemyFactory : IEnemyFactory, IRegistering
    {
        private readonly IContentManager _contentManager;
        private readonly IAnimationFactory _animationFactory;
        private ITexture2D _texture;

        public EnemyFactory(IContentManager contentManager, IAnimationFactory animationFactory)
        {
            _contentManager = contentManager;
            _animationFactory = animationFactory;
            _texture = _contentManager.Load("Graphics/mineAnimation");
        }

        public IEnemy Build()
        {
            var animation = _animationFactory.Build(_texture, 47, 30, 8);
            return new Enemy(animation);
        }
    }

Adding an enemy just calls the factory to create a new enemy:

    void AddEmemy()
    {
        var enemy = _enemyFactory.Build();
        enemy.SetPosition(new Vector2(
            GraphicsDevice.Viewport.Width + enemy.Width / 2,
            _random.Next(100, GraphicsDevice.Viewport.Height - 100)));
        _enemies.Add(enemy);
    }

Note we're still using the core MonoGame objects in places. I would expect the `enemy.SetPosition` call to eventually happen in the factory.

The rest of the application structure is pretty much the same as before, exept that it is calling the abstractions rather than directly talking to the underlying MonoGame objects.


### Conclusion

I've added dependency injection to a simple game that was going to be potentially difficult to maintain and extend, and the resulting abstractions should be easier to work with and refactor further down the track. I've also made it a lot easier to add unit tests.

My next post in this series will be around adding unit tests to the `Player` class, to help with some issues with viewport edge detection that have been plaguing me. I'll then add the ability for the ship to fire, then start displaying and keeping track of the score.

Oh, and here is an obligatory screenshot:

![](https://i.imgur.com/XkRIKoX.png)
