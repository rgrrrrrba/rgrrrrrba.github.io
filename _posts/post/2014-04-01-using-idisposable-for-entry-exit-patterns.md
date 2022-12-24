---
title: Using IDisposable for entry/exit pattern
layout: post
date: 2014-04-01
category: archived
---

I don't know if "entry/exit" is the right name for this pattern, but it gets used a lot in client-side code. It relates to the use of guard sections and cross-cutting concerns. For example, using Caliburn.Micro you might have a WPF viewmodel (implementing `PropertyChangedBase`) that sets  `IsNotifying` flag to false, executes some code that would otherwise cause notifications, then sets the `IsNotifying` flag to true.

	IsNotifying = false;

	Name = "foo";	// won't notify

	IsNotifying = true;

The problem with this is having to explicitly set the flags before and after the operation. What if some statements get moved around, or the assignations get dropped? This can be a tricky issue to resolve as the usage of the class isn't very clear to consumers.

A pattern I've seen is to wrap the code in an `Action` invoker:

	public void InvokeWithoutNotification(Action action) {
		IsNotifying = false;
		action();
		IsNotifying = true;
	}

	this.InvokeWithoutNotification(() => {
		Name = "foo";
	});

	// or:

	viewModel.InvokeWithoutNotification(() => viewModel.Name = "foo");	


This is much nicer, but there is a layer of indirection with the action. You could step right over the entire action payload when debugging, the stack trace has an extra entry. Still, this is a very attractive solution.

I had a thought that you could do something very similar using `IDisposable`:

	public class PropertyChangedBaseIsNotifyingGuard
		: IDisposable {
		PropertyChangeBase _target;

		public PropertyChangedBaseIsNotifyingGuard(PropertyChangeBase target) {
			_target = target;
			_target.IsNotifying = false;
		}

		public void Dispose() {
			_target.IsNotifying = true;
		}
	}

	public static class PropertyChangedBaseExtensions {
		public static PropertyChangedBaseIsNotifyingGuard WithoutNotification(
			this PropertyChangedBase target) {
			return new PropertyChangedBaseIsNotifyingGuard(target);
		}
	}

This gets used in a `using` statement:

	using (this.WithoutNotification()) {
		Name = "foo";
	}

	// or

	using (viewModel.WithoutNotification()) {
		viewModel.Name = "foo";
	}

There is more ceremony involved with the setup and there isn't a heap of immediate benefits. One is the seperation of this guard logic into its own class, which may or may not be appropriate but is at least an interesting possibility. You could also combine guards quite easily:

	using (viewModel.WithoutNotification())
	using (viewModel.WithoutValidation())
	using (viewModel.ValidateOnCompletion())
	{
		viewModel.Name = "foo";
	}

	// ValidateOnCompletion:
	public class SpecialViewModelValidateOnCompletionGuard
		: IDisposable {
		PropertyChangeBase _target;

		public SpecialViewModelValidateOnCompletionGuard(SpecialViewModel target) {
			_target = target;
		}

		public void Dispose() {
			_target.PerformValidation();
			_target.CommitValidation();
			_target.RainbowsAndUnicorns();
		}
	}
	public static class SpecialViewModelExtensions {
		public static SpecialViewModelValidateOnCompletionGuard ValidateOnCompletion(
			this SpecialViewModel target) {
			return new SpecialViewModelValidateOnCompletionGuard(target);
		}
	}

This is getting very similar to what you can achieve in [Aspect-oriented programming](https://en.wikipedia.org/wiki/Aspect-oriented_programming) using entry and exit pointcuts, however in AOP the pointcuts can only extend down to method granularity, so the payload would have to be its own method. I think it's a pretty neat compromise.
