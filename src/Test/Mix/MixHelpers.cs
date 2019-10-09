using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AspNetCore.Proxy.Tests
{
    internal static class MixHelpers
    {
        internal static Task RunMixServers(CancellationToken token)
        {
            var proxiedServerTask = WebHost.CreateDefaultBuilder()
                .SuppressStatusMessages(true)
                .ConfigureLogging(logging => logging.ClearProviders())
                .ConfigureKestrel(options =>
                {
                    options.ListenLocalhost(5004);
                })
                .Configure(app => app.UseWebSockets().Run(context =>
                {
                    if(context.WebSockets.IsWebSocketRequest)
                        return context.SocketBoomerang();
                        
                    return context.HttpBoomerang();
                }))
                .Build().RunAsync(token);

            var proxyServerTask = WebHost.CreateDefaultBuilder()
                .SuppressStatusMessages(true)
                .ConfigureLogging(logging => logging.ClearProviders())
                .ConfigureKestrel(options =>
                {
                    options.ListenLocalhost(5003);
                })
                .ConfigureServices(services => services.AddProxies().AddRouting().AddControllers())
                .Configure(app => 
                {
                    app.UseWebSockets();
                    app.UseRouting();
                    app.UseEndpoints(end => end.MapControllers());
                    
                    app.RunProxy(context =>
                    {
                        if(context.WebSockets.IsWebSocketRequest)
                            return $"ws://localhost:5004";
                        
                        return $"http://localhost:5004";
                    });
                })
                .Build().RunAsync(token);

            return Task.WhenAll(proxiedServerTask, proxyServerTask);
        }
    }
}