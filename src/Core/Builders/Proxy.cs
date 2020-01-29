using System;
using AspNetCore.Proxy.Options;

namespace AspNetCore.Proxy.Builders
{
    public interface IProxyBuilder : IBuilder<IProxyBuilder, Proxy>
    {
        IProxyBuilder WithRoute(string route);

        IProxyBuilder UseHttp(string endpoint, Action<IHttpProxyOptionsBuilder> builderAction = null);
        IProxyBuilder UseHttp(EndpointComputerToString endpointComputer, Action<IHttpProxyOptionsBuilder> builderAction = null);
        IProxyBuilder UseHttp(EndpointComputerToValueTask endpointComputer, Action<IHttpProxyOptionsBuilder> builderAction = null);
        IProxyBuilder UseHttp(Action<IHttpProxyBuilder> builderAction);
        IProxyBuilder UseHttp(IHttpProxyBuilder builder);

        IProxyBuilder UseWs(string endpoint, Action<IWsProxyOptionsBuilder> builderAction = null);
        IProxyBuilder UseWs(EndpointComputerToString endpointComputer, Action<IWsProxyOptionsBuilder> builderAction = null);
        IProxyBuilder UseWs(EndpointComputerToValueTask endpointComputer, Action<IWsProxyOptionsBuilder> builderAction = null);
        IProxyBuilder UseWs(Action<IWsProxyBuilder> builderAction);
        IProxyBuilder UseWs(IWsProxyBuilder builder);
    }

    public class ProxyBuilder : IProxyBuilder
    {
        private bool _isRouteless;
        private string _route;
        private IHttpProxyBuilder _httpProxyBuilder;
        private IWsProxyBuilder _wsProxyBuilder;

        private ProxyBuilder()
        {
        }

        public static ProxyBuilder Instance => new ProxyBuilder();

        internal IProxyBuilder WithIsRouteless(bool isRouteless)
        {
            _isRouteless = isRouteless;
            return this;
        }

        public IProxyBuilder New()
        {
            return Instance
                .WithIsRouteless(_isRouteless)
                .WithRoute(_route)
                .UseHttp(_httpProxyBuilder?.New())
                .UseWs(_wsProxyBuilder?.New());
        }

        public Proxy Build()
        {
            if(_httpProxyBuilder == null && _wsProxyBuilder == null)
                throw new Exception($"At least one endpoint must be defined with `{nameof(UseHttp)}` or `{nameof(UseWs)}`.");

            return new Proxy(
                _route,
                _httpProxyBuilder?.Build(),
                _wsProxyBuilder?.Build());
        }

        public IProxyBuilder WithRoute(string route)
        {
            if(_isRouteless)
                throw new Exception("This is a `routeless` Proxy builder (i.e., likely used with `RunProxy`): adding a route in this context is a no-op that should be removed.");

            _route = route;

            return this;
        }

        public IProxyBuilder UseHttp(string endpoint, Action<IHttpProxyOptionsBuilder> builderAction = null) => this.UseHttp(httpProxy => httpProxy.WithEndpoint(endpoint).WithOptions(builderAction));
        public IProxyBuilder UseHttp(EndpointComputerToString endpointComputer, Action<IHttpProxyOptionsBuilder> builderAction = null) => this.UseHttp(httpProxy => httpProxy.WithEndpoint(endpointComputer).WithOptions(builderAction));
        public IProxyBuilder UseHttp(EndpointComputerToValueTask endpointComputer, Action<IHttpProxyOptionsBuilder> builderAction = null) => this.UseHttp(httpProxy => httpProxy.WithEndpoint(endpointComputer).WithOptions(builderAction));

        public IProxyBuilder UseHttp(Action<IHttpProxyBuilder> builderAction)
        {
            var builder = HttpProxyBuilder.Instance;
            builderAction?.Invoke(builder);

            return this.UseHttp(builder);
        }

        public IProxyBuilder UseHttp(IHttpProxyBuilder builder)
        {
            if(_httpProxyBuilder != null)
                throw new InvalidOperationException("Cannot set more than one HTTP proxy endpoint.");

            _httpProxyBuilder = builder;

            return this;
        }

        public IProxyBuilder UseWs(string endpoint, Action<IWsProxyOptionsBuilder> builderAction = null) => this.UseWs(wsProxy => wsProxy.WithEndpoint(endpoint).WithOptions(builderAction));
        public IProxyBuilder UseWs(EndpointComputerToString endpointComputer, Action<IWsProxyOptionsBuilder> builderAction = null) => this.UseWs(wsProxy => wsProxy.WithEndpoint(endpointComputer).WithOptions(builderAction));
        public IProxyBuilder UseWs(EndpointComputerToValueTask endpointComputer, Action<IWsProxyOptionsBuilder> builderAction = null) => this.UseWs(wsProxy => wsProxy.WithEndpoint(endpointComputer).WithOptions(builderAction));

        public IProxyBuilder UseWs(Action<IWsProxyBuilder> builderAction)
        {
            var builder = WsProxyBuilder.Instance;
            builderAction?.Invoke(builder);

            return this.UseWs(builder);
        }

        public IProxyBuilder UseWs(IWsProxyBuilder builder)
        {
            if(_wsProxyBuilder != null)
                throw new InvalidOperationException("Cannot set more than one WebSocket proxy endpoint.");

            _wsProxyBuilder = builder;

            return this;
        }
    }

    public class Proxy
    {
        public string Route { get; internal set; }
        public HttpProxy HttpProxy { get; internal set; }
        public WsProxy WsProxy { get; internal set; }

        public Proxy(string route, HttpProxy httpProxy, WsProxy wsProxy)
        {
            Route = route;
            HttpProxy = httpProxy;
            WsProxy = wsProxy;
        }
    }
}