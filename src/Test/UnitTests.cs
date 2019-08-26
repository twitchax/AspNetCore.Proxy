using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Xunit;

namespace AspNetCore.Proxy.Tests
{
    public class UnitTests
    {
        private readonly TestServer _server;
        private readonly HttpClient _client;

        public UnitTests()
        {
            _server = new TestServer(new WebHostBuilder().UseStartup<Startup>());
            _client = _server.CreateClient();
        }

        [Fact]
        public async Task CanProxyAttributeToTask()
        {
            var response = await _client.GetAsync("api/posts/totask/1");
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            Assert.Contains("sunt aut facere repellat provident occaecati excepturi optio reprehenderit", JObject.Parse(responseString).Value<string>("title"));
        }

        [Fact]
        public async Task CanProxyAttributeToString()
        {
            var response = await _client.GetAsync("api/posts/tostring/1");
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            Assert.Contains("sunt aut facere repellat provident occaecati excepturi optio reprehenderit", JObject.Parse(responseString).Value<string>("title"));
        }

        [Fact]
        public async Task CanProxyAttributePostRequest()
        {
            var content = new StringContent("{\"title\": \"foo\", \"body\": \"bar\", \"userId\": 1}", Encoding.UTF8, "application/json");
            var response = await _client.PostAsync("api/posts", content);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            Assert.Contains("101", JObject.Parse(responseString).Value<string>("id"));
        }


        [Fact]
        public async Task CanProxyContentHeadersPostRequest()
        {
            var content = "hello world";
            var contentType = "application/xcustom";

            var stringContent = new StringContent(content);
            stringContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);

            var response = await _client.PostAsync("echo/post", stringContent);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            Assert.Equal(content, JObject.Parse(responseString).Value<string>("data"));
            Assert.Equal(contentType, JObject.Parse(responseString)["headers"]["content-type"]);
            Assert.Equal(content.Length, JObject.Parse(responseString)["headers"]["content-length"]);
        }


        [Fact]
        public async Task CanProxyAttributePostWithFormRequest()
        {
            var content = new FormUrlEncodedContent(new Dictionary<string, string> { { "xyz", "123" }, { "abc", "321" } });
            var response = await _client.PostAsync("api/posts", content);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            var json = JObject.Parse(responseString);

            Assert.Contains("101", json.Value<string>("id"));
            Assert.Equal(json["xyz"], "123");
            Assert.Equal(json["abc"], "321");
        }

        [Fact]
        public async Task CanProxyAttributeCatchAll()
        {
            var response = await _client.GetAsync("api/catchall/posts/1");
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            Assert.Contains("sunt aut facere repellat provident occaecati excepturi optio reprehenderit", JObject.Parse(responseString).Value<string>("title"));
        }

        [Fact]
        public async Task CanProxyMiddlewareWithContextAndArgsToTask()
        {
            var response = await _client.GetAsync("api/comments/contextandargstotask/1");
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            Assert.Contains("id labore ex et quam laborum", JObject.Parse(responseString).Value<string>("name"));
        }

        [Fact]
        public async Task CanProxyMiddlewareWithArgsToTask()
        {
            var response = await _client.GetAsync("api/comments/argstotask/1");
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            Assert.Contains("id labore ex et quam laborum", JObject.Parse(responseString).Value<string>("name"));
        }

        [Fact]
        public async Task CanProxyMiddlewareWithEmptyToTask()
        {
            var response = await _client.GetAsync("api/comments/emptytotask");
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            Assert.Contains("id labore ex et quam laborum", JObject.Parse(responseString).Value<string>("name"));
        }

        [Fact]
        public async Task CanProxyMiddlewareWithContextAndArgsToString()
        {
            var response = await _client.GetAsync("api/comments/contextandargstostring/1");
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            Assert.Contains("id labore ex et quam laborum", JObject.Parse(responseString).Value<string>("name"));
        }

        [Fact]
        public async Task CanProxyMiddlewareWithArgsToString()
        {
            var response = await _client.GetAsync("api/comments/argstostring/1");
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            Assert.Contains("id labore ex et quam laborum", JObject.Parse(responseString).Value<string>("name"));
        }

        [Fact]
        public async Task CanProxyMiddlewareWithEmptyToString()
        {
            var response = await _client.GetAsync("api/comments/emptytostring");
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            Assert.Contains("id labore ex et quam laborum", JObject.Parse(responseString).Value<string>("name"));
        }

        [Fact]
        public async Task CanProxyWithController()
        {
            var response = await _client.GetAsync("api/controller/posts/1");
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            Assert.Contains("sunt aut facere repellat provident occaecati excepturi optio reprehenderit", JObject.Parse(responseString).Value<string>("title"));
        }

        [Fact]
        public async Task CanModifyRequest()
        {
            var response = await _client.GetAsync("api/controller/customrequest/1");
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            Assert.Contains("qui est esse", JObject.Parse(responseString).Value<string>("title"));
        }

        [Fact]
        public async Task CanModifyResponse()
        {
            var response = await _client.GetAsync("api/controller/customresponse/1");
            response.EnsureSuccessStatusCode();
            
            Assert.Equal("It's all greek...er, Latin...to me!", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task CanGetGeneric502OnFailure()
        {
            var response = await _client.GetAsync("api/controller/fail/1");
            Assert.Equal("BadGateway", response.StatusCode.ToString());
        }

        [Fact]
        public async Task CanGetCustomFailure()
        {
            var response = await _client.GetAsync("api/controller/customfail/1");
            Assert.Equal("Forbidden", response.StatusCode.ToString());
            Assert.Equal("Things borked.", await response.Content.ReadAsStringAsync());
        }
    }

    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRouting();
            services.AddProxies();
            services.AddMvc();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseMiddleware<FakeIpAddressMiddleware>();
            app.UseMvc();
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

        public FakeIpAddressMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            httpContext.Connection.RemoteIpAddress = IPAddress.Parse("127.168.1.31");;
            httpContext.Connection.LocalIpAddress = IPAddress.Parse("127.168.1.32");;

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
                });

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
                });

            return this.ProxyAsync($"https://jsonplaceholder.typicode.com/posts/{postId}", options);
        }

        [Route("api/controller/fail/{postId}")]
        public Task GetWithGenericFail(int postId)
        {
            var options = ProxyOptions.Instance.WithBeforeSend((c, hrm) =>
            {
                var a = 0;
                var b = 1 / a;
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
                })
                .WithHandleFailure((c, e) =>
                {
                    c.Response.StatusCode = 403;
                    c.Response.WriteAsync("Things borked.");
                });

            return this.ProxyAsync($"https://jsonplaceholder.typicode.com/posts/{postId}", options);
        }
    }
}
