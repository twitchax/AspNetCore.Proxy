# AspNetCore.Proxy

[![Actions Status](https://github.com/twitchax/AspNetCore.Proxy/workflows/build/badge.svg)](https://github.com/twitchax/AspNetCore.Proxy/actions)
[![codecov](https://codecov.io/gh/twitchax/AspNetCore.Proxy/branch/master/graph/badge.svg)](https://codecov.io/gh/twitchax/AspNetCore.Proxy)
[![GitHub Release](https://img.shields.io/github/release/twitchax/aspnetcore.proxy.svg)](https://github.com/twitchax/aspnetcore.proxy/releases)
[![NuGet Version](https://img.shields.io/nuget/v/aspnetcore.proxy.svg)](https://www.nuget.org/packages/aspnetcore.proxy/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/aspnetcore.proxy.svg)](https://www.nuget.org/packages/aspnetcore.proxy/)

ASP.NET Core Proxies made easy.

## Information

### Install

```bash
dotnet add package AspNetCore.Proxy
```

### Test

Download the source and run.

```bash
dotnet restore
dotnet test src/Test/AspNetCore.Proxy.Tests.csproj
```

### Compatibility

Latest .NET Standard 2.0.

### Examples

First, you must add the required services.

```csharp
public void ConfigureServices(IServiceCollection services)
{
    ...
    services.AddProxies();
    ...
}
```

#### Existing Controller

You can use the proxy functionality on an existing `Controller` by leveraging the `Proxy` extension method.

```csharp
public class MyController : Controller
{
    [Route("api/posts/{postId}")]
    public Task GetPosts(int postId)
    {
        return this.ProxyAsync($"https://jsonplaceholder.typicode.com/posts/{postId}");
    }
}
```

You can also pass special options that apply when the proxy operation occurs.

```csharp
public class MyController : Controller
{
    [Route("api/posts/{postId}")]
    public Task GetPosts(int postId)
    {
        var options = ProxyOptions.Instance
            .WithShouldAddForwardedHeaders(false)
            .WithHttpClientName("MyCustomClient")
            .WithIntercept(async context =>
            {
                if(c.Connection.RemotePort == 7777)
                {
                    c.Response.StatusCode = 300;
                    await c.Response.WriteAsync("I don't like this port, so I am not proxying this request!");
                    return true;
                }

                return false;
            })
            .WithBeforeSend((c, hrm) =>
            {
                // Set something that is needed for the downstream endpoint.
                hrm.Headers.Authorization = new AuthenticationHeaderValue("Basic");

                return Task.CompletedTask;
            })
            .WithAfterReceive((c, hrm) =>
            {
                // Alter the content in  some way before sending back to client.
                var newContent = new StringContent("It's all greek...er, Latin...to me!");
                hrm.Content = newContent;

                return Task.CompletedTask;
            })
            .WithHandleFailure(async (c, e) =>
            {
                // Return a custom error response.
                c.Response.StatusCode = 403;
                await c.Response.WriteAsync("Things borked.");
            });

        return this.ProxyAsync($"https://jsonplaceholder.typicode.com/posts/{postId}");
    }
}
```

#### Application Builder

You can define a proxy in `Configure(IApplicationBuilder app, IHostingEnvironment env)`.  The arguments are passed to the underlying lambda as a `Dictionary`.

```csharp
app.UseProxy("api/{arg1}/{arg2}", async (args) => {
    // Get the proxied address.
    return await SomeCallThatComputesAUrl(args["arg1"], args["arg2"]);
});
```

#### `ProxyRoute` Attribute

You can also make the proxy look and feel almost like a route, but as part of a static method.

First, add the middleware.

```csharp
public void Configure(IApplicationBuilder app, IHostingEnvironment env)
{
    ...
    app.UseProxies();
    ...
}
```

Then, create a static method which returns a `Task<string>` or `string` (where the `string` is the URI to proxy).

```csharp
[ProxyRoute("api/posts/{arg1}/{arg2}")]
public static async Task<string> GetProxy(string arg1, string arg2)
{
    var uri = await SomeCallThatComputesAUrl(arg1, arg2);
    return uri;
}

[ProxyRoute("api/comments/{postId}")]
public static async Task<string> GetComments(int postId) => $"https://jsonplaceholder.typicode.com/posts/{postId}";
```

## License

```
The MIT License (MIT)

Copyright (c) 2017 Aaron Roney

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```
