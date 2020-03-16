using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AspNetCore.Proxy.Options;
using Microsoft.AspNetCore.Http;

namespace AspNetCore.Proxy.Builders
{
    /// <summary>
    /// Interface for an HTTP proxy builder.
    /// </summary>
    public interface IHttpProxyBuilder : IBuilder<IHttpProxyBuilder, HttpProxy>
    {
        /// <summary>
        /// Sets the endpoint on `this` instance that the proxy-to-build should use.
        /// </summary>
        /// <param name="endpoint">The endpoint to set.</param>
        /// <returns>The current instance with the specified endpoint set.</returns>
        IHttpProxyBuilder WithEndpoint(string endpoint);

        /// <summary>
        /// Sets the endpoint on `this` instance that the proxy-to-build should use.
        /// </summary>
        /// <param name="endpoint">The endpoint to set.  This takes the form `(<see cref="HttpContext"/>, <see cref="IDictionary{String, Object}"/>) => <see cref="String"/>`.</param>
        /// <returns>The current instance with the specified endpoint set.</returns>
        IHttpProxyBuilder WithEndpoint(EndpointComputerToString endpoint);

        /// <summary>
        /// Sets the endpoint on `this` instance that the proxy-to-build should use.
        /// </summary>
        /// <param name="endpoint">The endpoint to set.  This takes the form `(<see cref="HttpContext"/>, <see cref="IDictionary{String, Object}"/>) => <see cref="ValueTask{String}"/>`</param>
        /// <returns>The current instance with the specified endpoint set.</returns>
        IHttpProxyBuilder WithEndpoint(EndpointComputerToValueTask endpoint);

        /// <summary>
        /// Sets the options builder on `this` instance that the proxy-to-build should use.
        /// </summary>
        /// <param name="options">The options builder to set.</param>
        /// <returns>The current instance with the specified options set.</returns>
        IHttpProxyBuilder WithOptions(IHttpProxyOptionsBuilder options);

        /// <summary>
        /// Sets the options builder on `this` instance that the proxy-to-build should use.
        /// </summary>
        /// <param name="builderAction">The options builder action to set.  This takes the form `(<see cref="IHttpProxyOptionsBuilder"/>) => void`.</param>
        /// <returns>The current instance with the specified options set.</returns>
        IHttpProxyBuilder WithOptions(Action<IHttpProxyOptionsBuilder> builderAction);
    }

    /// <summary>
    /// Concrete type for an HTTP proxy builder.
    /// </summary>
    public sealed class HttpProxyBuilder : IHttpProxyBuilder
    {
        private EndpointComputerToValueTask _endpointComputer;

        private IHttpProxyOptionsBuilder _optionsBuilder;

        private HttpProxyBuilder()
        {
        }

        /// <summary>
        /// Gets a `new`, empty instance of this type.
        /// </summary>
        /// <returns>A `new` instance of <see cref="HttpProxyOptionsBuilder"/>.</returns>
        public static HttpProxyBuilder Instance => new HttpProxyBuilder();

        /// <inheritdoc/>
        public IHttpProxyBuilder New()
        {
            return Instance
                .WithEndpoint(_endpointComputer)
                .WithOptions(_optionsBuilder?.New());
        }

        /// <inheritdoc/>
        public HttpProxy Build()
        {
            if(_endpointComputer == null)
                throw new Exception("The endpoint must be specified on this HTTP proxy builder.");

            return new HttpProxy(
                _endpointComputer,
                _optionsBuilder?.Build());
        }

        /// <inheritdoc/>
        public IHttpProxyBuilder WithEndpoint(string endpoint) => this.WithEndpoint((context, args) => new ValueTask<string>(endpoint));

        /// <inheritdoc/>
        public IHttpProxyBuilder WithEndpoint(EndpointComputerToString endpointComputer) => this.WithEndpoint((context, args) => new ValueTask<string>(endpointComputer(context, args)));

        /// <inheritdoc/>
        public IHttpProxyBuilder WithEndpoint(EndpointComputerToValueTask endpointComputer)
        {
            _endpointComputer = endpointComputer;
            return this;
        }

        /// <inheritdoc/>
        public IHttpProxyBuilder WithOptions(IHttpProxyOptionsBuilder optionsBuilder)
        {
            _optionsBuilder = optionsBuilder;
            return this;
        }

        /// <inheritdoc/>
        public IHttpProxyBuilder WithOptions(Action<IHttpProxyOptionsBuilder> builderAction)
        {
            _optionsBuilder = HttpProxyOptionsBuilder.Instance;
            builderAction?.Invoke(_optionsBuilder);

            return this;
        }
    }

    /// <summary>
    /// Concrete type for an HTTP proxy definition.
    /// </summary>
    public sealed class HttpProxy
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
        public HttpProxyOptions Options { get; internal set; }

        internal HttpProxy(EndpointComputerToValueTask endpointComputer, HttpProxyOptions options)
        {
            EndpointComputer = endpointComputer;
            Options = options;
        }
    }
}