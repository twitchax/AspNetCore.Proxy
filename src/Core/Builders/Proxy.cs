using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AspNetCore.Proxy.Options;
using Microsoft.AspNetCore.Http;

namespace AspNetCore.Proxy.Builders
{
    /// <summary>
    /// Interface for a proxy builder.
    /// </summary>
    public interface IProxyBuilder : IBuilder<IProxyBuilder, Proxy>
    {
        /// <summary>
        /// Sets the route on `this` instance that the proxy-to-build should use.
        /// </summary>
        /// <param name="route">The route to set.</param>
        /// <returns>The current instance with the specified route set.</returns>
        IProxyBuilder WithRoute(string route);

        /// <summary>
        /// Sets the HTTP proxy route on `this` instance that the proxy-to-build should use.
        /// An HTTP proxy route may only be called once.
        /// </summary>
        /// <param name="endpoint">The endpoint to set.</param>
        /// <param name="builderAction">The options builder action to set.  This takes the form `(<see cref="IHttpProxyOptionsBuilder"/>) => void`.</param>
        /// <returns>The current instance with the specified proxy route set.</returns>
        IProxyBuilder UseHttp(string endpoint, Action<IHttpProxyOptionsBuilder> builderAction = null);

        /// <summary>
        /// Sets the HTTP proxy route on `this` instance that the proxy-to-build should use.
        /// An HTTP proxy route may only be called once.
        /// </summary>
        /// <param name="endpointComputer">The endpoint to set.  This takes the form `(<see cref="HttpContext"/>, <see cref="IDictionary{String, Object}"/>) => <see cref="String"/>`.</param>
        /// <param name="builderAction">The options builder action to set.  This takes the form `(<see cref="IHttpProxyOptionsBuilder"/>) => void`.</param>
        /// <returns>The current instance with the specified proxy route set.</returns>
        IProxyBuilder UseHttp(EndpointComputerToString endpointComputer, Action<IHttpProxyOptionsBuilder> builderAction = null);

        /// <summary>
        /// Sets the HTTP proxy route on `this` instance that the proxy-to-build should use.
        /// An HTTP proxy route may only be called once.
        /// </summary>
        /// <param name="endpointComputer">The endpoint to set.  This takes the form `(<see cref="HttpContext"/>, <see cref="IDictionary{String, Object}"/>) => <see cref="ValueTask{String}"/>`.</param>
        /// <param name="builderAction">The options builder action to set.  This takes the form `(<see cref="IHttpProxyOptionsBuilder"/>) => void`.</param>
        /// <returns>The current instance with the specified proxy route set.</returns>
        IProxyBuilder UseHttp(EndpointComputerToValueTask endpointComputer, Action<IHttpProxyOptionsBuilder> builderAction = null);

        /// <summary>
        /// Sets the HTTP proxy route on `this` instance that the proxy-to-build should use.
        /// An HTTP proxy route may only be called once.
        /// </summary>
        /// <param name="builderAction">The options builder action to set.  This takes the form `(<see cref="IHttpProxyOptionsBuilder"/>) => void`.</param>
        /// <returns>The current instance with the specified proxy route set.</returns>
        IProxyBuilder UseHttp(Action<IHttpProxyBuilder> builderAction);

        /// <summary>
        /// Sets the HTTP proxy route on `this` instance that the proxy-to-build should use.
        /// An HTTP proxy route may only be called once.
        /// </summary>
        /// <param name="builder">The options builder to set.</param>
        /// <returns>The current instance with the specified proxy route set.</returns>
        IProxyBuilder UseHttp(IHttpProxyBuilder builder);

        /// <summary>
        /// Sets the WS proxy route on `this` instance that the proxy-to-build should use.
        /// An WS proxy route may only be called once.
        /// </summary>
        /// <param name="endpoint">The endpoint to set.</param>
        /// <param name="builderAction">The options builder action to set.  This takes the form `(<see cref="IWsProxyOptionsBuilder"/>) => void`.</param>
        /// <returns>The current instance with the specified proxy route set.</returns>
        IProxyBuilder UseWs(string endpoint, Action<IWsProxyOptionsBuilder> builderAction = null);

        /// <summary>
        /// Sets the WS proxy route on `this` instance that the proxy-to-build should use.
        /// An WS proxy route may only be called once.
        /// </summary>
        /// <param name="endpointComputer">The endpoint to set.  This takes the form `(<see cref="HttpContext"/>, <see cref="IDictionary{String, Object}"/>) => <see cref="String"/>`.</param>
        /// <param name="builderAction">The options builder action to set.  This takes the form `(<see cref="IWsProxyOptionsBuilder"/>) => void`.</param>
        /// <returns>The current instance with the specified proxy route set.</returns>
        IProxyBuilder UseWs(EndpointComputerToString endpointComputer, Action<IWsProxyOptionsBuilder> builderAction = null);

        /// <summary>
        /// Sets the WS proxy route on `this` instance that the proxy-to-build should use.
        /// An WS proxy route may only be called once.
        /// </summary>
        /// <param name="endpointComputer">The endpoint to set.  This takes the form `(<see cref="HttpContext"/>, <see cref="IDictionary{String, Object}"/>) => <see cref="ValueTask{String}"/>`.</param>
        /// <param name="builderAction">The options builder action to set.  This takes the form `(<see cref="IWsProxyOptionsBuilder"/>) => void`.</param>
        /// <returns>The current instance with the specified proxy route set.</returns>
        IProxyBuilder UseWs(EndpointComputerToValueTask endpointComputer, Action<IWsProxyOptionsBuilder> builderAction = null);

        /// <summary>
        /// Sets the WS proxy route on `this` instance that the proxy-to-build should use.
        /// An WS proxy route may only be called once.
        /// </summary>
        /// <param name="builderAction">The options builder action to set.  This takes the form `(<see cref="IWsProxyOptionsBuilder"/>) => void`.</param>
        /// <returns>The current instance with the specified proxy route set.</returns>
        IProxyBuilder UseWs(Action<IWsProxyBuilder> builderAction);

        /// <summary>
        /// Sets the WS proxy route on `this` instance that the proxy-to-build should use.
        /// An WS proxy route may only be called once.
        /// </summary>
        /// <param name="builder">The options builder to set.</param>
        /// <returns>The current instance with the specified proxy route set.</returns>
        IProxyBuilder UseWs(IWsProxyBuilder builder);
    }

    /// <summary>
    /// Concrete type for a proxy builder.
    /// </summary>
    public sealed class ProxyBuilder : IProxyBuilder
    {
        private bool _isRouteless;
        private string _route;
        private IHttpProxyBuilder _httpProxyBuilder;
        private IWsProxyBuilder _wsProxyBuilder;

        private ProxyBuilder()
        {
        }

        /// <summary>
        /// Gets a `new`, empty instance of this type.
        /// </summary>
        /// <returns>A `new` instance of <see cref="ProxyBuilder"/>.</returns>
        public static ProxyBuilder Instance => new ProxyBuilder();

        internal IProxyBuilder WithIsRouteless(bool isRouteless)
        {
            _isRouteless = isRouteless;
            return this;
        }

        /// <inheritdoc/>
        public IProxyBuilder New()
        {
            return Instance
                .WithIsRouteless(_isRouteless)
                .WithRoute(_route)
                .UseHttp(_httpProxyBuilder?.New())
                .UseWs(_wsProxyBuilder?.New());
        }

        /// <inheritdoc/>
        public Proxy Build()
        {
            if(_httpProxyBuilder == null && _wsProxyBuilder == null)
                throw new Exception($"At least one endpoint must be defined with `{nameof(UseHttp)}` or `{nameof(UseWs)}`.");

            return new Proxy(
                _route,
                _httpProxyBuilder?.Build(),
                _wsProxyBuilder?.Build());
        }

        /// <inheritdoc/>
        public IProxyBuilder WithRoute(string route)
        {
            if(_isRouteless)
                throw new Exception("This is a `routeless` Proxy builder (i.e., likely used with `RunProxy`): adding a route in this context is a no-op that should be removed.");

            _route = route;

            return this;
        }

        /// <inheritdoc/>
        public IProxyBuilder UseHttp(string endpoint, Action<IHttpProxyOptionsBuilder> builderAction = null) => this.UseHttp(httpProxy => httpProxy.WithEndpoint(endpoint).WithOptions(builderAction));

        /// <inheritdoc/>
        public IProxyBuilder UseHttp(EndpointComputerToString endpointComputer, Action<IHttpProxyOptionsBuilder> builderAction = null) => this.UseHttp(httpProxy => httpProxy.WithEndpoint(endpointComputer).WithOptions(builderAction));

        /// <inheritdoc/>
        public IProxyBuilder UseHttp(EndpointComputerToValueTask endpointComputer, Action<IHttpProxyOptionsBuilder> builderAction = null) => this.UseHttp(httpProxy => httpProxy.WithEndpoint(endpointComputer).WithOptions(builderAction));

        /// <inheritdoc/>
        public IProxyBuilder UseHttp(Action<IHttpProxyBuilder> builderAction)
        {
            var builder = HttpProxyBuilder.Instance;
            builderAction?.Invoke(builder);

            return this.UseHttp(builder);
        }

        /// <inheritdoc/>
        public IProxyBuilder UseHttp(IHttpProxyBuilder builder)
        {
            if(_httpProxyBuilder != null)
                throw new InvalidOperationException("Cannot set more than one HTTP proxy endpoint.");

            _httpProxyBuilder = builder;

            return this;
        }

        /// <inheritdoc/>
        public IProxyBuilder UseWs(string endpoint, Action<IWsProxyOptionsBuilder> builderAction = null) => this.UseWs(wsProxy => wsProxy.WithEndpoint(endpoint).WithOptions(builderAction));

        /// <inheritdoc/>
        public IProxyBuilder UseWs(EndpointComputerToString endpointComputer, Action<IWsProxyOptionsBuilder> builderAction = null) => this.UseWs(wsProxy => wsProxy.WithEndpoint(endpointComputer).WithOptions(builderAction));

        /// <inheritdoc/>
        public IProxyBuilder UseWs(EndpointComputerToValueTask endpointComputer, Action<IWsProxyOptionsBuilder> builderAction = null) => this.UseWs(wsProxy => wsProxy.WithEndpoint(endpointComputer).WithOptions(builderAction));

        /// <inheritdoc/>
        public IProxyBuilder UseWs(Action<IWsProxyBuilder> builderAction)
        {
            var builder = WsProxyBuilder.Instance;
            builderAction?.Invoke(builder);

            return this.UseWs(builder);
        }

        /// <inheritdoc/>
        public IProxyBuilder UseWs(IWsProxyBuilder builder)
        {
            if(_wsProxyBuilder != null)
                throw new InvalidOperationException("Cannot set more than one WebSocket proxy endpoint.");

            _wsProxyBuilder = builder;

            return this;
        }
    }

    /// <summary>
    /// Concrete type for a proxy definition.
    /// </summary>
    public class Proxy
    {
        /// <summary>
        /// Route property.
        /// </summary>
        /// <value>The route to proxy.</value>
        public string Route { get; internal set; }

        /// <summary>
        /// HttpProxy property.
        /// </summary>
        /// <value>The route to proxy.</value>
        public HttpProxy HttpProxy { get; internal set; }

        /// <summary>
        /// HttpProxy property.
        /// </summary>
        /// <value>The route to proxy.</value>
        public WsProxy WsProxy { get; internal set; }

        internal Proxy(string route, HttpProxy httpProxy, WsProxy wsProxy)
        {
            Route = route;
            HttpProxy = httpProxy;
            WsProxy = wsProxy;
        }
    }
}