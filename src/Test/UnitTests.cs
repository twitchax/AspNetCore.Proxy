using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
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
        public async Task ProxyAttribute()
        {
            var response = await _client.GetAsync("api/posts/1");
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            Assert.Contains("sunt aut facere repellat provident occaecati excepturi optio reprehenderit", JObject.Parse(responseString).Value<string>("title"));
        }

        [Fact]
        public async Task ProxyMiddleware()
        {
            var response = await _client.GetAsync("api/comments/1");
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            Console.WriteLine(responseString);
            Assert.Contains("id labore ex et quam laborum", JObject.Parse(responseString).Value<string>("name"));
        }
    }

    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRouting();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseProxies();

            app.UseProxy("api/comments/{postId}", (args) => {
                return Task.FromResult($"https://jsonplaceholder.typicode.com/comments/{args["postId"]}");
            });
        }
    }

    public static class UseProxies
    {
        [ProxyRoute("api/posts/{postId}")]
        public static  Task<string> ProxyGoogle(int postId)
        {
            return Task.FromResult($"https://jsonplaceholder.typicode.com/posts/{postId}");
        }
    }
}
