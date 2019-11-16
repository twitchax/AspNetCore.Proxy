TODO:
* Test coverage.
* Test for round robin.
* XML docs.
* Add "catchall" example to docs to show how you can act as an API gateway.
* Add a note to the docs that `ProxyAsync` should not read the body of the request in any way for things like posts.


NEW TODO:
* Generally rethink `RunProxy` since it doesn't need a route...and args do not make sense for the endpoint computers.
* It looks like `RunProxy` needs its own proxy class.  Maybe call this "global" or "static" proxy?
* Same for other helpers like `ProxyAsync`?  This could use that same class since (1) route does not make sense, and (2) args do not make sense in the computers.
* Does an endpoint computer even make sense in the `ProxyAsync` case?  The controller itself has all of the data it should need to "compute" a destination at that time.
Probably:
* Classes for `RunProxy` that does not have a route, and does not have args for endpoint computer.
* Classes for `ProxyAsync` that does not have a route, and does not have an endpoint computer (i.e., string endpoint instead).