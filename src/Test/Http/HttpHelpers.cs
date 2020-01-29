using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using AspNetCore.Proxy.Extensions;
using AspNetCore.Proxy.Options;
using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Readability", "RCS1090", Justification = "Not a library, so no need for `ConfigureAwait`.")]

namespace AspNetCore.Proxy.Tests
{
    internal class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRouting();
            services.AddProxies();
            services.AddControllers();
            services.AddHttpClient("CustomClient", c =>
            {
                // Force a timeout.
                c.Timeout = TimeSpan.FromMilliseconds(1);
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseMiddleware<FakeIpAddressMiddleware>();
            app.UseRouting();
            app.UseEndpoints(endpoints => endpoints.MapControllers());

            app.UseProxies(proxies =>
            {
                proxies.Map("echo/post", proxy => proxy.UseHttp("https://postman-echo.com/post"));

                proxies.Map("api/comments/contextandargstotask/{postId}", proxy => proxy.UseHttp((_, args) => new ValueTask<string>($"https://jsonplaceholder.typicode.com/comments/{args["postId"]}")));

                proxies.Map("api/comments/contextandargstostring/{postId}", proxy => proxy.UseHttp((_, args) => $"https://jsonplaceholder.typicode.com/comments/{args["postId"]}"));
            });
        }
    }

    public class FakeIpAddressMiddleware
    {
        private readonly RequestDelegate next;
            private static readonly Random rand = new Random();

        public FakeIpAddressMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            var r = rand.NextDouble();

            if(r < .33)
            {
                httpContext.Connection.RemoteIpAddress = IPAddress.Parse("127.168.1.31");
                httpContext.Connection.LocalIpAddress = IPAddress.Parse("127.168.1.32");
            }
            else if (r < .66)
            {
                httpContext.Connection.RemoteIpAddress = IPAddress.Parse("2001:db8:85a3:8d3:1319:8a2e:370:7348");
                httpContext.Connection.LocalIpAddress = IPAddress.Parse("2001:db8:85a3:8d3:1319:8a2e:370:7349");
            }

            await this.next(httpContext);
        }
    }

    public class MvcController : ControllerBase
    {
        [Route("api/posts")]
        public Task ProxyPostRequest()
        {
            return this.HttpProxyAsync("https://jsonplaceholder.typicode.com/posts");
        }

        [Route("api/catchall/{**rest}")]
        public Task ProxyCatchAll(string rest)
        {
            return this.HttpProxyAsync($"https://jsonplaceholder.typicode.com/{rest}");
        }

        [Route("api/controller/posts/{postId}")]
        public Task GetPosts(int postId)
        {
            return this.HttpProxyAsync($"https://jsonplaceholder.typicode.com/posts/{postId}");
        }

        [Route("api/controller/intercept/{postId}")]
        public Task GetWithIntercept(int postId)
        {
            var options = HttpProxyOptionsBuilder.Instance
                .WithIntercept(async c =>
                {
                    c.Response.StatusCode = 200;
                    await c.Response.WriteAsync("This was intercepted and not proxied!");

                    return true;
                })
                .Build();

            return this.HttpProxyAsync($"https://jsonplaceholder.typicode.com/posts/{postId}", options);
        }

        [Route("api/controller/customrequest/{postId}")]
        public Task GetWithCustomRequest(int postId)
        {
            var options = HttpProxyOptionsBuilder.Instance
                .WithBeforeSend((_, hrm) =>
                {
                    hrm.RequestUri = new Uri("https://jsonplaceholder.typicode.com/posts/2");
                    return Task.CompletedTask;
                })
                .WithShouldAddForwardedHeaders(false)
                .Build();

            return this.HttpProxyAsync($"https://jsonplaceholder.typicode.com/posts/{postId}", options);
        }

        [Route("api/controller/customresponse/{postId}")]
        public Task GetWithCustomResponse(int postId)
        {
            var options = HttpProxyOptionsBuilder.Instance
                .WithAfterReceive((_, hrm) =>
                {
                    var newContent = new StringContent("It's all greek...er, Latin...to me!");
                    hrm.Content = newContent;
                    return Task.CompletedTask;
                })
                .Build();

            return this.HttpProxyAsync($"https://jsonplaceholder.typicode.com/posts/{postId}", options);
        }

        [Route("api/controller/customclient/{postId}")]
        public Task GetWithCustomClient(int postId)
        {
            var options = HttpProxyOptionsBuilder.Instance
                .WithHttpClientName("CustomClient")
                .Build();

            return this.HttpProxyAsync($"https://jsonplaceholder.typicode.com/posts/{postId}", options);
        }

        [Route("api/controller/badresponse/{postId}")]
        public Task GetWithBadResponse(int postId)
        {
            var options = HttpProxyOptionsBuilder.Instance
                .WithAfterReceive((_, hrm) =>
                {
                    if(hrm.StatusCode == HttpStatusCode.NotFound)
                    {
                        var newContent = new StringContent("I tried to proxy, but I chose a bad address, and it is not found.");
                        hrm.Content = newContent;
                    }

                    return Task.CompletedTask;
                })
                .Build();

            return this.HttpProxyAsync($"https://jsonplaceholder.typicode.com/badpath/{postId}", options);
        }

        [Route("api/controller/fail/{postId}")]
        public Task GetWithGenericFail(int postId)
        {
            var options = HttpProxyOptionsBuilder.Instance
                .WithBeforeSend((_, hrm) =>
                {
                    var a = 0;
                    var b = 1 / a;
                    return Task.CompletedTask;
                })
                .Build();

            return this.HttpProxyAsync($"https://jsonplaceholder.typicode.com/posts/{postId}", options);
        }

        [Route("api/controller/customfail/{postId}")]
        public Task GetWithCustomFail(int postId)
        {
            var options = HttpProxyOptionsBuilder.Instance
                .WithBeforeSend((_, hrm) =>
                {
                    var a = 0;
                    var b = 1 / a;
                    return Task.CompletedTask;
                })
                .WithHandleFailure((c, e) =>
                {
                    c.Response.StatusCode = 403;
                    return c.Response.WriteAsync("Things borked.");
                })
                .Build();

            return this.HttpProxyAsync($"https://jsonplaceholder.typicode.com/posts/{postId}", options);
        }
    }
}