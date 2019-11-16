using AspNetCore.Proxy.Builders;
using AspNetCore.Proxy.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

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
                
                var proxies = proxiesBuilder.Build();

                foreach(var proxy in proxies)
                {
                    builder.MapMiddlewareRoute(proxy.Route, proxyApp => {
                        proxyApp.Run(context => {
                            return context.ExecuteProxyOperationAsync(proxy);
                        });
                    });
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
            var proxyBuilder = ProxyBuilder.Instance;
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

        public static void RunHttpProxy(this IApplicationBuilder app, Action<IHttpProxyBuilder> httpBuilderOptionsAction) =>
            app.RunProxy(builder => builder
                .UseHttp(httpBuilderOptionsAction)
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

        public static void RunWsProxy(this IApplicationBuilder app, Action<IWsProxyBuilder> wsBuilderOptionsAction) =>
            app.RunProxy(builder => builder
                .UseWs(wsBuilderOptionsAction)
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

        /// <summary>
        /// A <see cref="Controller"/> extension method which allows for a single, simple call to use a proxy
        /// in existing controllers.
        /// </summary>
        /// <param name="controller">The calling controller.</param>
        /// <param name="uri">The URI to proxy.</param>
        /// <param name="options">Extra options to apply during proxying.</param>
        /// <returns>
        /// A <see cref="Task"/> which, upon completion, has proxied the specified address and copied the response contents into
        /// the response for the <see cref="HttpContext"/>.
        /// </returns>
        public static Task ProxyAsync(this ControllerBase controller, Action<IProxyBuilder> builderAction)
        {
            var proxyBuilder = ProxyBuilder.Instance;
            builderAction(proxyBuilder);
            var proxy = proxyBuilder.Build();

            return controller.HttpContext.ExecuteProxyOperationAsync(proxy);
        }

        public static Task ProxyAsync(this ControllerBase controller, Builders.Proxy proxy)
        {
            return controller.HttpContext.ExecuteProxyOperationAsync(proxy);
        }

        public static Task HttpProxyAsync(this ControllerBase controller, HttpProxy httpProxy)
        {
            return controller.HttpContext.ExecuteHttpProxyOperationAsync(httpProxy);
        }

        public static Task HttpProxyAsync(this ControllerBase controller, string endpoint, HttpProxyOptions options = null)
        {
            var httpProxy = new HttpProxy((c, a) => new ValueTask<string>(endpoint), options);
            return controller.HttpContext.ExecuteHttpProxyOperationAsync(httpProxy);
        }

        public static Task WsProxyAsync(this ControllerBase controller, WsProxy wsProxy)
        {
            return controller.HttpContext.ExecuteWsProxyOperationAsync(wsProxy);
        }

        public static Task WsProxyAsync(this ControllerBase controller, string endpoint, WsProxyOptions options = null)
        {
            var wsProxy = new WsProxy((c, a) => new ValueTask<string>(endpoint), options);
            return controller.HttpContext.ExecuteWsProxyOperationAsync(wsProxy);
        }

        #endregion

        #region Extension Helpers

        internal static Task ExecuteProxyOperationAsync(this HttpContext context, Builders.Proxy proxy)
        {
            if(context.WebSockets.IsWebSocketRequest && proxy.WsProxy != null)
                return context.ExecuteWsProxyOperationAsync(proxy.WsProxy);
            
            if(!context.WebSockets.IsWebSocketRequest && proxy.HttpProxy != null)
                return context.ExecuteHttpProxyOperationAsync(proxy.HttpProxy);

            throw new Exception("This should never happen due to `Build` calls on ProxyBuilders.");
        }

        internal static string GetEndpointFromComputer(this HttpContext context, EndpointComputerToString computer) => computer(context, context.GetRouteData().Values);
        internal static ValueTask<string> GetEndpointFromComputerAsync(this HttpContext context, EndpointComputerToValueTask computer) => computer(context, context.GetRouteData().Values);

        internal static EndpointComputerToValueTask GetRunProxyComputer(EndpointComputerToValueTask endpointComputer)
        {
            return async (context, args) => 
            {
                var endpoint = await GetEndpointFromComputerAsync(context, endpointComputer);
                return $"{endpoint}{context.Request.Path}";
            };
        }

        #endregion
    }
}
