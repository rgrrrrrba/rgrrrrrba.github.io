---
title: React - Early thoughts
layout: post
date: 2015-08-28
category: archived
---

I've just started playing with React, so here is a bit of a brain dump of my first impressions.


## There are some nice abstractions...

I'm comparing this mainly with Angular, which I have about 8 months experience with. React seems to be all about the components, which is similar to the directive-first approach that is working well for me in Angular. The components are very nice to work with, especially with JSX using ES6 features. Here's a very basic example:

	export default class HelloWorld extends React.Component {
		render() {
			return (
				<h1>Hello, world!</h1>
			);
		}
	}

	React.render(
		<HelloWorld/>,
		document.getElementById('content')
	)

This renders the `HelloWorld` component inside a div with the ID `content`. JSX's inline style of declaring markup seems strange to look at first, but it's really just syntactic sugar for commands that create shadow DOM elements.

## ...but it feels closer to the metal

Angular includes services such as `$http` for AJAX and `$q` for promises, and uses separate templates with a templating language to work with the UI. While you don't have to, use of the included services is recommended - you're largely buying into the whole ecosystem.

React is all about the DOM. Inline JSX is syntactic sugar for methods that work directly with the DOM. React doesn't provide or depend on any particular AJAX library - the tutorials suggest using jQuery directly. This makes React feel very lightweight and gives the impression that it's not trying to solve too many problems.


## New ES6 features and good practices

The [React tutorial][react-tutorial] is a good starting point, but it is built using ES5 syntax. ES6 adds classes, which makes working with JSX a lot easier. There are some important changes that need to made though.

I'm not a huge fan of ES6 and TypeScript classes, partly because it doesn't give you much over just using functions, and partly because it makes it more difficult to manage `this`.

Here's what I mean. In TypeScript ([pen][pen-typescript-parent-child-example]):

	class Child {
		constructor(private name) {}

		sayName() {
			console.log(this.name);
		}
	}

	class Parent {
		sayChildName;
	  
		constructor(private name, private child) {
			this.sayChildName = child.sayName;
		}
	}

	var c = new Child("child");
	var p = new Parent("parent", c);

	p.sayChildName()

This writes `"parent"` to the console, not `"child"` as expected. This is because of JavaScript's late binding - when the `sayChildName` reference is executed, `this` is the parent. The `this` reference in `sayName()` isn't closed over in the function. To get the correct `this` in function-based ES6, we usually assign `this` to `self`. The working equivalent is this:

<aside class="pull-right" style="width: 15em">
	In fact, by removing the <code>self.name = name</code> you can make <code>name</code> truly private, something that isn't possible (yet) in ES6/TS classes.
</aside>

	function Child(name) {
		var self = this;
		
		self.name = name;
		
		self.sayName = () => {
			console.log(self.name)
		};
	}

	function Parent(name, child) {
		var self = this;
		
		self.name = name;
		self.child = child;
		self.sayChildName = child.sayName;
	}

	var c = new Child("child");
	var p = new Parent("parent", c);

	p.sayChildName();

I wanted to use ES6 classes for React because React's way of creating a component functionally (`var MyComponent = React.createClass({...})`) uses object notation, which I find very frustrating to work with, and not having to deal with dependency injection (a la Angular) makes classes feel more lightweight.

From the tutorial, creating a component functionally is like this:

	var CommentForm = React.createClass({
	  handleSubmit: function(e) {
	    e.preventDefault();
	    var author = React.findDOMNode(this.refs.author).value.trim();
	    var text = React.findDOMNode(this.refs.text).value.trim();
	    if (!text || !author) {
	      return;
	    }
	    this.props.onCommentSubmit({author: author, text: text});
	    React.findDOMNode(this.refs.author).value = '';
	    React.findDOMNode(this.refs.text).value = '';
	  },
	  render: function() {
	    return (
	      <form className="commentForm" onSubmit={this.handleSubmit}>
	        <input type="text" placeholder="Your name" ref="author" />
	        <input type="text" placeholder="Say something..." ref="text" />
	        <input type="submit" value="Post" />
	      </form>
	    );
	  }
	});

Note that in the `render` method, the `onSubmit` event handler in the form is set to `this.handleSubmit`. This works because React [autobinds][react-autobind] `this` to the component instance when using `React.createClass`.

The equivalent using an ES6 class is this:

	export default class CommentForm extends React.Component {
		render() {
			let handleSubmit = e => {
				e.preventDefault();
				var author = React.findDOMNode(this.refs.author).value.trim();
				var text = React.findDOMNode(this.refs.text).value.trim();
				if (!text || !author) {
					return;
				}
				this.props.onCommentSubmit({
					author: author,
					text: text
				});
				React.findDOMNode(this.refs.author).value = '';
				React.findDOMNode(this.refs.text).value = '';
			};

			return (
				<form className="commentForm" onSubmit={handleSubmit}>
					<input type="text" placeholder="Your name" ref="author" />
					<input type="text" placeholder="Say something..." ref="text" />
					<input type="submit" value="Post" />
				</form>
			);
		}
	}

`handleSubmit` has changed from being a function on the class to an inline function within the `render()` method. This is because the `onSubmit` handler is executed in a different context, so if `handleSubmit` were a function directly on the `CommentForm` class `this` would have a different value and the call would fail. React's ES6 class support doesn't support autobinding `this` so this is a workaround for idiomatic ES6.

Making the function an inline value is equivalent to doing the following to the above parent/child TypeScript example:

	class Child {
		constructor(private name) {
			this.sayName = () => {
				console.log(this.name);
			};
		}

		sayName;
	}

	class Parent {
		sayChildName;

		constructor(private name, private child) {
			this.sayChildName = child.sayName;
		}
	}

	var c = new Child("child");
	var p = new Parent("parent", c);

	p.sayChildName()

This now outputs `"child"` as originally expected.

Another gotcha I found with using ES6 classes for React components is setting the initial `state` value. `this.state` is what React uses for one-way binding to the view. The way to set the initial value using the `React.createClass` syntax is with a `getInitialState` function:

	var CommentBox = React.createClass({
		getInitialState: function() {
			return {data: []};
		},
		// ...

Trying to do this with ES6 classes doesn't work. 

	export default class CommentBox extends React.Component {
		getInitialState() {
			return { data: []};
		}
		//...

	Warning: getInitialState was defined on CommentBox, a plain JavaScript class. This is only supported for classes created using React.createClass. Did you mean to define a state property instead?

The correct way is to set `this.state` from the constructor:

	export default class CommentBox extends React.Component {
		constructor() {
			super();

			this.state = { data: []};
		}
		//...

Note that `this.state` is only set directly like this _in the constructor_. Updating the state subsequently has to happen using `this.setState`:

	$.get('/comments').then(data => this.setState({
		data: data
	}));

**But** you can't use `this.setState` in the constructor, and setting the state _outside_ of the constructor has to be via `this.setState`. Important to remember.


## One-way binding

By default, Angular supports two-way binding between a view and its controller. It does that by automatically setting up watchers on binding expressions in the view. This works very well most of the time, but when it doesn't everything suddenly becomes very difficult to work with. The view is also mutating the state of the controller, leading to possible issues when debugging or tracing the application.

React also has two-way binding, but [it's opt-in][react-two-way-binding] with some quite explicit syntax. The default is one-way binding, from `this.state` to the view. The view pushes data back to the component using DOM events:

	export default class TestBinding extends React.Component {
		render() {
			let testChanged = e => {
				console.log(React.findDOMNode(this.refs.test).value);
			};

			return <input type="text" ref="test" onChange={testChanged} />;
		}
	}

## The error messages are superb

![](https://i.imgur.com/2DM7t7Q.png)



## Resources &amp; further reading:

- [React tutorial][react-tutorial]
- [React.js Conf 2015 - Hype!](https://youtu.be/z5e7kWSHWTg)
- [React.js Conf 2015 - Immutable Data and React](https://youtu.be/I7IdS-PbEgI)



[react-tutorial]: https://facebook.github.io/react/docs/tutorial.html
[pen-typescript-parent-child-example]: https://codepen.io/anon/pen/PPYOzJ?editors=001
[react-autobind]: https://facebook.github.io/react/docs/interactivity-and-dynamic-uis.html#under-the-hood-autobinding-and-event-delegation
[react-two-way-binding]: https://facebook.github.io/react/docs/two-way-binding-helpers.html

