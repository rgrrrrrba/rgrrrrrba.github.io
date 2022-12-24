---
title: MonoGame Shooter - Converting tests to BDD style
layout: post
date: 2014-06-08
category: archived
---

1. [Better boundary detection for the player](monogame-shooter-better-boundary-detection-for-the-player.html)
2. Converting tests to BDD style

This is the second post of hopefully many as I learn MonoGame by extending on a [tutorial series by Tara Walker](https://blogs.msdn.com/b/tarawalker/archive/2012/12/04/windows-8-game-development-using-c-xna-and-monogame-3-0-building-a-shooter-game-walkthrough-part-1-overview-installation-monogame-3-0-project-creation.aspx). The source code is available, both [before](https://github.com/becdetat/monogame-tw-tutorial/tree/251ab871697da3b2746dc6be265f15d5acdc2e8d) and [after](https://github.com/becdetat/monogame-tw-tutorial/tree/02000b940a0b0470a5b3ffb66618a54d52378d0b).

In this post I convert the player boundary detection tests I wrote in the last post to a behavior-driven development (BDD) style to remove some duplication and simplify the tests.


### Behavior-driven development

BDD is an approach to test-driven development that works from the outside in. A BDD test is described using a language that the business understands (the [ubiquitous language](https://martinfowler.com/bliki/UbiquitousLanguage.html)). It then describes the conditions, events and results that make up the desired behavior.


### Existing tests

The existing tests written in the previous post were quite repetitive. They were all around what happens to a player's position given some user inputs, such as if the player is at the top of the screen and the gamepad is pushed up, the player shouldn't move any further up.

    public class WhenPlayerIsOnTopBoundaryWithLeftThumbStickFullyUp
    {
        [Fact]
        public void ThenThePositionIsNotChanged()
        {
            var configuration = ObjectMother.Sprites.PlayerSprite.PlayerConfigurations.Default
                .WithTopBoundary(10)
                .Build();
            var viewport = ObjectMother.Core.Viewports.Default
                .WithHeight(100)
                .WithWidth(100)
                .Build();
            var player = ObjectMother.Sprites.PlayerSprite.Players.Default
                .WithPlayerConfiguration(configuration)
                .WithViewport(viewport)
                .Build();
            var initialPosition = new Vector2(20, 10);
            player.Position = initialPosition;

            var gamePadState = ObjectMother.Input.GamePadStates.Default
                .WithLeftThumbstickFullyUp()
                .Build();
            var state = ObjectMother.Core.ShooterGameInputStates.Default
                .WithCurrentGamePadState(gamePadState)
                .Build();

            player.Update(state);

            player.Position.Y.ShouldBe(10);
        }
    }

A lot of the initial setup and state creation is common across all of the tests. The naming convention used for the test class and method is describing the behaviors appropriately but it is still difficult to scan through the body of the test and immediately see what is happening.


### BDDfy

I'm going to use a framework called [BDDfy](https://docs.teststack.net/bddfy/index.html), which uses reflection to step through the methods in the test class and execute them in the 'given, when, then' order. Here's a really simple example:

    public class WhenEnemyIsDestroyedScenario
    {
        private Enemy _enemy;

        public void GivenAnEnemyThatIsAlive()
        {
            var animation = Substitute.For<IAnimation>();
            var spriteBatch = Substitute.For<ISpriteBatch>();

            _enemy = new Enemy(animation, spriteBatch);
        }

        public void WhenEnemyIsDestroyed()
        {
            _enemy.Destroy();
        }

        public void ThenTheEnemyHealthIsZero()
        {
            _enemy.Health.ShouldBe(0);
        }

        [Fact]
        public void Execute()
        {
            this.BDDfy();
        }
    }

The benefits in this example aren't huge or immediately compelling, but scanning through the method names makes the intent and structure of the test clear. If there were multiple outcomes, say if the enemy's state changes, then additional `Then` steps can be added. Parts of this could be shared as well, such as moving `GivenAnEnemyThatIsAlive()` into a `GivenAnEnemyThatIsAliveScenarioBase` base class.

BDD is a great way of revealing intent and increasing the value of the test suite, and BDDfy in particular gives us an easy implementation of BDD in a way that makes our test suites composable.

Install BDDfy using the package manager console with `install-package TestStack.BDDfy`.


### Reconstructing the first test

The tests are already in a consistent arrange/act/assert format. Rename the `WhenPlayerIsOnTopBoundaryWithLeftThumbStickFullyUp` class to `WhenPlayerIsOnTopBoundaryWithLeftThumbStickFullyUpScenario`, because the name of the test class is going to be the `When` part of the BDD style test.

Add an `Execute()` method to the bottom of the test

    [Fact]
    public void Execute()
    {
        this.BDDfy();
    }

This is needed to tell BDDfy to reflect over the class and execute the given/when/then methods. Remove the `[Fact]` attribute from the `ThenThePositionIsNotChanged()` method, otherwise it will be executed twice.

The first `Given` is the player. Setting the position will be the second `Given`, because this will change between tests. Copy down to where the player is built and extract this out into a new `GivenThePlayer()` method. The `player` object will be made into a class variable so it can be shared between the other test methods in the class.

    private Player _player;

    public void GivenThePlayer()
    {
        var configuration = ObjectMother.Sprites.PlayerSprite.PlayerConfigurations.Default
            .WithTopBoundary(10)
            .Build();
        var viewport = ObjectMother.Core.Viewports.Default
            .WithHeight(100)
            .WithWidth(100)
            .Build();
        _player = ObjectMother.Sprites.PlayerSprite.Players.Default
            .WithPlayerConfiguration(configuration)
            .WithViewport(viewport)
            .Build();
    }

The other tests set the other boundaries so as I convert them I'll add their boundaries to this shared method.

I like to prefix my class variables with an underscore. You should too, so fix the old references from `player` to `_player` then extract out the `AndGivenThePlayerIsAtTheTopOfTheScreen()` and `AndGivenTheThumbstickIsFullyUp()` methods.

    public void AndGivenThePlayerIsAtTheTopOfTheScreen()
    {
        _player.Position = new Vector2(20, 10);
    }

    private ShooterGameInputState _state;

    public void AndGivenTheThumbstickIsFullyUp()
    {
        var gamePadState = ObjectMother.Input.GamePadStates.Default
            .WithLeftThumbstickFullyUp()
            .Build();
        _state = ObjectMother.Core.ShooterGameInputStates.Default
            .WithCurrentGamePadState(gamePadState)
            .Build();
    }

Now extract the `_player.Update()` call into the `When` method:

    public void WhenUpdatingThePlayerState()
    {
        _player.Update(_state);
    }

The `ThenThePositionIsNotChanged()` method should now just be the position assertion. The name of the method already follows BDDfy's naming convention so it will be executed last.

    public void ThenThePositionIsNotChanged()
    {
        _player.Position.Y.ShouldBe(10);
    }

Running the test is successful so we can move on.


### Creating a scenario base

You may have noticed that although the test is now broken into easy to understand chunks, the test itself is now significantly larger than the original single method test. The next step is to extract the common part into a base class.

The only method I'm going to put into the base class is the `GivenThePlayer()` method. There will still be some duplication mainly with the `WhenUpdatingThePlayerState` but I would rather be able to see the main steps in each test.

Create a new class called `GivenThePlayerScenarioBase` and move the `GivenThePlayer()` method into the new class, making it `virtual` along the way. The `_player` instance should be moved as well and made protected (and renamed to `Player` to match C# naming conventions).

    public abstract class GivenThePlayerScenarioBase
    {
        protected Player Player;

        public virtual void GivenThePlayer()
        {
            var configuration = ObjectMother.Sprites.PlayerSprite.PlayerConfigurations.Default
                .WithTopBoundary(10)
                .Build();
            var viewport = ObjectMother.Core.Viewports.Default
                .WithHeight(100)
                .WithWidth(100)
                .Build();
            Player = ObjectMother.Sprites.PlayerSprite.Players.Default
                .WithPlayerConfiguration(configuration)
                .WithViewport(viewport)
                .Build();
        }
    }

I've made the `GivenThePlayer()` method virtual because I still want to have a method named `GivenThePlayer()` in the concrete scenario class. This is a matter of taste and doesn't affect BDDfy's operation, but it keeps all the steps in the scenario class which I find valuable.

Change `WhenPlayerIsOnTopBoundaryWithLeftThumbStickFullyUpScenario` to inherit from `GivenThePlayerScenarioBase` and rename the `_player` references to `Player`. The test still passes! Now we can go through the other classes and convert them to the hot new BDD style tests.


### Wrap-up

I won't go through the other conversions but for reference here is the complete `WhenPlayerIsOnTopBoundaryWithLeftThumbStickFullyUpScenario` class:

    public class WhenPlayerIsOnTopBoundaryWithLeftThumbStickFullyUpScenario
        : GivenThePlayerScenarioBase
    {
        private ShooterGameInputState _state;

        public override void GivenThePlayer()
        {
            base.GivenThePlayer();
        }

        public void AndGivenThePlayerIsAtTheTopOfTheScreen()
        {
            Player.Position = new Vector2(20, 10);
        }

        public void AndGivenTheThumbstickIsFullyUp()
        {
            var gamePadState = ObjectMother.Input.GamePadStates.Default
                .WithLeftThumbstickFullyUp()
                .Build();
            _state = ObjectMother.Core.ShooterGameInputStates.Default
                .WithCurrentGamePadState(gamePadState)
                .Build();
        }

        public void WhenUpdatingThePlayerState()
        {
            Player.Update(_state);
        }

        public void ThenThePositionIsNotChanged()
        {
            Player.Position.Y.ShouldBe(10);
        }

        [Fact]
        public void Execute()
        {
            this.BDDfy();
        }
    }

The completed `GivenThePlayerScenarioBase` with the different boundaries is here:

    public abstract class GivenThePlayerScenarioBase
    {
        protected Player Player;

        public virtual void GivenThePlayer()
        {
            var configuration = ObjectMother.Sprites.PlayerSprite.PlayerConfigurations.Default
                .WithTopBoundary(10)
                .WithRightBoundary(10)
                .WithBottomBoundary(10)
                .WithLeftBoundary(10)
                .Build();
            var viewport = ObjectMother.Core.Viewports.Default
                .WithHeight(100)
                .WithWidth(100)
                .Build();
            Player = ObjectMother.Sprites.PlayerSprite.Players.Default
                .WithPlayerConfiguration(configuration)
                .WithViewport(viewport)
                .Build();
        }
    }

As always you can refer to [the current source code](https://github.com/becdetat/monogame-tw-tutorial) or [the source code as at the finish of this post](https://github.com/becdetat/monogame-tw-tutorial/tree/02000b940a0b0470a5b3ffb66618a54d52378d0b).


### Coming up next

In my next post I'm going to implement killing the player when it gets hit by an enemy.


### Resources

- [Up and Running with MonoGame](up-and-running-with-monogame.html)
- [xUnit tests in a Windows 8.1 Store App](xunit-tests-with-a-windows-8-1-store-app.html)
- [Adding dependency injection to a MonoGame application](adding-dependency-injection-to-a-monogame-application.html)
- [Tara Walker's original Shooter Game tutorial](https://blogs.msdn.com/b/tarawalker/archive/2012/12/04/windows-8-game-development-using-c-xna-and-monogame-3-0-building-a-shooter-game-walkthrough-part-1-overview-installation-monogame-3-0-project-creation.aspx)
- [John Sonmez' Introduction to 2D Programming with XNA (Pluralsight video)](https://pluralsight.com/training/courses/TableOfContents?courseName=xna&highlight=john-sonmez_xna-m1-introduction*1,6#xna-m1-introduction) ($)


