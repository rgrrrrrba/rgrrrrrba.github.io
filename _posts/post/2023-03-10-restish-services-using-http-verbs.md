---
title: RESTish services using HTTP verbs
permalink: /restish-services-using-http-verbs
layout: post
date: 2023-03-10
category: post
---

This post is heavily based on a great resource I found: [Using HTTP Methods for RESTful Services](https://www.restapitutorial.com/lessons/httpmethods.html). I wanted to reproduce it a bit and add/subtract my own thoughts.

If you're a technical reader of this blog, you're probably aware of the terms [HTTP](https://en.wikipedia.org/wiki/HTTP) and [REST](https://en.wikipedia.org/wiki/Representational_state_transfer). If you're not a technical reader you can probably safely skip this one, I don't talk about Buddhism or consulting even _once_. Here's a brief explainer.

### HTTP verbs, and REST 'vs' RPC

<aside class="well pull-right" style="width: 15em">
	The description for HTTP verbs is very literal. To make a request for a web page, the client opens TCP port 80 (or more commonly 443 these days) and sends a request of the form <code>GET /my-page.aspx</code>. The server then responds with the content of the resource <code>/my-page.aspx</code>.
</aside>

**HTTP** (Hypertext Transfer Protocol) is a layer of the networks that make up the internet, which is used to request and modify resources. Some HTTP verbs are `GET`, which is a request to a web server to _get_ a resource, and `POST`, which is a way of submitting (or "posting") data to a web server.

**REST** (Representational State Transfer) is a style of software design that provides a framework around identifying and manipulating resources. Typically an architecture that is **RESTful** uses HTTP verbs against a web server, which does the work involved in getting or manipulating resources. Note that a RESTful architecture doesn't depend on being implemented using HTTP—the same architecture could be used in any scenario where there is a service accepting instructions from a client where the types of instructions can be clearly deliniated and the results can be passed back in a way that the client can understand.

Each different HTTP verb (`GET`, `POST`, `PUT`, `PATCH`, and `DELETE`) is used differently under a RESTful architecture.

The precursor, and in some ways still a competitor to REST is called RPC, or Remote Procedure Call. This is a relatively simple architecture, where a `GET` call is mapped to a procedure that returns data, and a `POST` (or other verb) maps to a procedure that is used to manipulate data. There isn't a very clear delineation between how RPC separates `GET` and `POST` calls, and some systems (especially larger systems as they grow) can have quite complex RPC architectures that become difficult to navigate, work with, and maintain.

Many advocates of REST insist that the architecture be based entirely and purely on REST practices, resulting in a completely RESTful architecture. REST is very opinionated, and following the architectural style can make for extremely flexible but difficult to navigate hierarchies. For example, in an application which handles replacing a tire and raising an invoice for the work, the program flow may look like this:

```
PATCH /customers/123/tires/456
POST /customers/123/invoices
GET /invoices/5454
```

Under strict REST (as I understand it), this has to be three separate operations:
1. Update the tire resource
2. Creating an invoice against the customer—this returns a link to the new invoice resource
3. Get the invoice so it can be printed out etc

This is great, but it adds some traffic overhead and design overhead. Plus, the system has to deal with situations where, for example, the `PATCH` operation is successful but the client gets terminated in some way before being able to send the `POST` operation to raise the invoice.

From the client's perspective, the three operations are meant to be atomic—they happen together or not at all. There are ways of doing this in REST by building server state in the form of a correlation ID, so that for example the `PATCH /customers/123/tires/456` is only made valid when the invoice is raised, otherwise it sits in limbo to be cleaned up by some other process, such as an audit process that looks for tire operations that don't have a corresponding invoice.

Although this sounds like a cool system, needless to say this could be a lot of work.

### REST-ish architecture
What I prefer to do is what I call REST-ish architecture. You can use the HTTP verbs as they were intended under the REST architecture, but be ok with making exceptions that are more RPC in style.

Say that this company currently _only_ does something to a single tire, then generates a single invoice for that tire. There's no plans for that to change, and we want to work lean—limiting the amount of work that is going to increase both the up-front cost and maintenance cost if we're not expecting any big changes to the processes in the near future.

You could do that in more of an RPC style like:
```
PUT /customers/123/replace-tire/456
```

The server would do something like:
```
Replace the tire on the customer's vehicle
Create an invoice against the customer referring to the tire
Return the invoice
```

So we're using a RESTful architecture at the start, but instead of using the `/customers/{id}/tires` path to modify a tire, we're calling a remote procedure on the customer itself. It's kind of the difference between using a `struct` or `record`, vs having a richer domain object that can perform actions on itself.

It's also a single call, so there's more built-in fault tolerance. The server would hopefully implement this in a single database transaction, ensuring that the system doesn't enter an error state requiring a cleaner process to get things back to a consistent state.

While I do recommend using REST architecture as much as possible, when I encounter a situation where I'm considering setting up some kind of transactional state between the client and the server, I strongly consider falling back to a RPC design. As a codebase evolves and grows I would tend to refactor toward RESTful architecture, while leaving older versioned RPC endpoints alone as long as is reasonable (especially if this is a public API).

I'm not saying that one architecture is better than the other, in fact I encourage using a mix of both in the same codebase to take advantage of the strength of both approaches.

### HTTP verbs and REST
When I'm building a REST-ish architecture, I tend to forget the correct verb to use for the specific situation. As I mentioned above, this article "[Using HTTP Methods for RESTful Services](https://www.restapitutorial.com/lessons/httpmethods.html)" was very helpful, because it's got a big table and lots of descriptions. The article is licensed under a CCA-SA license, so I'm going to work on it and make it a bit easier to digest for myself here.

| HTTP Verb | Operation type | Returns for eg. `/customers`                                                                                                                                                                                                                                                                    | Returns for eg. `/customers/{id}`                                                                                                                                                                   |
| --------- | -------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| GET       | Read           | `200 OK`, list of customers                                                                                                                                                                                                                                                                       | `200 OK`, single customer. `404 Not Found` if the ID is not found or invalid.                                                                                                                      |
| POST      | Create         | `201 Created` if it was valid, and returns a `Location` header containing the new ID. I would also just return the entire new object because most of the time it will be needed too. If there was an error return that as an error code—never as a `200 OK` with an error in the response body! | `404 Not Found`, `409 Conflict` if the resource already exists.                                                                                                                                    |
| PUT       | Replace        | `405 Method Not Allowed`, unless you're updating or replacing every resource in the collection.                                                                                                                                                                                                 | `200 OK` if returning the entire new item, `204 No Content` if nothing is going to be returned. `404 Not Found` if the ID isn't found or is invalid. Other appropriate response codes as required. |
| PATCH     | Modify         | `405 Method Not Allowed`, unless you want to modify the collection itself.                                                                                                                                                                                                                      | `200 OK` if returning the entire new item, `204 No Content` if nothing is going to be returned. `404 Not Found` if the ID isn't found or is invalid. Other appropriate response codes as required. |
| DELETE    | Delete         | `405 Method Not Allowed`, unless you want to delete the whole collection.                                                                                                                                                                                                                       | `200 OK`, `404 Not Found` if the ID is not found or is invalid. Other error codes as appropriate.                                                                                                  |

Another thing to note is that a full RESTful implementation would implement every verb for every possible resource that is being exposed via the API. For example, doing a `POST` on a collection will almost never happen. So rather than having to write a handler to return a `405 Method Not Allowed` I would just let it give a `404 Not Found`. I highly recommend using an API documentation tool such as Swagger which gives a nice interface with generated documentation around the application's API.

#### GET
`GET` is used to get, read, or retrieve a resource. Just like you could do a `GET /index.html` to get the contents of the file named `index.html` at the root of a web server, a `GET /customers/123/invoices/6543` would return invoice `6543` belonging to customer `123`. I would actually have (instead of or in addition to) a `GET /invoices/{id}` endpoint to make this easier to understand and avoid issues where the requested invoice ID doesn't exist on that particular customer.

If the resource exists (and the client has access to it) the response will be a `200 OK` with the representation (content) of the resource in the body of the response. Generally this will be XML or JSON.

In an error case, it should return the relevant error code and hopefully some clarification in the body. For example, if the ID isn't found or is otherwise invalid, the server should return a `404 Not Found` response.

`GET` operations are idempotent—they shouldn't have side effects, or at least shouldn't have side effects that affect the next `GET` call for the resource. So, calling `GET /customers/123` ten times should return the same customer data each time, unless another process has modified it in-between the `GET` calls.

#### POST
`POST` is used to create a new resource. So, if you wanted to raise an invoice against a particular customer, you could do a `POST /customers/123/invoices` with the data needed to create the invoice in the body of the `POST` request.

If the resource creation was a success, you should return a `201 Created` response with a link to the new resource in the `Location` header. I usually also return the new resource in the body of the response, especially if there are calculated values that weren't in the original `POST` request that the client will need. This is just my preference, it avoids having to do an extra call to `GET {location}`.

Note that `POST` is **not** idempotent—if you call `POST /customers/123/invoices` multiple times, you'll end up with multiple invoices, each with their own location/ID. This might be desirable, and it might not.

#### PUT
`PUT` is used to update an existing resource. The body of the request should contain the entire resource being updated.

`PUT` can also be used to create a new resource, in cases where the ID of the resource is determined by the client. I would tend away from this use of the `PUT` verb and try to generate IDs on the server, using `POST` to create the new resource (as above).

The server should return a `200 OK` if the update was successful and the entire resource is being returned. If no data is being returned in the response body, a `204 No Content` should be returned.

If the `PUT` was used to create a new resource, the server should respond with a `201 Created`—note that the `Location` header doesn't _have_ to be included because the client would already know the ID of the new resource, but I would include it just to be complete.

If you're updating an existing resource and the ID of the resource is invalid or not found, the server should respond with `404 Not Found`. Other errors should generate the appropriate error response (4xx for client errors and 5xx for server errors), and more detail should be provided in the response body if possible.

#### PATCH
`PATCH` is used to modify an existing resource. It sounds a lot like `PUT`, but the body of the request only needs to contain the data that needs to be modified. For example, changing the name of a particular customer might look like this:

```
PATCH /customers/123
{
	"name": "Jane Doe"
}
```

The changed data could become quite complex, especially if you're needing to change structures or append to an array or similar. [JSON Patch](https://jsonpatch.com/) or [XML Patch (RFC 5261)](https://www.rfc-editor.org/rfc/rfc5261.html) can be used to represent these complex changes.

`PATCH` requests aren't safe or idempotent. This is a concern when using one of the above patch specifications, as they can assume a base point in the patch that, if it is executed at a different base point, can corrupt data. This is a case where using some kind of correlation ID such as an ETag would be a good idea, to ensure that the `PATCH` operation fails early rather than corrupting data.

#### DELETE
`DELETE` is used to remove a resource, and, most likely, any resources that are children of the resource being removed.

`DELETE` operations are idempotent in that the state of the system doesn't change given multiple `DELETE` calls to the same resource. However, the first successful `DELETE` call should return a `200 OK` response, while successive calls should return `404 Not Found` responses.

If the response body is going to be empty, a `204 No Content` response is appropriate. Otherwise, the deleted object could be returned in the body of a `200 OK` response, or a wrapped response could be used. For example:
```
{
  "meta": {
    "status": 200,
    "message": "Successfully deleted the resource"
  },
  "data": null  // Or the full or partial resource that was deleted
}
```

### A plea from the author
Whatever you do, don't return a `200 OK` with an exception in the response body. Every time an API returns that, Tim Berners-Lee cries a little.