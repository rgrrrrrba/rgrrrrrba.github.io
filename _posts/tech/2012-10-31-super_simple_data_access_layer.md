---
title: Super-simple data access layer
layout: post
date: 2012-10-31
type: regular
category: tech
---

I've released a simple data access component that makes working with MS SQL databases in C# much more fun. It is a [single file](https://github.com/swxben/Shu-Er/blob/master/dotnet/DataAccess/src/Swxben.DataAccess/DataAccess.cs) that can be dropped into a project, passed a connection string, and used with very little interference. It is similar to the excellent [Massive](https://github.com/robconery/massive) but without as much magic.

Queries and commands can be executed using SQL, passing in an anonymous or strongly-typed model:

	dataAccess.ExecuteCommand( "INSERT INTO Customers(CustomerId, CustomerName) VALUES(@CustomerId, @CustomerName)", new { CustomerId = 1, CustomerName = "SWXBEN" } ); 

`INSERT` and `UPDATE` commands can also be created using some simple assumptions about the table (just adding 's' to the model name) and with no need for attributes or naming conventions:

	class Customer { public int CustomerId; public string CustomerName }  var customer = new Customer { CustomerId = 1, CustomerName = "SWXBEN" }; dataAccess.Insert(customer);  customer.CustomerName = "Software by Ben"; dataAccess.Update(customer, "CustomerId"); 

The name of the ID column is passed in to the `Update` method. In the future I may add some simple conventions to figure out the identifier automatically.