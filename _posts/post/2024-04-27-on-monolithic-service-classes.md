---
title: On monolithic service classes
permalink: /on-monolithic-service-classes
layout: post
date: 2024-04-27
category: post
---

You know the monolithic service class pattern.

```csharp
public interface IProductService
{
    Product AddNewProduct(string productName);
    IEnumerable<Product> Search(string phrase);
    void ApplyDiscount(IEnumerable<Product> products, decimal discount);
    // ...
}
```

They're a convenient place to locate business logic. Similar to the repository pattern, but looser in practice. All business functionality related to products is located in the same module, which makes finding and re-using that business logic convenient and logical.

Monolithic service classes are an easy way to start structuring a project. This pattern is incredibly common in the projects I've worked on. I can't count how many times I've created one myself.

First principles, Clarice. Although using a service class is much better practice than locating your business logic across different tiers of your application (directly in an API controller for example), they happen to break every one of the [SOLID principles of software development](https://en.wikipedia.org/wiki/SOLID).

- [**Single responsibility principle**](https://en.wikipedia.org/wiki/Single-responsibility_principle)
    - Putting different pieces of functionality in the one module means that module has multiple responsibilities. Modifying the class potentially affects all consumers of the class, not just the ones directly relying on the code being modified.
- [**Open-closed principle**](https://en.wikipedia.org/wiki/Open%E2%80%93closed_principle)
    - That is, open for extension, closed for modification. This is a tricky principle to adhere to, but the monolithic service class pattern doesn't lend itself to extension at all.
- [**Liskov substitution principle**](https://en.wikipedia.org/wiki/Liskov_substitution_principle)
    - The service class contains the business logic. If I wanted to substitute a module for a particular consumer of that logic, it's not trivial to create another implementation of part of the module - you can't trivially create a different implementation of `IProductService.AddNewProduct` that has different business rules for adding medical equipment.
- [**Interface segregation principle**](https://en.wikipedia.org/wiki/Interface_segregation_principle)
    - This is an easy one - from Wikipedia and probably directly from Robert Martin: "no code should be forced to depend on methods it does not use". If I want to write some code that discounts some products, I need to depend on a module that also contains methods for creating and searching for products.
- [**Dependency inversion principle**](https://en.wikipedia.org/wiki/Dependency_inversion_principle)
    - Dependency inversion is the broader context for what we do when we inject dependencies. We're doing dependency injnection when we inject `IProductService` into our consuming code. However, we're not *inverting* the dependency, because we're not just injecting the individual module that we depend on, and we can't trivially inject a different implementation of a product search without needing to create a new implementation of the product service or create a subclass of the existing implementation, which isn't what subclassing is for. Please don't do this. Apart from breaking the Open-closed principle it's also very nasty and difficult to understand and maintain.

I have some additional gripes with monolithic service classes:

- It's difficult to test a service class method in isolation. Instantiating the service class implementation relies on multiple dependencies, only some of which (or none of which) might be used by the method I want to test. I need to analyse the code I want to test to figure out what it actually depends on.
- Service classes grow over time. They never become _simpler_, only more complex. The largest service class I've seen was about 4000 lines of code.
- Service classes encourage internal dependencies. For example, `ProductService.ApplyDiscountToCategory()` might rely on `ProductService.GetProductsForCategory()`. This becomes complex - it can trivially add multiplers to the [cyclomatic complexity](https://en.wikipedia.org/wiki/Cyclomatic_complexity) of the module. And it's impossible to inject another implementation of `GetProductsForCategory` into `ApplyDiscountToCategory` - it depends on a concrete implmentation, again making it difficult to test in isolation.
- It's difficult to use service classes to _compose_ functionality. Related to the above, what if I wanted to have different `ApplyDiscountToCategory` that had different implementations depending on some attribute of the category? I could have `ApplyDiscountToCategoryForUK()` and `ApplyDiscountToCategoryForEU()`, but that's not easily composable or scalable.

I'm not saying that monolithic service classes are bad _per se_. There are much worse ways software can be structured. I'm saying that we can do _better_.

I'm a huge fan of the [Extract Method](https://refactoring.guru/extract-method) refactor, especially as a way to reduce the isolated cyclomatic complexity of a method in isolation. 

For example, given this method:

```csharp
public void ApplyDiscount(IEnumerable<Product> products, decimal discount)
{
    foreach (var product in products)
    {
        // Apply business rules to the product:
        if (product.somecondition)
        {
            // ... apply some business rule
        }
        else if (product.someothercondition)
        {
            if (anotherCondition)
            {
                // ... apply another business rule
            }

            // ... do something else
        }
        // ... etc
    }
}
```

This can be refactored to this:

```csharp
public void ApplyDiscount(IEnumerable<Product> products, decimal discount)
{
    foreach (var product in products)
    {
        ApplyDiscountToProduct(product, discount);
    }
}

private void ApplyDiscountToProduct(product, discount)
{
    // ... apply business rules to the product
}
```

This reduces the isolated cyclomatic complexity of `ApplyDiscount()` from 3 to 1. It's much easier to read and understand.

What I like to do, and the real point of this post, is to extend the Extract Method refactor into extracting the method into a class.

In other words, rather than just extracting the per-product business logic out into the `ApplyDiscountToProduct()` method, extract it into a kind of micro-service class, with a single responsibility.

```csharp
public interface IApplyDiscountToProduct
{
    void Execute(Product product, decimal discount);
}

public class ApplyDiscountToProduct : IApplyDiscountToProduct
{
    public void Execute(Product product, decimal discount)
    {
        // .. apply business rules to the product
    }
}
```

Then I inject and use the module in the consuming code:

```csharp
public void ApplyDiscount(IEnumerable<Product> products, decimal discount)
{
    foreach (var product in products)
    {
        _applyDiscountToProduct.Execute(product, discount);
    }
}
```

What are the benefits of doing this?

- The `ApplyDiscountToProduct` module is small - we're tending away from thousand line service classes
- The business logic in `ApplyDiscountToProduct` can be tested in isolation
- The business logic in `ProductService.ApplyDiscount()` can also be tested in isolation by injecting a mock `IApplyDiscountToProduct`, and it's fairly trivial to set up an integration test by injecting a concrete implementation
- It's relatively trivial to set up different `IApplyDiscountToProduct` implementations, if that's something you need to do
- You can compose functionality by pulling in different micro-service dependencies, rather than building up internal complexity
- It encourages code reuse - you can use an `IApplyDiscountToProduct` implementation in different places without having to depend on the service monolith
- It's easier to understand and test and maintain, which is an issue with thousand plus line monolithic service classes

How does this approach shape up with the SOLID principles?

- **Single responsibility principle**
    - The `ApplyDiscountToProduct` module has a single responsibility - applying a discount to a single product
- **Open-closed principle**
    - Composable code means that consuming code doesn't need to be modified to change the underlying implementation - either a change is limited to the relevant downstream module (the consuming code is closed to modification), or a different implementation of the downstream module can be injected into in the consuming code. Either way the consuming code is closed to modification and open for extension, and in the second case the downstream module is closed to modification as well.
- **Liskov substitution principle**
    - Using this pattern means that different implementations of the downstream module (`ApplyDiscountToEUProduct`, `ApplyDiscountToAustralianProduct`) can be substituted for each other. The consumer doesn't care what implementation of `IApplyDiscountToProduct` is doing the work.
- **Interface segregation principle**
    - Ideally, the module only has one public facing method to consume - for example, `IApplyDiscountToProduct` only exposes the `Execute()` method
- **Dependency inversion principle**
    - We're not just injecting what boils down to a mess of monolithic code into the consuming code now, we're truly inverting the dependency - we're injecting a module that has a single responsibility, and we don't care about the actual implementation from the consumer side.

As with most tools in our software development toolkit, this can be a sharp one. Don't automatically reach for this. I tend to use this a lot when I'm refactoring existing code and want to make it testable, and testable in isolation. It would be easy to take this pattern to the extreme and have hundreds and hundreds of tiny classes, increasing the consumer's complexity and defeating the purpose.

A module should only do one thing, but that doesn't mean it can't be internally complex. Here's an almost-real-world example. I needed to significantly change a search method that was in a service class. The result looked like this:

```csharp
public interface ISearchProducts
{
    Task<IEnumerable<Product>> ExecuteAsync(string phrase, CancellationToken cancellationToken);
}

public class SearchProducts : ISearchProducts
{
    private IDbContext _dbContext;
    private IBuildSearchSql _buildSearchSql;

    public SearchProducts(IDbContext dbContext, IBuildSearchSql buildSearchSql)
    {
        _dbContext = dbContext;
        _buildSearchSql = buildSearchSql;
    }

    public async Task<IEnumerable<Product>> ExecuteAsync(string phrase, CancellationToken cancellationToken)
    {
        if (DateTime.TryParse(phrase, out var timestamp))
        {
            return await ExecuteForDateAsync(timestamp, cancellationToken);
        }

        return await ExecuteForPhraseAsync(phrase, cancellationToken);
    }

    private async Task<IEnumerable<Product>> ExecuteForDateAsync(DateTime timestamp, CancellationToken cancellationToken)
    {
        return await _dbContext.Products
            // ... etc
    }

    private async Task<IEnumerable<Product>> ExecuteForPhraseAsync(string phrase, CancellationToken cancellationToken)
    {
        var (sql, parameters) = _buildSearchSql.Execute(phrase);

        return await _dbContext.Products.FromSqlRaw(sql, parameters)
            // ... etc
    }
}
```

I could have extracted `ExecuteForDateAync` and `ExecuteForPhraseAsync` into their own micro-service classes, but I didn't really feel the need to. In fact, because of the use of EF in the module, I just ended up integration testing the entire module, teasing out the different code paths as I went. To build up the SQL in the integration tests I injected the actual `IBuildSearchSql` implementation.

And, since the SQL is built up in isolation in the `IBuildSearchSql` implementation, I could easily unit test that SQL builder in isolation.

![Marge Simpson holding a potato](/images/2024-04-27-on-monolithic-service-classes/marge.jpg)

I just think they're neat.