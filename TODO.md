TODO:
  * Should UseProxy require the user to set all of the proxies at once? YES, in 4.0.0...`UseProxies` with builders.
  * Remove the [ProxyRoute] attribute?  Maybe, in 4.0.0.  If we keep it, change it to `UseStaticProxies`, and somehow return options?
  * Round robin helper, and protocol helper for `RunProxy`?  Maybe in 4.0.0.
  * Add options for WebSocket calls.
  * Make options handlers called `Async`?
  * Allow the user to set options via a lambda for builder purposes?

Some ideas of how `UseProxies` should work in 4.0.0.

```csharp

// Custom top-level extension method.
app.UseProxies(proxies => 
{   
    proxies.Map("/route/thingy")
        .ToHttp("http://mysite.com/")  // OR To(http, ws)
        .WithOption1();

    // OR

    proxies.Map("/route/thingy", proxy =>
    {
        // Make sure the proxy builder has HttpContext on it.
        proxy.ToHttp("http://mysite.com")
            .WithOption1(...);

        proxy.ToWs(...);
    });
});

// OR?

// Piggy-back on the ASP.NET Core 3 endpoints pattern.
app.UseEndpoints(endpoints =>
{
    endpoints.Map("/my/path", context =>
    {
        return context.ProxyAsync("http://mysite.com", options =>
        {
            options.WithOption1();
        });

        // OR?

        return context.HttpProxyTo("http://mysite.com", options =>
        {
            options.WithOption1();
        });

        // OR, maybe there is an `HttpProxyTo` and `WsProxyTo`, and a `ProxyTo` that does its best to decide.
    });
})
```