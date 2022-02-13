using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace AspNetCore.Proxy.Options
{
    /// <summary>
    /// Defines the builder options that can be set for proxied HTTP operations.
    /// </summary>
    public interface IHttpProxyOptionsBuilder : IBuilder<IHttpProxyOptionsBuilder, HttpProxyOptions>
    {
        /// <summary>
        /// Instructs the proxy operation to add `Forwarded` and `X-Forwarded-*` headers.
        /// Default behavior is `true`.
        /// </summary>
        /// <param name="shouldAddForwardedHeaders"></param>
        /// <returns>This instance.</returns>
        IHttpProxyOptionsBuilder WithShouldAddForwardedHeaders(bool shouldAddForwardedHeaders);

        /// <summary>
        /// Overrides the default <see cref="HttpClient"/> used for making the proxy call.
        /// Default is `null`.
        /// </summary>
        /// <param name="httpClientName"></param>
        /// <returns>This instance.</returns>
        IHttpProxyOptionsBuilder WithHttpClientName(string httpClientName);

        /// <summary>
        /// A <see cref="Func{HttpContext, Task}"/> that is invoked upon a call.
        /// The result should be `true` if the call is intercepted and **not** meant to be forwarded.
        /// </summary>
        /// <param name="intercept"></param>
        /// <returns>This instance.</returns>
        IHttpProxyOptionsBuilder WithIntercept(Func<HttpContext, ValueTask<bool>> intercept);

        /// <summary>
        /// A <see cref="Func{HttpContext, Boolean}"/> that is invoked upon a call.
        /// The result should be `true` if the call should go ahead and not be filtered
        /// </summary>
        /// <param name="filter"></param>
        /// <returns>This instance.</returns>
        IHttpProxyOptionsBuilder WithFilter(Func<HttpContext, bool> filter);

        /// <summary>
        /// An <see cref="Func{HttpContext, HttpRequestMessage, Task}"/> that is invoked before the call to the remote endpoint.
        /// The <see cref="HttpRequestMessage"/> can be edited before the call.
        /// </summary>
        /// <param name="beforeSend"></param>
        /// <returns>This instance.</returns>
        IHttpProxyOptionsBuilder WithBeforeSend(Func<HttpContext, HttpRequestMessage, Task> beforeSend);

        /// <summary>
        /// An <see cref="Func{HttpContext, HttpResponseMessage, Task}"/> that is invoked before the response is written to the client.
        /// The <see cref="HttpResponseMessage"/> can be edited before the response is written to the client.
        /// </summary>
        /// <param name="afterReceive"></param>
        /// <returns>This instance.</returns>
        IHttpProxyOptionsBuilder WithAfterReceive(Func<HttpContext, HttpResponseMessage, Task> afterReceive);

        /// <summary>
        /// A <see cref="Func{HttpContext, Exception, Task}"/> that is invoked once if the proxy operation fails.
        /// </summary>
        /// <param name="handleFailure"></param>
        /// <returns>This instance.</returns>
        IHttpProxyOptionsBuilder WithHandleFailure(Func<HttpContext, Exception, Task> handleFailure);
    }

    /// <summary>
    /// Defines the builder options that can be set for proxied HTTP operations.
    /// </summary>
    public sealed class HttpProxyOptionsBuilder : IHttpProxyOptionsBuilder
    {
        private bool _shouldAddForwardedHeaders = true;
        private string _httpClientName;
        private Func<HttpContext, ValueTask<bool>> _intercept;
        private Func<HttpContext, bool> _filter;
        private Func<HttpContext, HttpRequestMessage, Task> _beforeSend;
        private Func<HttpContext, HttpResponseMessage, Task> _afterReceive;
        private Func<HttpContext, Exception, Task> _handleFailure;

        /// <summary>
        /// The default constructor.
        /// </summary>
        private HttpProxyOptionsBuilder()
        {
        }

        /// <summary>
        /// Gets a `new`, empty instance of this type.
        /// </summary>
        /// <returns>A `new` instance of <see cref="HttpProxyOptionsBuilder"/>.</returns>
        public static HttpProxyOptionsBuilder Instance => new HttpProxyOptionsBuilder();

        /// <inheritdoc/>
        public IHttpProxyOptionsBuilder New()
        {
            return Instance
                .WithShouldAddForwardedHeaders(_shouldAddForwardedHeaders)
                .WithHttpClientName(_httpClientName)
                .WithIntercept(_intercept)
                .WithFilter(_filter)
                .WithBeforeSend(_beforeSend)
                .WithAfterReceive(_afterReceive)
                .WithHandleFailure(_handleFailure);
        }

        /// <inheritdoc/>
        public HttpProxyOptions Build()
        {
            return new HttpProxyOptions(
                _shouldAddForwardedHeaders,
                _httpClientName,
                _handleFailure,
                _intercept,
                _filter,
                _beforeSend,
                _afterReceive);
        }

        /// <summary>
        /// Sets the option that instructs the proxy operation to add `Forwarded` and `X-Forwarded-*` headers.
        /// Default behavior is `true`.
        /// </summary>
        /// <param name="shouldAddForwardedHeaders"></param>
        /// <returns>The current instance with the specified option set.</returns>
        public IHttpProxyOptionsBuilder WithShouldAddForwardedHeaders(bool shouldAddForwardedHeaders)
        {
            _shouldAddForwardedHeaders = shouldAddForwardedHeaders;
            return this;
        }

        /// <summary>
        /// Sets the option that overrides the default <see cref="HttpClient"/> used for making the proxy call.
        /// Default is `null`.
        /// </summary>
        /// <param name="httpClientName"></param>
        /// <returns>The current instance with the specified option set.</returns>
        public IHttpProxyOptionsBuilder WithHttpClientName(string httpClientName)
        {
            _httpClientName = httpClientName;
            return this;
        }

        /// <summary>
        /// Sets the <see cref="Func{HttpContext, Task}"/> that is invoked upon a call.
        /// The result should be `true` if the call is intercepted and **not** meant to be forwarded.
        /// </summary>
        /// <param name="intercept"></param>
        /// <returns>The current instance with the specified option set.</returns>
        public IHttpProxyOptionsBuilder WithIntercept(Func<HttpContext, ValueTask<bool>> intercept)
        {
            _intercept = intercept;
            return this;
        }

        /// <summary>
        /// A <see cref="Func{HttpContext, Boolean}"/> that is invoked upon a call.
        /// The result should be `true` if the call should go ahead and not be filtered
        /// </summary>
        /// <param name="filter"></param>
        /// <returns>This instance.</returns>
        public IHttpProxyOptionsBuilder WithFilter(Func<HttpContext, bool> filter)
        {
            _filter = filter;
            return this;
        }

        /// <summary>
        /// Sets the <see cref="Func{HttpContext, HttpRequestMessage, Task}"/> that is invoked before the call to the remote endpoint.
        /// The <see cref="HttpRequestMessage"/> can be edited before the call.
        /// </summary>
        /// <param name="beforeSend"></param>
        /// <returns>The current instance with the specified option set.</returns>
        public IHttpProxyOptionsBuilder WithBeforeSend(Func<HttpContext, HttpRequestMessage, Task> beforeSend)
        {
            _beforeSend = beforeSend;
            return this;
        }

        /// <summary>
        /// Sets the <see cref="Func{HttpContext, HttpResponseMessage, Task}"/> that is invoked before the response is written to the client.
        /// The <see cref="HttpResponseMessage"/> can be edited before the response is written to the client.
        /// </summary>
        /// <param name="afterReceive"></param>
        /// <returns>The current instance with the specified option set.</returns>
        public IHttpProxyOptionsBuilder WithAfterReceive(Func<HttpContext, HttpResponseMessage, Task> afterReceive)
        {
            _afterReceive = afterReceive;
            return this;
        }

        /// <summary>
        /// Sets the <see cref="Func{HttpContext, Exception, Task}"/> that is invoked once if the proxy operation fails.
        /// </summary>
        /// <param name="handleFailure"></param>
        /// <returns>The current instance with the specified option set.</returns>
        public IHttpProxyOptionsBuilder WithHandleFailure(Func<HttpContext, Exception, Task> handleFailure)
        {
            _handleFailure = handleFailure;
            return this;
        }
    }

    /// <summary>
    /// Defines the options that can be set for proxied HTTP operations.
    /// </summary>
    public sealed class HttpProxyOptions
    {
        /// <summary>
        /// ShouldAddForwardedHeaders property.
        /// </summary>
        /// <value>
        /// Instructs the proxy operation to add `Forwarded` and `X-Forwarded-*` headers.
        /// Default behavior is `true`.
        /// </value>
        public bool ShouldAddForwardedHeaders { get; } = true;

        /// <summary>
        /// HttpClientName property.
        /// </summary>
        /// <value>
        /// Overrides the default <see cref="HttpClient"/> used for making the proxy call.
        /// Default is `null`.
        /// </value>
        public string HttpClientName { get; }

        /// <summary>
        /// Intercept property.
        /// </summary>
        /// <value>
        /// A <see cref="Func{HttpContext, Task}"/> that is invoked upon a call.
        /// The result should be `true` if the call is intercepted and **not** meant to be forwarded.
        /// </value>
        public Func<HttpContext, ValueTask<bool>> Intercept { get; }
        
        /// <summary>
        /// Intercept property.
        /// </summary>
        /// <value>
        /// A <see cref="Func{HttpContext, Boolean}"/> that is invoked upon a call.
        /// The result should be `true` if the call should go ahead and not be filtered
        /// </value>
        public Func<HttpContext, bool> Filter { get; }

        /// <summary>
        /// BeforeSend property.
        /// </summary>
        /// <value>
        /// An <see cref="Func{HttpContext, HttpRequestMessage, Task}"/> that is invoked before the call to the remote endpoint.
        /// The <see cref="HttpRequestMessage"/> can be edited before the call.
        /// </value>
        public Func<HttpContext, HttpRequestMessage, Task> BeforeSend { get; }

        /// <summary>
        /// AfterReceive property.
        /// </summary>
        /// <value>
        /// An <see cref="Func{HttpContext, HttpResponseMessage, Task}"/> that is invoked before the response is written to the client.
        /// The <see cref="HttpResponseMessage"/> can be edited before the response is written to the client.
        /// </value>
        public Func<HttpContext, HttpResponseMessage, Task> AfterReceive { get; }

        /// <summary>
        /// HandleFailure property.
        /// </summary>
        /// <value>A <see cref="Func{HttpContext, Exception, Task}"/> that is invoked once if the proxy operation fails.</value>
        public Func<HttpContext, Exception, Task> HandleFailure { get; }

        internal HttpProxyOptions(bool shouldAddForwardedHeaders,
            string httpClientName,
            Func<HttpContext, Exception, Task> handleFailure,
            Func<HttpContext, ValueTask<bool>> intercept,
            Func<HttpContext, bool> filter,
            Func<HttpContext, HttpRequestMessage, Task> beforeSend,
            Func<HttpContext, HttpResponseMessage, Task> afterReceive)
        {
            ShouldAddForwardedHeaders = shouldAddForwardedHeaders;
            HttpClientName = httpClientName;
            HandleFailure = handleFailure;
            Intercept = intercept;
            Filter = filter;
            BeforeSend = beforeSend;
            AfterReceive = afterReceive;
        }
    }
}