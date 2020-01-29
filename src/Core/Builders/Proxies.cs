using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace AspNetCore.Proxy.Builders
{
    public interface IProxiesBuilder : IBuilder<IProxiesBuilder, Proxies>
    {
        IProxiesBuilder Map(string route, Action<IProxyBuilder> proxyAction);
        IProxiesBuilder Map(Action<IProxyBuilder> proxyAction);
        IProxiesBuilder Map(IProxyBuilder builder);
    }

    public class ProxiesBuilder : IProxiesBuilder
    {
        private readonly IList<IProxyBuilder> _proxyBuilders;

        private ProxiesBuilder()
        {
            _proxyBuilders = new List<IProxyBuilder>();
        }

        public static ProxiesBuilder Instance => new ProxiesBuilder();

        public IProxiesBuilder New()
        {
            var instance = Instance;

            foreach(var proxyBuilder in _proxyBuilders)
                instance.Map(proxyBuilder.New());

            return instance;
        }

        public Proxies Build()
        {
            return new Proxies(_proxyBuilders.Select(b => b.Build()));
        }

        public IProxiesBuilder Map(string route, Action<IProxyBuilder> builderAction) => this.Map(proxy => builderAction(proxy.WithRoute(route)));

        public IProxiesBuilder Map(Action<IProxyBuilder> builderAction)
        {
            if(builderAction == null)
                throw new ArgumentException($"{nameof(builderAction)} must not be `null`.");

            var builder = ProxyBuilder.Instance;
            builderAction(builder);

            return this.Map(builder);
        }

        public IProxiesBuilder Map(IProxyBuilder builder)
        {
            if(builder == null)
                throw new ArgumentException($"{nameof(builder)} must not be `null`.");

            _proxyBuilders.Add(builder);
            return this;
        }
    }

    public class Proxies : IEnumerable<Proxy>
    {
        private readonly IList<Proxy> _proxies;

        public Proxies(IEnumerable<Proxy> proxies)
        {
            _proxies = proxies.ToList();
        }

        public IEnumerator<Proxy> GetEnumerator() => _proxies.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }
}