using AspNetCore.Proxy.Builders;
using AspNetCore.Proxy.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

// For unit tests.
[assembly:InternalsVisibleTo("AspNetCore.Proxy.Tests")]

namespace AspNetCore.Proxy.Extensions
{
    public static class Basic
    {
        #region ServiceCollection Extensions

        /// <summary>
        /// Adds the required services needed for proxying requests.
        /// </summary>
        /// <param name="services">The application service collection.</param>
        /// <param name="configureProxyClient">An <see cref="Action"/> that can override the underlying `HttpClient` used for proxied calls.</param>
        /// <returns>The same instance.</returns>
        public static IServiceCollection AddProxies(this IServiceCollection services, Action<HttpClient> configureProxyClient = null)
        {
            services.AddRouting();

            if(configureProxyClient != null)
                services.AddHttpClient(Helpers.HttpProxyClientName, configureProxyClient);
            else
                services.AddHttpClient(Helpers.HttpProxyClientName);

            return services;
        }

        #endregion

        #region ApplicationBuilder Extensions

        public static IApplicationBuilder UseProxies(this IApplicationBuilder app, Action<IProxiesBuilder> builderAction)
        {
            // TODO: Could make use of `UseEndpoints` in ASP.NET Core 3?
            app.UseRouter(builder => {
                var proxiesBuilder = ProxiesBuilder.Instance;
                builderAction(proxiesBuilder);

                foreach(var proxy in proxiesBuilder.Build())
                {
                    builder.MapMiddlewareRoute(proxy.Route, proxyApp => proxyApp.Run(context => context.ExecuteProxyOperationAsync(proxy)));
                }
            });

            return app;
        }

        /// <summary>
        /// Terminating middleware which creates a proxy over a specified endpoint.
        /// </summary>
        /// <param name="app">The ASP.NET <see cref="IApplicationBuilder"/>.</param>
        /// <param name="proxiedAddress">The proxied address.</param>
        /// <param name="options">Extra options to apply during proxying.</param>
        public static void RunProxy(this IApplicationBuilder app, Action<IProxyBuilder> builderAction)
        {
            var proxyBuilder = ProxyBuilder.Instance.WithIsRouteless(true);
            builderAction(proxyBuilder);
            var proxy = proxyBuilder.Build();

            var oldHttpEndpointComputer = proxy.HttpProxy?.EndpointComputer.Clone() as EndpointComputerToValueTask;
            var oldWsEndpointComputer = proxy.WsProxy?.EndpointComputer.Clone() as EndpointComputerToValueTask;

            if(oldHttpEndpointComputer != null)
                proxy.HttpProxy.EndpointComputer = GetRunProxyComputer(oldHttpEndpointComputer);
            if(oldWsEndpointComputer != null)
                proxy.WsProxy.EndpointComputer = GetRunProxyComputer(oldWsEndpointComputer);

            app.Run(context => context.ExecuteProxyOperationAsync(proxy));
        }

        public static void RunProxy(
            this IApplicationBuilder app,
            EndpointComputerToValueTask httpEndpointComputer,
            EndpointComputerToValueTask wsEndpointComputer,
            Action<IHttpProxyOptionsBuilder> httpBuilderOptionsAction = null,
            Action<IWsProxyOptionsBuilder> wsBuilderOptionsAction = null)
        {
            app.RunProxy(builder => builder
                .UseHttp(httpBuilder => httpBuilder
                    .WithEndpoint(httpEndpointComputer)
                    .WithOptions(httpBuilderOptionsAction)
                )
                .UseWs(wsBuilder => wsBuilder
                    .WithEndpoint(wsEndpointComputer)
                    .WithOptions(wsBuilderOptionsAction)
                )
            );
        }

        public static void RunProxy(
            this IApplicationBuilder app, 
            EndpointComputerToString httpEndpointComputer,
            EndpointComputerToString wsEndpointComputer,
            Action<IHttpProxyOptionsBuilder> httpBuilderOptionsAction = null,
            Action<IWsProxyOptionsBuilder> wsBuilderOptionsAction = null)
        {
            app.RunProxy(builder => builder
                .UseHttp(httpBuilder => httpBuilder
                    .WithEndpoint(httpEndpointComputer)
                    .WithOptions(httpBuilderOptionsAction)
                )
                .UseWs(wsBuilder => wsBuilder
                    .WithEndpoint(wsEndpointComputer)
                    .WithOptions(wsBuilderOptionsAction)
                )
            );
        }

        public static void RunProxy(
            this IApplicationBuilder app,
            string httpEndpoint,
            string wsEndpoint,
            Action<IHttpProxyOptionsBuilder> httpBuilderOptionsAction = null,
            Action<IWsProxyOptionsBuilder> wsBuilderOptionsAction = null)
        {
            app.RunProxy(builder => builder
                .UseHttp(httpBuilder => httpBuilder
                    .WithEndpoint(httpEndpoint)
                    .WithOptions(httpBuilderOptionsAction)
                )
                .UseWs(wsBuilder => wsBuilder
                    .WithEndpoint(wsEndpoint)
                    .WithOptions(wsBuilderOptionsAction)
                )
            );
        }

        public static void RunHttpProxy(this IApplicationBuilder app, Action<IHttpProxyBuilder> httpBuilderAction) =>
            app.RunProxy(builder => builder
                .UseHttp(httpBuilderAction)
            );

        public static void RunHttpProxy(this IApplicationBuilder app, EndpointComputerToValueTask httpEndpointComputer, Action<IHttpProxyOptionsBuilder> httpBuilderOptionsAction = null) =>
            app.RunHttpProxy(builder => builder
                .WithEndpoint(httpEndpointComputer)
                .WithOptions(httpBuilderOptionsAction)
            );

        public static void RunHttpProxy(this IApplicationBuilder app, EndpointComputerToString httpEndpointComputer, Action<IHttpProxyOptionsBuilder> httpBuilderOptionsAction = null) =>
            app.RunHttpProxy(builder => builder
                .WithEndpoint(httpEndpointComputer)
                .WithOptions(httpBuilderOptionsAction)
            );

        public static void RunHttpProxy(this IApplicationBuilder app, string httpEndpoint, Action<IHttpProxyOptionsBuilder> httpBuilderOptionsAction = null) =>
            app.RunHttpProxy(builder => builder
                .WithEndpoint(httpEndpoint)
                .WithOptions(httpBuilderOptionsAction)
            );

        public static void RunWsProxy(this IApplicationBuilder app, Action<IWsProxyBuilder> wsBuilderAction) =>
            app.RunProxy(builder => builder
                .UseWs(wsBuilderAction)
            );

        public static void RunWsProxy(this IApplicationBuilder app, EndpointComputerToValueTask wsEndpointComputer, Action<IWsProxyOptionsBuilder> wsBuilderOptionsAction = null) =>
            app.RunWsProxy(builder => builder
                .WithEndpoint(wsEndpointComputer)
                .WithOptions(wsBuilderOptionsAction)
            );

        public static void RunWsProxy(this IApplicationBuilder app, EndpointComputerToString wsEndpointComputer, Action<IWsProxyOptionsBuilder> wsBuilderOptionsAction = null) => 
            app.RunWsProxy(builder => builder
                .WithEndpoint(wsEndpointComputer)
                .WithOptions(wsBuilderOptionsAction)
            );

        public static void RunWsProxy(this IApplicationBuilder app, string wsEndpoint, Action<IWsProxyOptionsBuilder> wsBuilderOptionsAction = null) =>
            app.RunWsProxy(builder => builder
                .WithEndpoint(wsEndpoint)
                .WithOptions(wsBuilderOptionsAction)
            );

        #endregion

        #region Controller Extensions

        public static Task ProxyAsync(this ControllerBase controller, string httpEndpoint, string wsEndpoint, HttpProxyOptions httpProxyOptions = null, WsProxyOptions wsProxyOptions = null)
        {
            var httpProxy = new HttpProxy((c, a) => new ValueTask<string>(httpEndpoint), httpProxyOptions);
            var wsProxy = new WsProxy((c, a) => new ValueTask<string>(wsEndpoint), wsProxyOptions);
            var proxy = new Builders.Proxy(null, httpProxy, wsProxy);
            return controller.HttpContext.ExecuteProxyOperationAsync(proxy);
        }

        public static Task HttpProxyAsync(this ControllerBase controller, string httpEndpoint, HttpProxyOptions httpProxyOptions = null)
        {
            var httpProxy = new HttpProxy((c, a) => new ValueTask<string>(httpEndpoint), httpProxyOptions);
            return controller.HttpContext.ExecuteHttpProxyOperationAsync(httpProxy);
        }

        public static Task WsProxyAsync(this ControllerBase controller, string wsEndpoint, WsProxyOptions wsProxyOptions = null)
        {
            var wsProxy = new WsProxy((c, a) => new ValueTask<string>(wsEndpoint), wsProxyOptions);
            return controller.HttpContext.ExecuteWsProxyOperationAsync(wsProxy);
        }

        #endregion

        #region Extension Helpers

        internal static async Task ExecuteProxyOperationAsync(this HttpContext context, Builders.Proxy proxy)
        {
            var isWebSocket = context.WebSockets.IsWebSocketRequest;
            if(isWebSocket && proxy.WsProxy != null)
            {
                await context.ExecuteWsProxyOperationAsync(proxy.WsProxy).ConfigureAwait(false);
                return;
            }

            if(!isWebSocket && proxy.HttpProxy != null)
            {
                await context.ExecuteHttpProxyOperationAsync(proxy.HttpProxy).ConfigureAwait(false);
                return;
            }

            var requestType = isWebSocket ? "WebSocket" : "HTTP(S)";

            // If the failures are not caught, then write a generic response.
            context.Response.StatusCode = 502 /* BAD GATEWAY */;
            await context.Response.WriteAsync($"Request could not be proxied.\n\nThe {requestType} request cannot be proxied because the underlying proxy definition does not have a definition of that type.").ConfigureAwait(false);
        }

        internal static ValueTask<string> GetEndpointFromComputerAsync(this HttpContext context, EndpointComputerToValueTask computer) => computer(context, context.GetRouteData().Values);

        internal static EndpointComputerToValueTask GetRunProxyComputer(EndpointComputerToValueTask endpointComputer)
        {
            return async (context, args) =>
            {
                var endpoint = await GetEndpointFromComputerAsync(context, endpointComputer).ConfigureAwait(false);
                return $"{endpoint}{context.Request.Path}";
            };
        }

        #endregion
    }
}
