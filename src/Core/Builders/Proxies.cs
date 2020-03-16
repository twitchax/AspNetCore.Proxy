using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace AspNetCore.Proxy.Builders
{
    /// <summary>
    /// Interface for a proxies builder.
    /// </summary>
    public interface IProxiesBuilder : IBuilder<IProxiesBuilder, Proxies>
    {
        /// <summary>
        /// Adds a proxy route to the set of routes `this` builder is tracking.
        /// </summary>
        /// <param name="route">The route.</param>
        /// <param name="proxyAction">An <see cref="Action"/> that mutates a proxy builder.</param>
        /// <returns>The current instance with the specified route mapped.</returns>
        IProxiesBuilder Map(string route, Action<IProxyBuilder> proxyAction);

        /// <summary>
        /// Adds a proxy route to the set of routes `this` builder is tracking.
        /// </summary>
        /// <param name="proxyAction">An <see cref="Action"/> that mutates a proxy builder.</param>
        /// <returns>The current instance with the specified route mapped.</returns>
        IProxiesBuilder Map(Action<IProxyBuilder> proxyAction);

        /// <summary>
        /// Adds a proxy route to the set of routes `this` builder is tracking.
        /// </summary>
        /// <param name="builder">A proxy builder to build for this route.</param>
        /// <returns>The current instance with the specified route mapped.</returns>
        IProxiesBuilder Map(IProxyBuilder builder);
    }

    /// <summary>
    /// Concrete type for a proxies builder.
    /// </summary>
    public sealed class ProxiesBuilder : IProxiesBuilder
    {
        private readonly IList<IProxyBuilder> _proxyBuilders;

        private ProxiesBuilder()
        {
            _proxyBuilders = new List<IProxyBuilder>();
        }

        /// <summary>
        /// Gets a `new`, empty instance of this type.
        /// </summary>
        /// <returns>A `new` instance of <see cref="ProxiesBuilder"/>.</returns>
        public static ProxiesBuilder Instance => new ProxiesBuilder();

        /// <inheritdoc/>
        public IProxiesBuilder New()
        {
            var instance = Instance;

            foreach(var proxyBuilder in _proxyBuilders)
                instance.Map(proxyBuilder.New());

            return instance;
        }

        /// <inheritdoc/>
        public Proxies Build()
        {
            return new Proxies(_proxyBuilders.Select(b => b.Build()));
        }

        /// <inheritdoc/>
        public IProxiesBuilder Map(string route, Action<IProxyBuilder> builderAction) => this.Map(proxy => builderAction(proxy.WithRoute(route)));

        /// <inheritdoc/>
        public IProxiesBuilder Map(Action<IProxyBuilder> builderAction)
        {
            if(builderAction == null)
                throw new ArgumentException($"{nameof(builderAction)} must not be `null`.");

            var builder = ProxyBuilder.Instance;
            builderAction(builder);

            return this.Map(builder);
        }

        /// <inheritdoc/>
        public IProxiesBuilder Map(IProxyBuilder builder)
        {
            if(builder == null)
                throw new ArgumentException($"{nameof(builder)} must not be `null`.");

            _proxyBuilders.Add(builder);
            return this;
        }
    }

    /// <summary>
    /// Concrete type for a proxies definition.
    /// </summary>
    public class Proxies : IEnumerable<Proxy>
    {
        private readonly IList<Proxy> _proxies;

        /// <summary>
        /// The constructor for <see cref="Proxies"/>.
        /// </summary>
        /// <param name="proxies">The set of proxy routes to handle.</param>
        internal Proxies(IEnumerable<Proxy> proxies)
        {
            _proxies = proxies.ToList();
        }

        /// <inheritdoc/>
        public IEnumerator<Proxy> GetEnumerator() => _proxies.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }
}