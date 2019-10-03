using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace AspNetCore.Proxy.Tests
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRouting();
            services.AddProxies();
            services.AddControllers();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseMiddleware<FakeIpAddressMiddleware>();
            app.UseRouting();
            app.UseEndpoints(endpoints => endpoints.MapControllers());
            app.UseProxies();

            app.UseProxy("echo/post", (context, args) => {
                return Task.FromResult($"https://postman-echo.com/post");
            });

            app.UseProxy("api/comments/contextandargstotask/{postId}", (context, args) => {
                context.GetHashCode();
                return Task.FromResult($"https://jsonplaceholder.typicode.com/comments/{args["postId"]}");
            });

            app.UseProxy("api/comments/argstotask/{postId}", (args) => {
                return Task.FromResult($"https://jsonplaceholder.typicode.com/comments/{args["postId"]}");
            });

            app.UseProxy("api/comments/emptytotask", () => {
                return Task.FromResult($"https://jsonplaceholder.typicode.com/comments/1");
            });

            app.UseProxy("api/comments/contextandargstostring/{postId}", (context, args) => {
                context.GetHashCode();
                return $"https://jsonplaceholder.typicode.com/comments/{args["postId"]}";
            });

            app.UseProxy("api/comments/argstostring/{postId}", (args) => {
                return $"https://jsonplaceholder.typicode.com/comments/{args["postId"]}";
            });

            app.UseProxy("api/comments/emptytostring", () => {
                return $"https://jsonplaceholder.typicode.com/comments/1";
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

    public static class UseProxies
    {
        [ProxyRoute("api/posts/totask/{postId}")]
        public static Task<string> ProxyToTask(int postId)
        {
            return Task.FromResult($"https://jsonplaceholder.typicode.com/posts/{postId}");
        }

        [ProxyRoute("api/posts/tostring/{postId}")]
        public static string ProxyToString(int postId)
        {
            return $"https://jsonplaceholder.typicode.com/posts/{postId}";
        }

        [ProxyRoute("api/posts")]
        public static string ProxyPostRequest()
        {
            return $"https://jsonplaceholder.typicode.com/posts";
        }

        [ProxyRoute("api/catchall/{*rest}")]
        public static string ProxyCatchAll(string rest)
        {
            return $"https://jsonplaceholder.typicode.com/{rest}";
        }
    }

    public class MvcController : ControllerBase
    {
        [Route("api/controller/posts/{postId}")]
        public Task GetPosts(int postId)
        {
            return this.ProxyAsync($"https://jsonplaceholder.typicode.com/posts/{postId}");
        }

        [Route("api/controller/customrequest/{postId}")]
        public Task GetWithCustomRequest(int postId)
        {
            var options = ProxyOptions.Instance
                .WithBeforeSend((c, hrm) =>
                {
                    hrm.RequestUri = new Uri("https://jsonplaceholder.typicode.com/posts/2");
                    return Task.CompletedTask;
                    })
                    .WithShouldAddForwardedHeaders(false);

            return this.ProxyAsync($"https://jsonplaceholder.typicode.com/posts/{postId}", options);
        }

        [Route("api/controller/customresponse/{postId}")]
        public Task GetWithCustomResponse(int postId)
        {
            var options = ProxyOptions.Instance
                .WithAfterReceive((c, hrm) =>
                {
                    var newContent = new StringContent("It's all greek...er, Latin...to me!");
                    hrm.Content = newContent;
                    return Task.CompletedTask;
                });

            return this.ProxyAsync($"https://jsonplaceholder.typicode.com/posts/{postId}", options);
        }

        [Route("api/controller/badresponse/{postId}")]
        public Task GetWithBadResponse(int postId)
        {
            var options = ProxyOptions.Instance
                .WithAfterReceive((c, hrm) =>
                {
                    if(hrm.StatusCode == HttpStatusCode.NotFound)
                    {
                        var newContent = new StringContent("I tried to proxy, but I chose a bad address, and it is not found.");
                        hrm.Content = newContent;
                    }

                    return Task.CompletedTask;
                });

            return this.ProxyAsync($"https://jsonplaceholder.typicode.com/badpath/{postId}", options);
        }

        [Route("api/controller/fail/{postId}")]
        public Task GetWithGenericFail(int postId)
        {
            var options = ProxyOptions.Instance.WithBeforeSend((c, hrm) =>
            {
                var a = 0;
                var b = 1 / a;
                return Task.CompletedTask;
            });
            return this.ProxyAsync($"https://jsonplaceholder.typicode.com/posts/{postId}", options);
        }

        [Route("api/controller/customfail/{postId}")]
        public Task GetWithCustomFail(int postId)
        {
            var options = ProxyOptions.Instance
                .WithBeforeSend((c, hrm) =>
                {
                    var a = 0;
                    var b = 1 / a;
                    return Task.CompletedTask;
                })
                .WithHandleFailure((c, e) =>
                {
                    c.Response.StatusCode = 403;
                    return c.Response.WriteAsync("Things borked.");
                });

            return this.ProxyAsync($"https://jsonplaceholder.typicode.com/posts/{postId}", options);
        }
    }
}