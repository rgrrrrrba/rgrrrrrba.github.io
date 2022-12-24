---
title: MonoGame Shooter - Better boundary detection for the player
layout: post
date: 2014-06-07
category: archived
---

1. Better boundary detection for the player
2. [Converting tests to BDD style](https://becdetat.com/monogame-shooter-converting-tests-to-bdd-style.html)

This is the first post of hopefully many as I learn MonoGame by extending on a [tutorial series by Tara Walker](https://blogs.msdn.com/b/tarawalker/archive/2012/12/04/windows-8-game-development-using-c-xna-and-monogame-3-0-building-a-shooter-game-walkthrough-part-1-overview-installation-monogame-3-0-project-creation.aspx). The source code is available, both [before](https://github.com/becdetat/monogame-tw-tutorial/commit/26a12e3595604d807fbe0c76417e34fb99155007) and [after](https://github.com/becdetat/monogame-tw-tutorial/tree/251ab871697da3b2746dc6be265f15d5acdc2e8d).

In this post I walk through adding screen edge detection to the player's ship, using Test-Driven Development practices where possible to keep a high quality of code.


### Changes since last post

I have created a `Sprite` class, which is the base class for both the `Player` class and the `Enemy` class. The `Sprite` class uses a velocity and maximum speed to calculate the position in each frame. I changed the `ViewportWrapper` to expose the `Viewport` directly. Turns out the `Viewport` object is just a basic `struct` without any dependencies, so it should play nice with tests. Because it is a `struct`, which is a value type, it can't be directly registered with AutoFac, so I need to keep the `IViewport` abstraction. I also added an `IDrawMyself` interface. MonoGame has an `IDrawable` interface but it comes with some baggage. `IDrawMyself` is implemented by `IAnimation` and will eventually be implemented by `ITexture2D` and `IParallaxingBackground`.

Implementations of the `Sprite` class use events to hook into the `Update()` and `Draw()` methods. Here is how the `Enemy` class updates its `IsActive` state before the position is recalculated (the event is subscribed to in the `Enemy` constructor):

    public Enemy(..) 
    {
        // ..

        BeforeUpdate += state =>
        {
            if (Position.X < -Width || _health <= 0)
            {
                IsActive = false;
            }
        };

        // ...
    }

I also moved to a normal Windows (DirectX) project instead of the Windows 8 Store app that I started with. The only reason for this is so I can use a nice mocking library with little friction, as the portable class libraries that you have to use with Windows 8 Store apps don't support `Reflection.Emit`. I only had to change one line of code to retarget the game, so I'm pretty sure moving to different targets further along will be straightforward.


### Boundary technique

This technique is borrowed from a [Pluralsight course](https://pluralsight.com/training/courses/TableOfContents?courseName=xna&highlight=john-sonmez_xna-m1-introduction*1,6#xna-m1-introduction) by [John Sonmez](https://twitter.com/jsonmez).

Basically we'll describe four rectangles around the boundary we want to limit the player's ship to. In practice these will be slightly inset from the screen and a bit further in on the right hand side, to keep the player from going too far forward.

![](https://i.imgur.com/sMbJiiv.png)

To keep things simple, these rectanges will be 'owned' by the player.


### Writing the first test

To make sure that everything is working as we expect, the first test will just confirm that given a ship somewhere in the middle of the screen, with no inputs (keys all up, game pad zeroed), when updating the player, the position does not change. This seems like an obvious test but it will give us a lot of guidance on how to write the rest of the tests and possibly make some changes to help testing along.

I'm using [xUnit](https://github.com/xunit/xunit) as the testing framework but the tests should be pretty much the same whether you're using xUnit, NUnit or some other framework. I have a post on [how to set up xUnit for a Windows 8 Store app](xunit-tests-with-a-windows-8-1-store-app.html) if that's what you're targeting. To install xUnit, open the Package Manager Console, select the tests assembly, and enter `install-package xunit`.

![](https://i.imgur.com/trq8uv3.png)

I will need to create some mock objects to pass into the `Player` constructor. I'm going to use [NSubstitute](https://nsubstitute.github.io/) (`install-package nsubstitute`).

    [Fact]
    public void ThePositionStaysTheSame()
    {
        var contentManager = Substitute.For<IContentManager>();
        var viewport = Substitute.For<IViewport>();
        var animationFactory = Substitute.For<IAnimationFactory>();
        var spriteBatch = Substitute.For<ISpriteBatch>();

        var player = new Player(contentManager, viewport, animationFactory, spriteBatch);
    }

I'm creating some substitutes for the player's dependencies and constructing the player object. If I run the tests at this point everything is successful, which is good because it means the player class is happy to construct itself using the substituted dependencies.

Now for the "act" part of the test. I'll set an initial position, then call `Update()` with no mocked inputs.

![](https://i.imgur.com/I4C0vsi.png)

Creating these test input states are going to be a problem in the long term. For now I'll just move it into a seperate method. I'll revisit this in the next test.

    var initialPosition = new Vector2(4, 5);
    player.Position = initialPosition;

    var state = GetInputState();
    player.Update(state);

`GetInputState()` returns a new `ShooterGameInputState` instance with default game pad, keyboard and mouse states and a default `GameTime`.

I run my test again to make sure everything is still happy, and I'm pleased to see it fail. It seems that the `Player.Update()` method was using some static `TouchPanel` calls, which resulted in a null reference exception outside of the game. I simply updated the `ShooterGameInputState` class with the touch panel state, although it looks like mocking out touch panel inputs will be problematic unless they are fully abstracted.

Now I need to assert that the player didn't move. I'm using [Shouldly](https://shouldly.github.io/) as the assertions library (`install-package shouldly`).

    player.Position.ShouldBe(initialPosition);

My test passes! I make sure the comparison is working by testing the position against `Vector2.Zero` (because I can't trust the tests quite yet).

### Testing the player's movement

If the left thumbstick is fully up and left, with the player's default speed of 8, the player should move up 8 and left 8 after one update (note that this isn't actually true, read on for an explanation). This will be the second test.

    public class WhenLeftThumbStickFullyLeftAndUp
    {
        [Fact]
        public void ThenThePositionMovesUpAndLeftAtFullSpeed()
        {
        }
    }

I don't want to simply copy the player initialization code, especially since I know I'm going to write a pile of these tests. So I'm going to extract out the creation using the [Object Mother](https://martinfowler.com/bliki/ObjectMother.html) pattern.

    public static partial class ObjectMother
    {
        public static partial class Sprites
        {
            public static partial class PlayerSprite
            {
                public static partial class Players
                {
                    public static PlayerBuilder Default { get { return new PlayerBuilder();} }

                    public class PlayerBuilder
                        : BuilderFor<Player>
                    {
                        public override Player Build()
                        {
                            return new Player(
                                Substitute.For<IContentManager>(),
                                Substitute.For<IViewport>(),
                                Substitute.For<IAnimationFactory>(),
                                Substitute.For<ISpriteBatch>());
                        }
                    }
                }
            }
        }
    }

The nested classes are so we can add to the object mothers and use the same naming convention as the object's namespace. If managed properly, the Object Mother pattern can scale out to hundreds of builders with a consistent interface, allowing a high amount of control using the domain language but keeping a very high level of code reuse.

    var player = ObjectMother.Sprites.PlayerSprite.Players.Default.Build();

    var initialPosition = new Vector2(4, 5);
    player.Position = initialPosition;

I'm also going to move the input state creation into an object mother but I'll add an entry point to set the left thumbstick position. I'll add an object mother for `GamePadState` since that's what will be passed into the input state. Here is the builder part:

    public class GamePadStateBuilder
        : BuilderFor<GamePadState>
    {
        private Vector2 _leftThumbStick = Vector2.Zero;

        public override GamePadState Build()
        {
            return new GamePadState(_leftThumbStick, Vector2.Zero, 0, 0);
        }

        public GamePadStateBuilder WithLeftThumbstick(Vector2 position)
        {
            _leftThumbStick = position;
            return this;
        }
    }

The `BuilderFor` class has some 'magic' to let you record the builder configuration using a [DIY fluent interface](https://github.com/becdetat/monogame-tw-tutorial/blob/master/src/ShooterGame.Tests/ObjectMothers/BuilderFor.cs). Here's next part of the test, creating the input state with the left gamepad stick fully up and right. Note that thumbstick vector's Y value is *positive* in the *up* direction and *negative* in the *down* direction, which is the opposite to the screen, which decreases to zero at you approach the top of the screen.

    var gamePadState = ObjectMother.Input.GamePadStates.Default
        .WithLeftThumbstick(new Vector2(1, 1))
        .Build();
    var state = ObjectMother.Core.ShooterGameInputStates.Default
        .WithCurrentGamePadState(gamePadState)
        .Build();

With that I add the 'act' and 'assert' parts of the test:

    player.Update(state);

    player.Position.ShouldBe(initialPosition + new Vector2(player.Speed, -player.Speed));

When running this I found something interesting about the game pad thumbsticks. When creating the `GamePadThumbSticks` object used to represent the thumbsticks, a static value `GamePadThumbSticks.GateType` is used to normalize the input value. The default gate is `Round`, which gates the position to a circular shape. Stand back, I'm going to do some math.

![](https://i.imgur.com/azB0IGU.png)

So the thumbstick at full extension to the top right is actually `(0.7071, 0.7071)`, so the player sprite will move `8 * 0.7071` pixels per update in the X and Y axes. I'll update the test accordingly:

    player.Update(state);

    var normalisedVelocity = Math.Sqrt(2)/2.0d;
    var changeInPosition = new Vector2(
        player.Speed*(float) normalisedVelocity,
        -player.Speed*(float) normalisedVelocity);

    player.Position.ShouldBe(initialPosition + changeInPosition);

This test finally passes. This revealed something about the keyboard, mouse and d-pad inputs which I hadn't noticed. When those inputs are triggering diagonal movement my player is moving with a velocity of `(1, 1)`, which is significantly faster than the thumbpad's velocity of `(0.7071, 0.7071)`. This will eventually have to be corrected.


### Configuring the player's boundaries

I want to be able to configure parts of the player. To be able to unit test the player I need to be able to set the configuration outside of the player class. I could do this by passing things into the constructor:

    new Player(contentManager, viewport, ...., leftBoundary, topBoundary, rightBoundary, ...)

This is a bit unwieldy and will get worse as new configurable things get added to the player. Instead I could wrap the configuration in another class:

    class PlayerConfiguration
    {
        public int LeftBoundary { get; set; }
        public int TopBoundary { get; set; }
        // ...
    }

    new Player(contentManager, viewport, ..., playerConfiguration);

Because I'm creating the player using Autofac I can make this more generic and let the runtime configuration be baked in. The sprite's width and height should also be in the configuration, because determining width and height using the animation is going to be messy for unit tests.

    public interface IPlayerConfiguration
    {
        int LeftBoundary { get; }
        int TopBoundary { get; }
        int RightBoundary { get; }
        int BottomBoundary { get; }
        int Width { get; }
        int Height { get; }
    }

    public class PlayerConfiguration : IPlayerConfiguration, IRegistering
    {
        public int LeftBoundary { get { return 20; } }
        public int TopBoundary { get { return 20; } }
        public int RightBoundary { get { return 60; } }
        public int BottomBoundary { get { return 20; } }
        public int Width { get { return 115; } }
        public int Height { get { return 69; } }
    }

By the way, the boundaries are going to be like padding. The player class will calculate the actual boundaries using the viewport later on.

I need to add the new player configuration to the player's object mother's `PlayerBuilder` class:

    public class PlayerBuilder
        : BuilderFor<Player>
    {
        private IPlayerConfiguration _playerConfiguration;

        public override Player Build()
        {
            return new Player(
                Substitute.For<IContentManager>(),
                Substitute.For<IViewport>(),
                Substitute.For<IAnimationFactory>(),
                Substitute.For<ISpriteBatch>(),
                _playerConfiguration ?? Substitute.For<IPlayerConfiguration>());
        }

        public PlayerBuilder WithPlayerConfiguration(IPlayerConfiguration playerConfiguration)
        {
            _playerConfiguration = playerConfiguration;
            return this;
        }
    }

This lets me either use a default `IPlayerConfiguration` make with NSubstitute (which will return a default of `0` for the boundaries), or pass in my own `IPlayerConfiguration` instance. To make this easier to work with I also made an object mother for `IPlayerConfiguration`:

    public static partial class ObjectMother
    {
        public static partial class Sprites
        {
            public static partial class PlayerSprite
            {
                public static partial class PlayerConfigurations
                {
                    public static PlayerConfigurationBuilder Default
                    {
                        get { return new PlayerConfigurationBuilder(); }
                    }

                    public class PlayerConfigurationBuilder
                        : BuilderFor<IPlayerConfiguration>
                    {
                        private readonly IPlayerConfiguration _playerConfiguration =
                            Substitute.For<IPlayerConfiguration>();

                        public PlayerConfigurationBuilder()
                        {
                            _playerConfiguration.Width.Returns(10);
                            _playerConfiguration.Height.Returns(10);
                        }

                        public override IPlayerConfiguration Build()
                        {
                            return _playerConfiguration;
                        }

                        public PlayerConfigurationBuilder WithLeftBoundary(int boundary)
                        {
                            _playerConfiguration.LeftBoundary.Returns(boundary);
                            return this;
                        }

                        public PlayerConfigurationBuilder WithRightBoundary(int boundary)
                        {
                            _playerConfiguration.RightBoundary.Returns(boundary);
                            return this;
                        }

                        public PlayerConfigurationBuilder WithTopBoundary(int boundary)
                        {
                            _playerConfiguration.TopBoundary.Returns(boundary);
                            return this;
                        }

                        public PlayerConfigurationBuilder WithBottomBoundary(int boundary)
                        {
                            _playerConfiguration.BottomBoundary.Returns(boundary);
                            return this;
                        }
                    }
                }
            }
        }
    }

I also need to provide the player with a sensible viewport in the tests. To do this I created an [object mother for the viewport](https://github.com/becdetat/monogame-tw-tutorial/blob/master/src/ShooterGame.Tests/ObjectMothers/BuilderFor.cs) and added a `WithViewport()` method to the player object mother.


### Testing the left boundary

I'm guessing that the easiest boundaries to test will be the left and top, since I don't have to worry about the size of the viewport yet. So my first test will be, with a left boundary of 10, with the player sitting on that boundary, with the the thumbstick to the left, the player's X position doesn't change.

    public class WhenPlayerIsOnLeftBoundaryWithLeftThumbStickFullyLeft
    {
        [Fact]
        public void ThenThePositionIsNotChanged()
        {
            var configuration = ObjectMother.Sprites.PlayerSprite.PlayerConfigurations.Default
                .WithLeftBoundary(10)
                .Build();
            var player = ObjectMother.Sprites.PlayerSprite.Players.Default
                .WithPlayerConfiguration(configuration)
                .Build();
            var initialPosition = new Vector2(10, 5);
            player.Position = initialPosition;

            var gamePadState = ObjectMother.Input.GamePadStates.Default
                .WithLeftThumbstickFullyLeft()
                .Build();
            var state = ObjectMother.Core.ShooterGameInputStates.Default
                .WithCurrentGamePadState(gamePadState)
                .Build();

            player.Update(state);

            player.Position.X.ShouldBe(initialPosition.X);
        }
    }

Initially this fails because the boundary isn't implemented:

    Shouldly.ChuckedAWobblyplayer.Position.X
            should be
        10
            but was
        2

This first boundary is pretty easy to describe. The player has a set of boundaries:

    public class Player : //...
    {
        private readonly Rectangle[] _boundaries;

The left boundary is created in the `Player` constructor:

    _boundaries = new[]
    {
        new Rectangle(-100, 0, 100 + _configuration.LeftBoundary, _viewport.Height),
    };

This is a rectangle starting 100 pixels to the left of the viewport, finishing at the left boundary, and the full height of the viewport.

Now I need a way of stopping the player from moving into that boundary. In the `Sprite` class (the base class for the player and the enemies) the position is set in the `Update` method:

    var deltaX = Velocity.X*Speed;
    var deltaY = Velocity.Y*Speed;

    Position += new Vector2(deltaX, deltaY);

So I can add a new virtual method to `Sprite` which checks the new position:

    protected virtual bool CheckNewPosition(Vector2 newPosition)
    {
        return true;
    }

The update method needs to be changed to use this method. Replace:

    Position += new Vector2(deltaX, deltaY);
 
 with:   

    var newPosition = Position + new Vector2(deltaX, deltaY);
    if (CheckNewPosition(newPosition))
    {
        Position = newPosition;
    }

The `Player` class now just needs to override the `CheckNewPosition` method. If I make it return `false` my new test passes but my existing test fails, but this is a good check to make sure everything is working as expected:

    protected override bool CheckNewPosition(Vector2 newPosition)
    {
        return false;
    }

When I run the game, the player now can't be moved at all. I need to change the `CheckNewPosition` method to use the boundaries:

    protected override bool CheckNewPosition(Vector2 newPosition)
    {
        var newBounds = new Rectangle((int)newPosition.X, (int)newPosition.Y, Width, Height);

        foreach (var boundary in _boundaries)
        {
            if (boundary.Intersects(newBounds))
            {
                return false;
            }
        }

        return true;
    }

This gets the tests passing, but something strange is going on with the boundaries in the game:

![](https://i.imgur.com/96xaBhi.png)

By debugging the game this is aparrently at `X=20`, which is obviously being clipped by the edge of the screen, so one of my assumptions is incorrect. The culprit turns out to be in the `Animation.Draw` method:

    var destRect = new Rectangle(
        (int) position.X - ScaledWidth/2,
        (int) position.Y - ScaledHeight/2,
        ScaledWidth,
        ScaledHeight);

The destination rectangle (the rectangle the sprite is being drawn at) is shifted by half the height and width, so the origin of the sprite is the center of the animation. This actually makes sense, so I need to change my logic to suit. The player's `GetBounds()` method is changed to this:

    public override Rectangle GetBounds()
    {
        return GetBoundsAt(Position);
    }

    protected Rectangle GetBoundsAt(Vector2 position)
    {
        return new Rectangle(
            (int) position.X - _configuration.Width / 2,
            (int) position.Y - _configuration.Height / 2,
            _configuration.Width,
            _configuration.Height);
    }

And the `CheckNewPosition()` method uses the new `GetBoundsAt()` method:

    protected override bool CheckNewPosition(Vector2 newPosition)
    {
        var newBounds = GetBoundsAt(newPosition);

        foreach (var boundary in _boundaries)
        {
            if (boundary.Intersects(newBounds))
            {
                return false;
            }
        }

        return true;
    }

My tests still pass because the player has a default width and height of 0. I will update my tests to make sure the width of the player is handled correctly but this will do for now. When I start the game I need to push the starting position right a bit so I can move the player.


### Testing the other boundaries

Implementing and testing the other boundaries is basically a rinse and repeat of the left boundary. Here is the test for the right boundary:

    public class WhenPlayerIsOnRightBoundaryWithLeftThumbStickFullyRight
    {
        [Fact]
        public void ThenThePositionIsNotChanged()
        {
            var configuration = ObjectMother.Sprites.PlayerSprite.PlayerConfigurations.Default
                .WithRightBoundary(10)
                .Build();
            var viewport = ObjectMother.Core.Viewports.Default
                .WithHeight(100)
                .WithWidth(100)
                .Build();
            var player = ObjectMother.Sprites.PlayerSprite.Players.Default
                .WithPlayerConfiguration(configuration)
                .WithViewport(viewport)
                .Build();
            var initialPosition = new Vector2(90, 5);
            player.Position = initialPosition;

            var gamePadState = ObjectMother.Input.GamePadStates.Default
                .WithLeftThumbstickFullyRight()
                .Build();
            var state = ObjectMother.Core.ShooterGameInputStates.Default
                .WithCurrentGamePadState(gamePadState)
                .Build();

            player.Update(state);

            player.Position.X.ShouldBe(90);
        }
    }

And here are the final set of boundaries, fully unit tested and configurable:

    _boundaries = new[]
    {
        new Rectangle(-100, 0, 100 + _configuration.LeftBoundary, _viewport.Height),
        new Rectangle(_viewport.Width - _configuration.RightBoundary, 0, 100, _viewport.Height), 
        new Rectangle(0, -100, _viewport.Width, 100 + _configuration.TopBoundary), 
        new Rectangle(0, _viewport.Height - _configuration.BottomBoundary, _viewport.Width, 100), 
    };


### Coming up next

In my next post I'm going to refactor my tests to use a BDD format, which will simplify the tests and reduce some duplication. That will pave the way for implementing killing the player when it gets hit by an enemy.



### Resources


- [Up and Running with MonoGame](up-and-running-with-monogame.html)
- [xUnit tests in a Windows 8.1 Store App](xunit-tests-with-a-windows-8-1-store-app.html)
- [Adding dependency injection to a MonoGame application](adding-dependency-injection-to-a-monogame-application.html)
- [Tara Walker's original Shooter Game tutorial](https://blogs.msdn.com/b/tarawalker/archive/2012/12/04/windows-8-game-development-using-c-xna-and-monogame-3-0-building-a-shooter-game-walkthrough-part-1-overview-installation-monogame-3-0-project-creation.aspx)
- [John Sonmez' Introduction to 2D Programming with XNA (Pluralsight video)](https://pluralsight.com/training/courses/TableOfContents?courseName=xna&highlight=john-sonmez_xna-m1-introduction*1,6#xna-m1-introduction) ($)