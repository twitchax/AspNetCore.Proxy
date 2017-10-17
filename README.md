# AspNetCore.Proxy

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

There are two main ways to use this library to proxy calls.

You can define a map and a proxy.

```csharp
app.UseProxy("api/{arg1}/{arg2}", async (args) => {
    // Get the proxied address.
    return await SomeCallThatComputesAUrl(args["arg1"], args["arg2"]);
});
```

However, you can also make the proxy look and feel almost like a route.

In your `Configure(IApplicationBuilder, IHostingEnvironment)` method, add the middleware.

```csharp
app.UseProxies();
```

Then, create a static method which returns a `Task<string>` (where the `string` is the proxied URI).

```csharp
[ProxyRoute("api/{arg1}/{arg2}")]
public static async Task<string> GetProxy(string arg1, string arg2)
{
    // Get the proxied address.
    return await SomeCallThatComputesAUrl(arg1, arg2);
}
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