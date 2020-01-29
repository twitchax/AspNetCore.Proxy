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
    public class HttpProxyOptionsBuilder : IHttpProxyOptionsBuilder
    {
        private bool _shouldAddForwardedHeaders = true;
        private string _httpClientName;
        private Func<HttpContext, ValueTask<bool>> _intercept;
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
        /// Creates a new instance of <see cref="HttpProxyOptions"/> for building purposes.
        /// </summary>
        /// <returns>A new, default, instance of <see cref="HttpProxyOptions"/>.</returns>
        public static HttpProxyOptionsBuilder Instance => new HttpProxyOptionsBuilder();

        /// <summary>
        /// Creates a new instance this builder with the same state as this one.
        /// </summary>
        /// <returns>A new instance of <see cref="IHttpProxyOptionsBuilder"/> with options copied.</returns>
        public IHttpProxyOptionsBuilder New()
        {
            return Instance
                .WithShouldAddForwardedHeaders(_shouldAddForwardedHeaders)
                .WithHttpClientName(_httpClientName)
                .WithIntercept(_intercept)
                .WithBeforeSend(_beforeSend)
                .WithAfterReceive(_afterReceive)
                .WithHandleFailure(_handleFailure);
        }

        /// <summary>
        /// Build the equivalent <see cref="HttpProxyOptions"/>.
        /// </summary>
        /// <returns>The equivalent <see cref="HttpProxyOptions"/>.</returns>
        public HttpProxyOptions Build()
        {
            return new HttpProxyOptions(
                _shouldAddForwardedHeaders,
                _httpClientName,
                _handleFailure,
                _intercept,
                _beforeSend,
                _afterReceive);
        }

        /// <summary>
        /// Instructs the proxy operation to add `Forwarded` and `X-Forwarded-*` headers.
        /// Default behavior is `true`.
        /// </summary>
        /// <param name="shouldAddForwardedHeaders"></param>
        /// <returns>This instance.</returns>
        public IHttpProxyOptionsBuilder WithShouldAddForwardedHeaders(bool shouldAddForwardedHeaders)
        {
            _shouldAddForwardedHeaders = shouldAddForwardedHeaders;
            return this;
        }

        /// <summary>
        /// Overrides the default <see cref="HttpClient"/> used for making the proxy call.
        /// Default is `null`.
        /// </summary>
        /// <param name="httpClientName"></param>
        /// <returns>This instance.</returns>
        public IHttpProxyOptionsBuilder WithHttpClientName(string httpClientName)
        {
            _httpClientName = httpClientName;
            return this;
        }

        /// <summary>
        /// A <see cref="Func{HttpContext, Task}"/> that is invoked upon a call.
        /// The result should be `true` if the call is intercepted and **not** meant to be forwarded.
        /// </summary>
        /// <param name="intercept"></param>
        /// <returns>This instance.</returns>
        public IHttpProxyOptionsBuilder WithIntercept(Func<HttpContext, ValueTask<bool>> intercept)
        {
            _intercept = intercept;
            return this;
        }

        /// <summary>
        /// An <see cref="Func{HttpContext, HttpRequestMessage, Task}"/> that is invoked before the call to the remote endpoint.
        /// The <see cref="HttpRequestMessage"/> can be edited before the call.
        /// </summary>
        /// <param name="beforeSend"></param>
        /// <returns>This instance.</returns>
        public IHttpProxyOptionsBuilder WithBeforeSend(Func<HttpContext, HttpRequestMessage, Task> beforeSend)
        {
            _beforeSend = beforeSend;
            return this;
        }

        /// <summary>
        /// An <see cref="Func{HttpContext, HttpResponseMessage, Task}"/> that is invoked before the response is written to the client.
        /// The <see cref="HttpResponseMessage"/> can be edited before the response is written to the client.
        /// </summary>
        /// <param name="afterReceive"></param>
        /// <returns>This instance.</returns>
        public IHttpProxyOptionsBuilder WithAfterReceive(Func<HttpContext, HttpResponseMessage, Task> afterReceive)
        {
            _afterReceive = afterReceive;
            return this;
        }

        /// <summary>
        /// A <see cref="Func{HttpContext, Exception, Task}"/> that is invoked once if the proxy operation fails.
        /// </summary>
        /// <param name="handleFailure"></param>
        /// <returns>This instance.</returns>
        public IHttpProxyOptionsBuilder WithHandleFailure(Func<HttpContext, Exception, Task> handleFailure)
        {
            _handleFailure = handleFailure;
            return this;
        }
    }

    /// <summary>
    /// Defines the options that can be set for proxied HTTP operations.
    /// </summary>
    public class HttpProxyOptions
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

        /// <summary>
        /// The constructor.
        /// </summary>
        /// <param name="shouldAddForwardedHeaders"></param>
        /// <param name="httpClientName"></param>
        /// <param name="handleFailure"></param>
        /// <param name="intercept"></param>
        /// <param name="beforeSend"></param>
        /// <param name="afterReceive"></param>
        public HttpProxyOptions(
            bool shouldAddForwardedHeaders,
            string httpClientName,
            Func<HttpContext, Exception, Task> handleFailure,
            Func<HttpContext, ValueTask<bool>> intercept,
            Func<HttpContext, HttpRequestMessage, Task> beforeSend,
            Func<HttpContext, HttpResponseMessage, Task> afterReceive)
        {
            ShouldAddForwardedHeaders = shouldAddForwardedHeaders;
            HttpClientName = httpClientName;
            HandleFailure = handleFailure;
            Intercept = intercept;
            BeforeSend = beforeSend;
            AfterReceive = afterReceive;
        }
    }
}