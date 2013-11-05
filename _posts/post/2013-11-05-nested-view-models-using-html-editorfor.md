---
title: Nested view models using Html.EditorFor
layout: post
date: 2013-11-05
category: post
---

This was written while figuring out the best way to compose a view with a nested view model. [Here is a git repo](https://github.com/bendetat/test-nested-view-model). I haven't spent much time in ASP.NET MVC so be fair.

I'm trying to illustrate composing a view. I suspect a better method would be `Html.EditorFor` but that approach doesn't seem as rich as using partials (how would I use different editors for the same view model in a different context?).

TL;DR: it turns out that `Html.EditorFor` is _exactly_ the correct solution, and as you can specify the template it is as flexible as you would expect.


### Using a partial

The views look like this:

`Index.cshtml`:

	@model NestedViewModel.Models.HomeViewModel

	<p>
	    @Html.LabelFor(x => x.HomeViewModelField)
	    @Html.TextBoxFor(x => x.HomeViewModelField)
	</p>

	@Html.Partial("_Nested", Model.Nested)

`_Nested.cshtml`:

	@model NestedViewModel.Models.MyNestedViewModel

	<p>
	    @Html.LabelFor(x => x.NestedViewModelField)
	    @Html.TextBoxFor(x => x.NestedViewModelField)
	</p>

The result looks like this:

	<p>
	    <label for="HomeViewModelField">HomeViewModelField</label>
	    <input id="HomeViewModelField" name="HomeViewModelField" type="text" value="" />
	</p>

	<p>
	    <label for="NestedViewModelField">NestedViewModelField</label>
	    <input id="NestedViewModelField" name="NestedViewModelField" type="text" value="" />
	</p>

Note that `NestedViewModelField`'s name won't bind back to the view model. For model binding to work the name should be `Nested.NestedViewModelField`.


### Using `Html.EditorFor`

If I make a view with the same name as the nested view model's type, in `Views/Home/EditorTemplates`, then `Html.EditorFor` will find it and use it. [More on this from @shanselman](http://www.hanselman.com/blog/ASPNETMVCDisplayTemplateAndEditorTemplatesForEntityFrameworkDbGeographySpatialTypes.aspx).

`Views/Home/EditorTemplates/MyNestedViewModel.cshtml` is the same as `_Nested.cshtml` above. The index view swaps out the `Html.Partial` for `Html.EditorFor`:

	@model NestedViewModel.Models.HomeViewModel

	<p>
	    @Html.LabelFor(x => x.HomeViewModelField)
	    @Html.TextBoxFor(x => x.HomeViewModelField)
	</p>

	@Html.EditorFor(x => x.Nested)

The result is cooler than I expected. The nested view model's field names are correct :win: :

	<p>
	    <label for="HomeViewModelField">HomeViewModelField</label>
	    <input id="HomeViewModelField" name="HomeViewModelField" type="text" value="" />
	</p>

	<p>
	    <label for="Nested_NestedViewModelField">NestedViewModelField</label>
	    <input id="Nested_NestedViewModelField" name="Nested.NestedViewModelField" type="text" value="" />
	</p>


### Composing views and DRY

The reason I wanted to explore nested view models was to be able to reuse parts of a form across different views and controllers, so that I am composing the views in a manner that is DRY. Partials are ok because they can be shared across the application, but they don't respect the view model. `EditorTemplates` (and `DisplayTemplates`) work better than I expected and look to be the correct solution. So how can I share an editor template across different controllers?

`Html.EditorFor` takes a `templateName` argument. I didn't expect that. As a special bonus, the engine searches for editor templates in `Views/Shared` by default ([SO](http://stackoverflow.com/a/7841835/149259)). So I dropped two editors for my nested view model into `/Shared/EditorTemplates/MyNestedViewModel/` and specified which templates to use:

	@Html.EditorFor(x => x.Nested, "MyNestedViewModel/EditorOne")
	@Html.EditorFor(x => x.Nested, "MyNestedViewModel/EditorTwo")

Magic. Intellisense even picked up the template name.




