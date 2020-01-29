using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AspNetCore.Proxy.Extensions;
using Microsoft.AspNetCore.Http;

namespace AspNetCore.Proxy.Tests
{
    public class WsController : ControllerBase
    {
        [Route("api/ws")]
        public Task WsProxyAsync()
        {
            return this.WsProxyAsync("ws://localhost:5002/");
        }

        [Route("api/ws2")]
        public Task ProxyAsync()
        {
            // Use spurious http endpoint for code coverage.
            return this.ProxyAsync("http://localhost:5002/", "ws://localhost:5002/");
        }

        [Route("api/http")]
        public Task HttpEndpoint()
        {
            // Use spurious http endpoint for code coverage.
            return this.HttpProxyAsync("http://localhost:5002/");
        }
    }

    internal static class WsHelpers
    {
        internal static Task RunWsServers(CancellationToken token)
        {
            var proxiedServerTask = WebHost.CreateDefaultBuilder()
                .SuppressStatusMessages(true)
                .ConfigureLogging(logging => logging.ClearProviders())
                .ConfigureKestrel(options => options.ListenLocalhost(5002))
                .Configure(app => app.UseWebSockets().Run(context => context.SocketBoomerang()))
                .Build().RunAsync(token);

            var proxyServerTask = WebHost.CreateDefaultBuilder()
                .SuppressStatusMessages(true)
                .ConfigureLogging(logging => logging.ClearProviders())
                .ConfigureKestrel(options => options.ListenLocalhost(5001))
                .ConfigureServices(services => services.AddProxies().AddRouting().AddControllers())
                .Configure(app => 
                {
                    app.UseWebSockets().UseRouting().UseEndpoints(end => end.MapControllers());

                    app.UseProxies(proxies =>
                    {
                        // Adding extra options to exercise them.
                        proxies.Map("/ws", proxy => proxy
                            .UseWs("ws://localhost:5002/", options => options
                                .WithBufferSize(8192)
                                .WithIntercept(context => new ValueTask<bool>(context.WebSockets.WebSocketRequestedProtocols.Contains("interceptedProtocol")))
                                .WithBeforeConnect((context, wso) =>
                                {
                                    wso.AddSubProtocol("myRandomProto");
                                    return Task.CompletedTask;
                                })
                                .WithHandleFailure(async (context, e) =>
                                {
                                    context.Response.StatusCode = 599;
                                    await context.Response.WriteAsync("Failure handeled.");
                                })
                        ));
                    });
                })
                .Build().RunAsync(token);

            return Task.WhenAll(proxiedServerTask, proxyServerTask);
        }
    }
}