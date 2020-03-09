using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AspNetCore.Proxy.Options;
using Microsoft.AspNetCore.Http;

namespace AspNetCore.Proxy.Builders
{
    /// <summary>
    /// Interface for a WS proxy builder.
    /// </summary>
    public interface IWsProxyBuilder : IBuilder<IWsProxyBuilder, WsProxy>
    {
        /// <summary>
        /// Sets the endpoint on `this` instance that the proxy-to-build should use.
        /// </summary>
        /// <param name="endpoint">The endpoint to set.</param>
        /// <returns>The current instance with the specified endpoint set.</returns>
        IWsProxyBuilder WithEndpoint(string endpoint);

        /// <summary>
        /// Sets the endpoint on `this` instance that the proxy-to-build should use.
        /// </summary>
        /// <param name="endpoint">The endpoint to set.  This takes the form `(<see cref="HttpContext"/>, <see cref="IDictionary{String, Object}"/>) => <see cref="String"/>`.</param>
        /// <returns>The current instance with the specified endpoint set.</returns>
        IWsProxyBuilder WithEndpoint(EndpointComputerToString endpoint);

        /// <summary>
        /// Sets the endpoint on `this` instance that the proxy-to-build should use.
        /// </summary>
        /// <param name="endpoint">The endpoint to set.  This takes the form `(<see cref="HttpContext"/>, <see cref="IDictionary{String, Object}"/>) => <see cref="ValueTask{String}"/>`</param>
        /// <returns>The current instance with the specified endpoint set.</returns>
        IWsProxyBuilder WithEndpoint(EndpointComputerToValueTask endpoint);

        /// <summary>
        /// Sets the options builder on `this` instance that the proxy-to-build should use.
        /// </summary>
        /// <param name="options">The options builder to set.</param>
        /// <returns>The current instance with the specified options set.</returns>
        IWsProxyBuilder WithOptions(IWsProxyOptionsBuilder options);

        /// <summary>
        /// Sets the options builder on `this` instance that the proxy-to-build should use.
        /// </summary>
        /// <param name="builderAction">The options builder action to set.  This takes the form `(<see cref="IHttpProxyOptionsBuilder"/>) => void`.</param>
        /// <returns>The current instance with the specified options set.</returns>
        IWsProxyBuilder WithOptions(Action<IWsProxyOptionsBuilder> builderAction);
    }

    /// <summary>
    /// Concrete type for a WS proxy builder.
    /// </summary>
    public sealed class WsProxyBuilder : IWsProxyBuilder
    {
        private EndpointComputerToValueTask _endpointComputer;

        private IWsProxyOptionsBuilder _optionsBuilder;

        private WsProxyBuilder()
        {
        }

        /// <summary>
        /// Gets a `new`, empty instance of this type.
        /// </summary>
        /// <returns>A `new` instance of <see cref="WsProxyBuilder"/>.</returns>
        public static WsProxyBuilder Instance => new WsProxyBuilder();

        /// <inheritdoc/>
        public IWsProxyBuilder New()
        {
            return Instance
                .WithEndpoint(_endpointComputer)
                .WithOptions(_optionsBuilder?.New());
        }

        /// <inheritdoc/>
        public WsProxy Build()
        {
            if(_endpointComputer == null)
                throw new Exception("The endpoint must be specified on this WebSocket proxy builder.");

            return new WsProxy(
                _endpointComputer,
                _optionsBuilder?.Build());
        }

        /// <inheritdoc/>
        public IWsProxyBuilder WithEndpoint(string endpoint) => this.WithEndpoint((context, args) => new ValueTask<string>(endpoint));

        /// <inheritdoc/>
        public IWsProxyBuilder WithEndpoint(EndpointComputerToString endpointComputer) => this.WithEndpoint((context, args) => new ValueTask<string>(endpointComputer(context, args)));

        /// <inheritdoc/>
        public IWsProxyBuilder WithEndpoint(EndpointComputerToValueTask endpointComputer)
        {
            _endpointComputer = endpointComputer;
            return this;
        }

        /// <inheritdoc/>
        public IWsProxyBuilder WithOptions(IWsProxyOptionsBuilder optionsBuilder)
        {
            _optionsBuilder = optionsBuilder;
            return this;
        }

        /// <inheritdoc/>
        public IWsProxyBuilder WithOptions(Action<IWsProxyOptionsBuilder> builderAction)
        {
            _optionsBuilder = WsProxyOptionsBuilder.Instance;
            builderAction?.Invoke(_optionsBuilder);

            return this;
        }
    }

    /// <summary>
    /// Concrete type for an WS proxy definition.
    /// </summary>
    public class WsProxy
    {
        /// <summary>
        /// EndpointComputer property.
        /// </summary>
        /// <value>The endpoint computer to use when proxying requests.</value>
        public EndpointComputerToValueTask EndpointComputer { get; internal set; }

        /// <summary>
        /// Options property.
        /// </summary>
        /// <value>The options to use when proxying requests.</value>
        public WsProxyOptions Options { get; internal set; }

        internal WsProxy(EndpointComputerToValueTask endpointComputer, WsProxyOptions options)
        {
            EndpointComputer = endpointComputer;
            Options = options;
        }
    }
}