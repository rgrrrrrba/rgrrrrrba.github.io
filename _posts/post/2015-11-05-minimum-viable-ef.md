---
title: Minimum Viable EF
layout: post
date: 2015-11-05
category: archived
---

LINQ to SQL provides a mechanism for generating complex queries without having to resort to inline SQL or custom query generation syntax. I happen to like LINQ to SQL as a SQL generation tool, and using EF to execute those queries and hydrate objects is a passable experience, especially in the more recent version of EF. Entity Framework also provides reasonable good batching of mutation (update/insert/delete) operations.

However, Entity Framework spoils us with navigation properties. We get largely simplified query generation at the cost of visibility, and if something fails we are faced with inscrutable errors and increasingly obtuse workarounds.

In this post I propose reducing and eventually eliminating the reliance on navigation properties. At the same time (and partly as a consequence) I'll look at moving functionality out of aggregate roots and reducing a tendency to prematurely DRY and deduplicate a design. After years of struggling with EF and building gloriously fragile architectures, I've personally had some success when embracing this technique and I'm especially happy with the resulting simplification of architecture that the technique encourages.

The examples I'm using are based on an extremely simplified investment portfolio management tool. There are multiple portfolios, multiple securities, and each portfolio can have multiple portfolio items. Each portfolio item is linked to a portfolio and a security. Portfolio items also have a count of units, and securities have a current price.

The test application I developed while writing this is on [GitHub](https://github.com/becdetat/minimal-ef-testbed).


## Navigation properties and lazy loading

Entity Framework has an interesting feature involving navigation properties called _lazy loading_. A navigation property (declared as a `virtual` property on the entity class) will have its value materialised from the database when it is accessed. This can keep the initial load low if only some navigation properties are required from the initial set of results. These are the simplified entities from my test application with navigation properties present:

	class Portfolio {
		public Guid Id { get; set; }
		public string Name { get; set; }
		public virtual ICollection<PortfolioItem> Items { get; set; }
	}

	class Security {
		public Guid Id { get; set; }
		public string Code { get; set; }
		public string Name { get; set; }
		public decimal Price { get; set; }
	}

	class PortfolioItem {
		public Guid Id { get; set; }
		public Guid PortfolioId { get; set; }
		public virtual Portfolio Portfolio { get; set; }
		public Guid SecurityId { get; set; }
		public virtual Security Security { get; set; }
	}

Navigation properties can be explicitly included in the initial query, opting them into eager loading. This example would get all portfolios, including their items - the `.ToArray()` executes the query so everything selected by the query is in memory - _materialised_ in EF parlance:

	var portfolios = context.Portfolios.Include(x => x.Items).ToArray();

This SQL that is generated from this query is roughly equivalent to this:

	SELECT p.*, i.*
	FROM [dbo].[Portfolio] p
	INNER OUTER JOIN [dbo].[PortfolioItem] i ON p.Id = i.PortfolioId

<aside class="pull-right well" style="width: 17em">
	This is the <em>only</em> time that I have been impressed by EF Code First migrations.
</aside>

The SQL that really gets generated is a bit more complex but the structure is basically the same. You can use LINQPad to view the SQL that gets generated for a given LINQ to SQL query. Add the Entity Framework NuGet package and this code - LINQPad managed to connect to my SQL Server instance and EF created the appropriate database and schema using a Code First migration:

	void Main()
	{
		var context = new Context();
		
		var query = context.Portfolios.Include(x => x.Items);
			
		var property = typeof(System.Data.Entity.Infrastructure.DbQuery<Portfolio>).GetProperty("InternalQuery", (BindingFlags)int.MaxValue);
		property.GetValue(query).ToString().Dump();
	}

	// ... entity classes as above

	class Context : DbContext {
		public IDbSet<Portfolio> Portfolios { get; set; }
	}

Whenever a navigation property that wasn't included in that initial load is accessed, the database is re-queried just for that one item. This is useful if you just wanted to get to the `Security` property for a given portfolio item:

	var item = context.Portfolios.First().Items.First();

	// Accessing .Security performs a second query
	var name = item.Security.Name;


## The problem(s)

If I try to pull out the security for every loaded portfolio item, the performance (in terms of database round-trips) goes from O(1) to O(n+1). Given 1000 portfolio items, 1000 additional queries will be round-tripped over the network and executed just to get the security for each item. This is a bad thing. It's possible to opt in to eager loading of a navigation property (using `.Include`) but I'll get into that later.

I have several problems with lazy loading of navigation properties:

- The cost (in terms of latency) isn't clear when accessing a navigation property - without some external tracking mechanism it's difficult to predict which properties nested in a collection will trigger the load.
- Entity Framework executes the navigation properties queries synchronously, whereas the initial query could be performed asynchronously. There is no control over the query mechanism.
- Complex or non-standard relationships such as composite keys have to be expressed using attributes or special configuration in the database context. Apart from the effort involved in configuring these relationships (very error prone) they are then an extra maintenance burden.

Unfortunately the prevailing practice of encapsulating logic in the entities by way of the [Aggregate pattern](https://martinfowler.com/bliki/DDD_Aggregate.html) when using Entity Framework requires the use of navigation properties for it to be efficient. When writing the query to produce performant code that consumes navigation properties somewhere down the call path you have no choice but to be aware of (usually _discover_, often through trial and error) the navigation properties that will be required when the results are processed.

Here's the `PortfolioItem` class, showing some encapsulated logic (the `Value` property):

	class PortfolioItem {
		//...
		public decimal Units { get; set; }
		public virtual Security Security { get; set; }
		public decimal Value => Units * Security.Price;
	}

This is obviously a trivial example, but what happens if the implementation of that encapsulated logic changes and requires a different navigation property? Each place where a query is generated that then uses that encapsulated logic _somewhere_ along the call path needs to be updated to reflect the new underlying structure. Otherwise you'll either get a pile of additional queries dragging down performance or a difficult to diagnose null reference exception if the underlying context is somehow closed. I personally prefer the null references - it's easier to fix your application crashing than to fix users complaining about performance.

These issues with query visibility are compounded if if we're reusing query fragments by operating on `IQueryable<T>`.

	public static IQueryable<Portfolio> GetAllPortfoliosBelongingToClient(this IQueryable<Portfolio> portfolios, Guid clientId) {
		return context.Portfolios
			.Include(x => x.Items)
			.Where(x => x.ClientId == clientId);
	}

	// ...

	var portfolios = _context.Portfolios.GetAllPortfoliosBelongingToClient(clientId).ToArray();

<aside class="pull-right well" style="width: 26em">
	The entire reusable query fragment pattern is dreadful, but I'll leave that for another rant. I usually lump it in with 'reusable' mapper classes, misused flag enums, <a href="a-short-executable-rant-on-why-i-dislike-object-initialization-syntax.html">object initialisation syntax for anything not a DTO</a>, tuples (although <a href="https://github.com/dotnet/roslyn/issues/347">C# 7 may fix this</a>) and nested ternary expressions.
</aside>

`Items` are included to make the query more performant by eager loading the collection for each portfolio. Note that the item securities are not included and will be lazily loaded.

Say that in one of the three places where this query is used, I want to access the current security price for every item. It makes sense to add the new include to the query, right? After all, this is where the query is defined and I'm already specifying that the portfolio items should be eagerly loaded here.

	return portfolios
		.Include(x => x.Items)
		.Include(x => x.Items.Select(i => i.Security))
		.Where(x => x.ClientId == clientId);

Except now the security is selected and pulled into memory every single time this query is used, which isn't appropriate in the other two usages of the query fragment and will cause extra load on the database and the network.

**By relying on navigation properties we're getting simplified query generation at the expense of clean code.**

The result is a spaghetti mess of unclear dependencies and configurations,  difficult to diagnose performance issues and 'fixes' that have require modifying unrelated parts of the system. Fixes that inevitably produce more bugs.


## Denormalising data

An easy, early optimisation that should happen is denormalising some data. When I first started learning about database design, way back in high school, a heavy emphasis was placed on normalisation. The goal with any database design was to get it to the highest Nth Normal Form possible, and damn the consequences.

A normalised design is crucial to having a performant, modular and extendable system. Aggressively deduplicating data and chasing the perfectly normalised schema imaginable can have precisely the opposite effect.

To get the security code for an item in a portfolio in SQL requires an extra table join:

	SELECT p.Id, s.Code
	FROM [dbo].[Portfolios] p
	INNER JOIN [dbo].[PortfolioItems] i ON i.[Id] = p.[Id]
	INNER JOIN [dbo].[Securities] s ON s.[Id] = p.[Id]	# <-- this one
	WHERE -- some condition

<aside class="pull-right well" style="width: 18em">
	For all of its faults (and there are plenty) SQL is still a great language for querying a relational database. Hashtag makuthink.
</aside>

That doesn't seem like a big deal. In fact, SQL Server is extremely good at joining tables. There should already be a foreign key relationship between `PortfolioItems` and `Securities`, which lets SQL Server perform optimisations.

The problem, as described above, is when our ORM - Entity Framework - needs its hand held to eagerly load the `Security` property:

	var selectedPortfolios = context.Portfolios
		.Include(x => x.Items)
		.Include(x => x.Items.Select(i => i.Security))
		.Where(x => /* some condition */)
		.ToArray();

	// later...
	var securityNames = selectedPortfolios
		.SelectMany(x => x.Items)
		.Select(i => i.Security.Name)
		.ToArray();

<aside class="pull-right well" style="width: 33em">
	The <code>// later...</code> comment is important, because I could just select out the security name in the initial query and obviate this entire example. I'm making an assumption that, as is usually the case in the systems I've worked in and created, the selected portfolios are queried in one place then being consumed elsewhere. Generally both of those places are only tangentially related through some common consumer. As I'll explain soon, this is a bad practice that I would like to challenge.
</aside>

If the `Security` property isn't eagerly loaded, I get lazy loading in that final `.Select(i => i.Security.Name)` resulting in O(n) performance.

To denormalise the structure I can apply a piece of domain knowledge. Security names don't change very often relative to other pieces of data in a financial system. Currently (start of November) there have been 95 name (and code) changes on the [Australian Stock Exchange](https://www.asx.com.au/prices/company-name-and-asx-code-changes-2015.htm) in 2015, out of around 2200 listed companies. So this isn't a piece of information that changes very frequently. If I copy the security name to the portfolio item, the `Security` property no longer needs eager loading.

	var securityNames = selectedPortfolios
		.SelectMany(x => x.Items)
		.Select(i => i.SecurityName)
		.ToArrray();

To maintain data integrity I can use a domain event that gets triggered whenever a security name is changed. The name of the security doesn't have to be immediately available to each portfolio item when it changes - a slight delay is ok.

	void Handle(SecurityNameChanged @event) {
		foreach (var item in portfolioItems.Where(x => x.SecurityId == @event.SecurityId).ToArray()) {
			item.UpdateSecurityName(@event.SecurityName);
		}
		portfolioItems.SaveChanges();
	}

In many cases this wouldn't even need to happen in a domain event. A maintenance task could regularly make sure the denormalised security name is consistent with the canonical security, ensuring _eventual_ data integrity.


## Remove navigation properties

Denormalising the structure is a good step when appropriate (and you should first be able to justify it with real numbers), however it does add complexity to the system as a whole when having to deal with data integrity. As I said before, SQL Server is really really good at joining tables.

What I propose is reducing the reliance on navigation properties - removing them altogether where possible - and replacing them with more explicit queries.

As an example, this is how you would write a query for all security codes for a portfolio using navigation properties:

	var query =
		from portfolio in context.Portfolios
		where portfolio.Id == portfolioId
		let codes = portfolio.Items.Select(x => x.Security.Code)
		select codes;
	var result = query.Single();

This is how you would write the same query without using navigation properties:

	var query =
		from portfolio in context.Portfolios
		where portfolio.Id == portfolioId
		let codes = 
			from item in context.PortfolioItems
			where item.PortfolioId == portfolio.Id
			let security = context.Securities.FirstOrDefault(s => s.Id == item.SecurityId)
			select security.Code
		select codes;
	var result = query.Single();


<aside class="pull-right well" style="width: 27em">
	<p>Note that I can't use <code>context.Securities.Single(...)</code> because, according to LINQ to SQL: "The methods 'Single' and 'SingleOrDefault' can only be used as a final query operation. Consider using the method 'FirstOrDefault' in this instance instead."</p>	
	<p><code>var result = query.Single()</code> is ok because it is the 'final query operation'.</p>
</aside>

The query is now more complex, but I've moved the previously hidden complexity of navigation properties back into the query. In my opinion the query is where that complexity belongs - not off-loaded to an ORM.

### Addressing lazy loading

Since I've removed the navigation properties, I can't do automatic lazy loading via EF any more. This is a good thing because it forces me to write another query to get the property. Remember that the query was going to happen anyway - I now just have control over how it happens. I can execute it asynchronously and I also have the option to only select the data I require.

Based on the earlier example of lazily loading the security for a given portfolio item:

	var item = items.First();

	//var name = item.Security.Name;

	var security = context.Securities.Single(x => x.Id == item.SecurityId);
	var name = security.Name;

	// or to just get the name
	var name = context.Securities
		.Where(x => x.Id == item.SecurityId)
		.Select(x => x.Name)
		.Single();


### Thinking about the query

This method forces me to think more about the query I'm writing and how that will be translated into SQL. This gives me an opportunity to write queries that are focused on the job at hand. I can't just select a heap of data and pass it somewhere else for processing without knowing how that processing will happen - the way to implement that when leveraging navigation properties is with a pile of fragments that get applied at different stages, which is an opaque and dangerous strategy.

This also means that the query itself can live closer to the calculations that get performed on it in memory. In fact, the query can often be co-located with the calculations that depend on it. This means less spaghetti code and a less 'sophisticated' (complicated) architecture.


### Strategies for changing logic in the entity

Removing navigation properties from the entity means that logic in the entity that depends on those navigation properties has to change. This is acceptable because it means that the consumers of that logic now have to be aware of what is required to perform that logic and I don't get saddled with obtuse configuration and O(n) performance issues.

To deal with this I need some strategies for reimplementing this entity logic.


#### Queries

The value of a portfolio item is simply the number of units multiplied by the current price of the security. A naive implementation of a query to generate a report showing the investments in a portfolio and the values would look like this:

    var query =
        from portfolio in _context.Portfolios
        where portfolio.Id == portfolioId
        let items =
            from item in _context.PortfolioItems
            where item.PortfolioId == portfolio.Id
            let security = _context.Securities.FirstOrDefault(s => s.Id == item.SecurityId)
            select new
            {
                security.Code,
                Value = item.Units*security.CurrentPrice
            }
        select items;

This is a very simple calculation so it isn't really a good target for applying DRY but I'll use it for now. If I was treating the portfolio item as an aggregate root using navigation properties, that `Value` calculation would live as a property on `PortfolioItem`:

	public decimal Value => this.Units * this.Security.CurrentPrice;

This can be implemented without the navigation property by explicitly passing the current price or the security itself:

	public decimal GetValue(decimal currentPrice) => this.Units * currentPrice;
	
	// or, if you wanted to hide the implementation detail
	// and not make the consumer concerned about where the
	// current price comes from:
	
	public decimal GetValue(Security security) => this.Units * security.CurrentPrice;

These methods can't be called directly by EF. This isn't because I removed the navigation properties. EF wouldn't be able to call the previous property either with a `get;` body or an expression body - LINQ to SQL simply can't translate the expression into SQL. So I need a second stage to map the results from the EF query into the the final results.

    var query =
        from portfolio in _context.Portfolios
        where portfolio.Id == portfolioId
        let items =
            from item in _context.PortfolioItems
            where item.PortfolioId == portfolio.Id
            let security = _context.Securities.FirstOrDefault(s => s.Id == item.SecurityId)
            select new
            {
                item,
                security
            }
        select items;
    var results =
        from item in query.Single().ToArray()
        select new
        {
            item.security.Code,
            Value = item.item.Value(item.security)
        };

You may be tempted to break the query and the map stages into separate classes, possibly even calling the map step a mapper. Reconsider! There is a good chance that the query and map are only going to be used in this one place (if not, they probably should be). If you try to DRY your application too much and too soon it will probably crack&trade;. Extracting the stages would also require a pile of intermediary classes to bridge between the stages. [Record types in C# 7](https://github.com/dotnet/roslyn/issues/206) (if they happen) will reduce the amount of boilerplate code but until then the overheads will likely outweigh any benefit. For most cases I would just keep the query and the map stage together. They can still be tested as a whole by simply mocking out the EF context.

That's not to say that individual complex calculations shouldn't be refactored into service classes when appropriate. Since the `Value` calculation is now being done in memory it could easily be extracted:

	class PortfolioItemValueCalculator {
		public decimal Calculate(PortfolioItem item, Security security) {
			return item.Units * security.CurrentValue;
		}
	}

This is a terrible example so here's a better one. Warning - I'm about to drop some serious domain knowledge. In a portfolio management system you often need to calculate the cost base of an investment, say to calculate the change in value for the investment, or to work out the tax consequences of selling the investment. There are several methods for calculating a cost base (financial planners are interested in performance, tax accountants are interested in tax consequences) and regardless of the method it can be quite complicated. Cost base is calculated using a number of variables such as the cost of the parcels that make up the investment, any capital returns received, the effect of corporate actions such as demergers, etc. So a cost base calculator for a given method is an excellent candidate for extraction and reuse as a service class. An example calculator signature might look something like this:

	class AverageTaxCostBaseCalculator {
		public decimal Calculate(
			Parcel[] parcels, 
			CapitalReturnDistribution[] capitalReturns,
			CorporateAction[] corporateActions);
	}


#### Updates, inserts and deletes

Navigation properties also have the advantage of already being tracked by EF, so any updates to them will be persisted when the context is saved. This doesn't change when pulling those entities out explicitly in queries - EF will track them as well, as long as you've used the same context. In other words, don't use `InstancePerDependency` to register the context in Autofac - use something like `InstancePerRequest` or `InstancePerLifetimeScope`.

Of course, this means that methods in an aggregate root that create or delete child entities will need to be restructured. Take this method that adds a new item to a portfolio using navigation properties:

	class Portfolio {
		// ...
		public virtual ICollection<PortfolioItem> Items { get; set; }
		public void AddItem(Security security, decimal units) {
			var item = new PortfolioItem(this, security, units);
			this.Items.Add(item);
		}
	}

To work without the `Items` navigation property, I need to pass the EF context to the method. Entities are constructed by EF and can't get dependencies injected by a DI container, so this has to be done explicitly. If you didn't pass the context in then the caller would be responsible for adding it to EF - a prime opportunity for creating a bug.

Another consequence of removing the navigation properties is that the portfolio entity _per se_ now doesn't have a concept of the items that it contains. To mitigate that the `AddItem` could return the new item, which could be consumed in the calling code:

    public PortfolioItem AddItem(IDbContext context, Security security, decimal units)
    {
        var item = new PortfolioItem(this, security, units);
        context.PortfolioItems.Add(item);
        return item;
    }

<aside class="pull-right well" style="width: 23em">
	There are more complicated scenarios such as making multiple passes over a collection of items that are alternately queried and updated. In this situation I argue that a domain object is the incorrect level of abstraction to be holding this collection. It should be the responsibility of the orchestrating function to maintain then persist its state.
</aside>

This has the smell of a dirty workaround that will be more trouble than it is worth. It raises some questions:

- During an read/write/delete operation, why do I _need_ to know the items that a portfolio contains? Surely this is an update, not a query? What is that collection of items actually giving me, if I shouldn't be using it in this operation anyway?
- Why does the portfolio even _have_ a concept of adding an item? An investment portfolio is a collection of assets and other related information. It isn't the thing that does the purchasing. In reality, the owner or manager of the portfolio instructs a stockbroker to purchase an asset in the name of the legal entity that owns the portfolio. I'm simply recording the fact that a new asset has been allocated to that portfolio. Why is purchasing a new investment considered an activity that the portfolio is taking at all?

If I can consider allocating an investment to a portfolio to be unrelated to the portfolio, this method should be removed from the portfolio (Single Responsibility Principle) and extracted into a service:

	class AddAssetToPortfolio {
		// ...
		public void Add(Portfolio portfolio, Security security, decimal units) {
			var item = new PortfolioItem(portfolio, security, units);

			_context.PortfolioItems.Add(item);
		}
	}

Note that the service doesn't call `_context.SaveChanges()`. That is left for the consuming code. It could be done in the service itself but because I've got a shared EF context I can take advantage of batching. I could also use a unit of work implementation that wraps the EF context and flushes the context automatically.

In an example this trivial I don't think a discrete service class is actually warranted. I may as well just perform the operation in the consuming code. Generally I would defer extracting out service classes until it is clear that the logic will actually be reused and is too complex to be safely duplicated.


## Doesn't this break the Aggregate Root concept?

To a degree. As discussed above, there are strategies for changing the implementation of an aggregate root's embedded logic that allows for the removal of navigation properties from the aggregate root itself.

Aggregate roots are a useful thought technology that inform us of how to model discrete pieces of domain functionality by aggregating it into subdomains. Generally this gets expressed as pushing all the domain logic into an entity class. I argue that this is an anti-pattern.

I've seen domain objects become demigod classes, with pages of methods and unseparated concerns. There is a strong and correct argument that these overly complex aggregate roots are the result of bad planning and a lack of large-scale refactoring over time, however the reality is that software does get built like this and I find it disingenuous to try to sell rewrites and large scale refactors to a client as 'paying off tech debt'.

Discounting this strict application of aggregate roots doesn't mean that we can't aggregate domain functionality in other ways, by pushing common functionality into small service classes or domain event handlers, and by breaking up the domain via microservices as needed. Domain-driven design doesn't mean simply mean lumping your application's functionality in your key domain objects.

 
### Domain event handler

The domain event handler for updating security names (above) is an example of extracting functionality from an aggregate root / entity. The aggregate root way of implementing updating security names would be a method on `Security`, using a collection of portfolio items linked to the security:

	class Security {
		// ...
		public virtual ICollection<PortfolioItem> PortfolioItems { get; set; }

		public void UpdateName(string name) {
			this.Name = name;
			foreach (var item in this.PortfolioItems) {
				item.UpdateSecurityName(name);
			}
		}
	}

Given this design, whenever I want to update a security name I need to remember to eagerly load the `PortfolioItems` as well. Extracting out the update part of that functionality reduces the complexity on the consumer side, and since the update can be performed out-of-band (maybe pushed into another process) the operation of updating a security name becomes simpler and more performant.


### Service class

Of course, a domain event handler is just a nice auto-wired wrapper for a (hopefully) single purpose service class. I could instead have a service class that replaces the above aggregate root style `Security.UpdateName()` method and also implements the domain event to update the denormalised names on `PortfolioItem`. 

This is a contrived example - in this situation a domain event handler is probably a much better choice unless you need to take advantage of EF's implicit database transaction to maintain integrity in case of a failure.

	class UpdateSecurityName {
		// _context...

		public void Update(Guid securityId, string name) {
			var query =
				from security in _context.Securities
				where security.Id == securityId
				let items = _context.PortfolioItems.Where(i => i.SecurityId == securityId)
				select new {
					security,
					items
				};
			var result = query.Single();

			result.security.UpdateName(name);
			foreach (var item in result.items) {
				item.UpdateSecurityName(name);
			}

			_context.SaveChanges();
		}
	}


## Isn't this all a bit too hard?

You don't have to buy in wholesale to this approach. It should be ideal for limited usage without affecting the system as a whole. I believe that treating EF as an efficient SQL generation library and data access layer is a viable alternative to attempting to deal with EF as a monolithic ORM, stopping far short of throwing it away in favour of something even more lightweight such as Massive or Dapper, or pivoting to a non-relational data store for a completely different approach.

I wouldn't try to implement this across the board in an application already using navigation properties - there's rarely any benefit to that kind of large scale adoption.

It also makes refactoring relationships more difficult, because those relationships are now referenced directly in consuming code rather than as a consequence of the simple possession of a navigation property.

I would argue that refactoring relationships _should_ be non-trivial. Changing a relationship can have unconsidered and difficult to discover effects on the performance of an application that makes use of navigation properties.

My opinion is that making complexity explicit up-front reduces the overall cost of development. By hiding this complexity away you're buying into a different class of complexity - one that is notoriously difficult to manage and maintain. Combined with an action-oriented approach (vs a domain model / aggregate root based approach) and minimising the code put into a domain model class, using more explicit queries while reducing the dependence on Entity Framework features can vastly simplify an application's architecture.



